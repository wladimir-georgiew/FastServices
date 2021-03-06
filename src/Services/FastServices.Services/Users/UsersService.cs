﻿using FastServices.Web.ViewModels.Administration;
using FastServices.Web.ViewModels.Employees;

namespace FastServices.Services.Users
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using FastServices.Data;
    using FastServices.Data.Common.Repositories;
    using FastServices.Data.Models;
    using FastServices.Services.Comments;
    using FastServices.Services.Orders;
    using FastServices.Web.ViewModels.Orders;
    using Microsoft.AspNetCore.Identity;

    public class UsersService : IUsersService
    {
        private readonly IDeletableEntityRepository<ApplicationUser> repository;
        private readonly ICommentsService commentsService;
        private readonly RoleManager<ApplicationRole> roleManager;
        private readonly UserManager<ApplicationUser> userManager;

        public UsersService(
            IDeletableEntityRepository<ApplicationUser> repository,
            ICommentsService commentsService,
            RoleManager<ApplicationRole> roleManager,
            UserManager<ApplicationUser> userManager)
        {
            this.repository = repository;
            this.commentsService = commentsService;
            this.roleManager = roleManager;
            this.userManager = userManager;
        }

        public bool IsUserAllowedToComment(string userId)
        {
            var userLatestComment = this.GetUserLatestComment(userId);

            if (userLatestComment != null)
            {
                var dateDiff = DateTime.UtcNow.Subtract(userLatestComment.CreatedOn);

                if (dateDiff.Days < 1)
                {
                    return false;
                }
            }

            return true;
        }

        public Comment GetUserLatestComment(string userId) =>
            this.commentsService.GetAll()
            .Where(x => x.ApplicationUserId == userId)
            .OrderByDescending(x => x.CreatedOn)
            .FirstOrDefault();

        public IQueryable<ApplicationUser> GetAll() => this.repository.All();

        public async Task<ApplicationUser> GetByIdWithDeletedAsync(string id) => await this.repository.GetByIdWithDeletedAsync(id);

        public async Task UpdateUserAvatarImg(string userId, string uniqueFileName)
        {
            var user = await this.GetByIdWithDeletedAsync(userId);

            user.AvatarImgSrc = "/images/" + uniqueFileName;

            await this.repository.SaveChangesAsync();
        }

        public bool IsUserAllowedToSubmitOrder(string userId)
        {
            // if user has any active order
            if (this.repository.All()
                .Where(x => x.Id == userId)
                .SelectMany(x => x.Orders)
                .Any(x => x.Status != Data.Models.Enumerators.OrderStatus.Completed))
            {
                return false;
            }

            return true;
        }

        public async Task AssignUserToRoleAsync(string roleName, ApplicationUser user)
        {
            var role = await this.roleManager.FindByNameAsync(roleName);

            if (role != null && user != null &&
                !user.Roles.Any(x => x.RoleId == role.Id))
            {
                await this.userManager.AddToRoleAsync(user, roleName);
            }

            await this.repository.SaveChangesAsync();
        }

        public async Task<ApplicationUser> CreateUserAsync(EmployeeInputModel model, string uniqueFileName)
        {
            var user = new ApplicationUser
            {
                Name = model.FirstName,
                NormalizedUserName = model.FirstName.ToUpper(),
                Email = model.Email,
                NormalizedEmail = model.Email.ToUpper(),
                UserName = model.Email,
                PhoneNumber = model.PhoneNumber,
                AvatarImgSrc = !string.IsNullOrEmpty(uniqueFileName)
                    ? ("/images/" + uniqueFileName)
                    : "/defaultImages/defEmployeeAvatarImg.png",
            };
            await this.userManager.CreateAsync(user, model.Password);

            return user;
        }
    }
}
