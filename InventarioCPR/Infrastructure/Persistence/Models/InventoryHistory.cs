namespace InventarioCPR.Infrastructure.Persistence.Models;

public sealed class EquipmentInspection
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;
    public Guid InspectorId { get; set; }
    public StaffMember Inspector { get; set; } = null!;
    public DateTimeOffset InspectedAtUtc { get; set; }
    // Catalog values are deliberately not pre-approved; either dimension can remain unknown.
    public string? PhysicalCondition { get; set; }
    public string? OperationalCondition { get; set; }
    public string? Notes { get; set; }
}

public sealed class TechnicalVerification
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;
    public Guid VerifierId { get; set; }
    public StaffMember Verifier { get; set; } = null!;
    public DateTimeOffset VerifiedAtUtc { get; set; }
    public string? Notes { get; set; }
}

public sealed class EquipmentObservation
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;
    public string Text { get; set; } = null!;
    public string Source { get; set; } = null!; // Excel or later addition; preserve source without modifying original text.
    public DateTimeOffset RecordedAtUtc { get; set; }
    public Guid? AuthorId { get; set; }
    public StaffMember? Author { get; set; }
}

// A physical observation is separate from custody and condition; no assumed current/habitual location.
public sealed class EquipmentLocationObservation
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;
    public Guid LocationId { get; set; }
    public Location Location { get; set; } = null!;
    public DateTimeOffset ObservedAtUtc { get; set; }
    public string? Notes { get; set; }
}

// Explicit pending markers: absence means NotRecorded, not verified or not applicable.
public sealed class EquipmentDataMarker
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;
    public string FieldKey { get; set; } = null!;
    public DataAvailability Availability { get; set; }
}
