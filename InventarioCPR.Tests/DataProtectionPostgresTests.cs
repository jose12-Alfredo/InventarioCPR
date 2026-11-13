using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using InventarioCPR.Infrastructure.Security.DataProtection;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;
using Xunit;

namespace InventarioCPR.Tests;

public sealed class LocalProtectionPostgresFactAttribute : FactAttribute
{
    public LocalProtectionPostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("INVENTARIOCPR_DP_LOCAL_POSTGRES")))
            Skip = "Requiere PostgreSQL local desechable mediante INVENTARIOCPR_DP_LOCAL_POSTGRES; nunca Neon.";
    }
}

public sealed class DataProtectionPostgresTests
{
    [LocalProtectionPostgresFact]
    public async Task IsolatedPostgresMigrationAndEncryptedKeyRingSurviveProviderRestart()
    {
        var admin = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("INVENTARIOCPR_DP_LOCAL_POSTGRES"));
        // Explicitly reject external hosts, multi-host strings, sockets and preselected application databases.
        if (admin.Host is not ("localhost" or "127.0.0.1" or "::1") || admin.Database != "postgres" || admin.Port <= 0)
            throw new InvalidOperationException("La prueba exige host loopback y base administrativa postgres.");
        admin.Pooling = false;
        admin.Timeout = 5;
        admin.IncludeErrorDetail = false;
        var database = "inventariocpr_dp_test_" + Guid.NewGuid().ToString("N");
        await using var connection = new NpgsqlConnection(admin.ConnectionString);
        await connection.OpenAsync();
        await using (var create = new NpgsqlCommand($"CREATE DATABASE \"{database}\"", connection))
            await create.ExecuteNonQueryAsync();
        try
        {
            var test = new NpgsqlConnectionStringBuilder(admin.ConnectionString) { Database = database };
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=LOCAL-POSTGRES-TEST-ONLY", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddDays(1));
            using var first = Provider(test.ConnectionString, certificate);
            using (var scope = first.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ProtectionDbContext>();
                await db.Database.MigrateAsync(); // Only the randomly created loopback database above.
                Assert.Empty(await db.Database.GetPendingMigrationsAsync());
            }
            var protectedValue = first.GetRequiredService<IDataProtectionProvider>().CreateProtector("postgres-test").Protect("persisted");
            using var second = Provider(test.ConnectionString, certificate);
            Assert.Equal("persisted", second.GetRequiredService<IDataProtectionProvider>().CreateProtector("postgres-test").Unprotect(protectedValue));
            using var read = second.CreateScope();
            var keys = await read.ServiceProvider.GetRequiredService<ProtectionDbContext>().DataProtectionKeys.ToListAsync();
            Assert.NotEmpty(keys);
            Assert.All(keys, key => Assert.Contains("encryptedSecret", key.Xml!));
            await using var check = new NpgsqlConnection(test.ConnectionString);
            await check.OpenAsync();
            await using var tables = new NpgsqlCommand("SELECT count(*) FROM pg_tables WHERE schemaname='public'", check);
            Assert.Equal(0L, await tables.ExecuteScalarAsync());
        }
        finally
        {
            await using var drop = new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)", connection);
            await drop.ExecuteNonQueryAsync();
        }
    }

    private static ServiceProvider Provider(string connection, X509Certificate2 certificate)
    {
        var services = new ServiceCollection().AddLogging(logging => logging.ClearProviders());
        services.AddDbContext<ProtectionDbContext>(options => options.UseNpgsql(connection,
            pg => pg.MigrationsHistoryTable("__DataProtectionMigrationsHistory", "security")));
        services.AddDataProtection().SetApplicationName(ProtectionConfiguration.ApplicationName)
            .PersistKeysToDbContext<ProtectionDbContext>().ProtectKeysWithCertificate(certificate);
        return services.BuildServiceProvider();
    }
}
