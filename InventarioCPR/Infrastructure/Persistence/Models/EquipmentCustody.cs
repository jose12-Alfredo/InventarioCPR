namespace InventarioCPR.Infrastructure.Persistence.Models;

public sealed class Company
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string NameNormalized { get; private set; } = null!;
    public string? LegalName { get; set; }
    public string? TaxIdentifier { get; set; }
    public bool IsActive { get; set; } = true;
    public int Revision { get; set; }
}

public enum CustodyKind { Individual, SharedOffice }
public enum CustodyClosure { Return, Transfer }

public sealed class EquipmentCustody
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;
    public Guid WorkerId { get; set; }
    public StaffMember Worker { get; set; } = null!;
    public Guid WorkerAreaId { get; set; }
    public Area WorkerArea { get; set; } = null!;
    public CustodyKind Kind { get; set; }
    public Guid? SharedLocationId { get; set; }
    public Location? SharedLocation { get; set; }
    public DateTimeOffset StartedAtUtc { get; set; }
    public DateTimeOffset? EndedAtUtc { get; set; }
    public CustodyClosure? Closure { get; set; }
    public bool IsInitialReconciliation { get; set; }
    // Nullable when the original physical delivery date is not known during reconciliation.
    public DateTimeOffset? OriginalDeliveryAtUtc { get; set; }
    public string WorkerName { get; set; } = null!;
    public string WorkerAreaName { get; set; } = null!;
    public string WorkerDepartmentName { get; set; } = null!;
    public string? OwnerCompanyName { get; set; }
    public string? UsingCompanyName { get; set; }
    public string? OwnerAreaName { get; set; }
    public string? SharedLocationName { get; set; }
    public string? SharedSiteName { get; set; }
}

// Permanent inventory audit and retry receipt in the SAME transaction as the operation.
// Actor IDs are Identity IDs, never StaffMember IDs; snapshots survive catalog/account renames.
public sealed class EquipmentOperation
{
    public Guid Id { get; set; }
    public Guid RequestId { get; set; }
    public string RequestHash { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public Guid EntityId { get; set; }
    public Guid? EquipmentId { get; set; }
    public string Action { get; set; } = null!;
    public Guid? SourceCustodyId { get; set; }
    public Guid? DestinationCustodyId { get; set; }
    public Guid ActorId { get; set; }
    public string ActorName { get; set; } = null!;
    public DateTimeOffset RecordedAtUtc { get; set; }
    public DateTimeOffset? EffectiveAtUtc { get; set; }
    public string? Note { get; set; }
    public string? BeforeJson { get; set; }
    public string AfterJson { get; set; } = null!;
    public string? PreviousName { get; set; }
    public string? NewName { get; set; }
}
