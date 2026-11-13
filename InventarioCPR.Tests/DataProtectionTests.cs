using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using InventarioCPR.Application.Security;
using InventarioCPR.Infrastructure.Security;
using InventarioCPR.Infrastructure.Security.DataProtection;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit;

namespace InventarioCPR.Tests;

public sealed class DataProtectionTests
{
    [Fact]
    public async Task EncryptedKeysSurviveNewHostAndCertificateRotation()
    {
        using var fixture = new ProtectionFixture();
        var first = fixture.Certificate();
        var second = fixture.Certificate();
        string payload;
        await using (var host = await fixture.Start(first))
        {
            payload = host.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("test").Protect("private-value");
            Assert.Throws<CryptographicException>(() => host.Services.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("different-purpose").Unprotect(payload));
            Assert.Throws<CryptographicException>(() => host.Services.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("test").Unprotect(payload[..^5] + "AAAAA"));
            using var scope = host.Services.CreateScope();
            var xml = await scope.ServiceProvider.GetRequiredService<ProtectionDbContext>().DataProtectionKeys.Select(k => k.Xml).FirstAsync();
            Assert.Contains("encryptedSecret", xml!);
            Assert.DoesNotContain("<masterKey", xml!);
            Assert.DoesNotContain("private-value", xml!);
        }
        await using (var host = await fixture.Start(second, first))
        {
            Assert.Equal("private-value", host.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("test").Unprotect(payload));
            host.Services.GetRequiredService<IKeyManager>().CreateNewKey(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(90));
        }
        await using (var host = await fixture.Start(second, first))
        {
            Assert.Equal("private-value", host.Services.GetRequiredService<IDataProtectionProvider>().CreateProtector("test").Unprotect(payload));
            using var scope = host.Services.CreateScope();
            var keys = await scope.ServiceProvider.GetRequiredService<ProtectionDbContext>().DataProtectionKeys.Select(k => k.Xml).ToListAsync();
            Assert.True(keys.Count >= 2);
            Assert.All(keys, xml => Assert.Contains("encryptedSecret", xml!));
            using var newCertificate = X509CertificateLoader.LoadPkcs12(Convert.FromBase64String(second), "test-only-password",
                X509KeyStorageFlags.EphemeralKeySet);
            Assert.Contains(keys, xml => System.Xml.Linq.XDocument.Parse(xml!).Descendants()
                .Any(element => element.Name.LocalName == "X509Certificate" &&
                    Convert.FromBase64String(element.Value).SequenceEqual(newCertificate.RawData)));
        }
        // A missing old private key is a startup error, not a silently replaced key ring.
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Start(second));
    }

    [Fact]
    public void CertificateCanBeLoadedFromRenderSecretFileWithoutPersistentDisk()
    {
        using var fixture = new ProtectionFixture();
        var settings = fixture.Settings(fixture.Certificate());
        var path = Path.Combine(Path.GetTempPath(), "inventariocpr-test-" + Guid.NewGuid().ToString("N") + ".pfx");
        try
        {
            File.WriteAllBytes(path, Convert.FromBase64String(settings["DataProtection:Certificate:Base64"]!));
            settings.Remove("DataProtection:Certificate:Base64");
            settings["DataProtection:Certificate:Path"] = path;
            using var certificates = new ProtectionCertificates(new ConfigurationBuilder().AddInMemoryCollection(settings).Build());
            Assert.True(certificates.Active.HasPrivateKey);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void MissingConnectionIsRejectedAndUnconfiguredDevelopmentRemainsUsable()
    {
        using var fixture = new ProtectionFixture();
        var settings = fixture.Settings(fixture.Certificate());
        settings.Remove("ConnectionStrings:DataProtection");
        Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddInventoryDataProtection(
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build(), false, true));
        var local = new ServiceCollection();
        local.AddInventoryDataProtection(new ConfigurationBuilder().Build(), true, false);
        Assert.DoesNotContain(local, registration => registration.ServiceType == typeof(ProtectionDbContext));
    }

    [Fact]
    public async Task IdentityLoginCookieAntiforgeryAndLogoutSurviveHostRestart()
    {
        using var fixture = new ProtectionFixture();
        var certificate = fixture.Certificate();
        var cookies = new Dictionary<string, string>();
        string anonymousToken;
        await using (var host = await fixture.Start(certificate))
        {
            var client = host.GetTestClient();
            Assert.Equal(HttpStatusCode.Redirect, (await Send(client, "/private", cookies)).StatusCode);
            var page = await Send(client, "/acceso", cookies);
            anonymousToken = Token(await page.Content.ReadAsStringAsync());
            Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, "/acceso", cookies, Login("invalid"))).StatusCode);
            var response = await Send(client, "/acceso", cookies, Login(anonymousToken));
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
            Assert.Equal("/private", response.Headers.Location!.OriginalString);
            Assert.Equal(HttpStatusCode.OK, (await Send(client, "/private", cookies)).StatusCode);
        }
        await using (var host = await fixture.Start(certificate))
        {
            var client = host.GetTestClient();
            Assert.Equal(HttpStatusCode.OK, (await Send(client, "/private", cookies)).StatusCode);
            var token = await (await Send(client, "/token", cookies)).Content.ReadAsStringAsync();
            var priorSession = new Dictionary<string, string>(cookies);
            Assert.Equal(HttpStatusCode.BadRequest, (await Send(client, "/salir", cookies, [])).StatusCode);
            Assert.Equal(HttpStatusCode.Redirect, (await Send(client, "/salir", cookies,
                new() { ["__RequestVerificationToken"] = token })).StatusCode);
            Assert.Equal(HttpStatusCode.Redirect, (await Send(client, "/private", cookies)).StatusCode);
            Assert.Equal(HttpStatusCode.Redirect, (await Send(client, "/private", priorSession)).StatusCode);
        }
        // Anonymous antiforgery form issued before a restart is still decryptable afterwards.
        var anonymousCookies = new Dictionary<string, string>();
        await using (var host = await fixture.Start(certificate))
        {
            anonymousToken = Token(await (await Send(host.GetTestClient(), "/acceso", anonymousCookies)).Content.ReadAsStringAsync());
        }
        await using (var host = await fixture.Start(certificate))
        {
            var response = await Send(host.GetTestClient(), "/acceso", anonymousCookies, Login(anonymousToken));
            Assert.Equal("/private", response.Headers.Location!.OriginalString);
        }
    }

    [Theory]
    [InlineData("disabled")]
    [InlineData("missing-certificate")]
    [InlineData("missing-password")]
    [InlineData("wrong-password")]
    [InlineData("expired")]
    [InlineData("public-only")]
    [InlineData("both-sources")]
    public void RenderRejectsIncompleteOrInvalidConfiguration(string scenario)
    {
        using var fixture = new ProtectionFixture();
        var settings = fixture.Settings(fixture.Certificate(expired: scenario == "expired", publicOnly: scenario == "public-only"));
        if (scenario == "disabled") settings["DataProtection:UseDatabase"] = "false";
        if (scenario == "missing-certificate") settings.Remove("DataProtection:Certificate:Base64");
        if (scenario == "missing-password") settings.Remove("DataProtection:Certificate:Password");
        if (scenario == "wrong-password") settings["DataProtection:Certificate:Password"] = "incorrect-test-password";
        if (scenario == "both-sources") settings["DataProtection:Certificate:Path"] = Path.GetFullPath("ambiguous.pfx");
        var error = Assert.Throws<InvalidOperationException>(() => new ServiceCollection().AddInventoryDataProtection(
            new ConfigurationBuilder().AddInMemoryCollection(settings).Build(), false, true));
        Assert.DoesNotContain(settings.GetValueOrDefault("DataProtection:Certificate:Base64") ?? "not-present", error.ToString());
        Assert.DoesNotContain("test-only-password", error.ToString());
    }

    [Fact]
    public async Task MissingSchemaRejectsStartupWithoutCreatingTables()
    {
        using var fixture = new ProtectionFixture();
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Start(fixture.Certificate(), initialize: false));
        using var db = new ProtectionDbContext(new DbContextOptionsBuilder<ProtectionDbContext>().UseSqlite(fixture.KeyConnection).Options);
        var connection = db.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT count(*) FROM sqlite_master WHERE type='table'";
        Assert.Equal(0L, await command.ExecuteScalarAsync());
    }

    [Fact]
    public void PostgresModelAndMigrationAreIsolatedAndRenderNeedsNoDisk()
    {
        using var fixture = new ProtectionFixture();
        var services = new ServiceCollection().AddLogging();
        services.AddInventoryDataProtection(new ConfigurationBuilder().AddInMemoryCollection(fixture.Settings(fixture.Certificate())).Build(), false, true);
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ProtectionDbContext>();
        Assert.Equal("Npgsql.EntityFrameworkCore.PostgreSQL", db.Database.ProviderName);
        Assert.Single(db.Model.GetEntityTypes());
        Assert.All(db.Model.GetEntityTypes(), entity => Assert.Equal("security", entity.GetSchema()));
        var sql = db.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
        Assert.Contains("security.data_protection_keys", sql);
        Assert.Contains("security.\"__DataProtectionMigrationsHistory\"", sql);
        Assert.DoesNotContain("public.", sql);
        Assert.DoesNotContain("DROP TABLE", sql);
    }

    private static Dictionary<string, string> Login(string token) => new()
    {
        ["usuario"] = "test-admin", ["clave"] = "Temporary-Only!Pass928", ["returnUrl"] = "/private",
        ["__RequestVerificationToken"] = token
    };

    private static string Token(string html) => Regex.Match(html, "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"").Groups[1].Value;

    private static async Task<HttpResponseMessage> Send(HttpClient client, string path, Dictionary<string, string> cookies,
        Dictionary<string, string>? form = null)
    {
        using var request = new HttpRequestMessage(form is null ? HttpMethod.Get : HttpMethod.Post, "https://localhost" + path);
        if (form is not null) request.Content = new FormUrlEncodedContent(form);
        if (cookies.Count > 0) request.Headers.Add("Cookie", string.Join("; ", cookies.Select(pair => pair.Key + "=" + pair.Value)));
        var response = await client.SendAsync(request);
        if (response.Headers.TryGetValues("Set-Cookie", out var headers))
            foreach (var header in headers)
            {
                var pair = header.Split(';')[0].Split('=', 2);
                cookies[pair[0]] = pair[1];
            }
        return response;
    }

    private sealed class ProtectionFixture : IDisposable
    {
        private readonly string directory = Path.Combine(Path.GetTempPath(), "inventariocpr-dp-test-" + Guid.NewGuid().ToString("N"));
        public string KeyConnection => $"Data Source={Path.Combine(directory, "keys.sqlite")};Pooling=False";
        public ProtectionFixture() => Directory.CreateDirectory(directory);

        public string Certificate(bool expired = false, bool publicOnly = false)
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=InventarioCPR-TEST-ONLY", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-2),
                expired ? DateTimeOffset.UtcNow.AddDays(-1) : DateTimeOffset.UtcNow.AddDays(2));
            if (publicOnly)
            {
                using var publicCertificate = X509CertificateLoader.LoadCertificate(certificate.Export(X509ContentType.Cert));
                return Convert.ToBase64String(publicCertificate.Export(X509ContentType.Pfx, "test-only-password"));
            }
            return Convert.ToBase64String(certificate.Export(X509ContentType.Pfx, "test-only-password"));
        }

        public Dictionary<string, string?> Settings(string certificate, string? previous = null)
        {
            var values = new Dictionary<string, string?>
            {
                ["DataProtection:UseDatabase"] = "true",
                ["ConnectionStrings:DataProtection"] = "Host=localhost;Database=unused_render_configuration_test;Username=test",
                ["DataProtection:Certificate:Base64"] = certificate,
                ["DataProtection:Certificate:Password"] = "test-only-password"
            };
            if (previous is not null)
            {
                values["DataProtection:PreviousCertificates:0:Base64"] = previous;
                values["DataProtection:PreviousCertificates:0:Password"] = "test-only-password";
            }
            return values;
        }

        public async Task<WebApplication> Start(string certificate, string? previous = null, bool initialize = true)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
            builder.Logging.ClearProviders();
            builder.WebHost.UseTestServer();
            builder.Services.AddInventoryDataProtection(new ConfigurationBuilder().AddInMemoryCollection(Settings(certificate, previous)).Build(), false, true);
            // Real EF Data Protection repository; replace only the database transport for disposable tests.
            builder.Services.RemoveAll<DbContextOptions<ProtectionDbContext>>();
            builder.Services.RemoveAll<IDbContextOptionsConfiguration<ProtectionDbContext>>();
            builder.Services.AddDbContext<ProtectionDbContext>(options => options.UseSqlite(KeyConnection,
                sqlite => sqlite.MigrationsHistoryTable("__DataProtectionMigrationsHistory")));
            builder.Services.AddDbContext<AccessDbContext>(options => options.UseSqlite($"Data Source={Path.Combine(directory, "identity.sqlite")};Pooling=False"));
            builder.Services.AddIdentity<AccessUser, IdentityRole<Guid>>().AddEntityFrameworkStores<AccessDbContext>().AddDefaultTokenProviders();
            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/acceso";
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.ExpireTimeSpan = TimeSpan.FromHours(8);
            });
            // Exercise revocation immediately instead of waiting the production five-minute validation interval.
            builder.Services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);
            builder.Services.AddAuthorization(InventoryPolicies.Configure);
            builder.Services.AddAntiforgery();
            builder.Services.AddRateLimiter(options => options.AddFixedWindowLimiter("login", limiter =>
            {
                limiter.PermitLimit = 100;
                limiter.Window = TimeSpan.FromMinutes(1);
            }));
            var app = builder.Build();
            try
            {
                if (initialize)
                {
                    using var scope = app.Services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ProtectionDbContext>();
                    await db.Database.EnsureCreatedAsync();
                    var history = db.GetService<IHistoryRepository>();
                    await db.Database.ExecuteSqlRawAsync(history.GetCreateIfNotExistsScript());
                    if (!(await history.GetAppliedMigrationsAsync()).Any())
                        foreach (var migration in db.Database.GetMigrations())
                            await db.Database.ExecuteSqlRawAsync(history.GetInsertScript(new HistoryRow(migration, "10.0.9")));
                    var identity = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
                    await identity.Database.EnsureCreatedAsync();
                    var users = scope.ServiceProvider.GetRequiredService<UserManager<AccessUser>>();
                    if (await users.FindByNameAsync("test-admin") is null)
                        Assert.True((await users.CreateAsync(new AccessUser { UserName = "test-admin", Email = "test@example.invalid" }, "Temporary-Only!Pass928")).Succeeded);
                }
                app.UseAuthentication();
                app.UseRateLimiter();
                app.UseAuthorization();
                app.UseAntiforgery();
                SecurityEndpoints.Map(app); // Actual application login/logout endpoints, not simulated handlers.
                app.MapGet("/private", () => "authenticated").RequireAuthorization();
                app.MapGet("/token", (HttpContext context, IAntiforgery antiforgery) => antiforgery.GetAndStoreTokens(context).RequestToken!).RequireAuthorization();
                await app.StartAsync();
                return app;
            }
            catch { await app.DisposeAsync(); throw; }
        }

        public void Dispose() => Directory.Delete(directory, recursive: true);
    }
}
