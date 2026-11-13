namespace InventarioCPR.Infrastructure.Persistence.Models;

public sealed class Equipment
{
    public Guid Id { get; set; }
    // Preserve the source spelling (e.g. EQ_1); the database enforces uniqueness on CodeNormalized.
    public string Code { get; set; } = null!;
    public string CodeNormalized { get; private set; } = null!;
    public Guid CategoryId { get; set; }
    public EquipmentCategory Category { get; set; } = null!;
    public string Name { get; set; } = null!;
    public EquipmentRegistrationState RegistrationState { get; set; } = EquipmentRegistrationState.Draft;
    // False only for records backfilled by the migration; never a user-editable exemption.
    public bool RequiresRegistrationPhoto { get; set; } = true;
    public string? DraftPayload { get; set; }
    public int RegistrationRevision { get; set; }
    public DateTimeOffset? RegistrationUpdatedAtUtc { get; set; }
    public DateTimeOffset? FinalizedAtUtc { get; set; }
    public Guid? BrandId { get; set; }
    public EquipmentBrand? Brand { get; set; }
    public Guid? CommercialModelId { get; set; }
    public CommercialModel? CommercialModel { get; set; }
    public string? SerialNumber { get; set; }
    public DataAvailability SerialAvailability { get; set; } = DataAvailability.NotRecorded;
    public ICollection<TechnicalConfiguration> TechnicalConfigurations { get; set; } = new List<TechnicalConfiguration>();
    public ICollection<EquipmentObservation> Observations { get; set; } = new List<EquipmentObservation>();
    public ICollection<EquipmentInspection> Inspections { get; set; } = new List<EquipmentInspection>();
}

public enum EquipmentRegistrationState { Draft, Definitive }
