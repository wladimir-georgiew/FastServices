﻿namespace FastServices.Services.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using FastServices.Data;
    using FastServices.Data.Common.Repositories;
    using FastServices.Data.Models;

    public class ServicesService : IServicesService
    {
        private readonly IDeletableEntityRepository<Service> repository;

        public ServicesService(IDeletableEntityRepository<Service> repository)
        {
            this.repository = repository;
        }

        public IQueryable<Service> GetAllServices() => this.repository.All();

        public IQueryable<Service> GetAllServicesWithDeleted() => this.repository.AllWithDeleted();

        public async Task<Service> GetByIdWtihDeletedAsync(int id) => await this.repository.GetByIdWithDeletedAsync(id);
    }
}
