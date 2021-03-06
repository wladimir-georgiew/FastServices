﻿namespace FastServices.Data.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    using FastServices.Data.Common.Models;

    public class Service : IDeletableEntity, IAuditInfo
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; }

        public Department Department { get; set; }

        [Required]
        public int DepartmentId { get; set; }

        [Required]
        public decimal Fee { get; set; }

        [Required]
        [MaxLength(300)]
        [MinLength(20)]
        public string Description { get; set; }

        [Required]
        public string CardImgSrc { get; set; }

        // Audit info
        public DateTime CreatedOn { get; set; }

        public DateTime? ModifiedOn { get; set; }

        // Deletable entity
        public bool IsDeleted { get; set; }

        public DateTime? DeletedOn { get; set; }
    }
}
