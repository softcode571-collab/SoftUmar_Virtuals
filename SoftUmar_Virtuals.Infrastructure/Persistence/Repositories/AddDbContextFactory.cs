using Microsoft.EntityFrameworkCore;
using SoftUmar_Virtuals.Domain.Entities;

namespace VirtualNumber.Infrastructure.Persistence.Repositories
{
    public class ApplicationDbContextFactory : DbContext
    {
        public ApplicationDbContextFactory(DbContextOptions<ApplicationDbContextFactory> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<Login> Logins { get; set; }

        public DbSet<Wallets> Wallets { get; set; }

        public DbSet<WalletTransaction> WalletTransactions { get; set; }

        public DbSet<FiveSimOrder> FiveSimOrders { get; set; }

        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }

        public DbSet<Refund> Refunds { get; set; }
        
        public DbSet<PlatformSettings> PlatformSetting { get; set; }

        public DbSet<AuditLogs> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasOne(u => u.Wallet)
                .WithOne(w => w.User)
                .HasForeignKey<Wallets>(w => w.UserId);

            // ... your existing config ...

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}