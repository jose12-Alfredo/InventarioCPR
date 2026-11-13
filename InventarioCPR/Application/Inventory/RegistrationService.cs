using System.Security.Cryptography;
using System.Text.Json;
using InventarioCPR.Application.Photos;
using InventarioCPR.Infrastructure.Persistence;
using InventarioCPR.Infrastructure.Persistence.Models;
using InventarioCPR.Infrastructure.Photos;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace InventarioCPR.Application.Inventory;

public sealed record RegistrationReadiness(bool IsDraft, int Revision, Guid? PrincipalId, bool CanFinalize, string Message);

public sealed partial class InventoryService
{
    private static EquipmentInput ReadDraft(Equipment equipment) => equipment.DraftPayload is { } json
        ? JsonSerializer.Deserialize<EquipmentInput>(json) ?? new()
        : new EquipmentInput { Code = equipment.Code, Name = equipment.Name, Category = "other" };

    private static async Task<EquipmentCategory> RegistrationCategoryAsync(InventoryDbContext db, string key, CancellationToken ct)
    {
        var name = Categories.GetValueOrDefault(key) ?? Categories["other"];
        var category = db.Categories.Local.FirstOrDefault(x => x.Name == name) ??
            await db.Categories.SingleOrDefaultAsync(x => x.Name == name, ct);
        if (category is not null) return category;
        category = new EquipmentCategory { Id = Guid.NewGuid(), Name = name };
        db.Categories.Add(category);
        return category;
    }

    public async Task<Guid> SaveDraftAsync(EquipmentInput input, Guid draftId, int expectedRevision, CancellationToken ct = default)
    {
        await access.EnsureWriteAsync();
        if (!CanWrite) throw new InventoryValidationException("Las escrituras de inventario no están habilitadas.");
        if (draftId == Guid.Empty) throw new InventoryValidationException("El borrador necesita un identificador estable.");
        // A draft can be incomplete or inconsistent; it cannot be unbounded. No inspection/verification facts
        // are emitted until strict validation succeeds at finalization.
        var json = JsonSerializer.Serialize(input);
        if (json.Length > 65536 || input.Code.Length > 80 || input.Name.Length > 200 || input.Category.Length > 40 ||
            input.RamModules.Count > 32 || input.StorageUnits.Count > 32)
            throw new InventoryValidationException("El borrador supera el tamaño permitido. Revise los campos y componentes.");
        await using var db = await contexts.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        try
        {
            // Also serialize a repeated creation request whose equipment row does not yet exist.
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({draftId.ToString()}, 0))", ct);
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM equipment WHERE \"Id\" = {draftId} FOR UPDATE", ct);
            var equipment = await db.Equipment.SingleOrDefaultAsync(x => x.Id == draftId, ct);
            if (equipment is not null)
            {
                if (equipment.RegistrationState != EquipmentRegistrationState.Draft)
                    throw new InventoryValidationException("El registro ya es definitivo. Abra su expediente para editarlo.");
                if (equipment.DraftPayload == json) return equipment.Id; // Lost-response retry, including stale revision.
                if (equipment.RegistrationRevision != expectedRevision)
                    throw new InventoryValidationException("El borrador cambió en otra sesión. Vuelva a abrirlo para conservar sus cambios.");
            }
            else
            {
                if (expectedRevision != 0) throw new InventoryValidationException("No se encontró el borrador que intentaba retomar.");
                equipment = new Equipment { Id = draftId };
                db.Equipment.Add(equipment);
            }
            equipment.Code = Clean(input.Code) ?? $"~BORRADOR-{draftId:N}";
            equipment.Name = Clean(input.Name) ?? "Borrador sin nombre";
            equipment.Category = await RegistrationCategoryAsync(db, input.Category, ct);
            equipment.DraftPayload = json;
            equipment.RegistrationRevision++;
            equipment.RegistrationUpdatedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return equipment.Id;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new InventoryValidationException("Ya existe ese código en un equipo o borrador. Retome el expediente existente.");
        }
    }

    public async Task<RegistrationReadiness> GetRegistrationReadinessAsync(Guid id, CancellationToken ct = default)
    {
        await access.EnsureReadAsync();
        await using var db = await contexts.CreateDbContextAsync(ct);
        var equipment = await db.Equipment.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InventoryValidationException("El equipo ya no existe.");
        if (equipment.RegistrationState == EquipmentRegistrationState.Definitive)
            return new(false, equipment.RegistrationRevision, null, false, "Registro definitivo.");
        var principal = await ConfirmedPrincipalAsync(db, id, ct);
        var message = "Falta una fotografía principal confirmada. Suba una imagen o recupere la subida pendiente.";
        var valid = false;
        try
        {
            var input = equipment.DraftPayload is null ? (await GetAsync(id, ct))!.Input : ReadDraft(equipment);
            ValidateDraftForFinalization(input);
            valid = true;
        }
        catch (InventoryValidationException error) { message = error.Message; }
        var ready = valid && principal is not null && PhotosAvailable;
        if (!PhotosAvailable) message = "Fotografías no disponibles en este entorno. Puede conservar y retomar el borrador.";
        return new(true, equipment.RegistrationRevision, principal?.Id, ready,
            ready ? "Fotografía principal confirmada. Puede finalizar el registro." : message);
    }

    public async Task<Guid> FinalizeRegistrationAsync(Guid id, int expectedRevision, CancellationToken ct = default)
    {
        await access.EnsureWriteAsync();
        if (!CanWrite) throw new InventoryValidationException("Las escrituras de inventario no están habilitadas.");
        await using var db = await contexts.CreateDbContextAsync(ct);
        var equipment = await db.Equipment.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InventoryValidationException("El borrador ya no existe.");
        if (equipment.RegistrationState == EquipmentRegistrationState.Definitive) return id;
        if (equipment.RegistrationRevision != expectedRevision)
            throw new InventoryValidationException("El borrador cambió en otra sesión. Vuelva a abrirlo antes de finalizar.");
        // An older deployed binary can save after the schema upgrade without a JSON payload.
        // Rehydrate its existing facts instead of inventing a category or dropping technical data.
        var input = equipment.DraftPayload is null ? (await GetAsync(id, ct))!.Input : ReadDraft(equipment);
        ValidateDraftForFinalization(input);
        input.UpdateTechnicalData = true;
        input.ExpectedTechnicalRevision ??= 0;
        return await SaveDefinitiveAsync(input, id, true, expectedRevision, ct);
    }

    private bool PhotosAvailable => configuration.GetValue<bool>("Inventory:PhotosEnabled") &&
        configuration.GetValue("Security:EnableIdentity", true);

    private static void ValidateDraftForFinalization(EquipmentInput input)
    {
        Validate(input);
        if ((Clean(input.PhysicalCondition) is not null || Clean(input.OperationalCondition) is not null) && Clean(input.Inspector) is null)
            throw new InventoryValidationException("Indique la persona que inspeccionó las condiciones antes de finalizar.");
    }

    internal static Task<PhotoEvidence?> ConfirmedPrincipalAsync(InventoryDbContext db, Guid id, CancellationToken ct) =>
        (from period in db.PhotoPrincipalPeriods
         join photo in db.PhotoEvidence on period.PhotoId equals photo.Id
         where period.EquipmentId == id && period.ToUtc == null && photo.EquipmentId == id &&
            photo.CloudinaryVersion > 0 && photo.Sha256 != null && photo.Width > 0 && photo.Height > 0 &&
            photo.ByteLength > 0 && (photo.Format == "jpg" || photo.Format == "png") &&
            db.PhotoUploads.Any(upload => upload.EquipmentId == id && upload.EvidenceId == photo.Id &&
                upload.CompletedAtUtc != null && upload.Sha256 == photo.Sha256)
         select photo).AsNoTracking().SingleOrDefaultAsync(ct);

    private async Task ConfirmRegistrationPhotoAsync(InventoryDbContext db, Guid id, CancellationToken ct)
    {
        if (!PhotosAvailable || photoStorage is null)
            throw new InventoryValidationException("No se puede finalizar: fotografías no disponibles en este entorno.");
        var principal = await ConfirmedPrincipalAsync(db, id, ct)
            ?? throw new InventoryValidationException("Para finalizar se exige una fotografía principal real y confirmada de este equipo.");
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(45));
            var bytes = await photoStorage.ReadAsync(principal.CloudinaryPublicId, principal.CloudinaryVersion, principal.Format, timeout.Token);
            if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(bytes), Convert.FromHexString(principal.Sha256!)))
                throw new PhotoException("La integridad de la imagen principal no coincide.");
            _ = PhotoImageProcessor.ValidateStored(bytes);
        }
        catch (Exception error) when (error is PhotoException or HttpRequestException or OperationCanceledException)
        {
            throw new InventoryValidationException("No se pudo confirmar la fotografía principal en Cloudinary. El borrador se conserva; reintente cuando la imagen esté disponible.");
        }
    }
}
