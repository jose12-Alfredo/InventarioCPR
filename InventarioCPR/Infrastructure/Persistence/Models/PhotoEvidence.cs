namespace InventarioCPR.Infrastructure.Persistence.Models;

public sealed class PhotoEvidence
{
    public Guid Id { get; private set; }
    public Guid EquipmentId { get; private set; }
    public Equipment Equipment { get; private set; } = null!;
    public Guid? InspectionId { get; private set; }
    public EquipmentInspection? Inspection { get; private set; }
    public string CloudinaryPublicId { get; private set; } = null!;
    public string EvidenceType { get; private set; } = null!;
    public DateTimeOffset UploadedAtUtc { get; private set; }
    // Nullable for legacy records: never invent an author, capture time or integrity metadata.
    public Guid? UploadedById { get; private set; }
    public string? UploadedByName { get; private set; }
    public string? Observation { get; private set; }
    public string? Source { get; private set; }
    public string? Sha256 { get; private set; }
    public string? Format { get; private set; }
    public int? Width { get; private set; }
    public int? Height { get; private set; }
    public long? ByteLength { get; private set; }
    public long? CloudinaryVersion { get; private set; }

    private PhotoEvidence() { }

    public PhotoEvidence(Guid equipmentId, string cloudinaryPublicId, string evidenceType, DateTimeOffset uploadedAtUtc)
    {
        Id = Guid.NewGuid();
        EquipmentId = equipmentId;
        CloudinaryPublicId = cloudinaryPublicId;
        EvidenceType = evidenceType;
        UploadedAtUtc = uploadedAtUtc;
    }

    public static PhotoEvidence FromUpload(PhotoUpload upload, long version, DateTimeOffset now)
    {
        return new PhotoEvidence(upload.EquipmentId, upload.PublicId, upload.EvidenceType, now)
        {
            UploadedById = upload.ActorId, UploadedByName = upload.ActorName,
            Observation = upload.Note, Source = upload.Source, Sha256 = upload.Sha256,
            Format = upload.Format, Width = upload.Width, Height = upload.Height,
            ByteLength = upload.ByteLength, CloudinaryVersion = version
        };
    }
}
