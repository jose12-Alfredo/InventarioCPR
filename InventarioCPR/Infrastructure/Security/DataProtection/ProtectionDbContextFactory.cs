using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InventarioCPR.Infrastructure.Security.DataProtection;

public sealed class ProtectionDbContextFactory : IDesignTimeDbContextFactory<ProtectionDbContext>
{
    public ProtectionDbContext CreateDbContext(string[] args)
    {
        // Offline scaffolding only. No configuration or real connection is loaded.
        var options = new DbContextOptionsBuilder<ProtectionDbContext>();
        options.UseNpgsql("Host=localhost;Database=protection_design_only;Username=design_only",
            npgsql => npgsql.MigrationsHistoryTable("__DataProtectionMigrationsHistory", "security"));
        return new ProtectionDbContext(options.Options);
    }
}
