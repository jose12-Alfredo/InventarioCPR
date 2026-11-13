using System.Collections.Concurrent;
using System.Security.Claims;
using InventarioCPR.Application.Inventory;
using InventarioCPR.Application.Photos;
using InventarioCPR.Application.Security;
using InventarioCPR.Infrastructure.Persistence;
using InventarioCPR.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Npgsql;
using SkiaSharp;
using Xunit;

namespace InventarioCPR.Tests;

public sealed class RegistrationLocalPostgresFactAttribute : FactAttribute
{
    public RegistrationLocalPostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("INVENTARIOCPR_REGISTRATION_LOCAL_POSTGRES")))
            Skip = "Requiere PostgreSQL desechable loopback; no admite Neon ni datos existentes.";
    }
}

public sealed class MandatoryPhotoRegistrationTests
{
    [RegistrationLocalPostgresFact]
    public async Task CompleteDraftCreatesFactsOnlyOnceAndTechnicalEditsKeepRevisions()
    {
        await using var f = await Fixture.Create();
        var input = new EquipmentInput
        {
            Code = "COMPLETE_DRAFT", Category = "laptop", Name = "Laptop ficticia",
            ProcessorName = "CPU ficticia", TotalRamGb = 16, RamModulesComplete = true,
            RamModules = [new() { CapacityGb = 8 }, new() { CapacityGb = 8 }],
            StorageUnits = [new() { Technology = "SSD", Interface = "NVMe", Capacity = 512, Unit = "GB" }],
            Site = "Sede ficticia", Location = "Mesa ficticia", PhysicalCondition = "Inspección ficticia",
            Inspector = "Inspector ficticio", VerifyNow = true, Verifier = "Verificador ficticio", Observation = "Nota ficticia"
        };
        var inspector = input.Inspector;
        input.Inspector = null;
        var id = await f.Inventory.SaveAsync(input);
        using (var check = f.Db()) Assert.Empty(await check.Inspections.ToListAsync());
        await f.Upload(id);
        Assert.False((await f.Inventory.GetRegistrationReadinessAsync(id)).CanFinalize);
        input.Inspector = inspector;
        await f.Inventory.SaveDraftAsync(input, id, 1);
        await f.Inventory.FinalizeRegistrationAsync(id, 2);
        await f.Inventory.FinalizeRegistrationAsync(id, 2);
        using (var check = f.Db())
        {
            Assert.Equal(1, await check.Inspections.CountAsync());
            Assert.Equal(1, await check.TechnicalVerifications.CountAsync());
            Assert.Equal(1, await check.Observations.CountAsync());
            Assert.Equal(2, await check.RamModules.CountAsync());
        }
        var edit = (await f.Inventory.GetAsync(id))!.Input;
        edit.RamModules.ForEach(module => module.CapacityGb = 16); edit.TotalRamGb = 32;
        await f.Inventory.SaveAsync(edit, id);
        using var final = f.Db();
        Assert.Equal(2, await final.TechnicalConfigurations.CountAsync());
        var historical = new PhotoEvidence(id, "unconfirmed-historical", "General", DateTimeOffset.UtcNow);
        final.PhotoEvidence.Add(historical); await final.SaveChangesAsync();
        await Assert.ThrowsAsync<PhotoException>(() => f.Photos.SetPrincipalAsync(f.User, id, historical.Id, default));
        Assert.Equal(1, await final.PhotoPrincipalPeriods.CountAsync(x => x.ToUtc == null));
        var evidence = await final.PhotoEvidence.FirstAsync(); final.PhotoEvidence.Remove(evidence);
        await Assert.ThrowsAsync<InvalidOperationException>(() => final.SaveChangesAsync());
    }

    [RegistrationLocalPostgresFact]
    public async Task IncompleteDraftIsResumableExcludedAndIdempotent()
    {
        await using var f = await Fixture.Create();
        var id = Guid.NewGuid();
        var input = new EquipmentInput { Observation = "Nota incompleta", Site = "Sede sin ubicación", RamModules = [new() { CapacityGb = null }] };
        await f.Inventory.SaveDraftAsync(input, id, 0);
        await f.Inventory.SaveDraftAsync(input, id, 0); // lost response retry
        var saved = (await f.Inventory.GetAsync(id))!;
        Assert.True(saved.IsDraft);
        Assert.Equal(1, saved.RegistrationRevision);
        Assert.Empty(saved.Input.Code);
        Assert.Equal("Sede sin ubicación", saved.Input.Site);
        Assert.Null(Assert.Single(saved.Input.RamModules).CapacityGb);
        Assert.Equal(0, (await f.Inventory.ListAsync()).Total);
        Assert.Equal(1, (await f.Inventory.ListAsync(draftsOnly: true)).Total);
        await Assert.ThrowsAsync<InventoryValidationException>(() => f.Inventory.FinalizeRegistrationAsync(id, 1));
        using var db = f.Db();
        Assert.Empty(await db.Observations.ToListAsync());
        Assert.Empty(await db.TechnicalConfigurations.ToListAsync());
    }

    [RegistrationLocalPostgresFact]
    public async Task NewSaveCannotBypassPhotoAndDuplicateCodesIncludeDrafts()
    {
        await using var f = await Fixture.Create();
        var id = await f.Inventory.SaveAsync(Input());
        Assert.True((await f.Inventory.GetAsync(id))!.IsDraft);
        await Assert.ThrowsAsync<InventoryValidationException>(() => f.Inventory.FinalizeRegistrationAsync(id, 1));
        await Assert.ThrowsAsync<InventoryValidationException>(() => f.Inventory.SaveDraftAsync(Input(), Guid.NewGuid(), 0));
        await Assert.ThrowsAsync<InventoryValidationException>(() => f.Inventory.SaveAsync(Input(), id));
        using var db = f.Db();
        Assert.Equal(1, await db.Equipment.CountAsync());
    }

    [RegistrationLocalPostgresFact]
    public async Task UploadFailureRetryMultiplePhotosAndFinalizationKeepOneExpedienteAndHistory()
    {
        await using var f = await Fixture.Create();
        var id = await f.Inventory.SaveAsync(Input());
        var request = new PhotoBatchRequest(Guid.NewGuid(), [new("a.png", "General", "Frente", "Archivo"), new("b.png", "Serial", "Detalle", "Camara")]);
        var reserved = await f.Photos.ReserveAsync(f.User, id, request, default);
        var repeated = await f.Photos.ReserveAsync(f.User, id, request, default);
        Assert.Equal(reserved.Select(x => x.Id), repeated.Select(x => x.Id));
        f.Storage.FailStore = true;
        await Assert.ThrowsAsync<PhotoException>(() => f.Photos.UploadAsync(f.User, id, reserved[0].Id, Image(SKColors.Red), default));
        Assert.False((await f.Inventory.GetRegistrationReadinessAsync(id)).CanFinalize);
        f.Storage.FailStore = false;
        var first = await f.Photos.UploadAsync(f.User, id, reserved[0].Id, Image(SKColors.Red), default);
        var retry = await f.Photos.UploadAsync(f.User, id, reserved[0].Id, Image(SKColors.Red), default);
        Assert.Equal(first.Id, retry.Id);
        var second = await f.Photos.UploadAsync(f.User, id, reserved[1].Id, Image(SKColors.Blue), default);
        Assert.Equal(first.Id, (await f.Inventory.GetRegistrationReadinessAsync(id)).PrincipalId);
        await f.Photos.SetPrincipalAsync(f.User, id, second.Id, default);
        Assert.True((await f.Inventory.GetRegistrationReadinessAsync(id)).CanFinalize);
        await f.Inventory.FinalizeRegistrationAsync(id, 1);
        await f.Inventory.FinalizeRegistrationAsync(id, 1); // finalization is idempotent
        Assert.False((await f.Inventory.GetAsync(id))!.IsDraft);
        Assert.Equal(1, (await f.Inventory.ListAsync()).Total);
        Assert.Equal(0, (await f.Inventory.ListAsync(draftsOnly: true)).Total);
        using var db = f.Db();
        Assert.Equal(1, await db.Equipment.CountAsync());
        Assert.Equal(2, await db.PhotoEvidence.CountAsync());
        Assert.Equal(2, await db.PhotoPrincipalPeriods.CountAsync());
        Assert.Equal(1, await db.PhotoPrincipalPeriods.CountAsync(x => x.ToUtc == null));
        Assert.NotNull((await db.Equipment.SingleAsync()).FinalizedAtUtc);
        Assert.Null((await db.Equipment.SingleAsync()).DraftPayload);
        await Assert.ThrowsAsync<InventoryValidationException>(() => f.Inventory.SaveDraftAsync(Input(), id, 2));
    }

    [RegistrationLocalPostgresFact]
    public async Task MissingCloudImageOrCorruptBytesCannotFinalizeAndDoNotLoseDraft()
    {
        await using var f = await Fixture.Create();
        var id = await f.Inventory.SaveAsync(Input());
        await f.Upload(id);
        f.Storage.FailRead = true;
        await Assert.ThrowsAsync<InventoryValidationException>(() => f.Inventory.FinalizeRegistrationAsync(id, 1));
        Assert.True((await f.Inventory.GetAsync(id))!.IsDraft);
        f.Storage.FailRead = false; f.Storage.CorruptRead = true;
        await Assert.ThrowsAsync<InventoryValidationException>(() => f.Inventory.FinalizeRegistrationAsync(id, 1));
        f.Storage.CorruptRead = false;
        await f.Inventory.FinalizeRegistrationAsync(id, 1);
    }

    [RegistrationLocalPostgresFact]
    public async Task StaleAndConcurrentRequestsCannotOverwriteOrDuplicateFinalization()
    {
        await using var f = await Fixture.Create();
        var id = Guid.NewGuid(); var input = Input();
        await Task.WhenAll(f.Inventory.SaveDraftAsync(input, id, 0), f.Inventory.SaveDraftAsync(input, id, 0));
        input.Name = "Nombre actualizado";
        await f.Inventory.SaveDraftAsync(input, id, 1);
        await Assert.ThrowsAsync<InventoryValidationException>(() => f.Inventory.SaveDraftAsync(Input(), id, 1));
        await f.Upload(id);
        await Assert.ThrowsAsync<InventoryValidationException>(() => f.Inventory.FinalizeRegistrationAsync(id, 1));
        await Task.WhenAll(f.Inventory.FinalizeRegistrationAsync(id, 2), f.Inventory.FinalizeRegistrationAsync(id, 2));
        using var db = f.Db();
        Assert.Equal(1, await db.Equipment.CountAsync());
        Assert.Equal(3, (await db.Equipment.SingleAsync()).RegistrationRevision);
    }

    [RegistrationLocalPostgresFact]
    public async Task ReservationAndUnconfirmedEvidenceDoNotCountAsPrincipal()
    {
        await using var f = await Fixture.Create();
        var id = await f.Inventory.SaveAsync(Input());
        var reservation = (await f.Photos.ReserveAsync(f.User, id, new(Guid.NewGuid(), [new("unconfirmed.png", "General", "", "Archivo")]), default)).Single();
        using (var db = f.Db())
        {
            var evidence = new PhotoEvidence(id, "historical-unconfirmed", "General", DateTimeOffset.UtcNow);
            db.PhotoEvidence.Add(evidence);
            db.PhotoPrincipalPeriods.Add(new PhotoPrincipalPeriod { Id = Guid.NewGuid(), EquipmentId = id, PhotoId = evidence.Id, FromUtc = DateTimeOffset.UtcNow, ActorId = f.Actor, ActorName = "test" });
            await db.SaveChangesAsync();
        }
        Assert.False((await f.Inventory.GetRegistrationReadinessAsync(id)).CanFinalize);
        await Assert.ThrowsAsync<InventoryValidationException>(() => f.Inventory.FinalizeRegistrationAsync(id, 1));
        var other = await f.Inventory.SaveDraftAsync(new() { Code = "SECOND", Category = "monitor", Name = "Otro" }, Guid.NewGuid(), 0);
        var otherPhoto = await f.Upload(other);
        await Assert.ThrowsAsync<PhotoException>(() => f.Photos.SetPrincipalAsync(f.User, id, otherPhoto, default));
    }

    [RegistrationLocalPostgresFact]
    public async Task PermissionsAndDisabledPhotosRemainEnforced()
    {
        await using var f = await Fixture.Create();
        var id = await f.Inventory.SaveAsync(Input());
        await f.Upload(id);
        f.Access.AllowWrite = false;
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Inventory.FinalizeRegistrationAsync(id, 1));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => f.Inventory.SaveDraftAsync(Input(), Guid.NewGuid(), 0));
        f.Access.AllowWrite = true;
        f.Config["Inventory:PhotosEnabled"] = "false";
        Assert.False((await f.Inventory.GetRegistrationReadinessAsync(id)).CanFinalize);
        await Assert.ThrowsAsync<InventoryValidationException>(() => f.Inventory.FinalizeRegistrationAsync(id, 1));
    }

    [RegistrationLocalPostgresFact]
    public async Task MigrationKeepsLegacyDefinitiveWithoutPhotosAndRollbackProtectsNewRecords()
    {
        await using var f = await Fixture.Create(migrate: false);
        var id = Guid.NewGuid();var category = Guid.NewGuid();
        using (var db = f.Db())
        {
            await db.GetService<IMigrator>().MigrateAsync("20261109161923_EquipmentPhotoHistory");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO equipment_categories (\"Id\",\"Name\") VALUES ({category}, 'Monitor')");
            await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO equipment (\"Id\",\"Code\",\"Name\",\"CategoryId\") VALUES ({id}, 'LEGACY_NO_PHOTO', 'Anterior sin foto', {category})");
            await db.Database.MigrateAsync();
            var old = await db.Equipment.SingleAsync();
            Assert.Equal(EquipmentRegistrationState.Definitive, old.RegistrationState);
            Assert.False(old.RequiresRegistrationPhoto);
            Assert.Null(old.FinalizedAtUtc);
        }
        Assert.Equal(1, (await f.Inventory.ListAsync()).Total);
        var input = (await f.Inventory.GetAsync(id))!.Input;
        input.Name = "Anterior conservado";
        await f.Inventory.SaveAsync(input, id); // No fake photo required for legacy.
        await f.Inventory.SaveAsync(Input());
        using var check = f.Db();
        await Assert.ThrowsAsync<PostgresException>(() => check.GetService<IMigrator>().MigrateAsync("20261109161923_EquipmentPhotoHistory"));
        Assert.Equal(2, await check.Equipment.CountAsync());
    }

    static EquipmentInput Input() => new() { Code = "PHOTO_REQUIRED", Category = "monitor", Name = "Monitor ficticio" };
    static MemoryStream Image(SKColor color)
    {
        using var bitmap = new SKBitmap(40, 30); bitmap.Erase(color);
        using var image = SKImage.FromBitmap(bitmap); using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return new MemoryStream(data.ToArray());
    }

    sealed class Access : IInventoryAccess
    {
        public bool AllowWrite = true;
        public Task<bool> CanReadAsync() => Task.FromResult(true);
        public Task<bool> CanWriteAsync() => Task.FromResult(AllowWrite);
        public Task EnsureReadAsync() => Task.CompletedTask;
        public Task EnsureWriteAsync() => AllowWrite ? Task.CompletedTask : throw new UnauthorizedAccessException();
    }
    sealed class PhotoAccess(Guid actor) : IPhotoAccess
    {
        public Task<PhotoActor> AuthorizeAsync(ClaimsPrincipal user, bool write) => Task.FromResult(new PhotoActor(actor, "Prueba local", true));
    }
    sealed class Storage : IPhotoStorage
    {
        public bool FailStore, FailRead, CorruptRead;
        readonly ConcurrentDictionary<string, byte[]> bytes = new();
        public Task<StoredPhoto> StoreAsync(PhotoContent content, string id, CancellationToken ct = default)
        {
            if (FailStore) throw new PhotoException("Fallo remoto simulado");
            bytes.TryAdd(id, content.Bytes);
            return Task.FromResult(new StoredPhoto(id, 1));
        }
        public Task<byte[]> ReadAsync(string id, long? version, string? format, CancellationToken ct = default)
        {
            if (FailRead) throw new PhotoException("Imagen remota no disponible");
            return Task.FromResult(CorruptRead ? new byte[] { 0, 1, 2 } : bytes[id]);
        }
    }
    sealed class Fixture : IAsyncDisposable
    {
        public Guid Actor = Guid.NewGuid();
        public ClaimsPrincipal User = new(new ClaimsIdentity("test"));
        public Access Access = new(); public Storage Storage = new();
        public IConfigurationRoot Config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?> { ["Inventory:ProductionWritesEnabled"] = "true", ["Inventory:PhotosEnabled"] = "true", ["Security:EnableIdentity"] = "true" }).Build();
        public InventoryService Inventory = null!; public EquipmentPhotoService Photos = null!;
        public string Connection = ""; string admin = "", database = "";
        public InventoryDbContext Db() => new(new DbContextOptionsBuilder<InventoryDbContext>().UseNpgsql(Connection).Options);
        public static async Task<Fixture> Create(bool migrate = true)
        {
            var f = new Fixture();
            var admin = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("INVENTARIOCPR_REGISTRATION_LOCAL_POSTGRES"));
            if (admin.Host is not ("127.0.0.1" or "localhost" or "::1") || admin.Database != "postgres") throw new InvalidOperationException("Solo PostgreSQL local desechable.");
            admin.Pooling = false;f.admin = admin.ConnectionString; f.database = "inventariocpr_registration_test_" + Guid.NewGuid().ToString("N");
            await using var c = new NpgsqlConnection(f.admin); await c.OpenAsync();
            await using var cmd = new NpgsqlCommand($"CREATE DATABASE \"{f.database}\"", c); await cmd.ExecuteNonQueryAsync();
            admin.Database = f.database; f.Connection = admin.ConnectionString;
            var factory = new PooledDbContextFactory<InventoryDbContext>(new DbContextOptionsBuilder<InventoryDbContext>().UseNpgsql(f.Connection).Options);
            f.Inventory = new(factory, f.Config, f.Access, f.Storage);
            f.Photos = new(factory, new PhotoAccess(f.Actor), f.Storage, f.Config, new PhotoWorkGate());
            if (migrate) { using var db = f.Db();await db.Database.MigrateAsync(); }
            return f;
        }
        public async Task<Guid> Upload(Guid id)
        {
            var reserve = (await Photos.ReserveAsync(User, id, new(Guid.NewGuid(), [new("real-bytes.png", "General", "", "Archivo")]), default)).Single();
            return (await Photos.UploadAsync(User, id, reserve.Id, Image(SKColors.Green), default)).Id;
        }
        public async ValueTask DisposeAsync()
        {
            await using var c = new NpgsqlConnection(admin);await c.OpenAsync();
            await using var cmd = new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)", c);await cmd.ExecuteNonQueryAsync();
        }
    }
}
