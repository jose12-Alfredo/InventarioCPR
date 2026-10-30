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

    private PhotoEvidence() { }

    public PhotoEvidence(Guid equipmentId, string cloudinaryPublicId, string evidenceType, DateTimeOffset uploadedAtUtc)
    {
        Id = Guid.NewGuid();
        EquipmentId = equipmentId;
        CloudinaryPublicId = cloudinaryPublicId;
        EvidenceType = evidenceType;
        UploadedAtUtc = uploadedAtUtc;
    }
}
