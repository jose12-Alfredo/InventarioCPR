namespace InventarioCPR.Infrastructure.Persistence.Models;

// A committed reservation precedes all Cloudinary writes. Its ID is also the immutable storage key.
public sealed class PhotoUpload
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public Guid BatchId { get; set; }
    public int Position { get; set; }
    public Guid ActorId { get; set; }
    public string ActorName { get; set; } = "";
    public string FileName { get; set; } = "";
    public string EvidenceType { get; set; } = "General";
    public string Note { get; set; } = "";
    public string Source { get; set; } = "Archivo";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? Sha256 { get; set; }
    public string? Format { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public long? ByteLength { get; set; }
    public Guid? EvidenceId { get; set; }
    public string PublicId => $"inventario/equipos/{EquipmentId:N}/{Id:N}";
}

public sealed class PhotoPrincipalPeriod
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public Guid PhotoId { get; set; }
    public DateTimeOffset FromUtc { get; set; }
    public DateTimeOffset? ToUtc { get; set; }
    public Guid ActorId { get; set; }
    public string ActorName { get; set; } = "";
}
