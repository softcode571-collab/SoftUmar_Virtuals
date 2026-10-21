namespace SoftUmar_Virtuals.Domain.Entities
{
    // Single-row settings table - there should only ever be ONE row in this
    // table, representing the current platform-wide pricing config. Add a
    // DbSet<PlatformSettings> to your DbContext and run a migration for this
    // like any other new entity.
    public class PlatformSettings
    {
        public Guid Id { get; set; }

        // How many Naira is 1 US Dollar worth RIGHT NOW, as YOU decide it -
        // not a live forex rate. e.g. 1650 means $1 = N1650.
        public decimal UsdToNgnRate { get; set; }

        // Your profit margin, as a percentage added ON TOP of 5SIM's raw
        // USD cost, BEFORE converting to Naira. e.g. 20 means you charge
        // 20% more than what 5SIM actually charges you.
        public decimal MarkupPercentage { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}