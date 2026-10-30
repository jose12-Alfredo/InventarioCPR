using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace InventarioCPR.Infrastructure.Persistence;

// EF CLI must explicitly opt into the test branch; never fall back to NeonDirect for migrations.
public sealed class MigrationTestDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        if (!args.Contains("--migration-test", StringComparer.Ordinal))
            throw new InvalidOperationException("EF CLI requiere -- --migration-test; no se permite apuntar a NeonDirect.");

        var configuration = new ConfigurationBuilder().AddUserSecrets("InventarioCPR-local").Build();
        var raw = configuration.GetConnectionString("NeonMigrationTest");
        var expected = configuration["NeonMigrationTest:ExpectedHost"];
        if (string.IsNullOrWhiteSpace(raw) || string.IsNullOrWhiteSpace(expected))
            throw new InvalidOperationException("Configure el endpoint de prueba aprobado en User Secrets.");

        var test = new NpgsqlConnectionStringBuilder(raw) { SslMode = SslMode.VerifyFull };
        var host = (test.Host ?? "").Replace("-pooler.", ".", StringComparison.OrdinalIgnoreCase);
        var expectedHost = expected.Replace("-pooler.", ".", StringComparison.OrdinalIgnoreCase);
        if (!string.Equals(host, expectedHost, StringComparison.OrdinalIgnoreCase) || test.Database != "neondb")
            throw new InvalidOperationException("El destino de la migración no coincide con la rama de prueba aprobada.");

        var defaultRaw = configuration.GetConnectionString("NeonDirect");
        if (!string.IsNullOrWhiteSpace(defaultRaw))
        {
            var defaultHost = (new NpgsqlConnectionStringBuilder(defaultRaw).Host ?? "")
                .Replace("-pooler.", ".", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(host, defaultHost, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("La conexión de prueba coincide con NeonDirect.");
        }

        return new InventoryDbContext(new DbContextOptionsBuilder<InventoryDbContext>()
            .UseNpgsql(test.ConnectionString).Options);
    }
}
