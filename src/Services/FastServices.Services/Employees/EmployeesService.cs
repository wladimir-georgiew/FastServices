﻿namespace FastServices.Services.Employees
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using FastServices.Data;
    using FastServices.Data.Common.Repositories;
    using FastServices.Data.Models;

    public class EmployeesService : IEmployeesService
    {
        private readonly IDeletableEntityRepository<Employee> repository;

        public EmployeesService(IDeletableEntityRepository<Employee> repository)
        {
            this.repository = repository;
        }

        public async Task AddAsync(Employee employee)
        {
            await this.repository.AddAsync(employee);
        }

        public IQueryable<Employee> GetAll() => this.repository.All();

        public IQueryable<Employee> GetAvailable() => this.repository.All().Where(x => x.IsAvailable == true);

        public IQueryable<Employee> GetDeleted() => this.repository.AllWithDeleted().Where(x => x.IsDeleted == true);

        public IQueryable<Employee> GetAllWithDeleted() => this.repository.AllWithDeleted();

        public async Task<Employee> GetByIdWithDeletedAsync(string id) => await this.repository.GetByIdWithDeletedAsync(id);

        public async Task UndeleteByIdAsync(string id)
        {
            this.repository.Undelete(await this.GetByIdWithDeletedAsync(id));
            await this.repository.SaveChangesAsync();
        }

        public async Task DeleteByIdAsync(string id)
        {
            this.repository.Delete(await this.GetByIdWithDeletedAsync(id));
            await this.repository.SaveChangesAsync();
        }
    }
}
