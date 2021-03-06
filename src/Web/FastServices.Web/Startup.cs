﻿namespace FastServices.Web
{
    using System.Reflection;
    using System.Security.Principal;

    using FastServices.Data;
    using FastServices.Data.Common;
    using FastServices.Data.Common.Repositories;
    using FastServices.Data.Models;
    using FastServices.Data.Repositories;
    using FastServices.Data.Seeding;
    using FastServices.Services;
    using FastServices.Services.Comments;
    using FastServices.Services.Complaints;
    using FastServices.Services.Departments;
    using FastServices.Services.EmployeeOrders;
    using FastServices.Services.Employees;
    using FastServices.Services.Images;
    using FastServices.Services.Mapping;
    using FastServices.Services.Messaging;
    using FastServices.Services.Orders;
    using FastServices.Services.Quartz;
    using FastServices.Services.Services;
    using FastServices.Services.Users;
    using FastServices.Web.Quartz.Jobs.Orders;
    using FastServices.Web.ViewModels;
    using global::Quartz;
    using global::Quartz.Impl;
    using global::Quartz.Spi;
    using GoogleReCaptcha.V3;
    using GoogleReCaptcha.V3.Interface;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Stripe;

    public class Startup
    {
        private readonly IConfiguration configuration;

        public Startup(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add Quartz services
            services.AddSingleton<IJobFactory, SingletonJobFactory>();
            services.AddSingleton<ISchedulerFactory, StdSchedulerFactory>();

            // Quartz jobs
            services.AddSingleton<ChangeOrderStatus>();
            services.AddSingleton(new JobSchedule(
                jobType: typeof(ChangeOrderStatus),
                cronExpression: "0 0/30 * * * ?")); // runs every 30 minutes

            services.AddSingleton<IncreaseEmployeeSalary>();
            services.AddSingleton(new JobSchedule(
                jobType: typeof(IncreaseEmployeeSalary),
                cronExpression: "0 30 10 1 * ?")); // runs on the first day of every month at 10:30 AM
        //cronExpression: "0 0/1 * * * ?")); // runs every 1 minute

            services.AddHostedService<QuartzHostedService>();

            services.AddDbContext<ApplicationDbContext>(
                options => options.UseSqlServer(this.configuration.GetConnectionString("DefaultConnection")));

            services.AddDefaultIdentity<ApplicationUser>(IdentityOptionsProvider.GetIdentityOptions)
                .AddRoles<ApplicationRole>().AddEntityFrameworkStores<ApplicationDbContext>();

            services.Configure<CookiePolicyOptions>(
                options =>
                    {
                        options.CheckConsentNeeded = context => true;
                        options.MinimumSameSitePolicy = SameSiteMode.None;
                    });

            services.AddControllersWithViews(
                options =>
                    {
                        options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute());
                    }).AddRazorRuntimeCompilation();
            services.AddRazorPages();

            // Google ReCaptcha v3
            services.AddHttpClient<ICaptchaValidator, GoogleReCaptchaValidator>();

            services.AddSingleton(this.configuration);

            // Data repositories
            services.AddScoped(typeof(IDeletableEntityRepository<>), typeof(EfDeletableEntityRepository<>));
            services.AddScoped(typeof(IRepository<>), typeof(EfRepository<>));
            services.AddScoped<IDbQueryRunner, DbQueryRunner>();

            // Application services
            services.AddTransient<IEmailSender>(
                x => new SendGridEmailSender(
                        this.configuration.GetSection("SendGrid")["ApiKey"]));
            services.AddTransient<IDepartmentsService, DepartmentsService>();
            services.AddTransient<IServicesService, ServicesService>();
            services.AddTransient<IOrdersService, OrdersService>();
            services.AddTransient<ICommentsService, CommentsService>();
            services.AddTransient<IUsersService, UsersService>();
            services.AddTransient<IEmployeesService, EmployeesService>();
            services.AddTransient<IEmployeeOrdersService, EmployeeOrdersService>();
            services.AddTransient<IComplaintsService, ComplaintsService>();
            services.AddTransient<IImageServices, ImageServices>();

            // inject current user anywhere
            services.AddTransient<IPrincipal>(provider => provider.GetService<IHttpContextAccessor>().HttpContext.User);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            StripeConfiguration.ApiKey = this.configuration.GetSection("Stripe")["PrivateKey"];

            AutoMapperConfig.RegisterMappings(typeof(ErrorViewModel).GetTypeInfo().Assembly);

            // Seed data on application startup
            using (var serviceScope = app.ApplicationServices.CreateScope())
            {
                var dbContext = serviceScope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                dbContext.Database.Migrate();
                new ApplicationDbContextSeeder().SeedAsync(dbContext, serviceScope.ServiceProvider).GetAwaiter().GetResult();
            }

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
            }
            else
            {
                app.UseStatusCodePagesWithRedirects("/Home/Error?code={0}");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(
                endpoints =>
                    {
                        endpoints.MapControllerRoute("areaRoute", "{area:exists}/{controller=Home}/{action=Index}/{id?}");
                        endpoints.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
                        endpoints.MapRazorPages();
                    });
        }
    }
}
