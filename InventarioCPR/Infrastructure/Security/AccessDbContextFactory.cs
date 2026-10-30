using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Npgsql;

namespace InventarioCPR.Infrastructure.Security;

public sealed class AccessDbContextFactory : IDesignTimeDbContextFactory<AccessDbContext>
{
    public AccessDbContext CreateDbContext(string[] args)
    {
        if (!args.Contains("--auth-test", StringComparer.Ordinal))
            throw new InvalidOperationException("EF CLI para Identity requiere -- --auth-test; production no está habilitada.");

        var configuration = new ConfigurationBuilder().AddUserSecrets("InventarioCPR-local").Build();
        var raw = configuration.GetConnectionString("NeonMigrationTest");
        var expected = configuration["NeonMigrationTest:ExpectedHost"];
        if (string.IsNullOrWhiteSpace(raw) || string.IsNullOrWhiteSpace(expected))
            throw new InvalidOperationException("No está configurado el endpoint aislado autorizado.");

        var test = new NpgsqlConnectionStringBuilder(raw) { SslMode = SslMode.VerifyFull };
        var host = (test.Host ?? "").Replace("-pooler.", ".", StringComparison.OrdinalIgnoreCase);
        var expectedHost = expected.Replace("-pooler.", ".", StringComparison.OrdinalIgnoreCase);
        var defaultRaw = configuration.GetConnectionString("NeonDirect");
        var defaultHost = string.IsNullOrWhiteSpace(defaultRaw) ? null :
            (new NpgsqlConnectionStringBuilder(defaultRaw).Host ?? "").Replace("-pooler.", ".", StringComparison.OrdinalIgnoreCase);
        if (!string.Equals(host, expectedHost, StringComparison.OrdinalIgnoreCase) || test.Database != "neondb" ||
            string.Equals(host, defaultHost, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("El destino de Identity no es la rama de pruebas aprobada.");

        return new AccessDbContext(new DbContextOptionsBuilder<AccessDbContext>()
            .UseNpgsql(test.ConnectionString, x => x.MigrationsHistoryTable("__AuthMigrationsHistory")).Options);
    }
}
