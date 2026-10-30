using Npgsql;

namespace InventarioCPR.Infrastructure.Persistence;

public static class NeonConnection
{
    public static string Direct(IConfiguration configuration)
    {
        var raw = configuration.GetConnectionString("NeonDirect");
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("Configure ConnectionStrings:NeonDirect en User Secrets.");

        var connection = new NpgsqlConnectionStringBuilder(raw);
        if (string.IsNullOrWhiteSpace(connection.Host))
            throw new InvalidOperationException("Configure un host de Neon válido.");

        connection.SslMode = SslMode.VerifyFull;
        return connection.ConnectionString;
    }

    public static string DevelopmentApp(IConfiguration configuration)
    {
        var raw = configuration.GetConnectionString("NeonDevelopmentApp");
        var migrationRaw = configuration.GetConnectionString("NeonMigrationTest");
        var expectedHost = configuration["NeonMigrationTest:ExpectedHost"];
        if (string.IsNullOrWhiteSpace(raw) || string.IsNullOrWhiteSpace(migrationRaw) ||
            string.IsNullOrWhiteSpace(expectedHost))
            throw new InvalidOperationException("Configure NeonDevelopmentApp, NeonMigrationTest y ExpectedHost en User Secrets.");

        var app = new NpgsqlConnectionStringBuilder(raw) { SslMode = SslMode.VerifyFull, Pooling = true, MaxPoolSize = 20 };
        var migration = new NpgsqlConnectionStringBuilder(migrationRaw);
        var appHost = (app.Host ?? "").Replace("-pooler.", ".", StringComparison.OrdinalIgnoreCase);
        var migrationHost = (migration.Host ?? "").Replace("-pooler.", ".", StringComparison.OrdinalIgnoreCase);
        var expected = expectedHost.Replace("-pooler.", ".", StringComparison.OrdinalIgnoreCase);
        if (app.Database != "neondb" ||
            !(app.Host ?? "").Contains("-pooler.", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(appHost, expected, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(appHost, migrationHost, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("La conexión de aplicación no apunta al endpoint de desarrollo aprobado.");

        var defaultRaw = configuration.GetConnectionString("NeonDirect");
        if (!string.IsNullOrWhiteSpace(defaultRaw))
        {
            var defaultHost = (new NpgsqlConnectionStringBuilder(defaultRaw).Host ?? "")
                .Replace("-pooler.", ".", StringComparison.OrdinalIgnoreCase);
            if (string.Equals(appHost, defaultHost, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("La conexión de aplicación coincide con NeonDirect.");
        }

        return app.ConnectionString;
    }

    public static string Runtime(IConfiguration configuration, bool isDevelopment)
    {
        if (isDevelopment) return DevelopmentApp(configuration);

        var raw = configuration.GetConnectionString("Database");
        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("Configure ConnectionStrings:Database mediante variables de entorno.");

        var connection = new NpgsqlConnectionStringBuilder(raw)
        {
            SslMode = SslMode.VerifyFull,
            Pooling = true,
            MaxPoolSize = 20
        };
        if (string.IsNullOrWhiteSpace(connection.Host) || string.IsNullOrWhiteSpace(connection.Database))
            throw new InvalidOperationException("La conexión de producción requiere host y base de datos.");

        return connection.ConnectionString;
    }
}
