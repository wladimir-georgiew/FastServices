﻿using FastServices.Services.Employees;

namespace FastServices.Web.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Claims;
    using System.Threading.Tasks;

    using FastServices.Common;
    using FastServices.Data.Models;
    using FastServices.Services.Departments;
    using FastServices.Services.Orders;
    using FastServices.Services.Services;
    using FastServices.Services.Users;
    using FastServices.Web.ViewModels.Orders;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Identity;
    using Microsoft.AspNetCore.Mvc;

    public class ServicesController : Controller
    {
        private readonly IDepartmentsService departmentsService;
        private readonly IServicesService servicesService;
        private readonly IOrdersService ordersService;
        private readonly IUsersService usersService;
        private readonly UserManager<ApplicationUser> userManager;
        private readonly IEmployeesService employeesService;

        public ServicesController(
            IDepartmentsService departmentsService,
            IServicesService servicesService,
            IOrdersService ordersService,
            IUsersService usersService,
            IEmployeesService employeesService,
            UserManager<ApplicationUser> userManager)
        {
            this.departmentsService = departmentsService;
            this.servicesService = servicesService;
            this.ordersService = ordersService;
            this.usersService = usersService;
            this.userManager = userManager;
            this.employeesService = employeesService;
        }

        [Authorize]
        public async Task<IActionResult> Service(int serviceId)
        {
            var service = await this.servicesService.GetByIdWithDeletedAsync(serviceId);

            if (service == null)
            {
                return this.NotFound();
            }

            var department = await this.departmentsService.GetDepartmentByIdAsync(service.DepartmentId);

            this.ViewData["topImageNavUrl"] = department.BackgroundImgSrc;
            this.ViewData["serviceName"] = service.Name;
            this.ViewData["description"] = service.Description.ToString();
            return this.View();
        }

        // Add Order
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Service(OrderInputModel input)
        {
            var service = await this.servicesService.GetByIdWithDeletedAsync(input.ServiceId);
            var department = await this.departmentsService.GetDepartmentByIdAsync(service.DepartmentId);
            var userId = this.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await this.usersService.GetByIdWithDeletedAsync(userId);

            this.ViewData["topImageNavUrl"] = department.BackgroundImgSrc;
            this.ViewData["serviceName"] = service.Name;
            this.ViewData["description"] = service.Description.ToString();

            var availableEmployees = this.employeesService
                .GetAllAvailableEmployees(department.Id, input.StartDate, input.DueDate);

            var order = this.ordersService.GetOrderFromInputModel(input);
            order.Price = ((GlobalConstants.HourlyFeePerWorker * input.WorkersCount) * input.HoursBooked) + service.Fee;

            var roles = this.userManager.GetRolesAsync(user).GetAwaiter().GetResult();

            if (roles.Contains(GlobalConstants.EmployeeRoleName) || roles.Contains(GlobalConstants.AdministratorRoleName))
            {
                this.ModelState.AddModelError(string.Empty, GlobalConstants.ErrorRoleSubmitOrder);
                return this.View(input);
            }

            if (!this.ModelState.IsValid)
            {
                return this.View(input);
            }

            if (!this.usersService.IsUserAllowedToSubmitOrder(userId))
            {
                this.ModelState.AddModelError(string.Empty, GlobalConstants.ErrorOrderSubmitOneOrderAtATime);
                return this.View(input);
            }

            if (!this.ordersService.HasAvailableEmployeesForTheOrderAsync(availableEmployees, order))
            {
                this.ModelState.AddModelError(string.Empty, GlobalConstants.ErrorOrderNotEnoughAvailableEmployees);
                return this.View(input);
            }

            await this.ordersService.AddAsync(order, availableEmployees, user);

            this.TempData["msg"] = GlobalConstants.SuccessOrderSubmitted;
            return this.View();
        }
    }
}
