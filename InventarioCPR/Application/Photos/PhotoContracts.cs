using System.Security.Cryptography;

namespace InventarioCPR.Application.Photos;

public static class PhotoRules
{
    public const int MaxFiles = 10;
    public const int MaxBytes = 10 * 1024 * 1024;
    public const long MaxPixels = 25_000_000;
    // Event-specific evidence is intentionally deferred to stage B.
    public static readonly string[] EquipmentTypes = ["General", "Serial", "Accesorio"];
    public static string ContentUrl(Guid equipment, Guid photo, bool thumbnail = false) =>
        $"/api/equipment/{equipment}/photos/{photo}/content{(thumbnail ? "?size=thumb" : "")}";

    public static void Validate(PhotoDraft draft)
    {
        if (draft is null || !EquipmentTypes.Contains(draft.Type) || draft.Note is null || draft.Note.Length > 2000 ||
            draft.FileName is null || draft.FileName.Length is < 1 or > 200 || draft.Source is not ("Archivo" or "Camara"))
            throw new PhotoException("Revise el tipo, origen, nombre y observación de la fotografía.", 400);
    }
}

public sealed record PhotoDraft(string FileName, string Type, string Note, string Source);
public sealed record PhotoBatchRequest(Guid BatchId, IReadOnlyList<PhotoDraft> Files);
public sealed record PhotoItem(Guid Id, string Type, string? Note, DateTimeOffset UploadedAtUtc,
    string? Author, bool IsPrincipal, string ThumbnailUrl, string ContentUrl);
public sealed record PhotoPeriodItem(Guid PhotoId, DateTimeOffset FromUtc, DateTimeOffset? ToUtc, string Author);
public sealed record PhotoPendingItem(Guid Id, string FileName, string Type, string? Note, string Source);
public sealed record PhotoPage(bool Enabled, bool CanWrite, string? Message, IReadOnlyList<PhotoItem> Items,
    IReadOnlyList<PhotoPeriodItem> PrincipalHistory, IReadOnlyList<PhotoPendingItem> Pending, int Page, bool HasMore);
public sealed class PhotoException(string message, int status = 409) : Exception(message)
{
    public int Status { get; } = status;
}

public sealed record PhotoContent(byte[] Bytes, string Format, int Width, int Height)
{
    public string Sha256 => Convert.ToHexString(SHA256.HashData(Bytes));
    public string ContentType => Format == "png" ? "image/png" : "image/jpeg";
}

public sealed record StoredPhoto(string PublicId, long Version);
public sealed record PhotoUploadResult(Guid Id, bool AlreadyExists, string Type);
