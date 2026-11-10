using System.Security.Claims;
using System.Security.Cryptography;
using InventarioCPR.Infrastructure.Persistence;
using InventarioCPR.Infrastructure.Persistence.Models;
using InventarioCPR.Infrastructure.Photos;
using Microsoft.EntityFrameworkCore;

namespace InventarioCPR.Application.Photos;

public sealed class EquipmentPhotoService(IDbContextFactory<InventoryDbContext> contexts, IPhotoAccess access,
    IPhotoStorage storage, IConfiguration configuration, PhotoWorkGate work)
{
    public bool Enabled => configuration.GetValue<bool>("Inventory:PhotosEnabled") && configuration.GetValue("Security:EnableIdentity", true);

    private async Task ReadyAsync(InventoryDbContext db, CancellationToken ct)
    {
        if (!Enabled) throw new PhotoException("Fotografías pendientes de autorización de la migración.", 503);
        if ((await db.Database.GetPendingMigrationsAsync(ct)).Any())
            throw new PhotoException("La migración de fotografías está pendiente de revisión y aplicación.", 503);
    }

    private static async Task EquipmentAsync(InventoryDbContext db, Guid equipment, CancellationToken ct)
    {
        if (!await db.Equipment.AnyAsync(x => x.Id == equipment, ct)) throw new PhotoException("Equipo no encontrado.", 404);
    }

    public async Task<PhotoPage> ListAsync(ClaimsPrincipal user, Guid equipment, int page, CancellationToken ct)
    {
        var actor = await access.AuthorizeAsync(user, false);
        if (!Enabled) return new PhotoPage(false, false, "Fotografías preparadas; habilitación pendiente de la migración aprobada.", [], [], [], 0, false);
        await using var db = await contexts.CreateDbContextAsync(ct);
        await ReadyAsync(db, ct);
        await EquipmentAsync(db, equipment, ct);
        page = Math.Clamp(page, 0, 100000);
        var principal = await db.PhotoPrincipalPeriods.Where(x => x.EquipmentId == equipment && x.ToUtc == null).Select(x => (Guid?)x.PhotoId).SingleOrDefaultAsync(ct);
        var photos = await db.PhotoEvidence.AsNoTracking().Where(x => x.EquipmentId == equipment)
            .OrderByDescending(x => x.UploadedAtUtc).ThenByDescending(x => x.Id).Skip(page * 24).Take(25).ToListAsync(ct);
        var history = await db.PhotoPrincipalPeriods.AsNoTracking().Where(x => x.EquipmentId == equipment)
            .OrderByDescending(x => x.FromUtc).ThenByDescending(x => x.Id).Skip(page * 24).Take(25).ToListAsync(ct);
        var pending = actor.CanWrite ? await db.PhotoUploads.AsNoTracking()
            .Where(x => x.EquipmentId == equipment && x.ActorId == actor.Id && x.CompletedAtUtc == null)
            .OrderByDescending(x => x.CreatedAtUtc).Take(50).ToListAsync(ct) : [];
        return new PhotoPage(true, actor.CanWrite, null,
            photos.Take(24).Select(x => new PhotoItem(x.Id, x.EvidenceType, x.Observation, x.UploadedAtUtc,
                x.UploadedByName, x.Id == principal, PhotoRules.ContentUrl(equipment, x.Id, true), PhotoRules.ContentUrl(equipment, x.Id))).ToList(),
            history.Take(24).Select(x => new PhotoPeriodItem(x.PhotoId, x.FromUtc, x.ToUtc, x.ActorName)).ToList(),
            pending.Select(x => new PhotoPendingItem(x.Id, x.FileName, x.EvidenceType, x.Note, x.Source)).ToList(),
            page, photos.Count > 24 || history.Count > 24);
    }

    public async Task<IReadOnlyList<PhotoPendingItem>> ReserveAsync(ClaimsPrincipal user, Guid equipment, PhotoBatchRequest request, CancellationToken ct)
    {
        var actor = await access.AuthorizeAsync(user, true);
        if (request.BatchId == Guid.Empty || request.Files is null || request.Files.Count is < 1 or > PhotoRules.MaxFiles)
            throw new PhotoException("Seleccione entre 1 y 10 fotografías por lote.", 400);
        foreach (var file in request.Files) PhotoRules.Validate(file);
        await using var db = await contexts.CreateDbContextAsync(ct);
        await ReadyAsync(db, ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await EquipmentAsync(db, equipment, ct);
        await LockEquipmentAsync(db, equipment, ct);
        var uploads = await db.PhotoUploads.Where(x => x.EquipmentId == equipment && x.ActorId == actor.Id && x.BatchId == request.BatchId)
            .OrderBy(x => x.Position).ToListAsync(ct);
        if (uploads.Count != 0)
        {
            if (uploads.Count != request.Files.Count || uploads.Where((x, i) =>
                x.FileName != request.Files[i].FileName || x.EvidenceType != request.Files[i].Type || x.Note != request.Files[i].Note || x.Source != request.Files[i].Source).Any())
                throw new PhotoException("El lote ya existe con otros metadatos. No se modificará.");
        }
        else
        {
            if (await db.PhotoUploads.CountAsync(x => x.EquipmentId == equipment && x.ActorId == actor.Id && x.CompletedAtUtc == null, ct) + request.Files.Count > 50)
                throw new PhotoException("Hay 50 reservas pendientes en este equipo. Recupere las anteriores antes de crear otro lote.");
            uploads = request.Files.Select((x, i) => new PhotoUpload
            {
                Id = Guid.NewGuid(), EquipmentId = equipment, BatchId = request.BatchId, Position = i,
                ActorId = actor.Id, ActorName = actor.Name, FileName = x.FileName, EvidenceType = x.Type,
                Note = x.Note, Source = x.Source, CreatedAtUtc = DateTimeOffset.UtcNow
            }).ToList();
            db.PhotoUploads.AddRange(uploads);
            await db.SaveChangesAsync(ct);
        }
        await transaction.CommitAsync(ct);
        return uploads.Select(x => new PhotoPendingItem(x.Id, x.FileName, x.EvidenceType, x.Note, x.Source)).ToList();
    }

    public async Task<PhotoUploadResult> UploadAsync(ClaimsPrincipal user, Guid equipment, Guid uploadId, Stream content, CancellationToken ct)
    {
        var actor = await access.AuthorizeAsync(user, true);
        using var workLease = await work.EnterAsync(ct);
        await using var db = await contexts.CreateDbContextAsync(ct);
        await ReadyAsync(db, ct);
        var upload = await db.PhotoUploads.SingleOrDefaultAsync(x => x.Id == uploadId && x.EquipmentId == equipment && x.ActorId == actor.Id, ct)
            ?? throw new PhotoException("Reserva no encontrada para este expediente y usuario.", 404);
        // Check content even on an idempotent retry: never accept a different file for the same reservation.
        var prepared = await PhotoImageProcessor.PrepareAsync(content, ct);
        await using (var preparation = await db.Database.BeginTransactionAsync(ct))
        {
            await LockEquipmentAsync(db, equipment, ct);
            await db.Entry(upload).ReloadAsync(ct);
            if (upload.Sha256 is not null && upload.Sha256 != prepared.Sha256)
                throw new PhotoException("Seleccione el mismo archivo para recuperar esta reserva; los bytes no se reemplazan.");
            if (upload.EvidenceId is Guid completed)
                return new PhotoUploadResult(completed, true, await db.PhotoEvidence.Where(x => x.Id == completed).Select(x => x.EvidenceType).SingleAsync(ct));
            upload.Sha256 = prepared.Sha256;
            upload.Format = prepared.Format;
            upload.Width = prepared.Width;
            upload.Height = prepared.Height;
            upload.ByteLength = prepared.Bytes.Length;
            await db.SaveChangesAsync(ct);
            await preparation.CommitAsync(ct);
        }

        // Same-equipment serialization also protects duplicate detection and primary changes.
        // The committed reservation survives cloud timeout, process termination and a failed final DB commit.
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await LockEquipmentAsync(db, equipment, ct);
        await db.Entry(upload).ReloadAsync(ct);
        if (upload.EvidenceId is Guid previous)
            return new PhotoUploadResult(previous, true, await db.PhotoEvidence.Where(x => x.Id == previous).Select(x => x.EvidenceType).SingleAsync(ct));
        var duplicate = await db.PhotoEvidence.SingleOrDefaultAsync(x => x.EquipmentId == equipment && x.Sha256 == prepared.Sha256, ct);
        if (duplicate is not null)
        {
            upload.EvidenceId = duplicate.Id;
            upload.CompletedAtUtc = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return new PhotoUploadResult(duplicate.Id, true, duplicate.EvidenceType);
        }
        using var cloudTimeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cloudTimeout.CancelAfter(TimeSpan.FromSeconds(45));
        var stored = await storage.StoreAsync(prepared, upload.PublicId, cloudTimeout.Token);
        if (stored.PublicId != upload.PublicId || stored.Version <= 0) throw new PhotoException("No se confirmó la identidad del archivo privado.", 502);
        var evidence = PhotoEvidence.FromUpload(upload, stored.Version, DateTimeOffset.UtcNow);
        db.PhotoEvidence.Add(evidence);
        await db.SaveChangesAsync(ct);
        upload.CompletedAtUtc = DateTimeOffset.UtcNow;
        upload.EvidenceId = evidence.Id;
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new PhotoUploadResult(evidence.Id, false, evidence.EvidenceType);
    }

    public async Task SetPrincipalAsync(ClaimsPrincipal user, Guid equipment, Guid photo, CancellationToken ct)
    {
        var actor = await access.AuthorizeAsync(user, true);
        await using var db = await contexts.CreateDbContextAsync(ct);
        await ReadyAsync(db, ct);
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        await EquipmentAsync(db, equipment, ct);
        await LockEquipmentAsync(db, equipment, ct);
        if (!await db.PhotoEvidence.AnyAsync(x => x.Id == photo && x.EquipmentId == equipment, ct))
            throw new PhotoException("La fotografía no pertenece al expediente.", 404);
        var current = await db.PhotoPrincipalPeriods.SingleOrDefaultAsync(x => x.EquipmentId == equipment && x.ToUtc == null, ct);
        if (current?.PhotoId == photo) return;
        var now = DateTimeOffset.UtcNow;
        if (current is not null)
        {
            if (now <= current.FromUtc) now = current.FromUtc.AddTicks(10);
            current.ToUtc = now;
            await db.SaveChangesAsync(ct); // Close previous period before inserting new filtered-unique row.
        }
        db.PhotoPrincipalPeriods.Add(new PhotoPrincipalPeriod
        {
            Id = Guid.NewGuid(), EquipmentId = equipment, PhotoId = photo,
            FromUtc = now, ActorId = actor.Id, ActorName = actor.Name
        });
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<(byte[] Bytes, string ContentType)> ReadAsync(ClaimsPrincipal user, Guid equipment, Guid photo, bool thumbnail, CancellationToken ct)
    {
        await access.AuthorizeAsync(user, false);
        using var workLease = await work.EnterAsync(ct);
        await using var db = await contexts.CreateDbContextAsync(ct);
        await ReadyAsync(db, ct);
        var evidence = await db.PhotoEvidence.AsNoTracking().SingleOrDefaultAsync(x => x.Id == photo && x.EquipmentId == equipment, ct)
            ?? throw new PhotoException("Fotografía no encontrada en este expediente.", 404);
        var bytes = await storage.ReadAsync(evidence.CloudinaryPublicId, evidence.CloudinaryVersion, evidence.Format, ct);
        if (evidence.Sha256 is not null && !CryptographicOperations.FixedTimeEquals(SHA256.HashData(bytes), Convert.FromHexString(evidence.Sha256)))
            throw new PhotoException("La integridad de la imagen no coincide. No se entregará el archivo.", 502);
        if (thumbnail) return (PhotoImageProcessor.Thumbnail(bytes), "image/jpeg");
        return (bytes, PhotoImageProcessor.ValidateStored(bytes));
    }

    private static Task<int> LockEquipmentAsync(InventoryDbContext db, Guid id, CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM equipment WHERE \"Id\" = {id} FOR UPDATE", ct);
}
