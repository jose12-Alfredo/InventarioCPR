using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using InventarioCPR.Infrastructure.Persistence;

namespace InventarioCPR.Infrastructure.Security.DataProtection;

public static class ProtectionConfiguration
{
    public const string ApplicationName = "InventarioCPR";

    public static IServiceCollection AddInventoryDataProtection(this IServiceCollection services,
        IConfiguration configuration, bool isDevelopment, bool isRender)
    {
        var enabled = configuration.GetValue<bool>("DataProtection:UseDatabase");
        if (isRender && !enabled)
            throw new InvalidOperationException("Render requiere DataProtection:UseDatabase=true y certificado de protección.");
        if (!enabled) return services; // Existing local developer key ring remains usable.

        var raw = configuration.GetConnectionString("DataProtection");
        var connection = string.IsNullOrWhiteSpace(raw)
            ? NeonConnection.Runtime(configuration, isDevelopment)
            : SecureConnection(raw);
        var certificates = new ProtectionCertificates(configuration);
        services.AddSingleton(_ => certificates);
        // Eager validation; the container owns disposal after it resolves the holder at startup.
        services.AddSingleton<IHostedService, ProtectionStartupCheck>();
        services.AddDbContext<ProtectionDbContext>(options => options.UseNpgsql(connection,
            npgsql => npgsql.MigrationsHistoryTable("__DataProtectionMigrationsHistory", "security")));
        services.AddDataProtection().SetApplicationName(ApplicationName)
            .PersistKeysToDbContext<ProtectionDbContext>()
            .ProtectKeysWithCertificate(certificates.Active)
            .UnprotectKeysWithAnyCertificate(certificates.All);
        return services;
    }

    private static string SecureConnection(string raw)
    {
        try
        {
            var connection = new Npgsql.NpgsqlConnectionStringBuilder(raw)
            {
                SslMode = Npgsql.SslMode.VerifyFull, Pooling = true, MaxPoolSize = 5,
                IncludeErrorDetail = false, LogParameters = false
            };
            if (string.IsNullOrWhiteSpace(connection.Host) || string.IsNullOrWhiteSpace(connection.Database))
                throw new InvalidOperationException();
            return connection.ConnectionString;
        }
        catch { throw new InvalidOperationException("ConnectionStrings:DataProtection no es válida; valores omitidos."); }
    }
}

public sealed class ProtectionCertificates : IDisposable
{
    public X509Certificate2 Active { get; }
    public X509Certificate2[] All { get; }

    public ProtectionCertificates(IConfiguration configuration)
    {
        var loaded = new List<X509Certificate2>();
        try
        {
            Active = Load(configuration.GetSection("DataProtection:Certificate"), requireCurrent: true);
            loaded.Add(Active);
            foreach (var previous in configuration.GetSection("DataProtection:PreviousCertificates").GetChildren())
                loaded.Add(Load(previous, requireCurrent: false));
            All = loaded.ToArray();
        }
        catch
        {
            foreach (var certificate in loaded) certificate.Dispose();
            throw new InvalidOperationException("Configuración de certificados Data Protection incompleta o inválida; valores omitidos.");
        }
    }

    private static X509Certificate2 Load(IConfigurationSection section, bool requireCurrent)
    {
        var path = section["Path"];
        var base64 = section["Base64"];
        var password = section["Password"];
        if (string.IsNullOrWhiteSpace(password) ||
            (string.IsNullOrWhiteSpace(path) == string.IsNullOrWhiteSpace(base64)))
            throw new InvalidOperationException();
        if (path is not null && !Path.IsPathRooted(path)) throw new InvalidOperationException();
        var bytes = path is not null ? File.ReadAllBytes(path) : Convert.FromBase64String(base64!);
        X509Certificate2? certificate = null;
        try
        {
            certificate = X509CertificateLoader.LoadPkcs12(bytes, password, X509KeyStorageFlags.EphemeralKeySet);
            using var rsa = certificate.GetRSAPrivateKey();
            if (rsa is null || rsa.KeySize < 2048 || (requireCurrent &&
                (certificate.NotBefore.ToUniversalTime() > DateTime.UtcNow || certificate.NotAfter.ToUniversalTime() <= DateTime.UtcNow)))
                throw new InvalidOperationException();
            return certificate;
        }
        catch { certificate?.Dispose(); throw; }
        finally { CryptographicOperations.ZeroMemory(bytes); }
    }

    public void Dispose()
    {
        foreach (var certificate in All) certificate.Dispose();
    }
}

internal sealed class ProtectionStartupCheck(IServiceProvider services, ProtectionCertificates certificates) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _ = certificates.Active;
            await using var scope = services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ProtectionDbContext>();
            if ((await db.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
                throw new InvalidOperationException();
            var keys = services.GetRequiredService<Microsoft.AspNetCore.DataProtection.KeyManagement.IKeyManager>();
            foreach (var key in keys.GetAllKeys().Where(key => !key.IsRevoked))
                _ = key.CreateEncryptor(); // Do not silently replace an unreadable historical key ring.
            // Exercises actual read, encrypted write when a new key is needed, and decrypt before accepting traffic.
            var protector = services.GetRequiredService<IDataProtectionProvider>().CreateProtector("startup-probe");
            if (protector.Unprotect(protector.Protect("InventarioCPR")) != "InventarioCPR")
                throw new CryptographicException();
        }
        catch
        {
            throw new InvalidOperationException("Data Protection no está lista: revise conexión, migración autorizada y certificados. No se aplicaron migraciones automáticamente.");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
