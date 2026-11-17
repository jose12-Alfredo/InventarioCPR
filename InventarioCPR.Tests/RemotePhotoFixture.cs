using InventarioCPR.Application.Inventory;
using InventarioCPR.Application.Photos;
using InventarioCPR.Application.Security;
using InventarioCPR.Infrastructure.Persistence;
using InventarioCPR.Infrastructure.Photos;
using InventarioCPR.Infrastructure.Security;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using SkiaSharp;
using System.Security.Claims;

namespace InventarioCPR.Tests;

// Called only by explicitly opted-in persistent Neon tests. Never exempts a new record from its photo.
internal static class RemotePhotoFixture
{
    internal static IConfiguration PhotoConfiguration(IConfiguration original) => new ConfigurationBuilder().AddConfiguration(original)
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Inventory:PhotosEnabled"] = "true", ["Security:EnableIdentity"] = "true",
            ["Cloudinary:TestAssetFolder"] = "inventariocpr-pruebas/asignaciones-20261117"
        }).Build();

    internal static InventoryService CreateService(IDbContextFactory<InventoryDbContext> factory, IConfiguration original, IInventoryAccess access)
    {
        var config = PhotoConfiguration(original);
        return new InventoryService(factory, config, access, new CloudinaryPhotoStorage(config, new FixtureHttpClients()));
    }

    internal static async Task AssertTargetAsync(string connection)
    {
        var target = new NpgsqlConnectionStringBuilder(connection);
        if (target.Host != "ep-raspy-dream-b476uukh-pooler.c-6.us-east-2.aws.neon.tech" || target.Database != "neondb" || target.SslMode != SslMode.VerifyFull)
            throw new InvalidOperationException("El destino no es la rama autorizada con TLS estricto.");
        await using var pg = new NpgsqlConnection(connection);
        await pg.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT current_setting('neon.branch_id',true),current_setting('neon.endpoint_id',true)", pg);
        await using var reader = await command.ExecuteReaderAsync();
        await reader.ReadAsync();
        if (reader.GetString(0) != "br-flat-smoke-b4kdmosq" || reader.GetString(1) != "ep-raspy-dream-b476uukh")
            throw new InvalidOperationException("La identidad remota no coincide con la autorización.");
    }

    internal static async Task FinalizeAsync(IConfiguration original, Guid id)
    {
        var connection = NeonConnection.DevelopmentApp(original);
        await AssertTargetAsync(connection);
        var config = PhotoConfiguration(original);
        var services = new ServiceCollection().AddLogging();
        services.AddHttpContextAccessor();
        services.AddDbContext<AccessDbContext>(o => o.UseNpgsql(connection, n => n.MigrationsHistoryTable("__AuthMigrationsHistory")));
        services.AddIdentity<AccessUser, IdentityRole<Guid>>().AddEntityFrameworkStores<AccessDbContext>().AddDefaultTokenProviders();
        services.AddAuthorization(InventoryPolicies.Configure);
        services.AddHttpClient("private-photos", c => c.Timeout = TimeSpan.FromSeconds(45)).RemoveAllLoggers()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var auth = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
        var user = await auth.Users.SingleAsync(x => x.IsActive);
        var principal = await scope.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<AccessUser>>().CreateAsync(user);
        var access = new IdentityInventoryAccess(new FixedAuthentication(principal),
            scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Authorization.IAuthorizationService>(),
            scope.ServiceProvider.GetRequiredService<UserManager<AccessUser>>(), scope.ServiceProvider.GetRequiredService<SignInManager<AccessUser>>());
        var photoAccess = new PhotoAccess(scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Authorization.IAuthorizationService>(),
            scope.ServiceProvider.GetRequiredService<SignInManager<AccessUser>>(), scope.ServiceProvider.GetRequiredService<UserManager<AccessUser>>(), config);
        var factory = new PooledDbContextFactory<InventoryDbContext>(new DbContextOptionsBuilder<InventoryDbContext>().UseNpgsql(connection).Options);
        await using (var db = await factory.CreateDbContextAsync())
        {
            var equipment = await db.Equipment.AsNoTracking().SingleAsync(x => x.Id == id);
            if (!(equipment.Code.StartsWith("PRUEBA_") || equipment.Code.StartsWith("REG_SIMPLE_META_") || equipment.Code.StartsWith("VALIDA_ASIG_")))
                throw new InvalidOperationException("Solo se permite finalizar el fixture ficticio de esta validación.");
        }
        var storage = new CloudinaryPhotoStorage(config, provider.GetRequiredService<IHttpClientFactory>());
        var photos = new EquipmentPhotoService(factory, photoAccess, storage, config, new PhotoWorkGate());
        var upload = (await photos.ReserveAsync(principal, id, new(Guid.NewGuid(), [new("FIXTURE_ASIGNACIONES.png", "General", "Imagen ficticia de validación autorizada.", "Archivo")]), default)).Single();
        using var bitmap = new SKBitmap(400, 240);
        using (var canvas = new SKCanvas(bitmap)) canvas.Clear(SKColors.SlateBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = new MemoryStream(data.ToArray());
        await photos.UploadAsync(principal, id, upload.Id, stream, default);
        var inventory = new InventoryService(factory, config, access, storage);
        var ready = await inventory.GetRegistrationReadinessAsync(id);
        if (!ready.CanFinalize) throw new InvalidOperationException("El fixture no cumple los requisitos de finalización.");
        // A new authenticated CDN asset may not yet be readable. Retry the same finalized request,
        // retaining the photo and draft IDs; every attempt still verifies the real bytes and hash.
        for (var attempt = 0; ; attempt++)
        {
            try { await inventory.FinalizeRegistrationAsync(id, ready.Revision); break; }
            catch (InventoryValidationException error) when (attempt < 3 && error.Message.StartsWith("No se pudo confirmar la fotografía principal"))
            { await Task.Delay(TimeSpan.FromSeconds(2 * (attempt + 1))); }
        }
    }

    private sealed class FixedAuthentication(ClaimsPrincipal principal) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(principal));
    }

    private sealed class FixtureHttpClients : IHttpClientFactory
    {
        private static readonly HttpClient Client = new(new HttpClientHandler { AllowAutoRedirect = false }) { Timeout = TimeSpan.FromSeconds(45) };
        public HttpClient CreateClient(string name) => Client;
    }
}
