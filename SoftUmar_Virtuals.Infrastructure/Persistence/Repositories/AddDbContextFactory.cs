using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace VirtualNumber.Infrastructure.Persistence.Repositories
{
    public class ApplicationDbContextFactory(DbContextOptions<ApplicationDbContextFactory> options) : DbContext(options)
    {
    }
}
