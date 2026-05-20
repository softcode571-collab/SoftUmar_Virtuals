using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using SoftUmar_Virtuals.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace VirtualNumber.Infrastructure.Persistence.Repositories
{
    public class ApplicationDbContextFactory(DbContextOptions<ApplicationDbContextFactory> options) : DbContext(options)
    {
        public DbSet<User> user { get; set; }
    }
}
