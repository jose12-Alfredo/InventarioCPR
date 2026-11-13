using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InventarioCPR.Infrastructure.Security.DataProtection;

// Deliberately independent of inventory and Identity models and migration histories.
public sealed class ProtectionDbContext(DbContextOptions<ProtectionDbContext> options)
    : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("security");
        modelBuilder.Entity<DataProtectionKey>().ToTable("data_protection_keys");
    }
}
