using InventarioCPR.Infrastructure.Persistence.Models;

namespace InventarioCPR.Application.Assignments;

public enum EquipmentAvailability { Free, Occupied }
public enum DirectoryKind { Company, Department, Area, Worker, Site, Location }
public enum CustodyAction { Assign, Transfer, Return, ReconcileAssigned, ReconcileFree }
public sealed record InventoryScope(Guid? OwnerCompanyId = null, bool WithoutOwnerCompany = false,
    Guid? UsingCompanyId = null, bool WithoutUsingCompany = false, Guid? OwnerAreaId = null,
    Guid? WorkerId = null, EquipmentAvailability? Availability = null, bool FreeFunctional = false);
public sealed record InventoryStatistics(int Total, int Free, int Occupied, int ReconciliationPending);
public sealed record DirectoryOption(Guid Id, string Name, bool Active, string? Context = null)
{
    public DirectoryOption() : this(Guid.Empty, "", false, null) { }
}
public sealed record DirectoryEntry(Guid Id, string Name, bool IsActive, int Revision, Guid? ParentId,
    string? LegalName = null, string? TaxIdentifier = null, bool CanReceiveEquipment = false, string? PersonnelCode = null);
public sealed record DirectoryEdit(Guid RequestId, Guid Id, DirectoryKind Kind, string Name, bool IsActive,
    int ExpectedRevision, Guid? ParentId = null, string? LegalName = null, string? TaxIdentifier = null,
    bool CanReceiveEquipment = false, string? PersonnelCode = null);
public sealed record AssetClassification(Guid RequestId, Guid EquipmentId, int ExpectedRevision,
    Guid? OwnerCompanyId, Guid? UsingCompanyId, Guid? OwnerAreaId, string? Note);
public sealed record CustodyCommand(Guid RequestId, Guid EquipmentId, int ExpectedRevision,
    Guid? ExpectedCustodyId, CustodyAction Action, Guid? WorkerId, CustodyKind Kind,
    Guid? SharedLocationId, DateTimeOffset EffectiveAtUtc, string? Note = null,
    DateTimeOffset? OriginalDeliveryAtUtc = null, Guid? ConditionInspectorId = null,
    string? DeliveryPhysicalCondition = null, string? DeliveryOperationalCondition = null,
    string? ReturnPhysicalCondition = null, string? ReturnOperationalCondition = null);
public sealed record OperationResult(Guid OperationId, Guid EquipmentId, int Revision);
public sealed record CustodyHistoryItem(Guid Id, Guid WorkerId, string Worker, string WorkerArea, string Department,
    CustodyKind Kind, string? Office, DateTimeOffset FromUtc, DateTimeOffset? ToUtc, string? Closure,
    string? OwnerCompany, string? UsingCompany, string? OwnerArea, bool Reconciled, DateTimeOffset? OriginalDeliveryAtUtc);
public sealed record OperationHistoryItem(Guid Id, string Action, string Actor, DateTimeOffset RecordedAtUtc,
    DateTimeOffset? EffectiveAtUtc, string? Note, string? BeforeJson, string AfterJson);
public sealed record AssignmentSummary(Guid EquipmentId, int Revision, Guid? OwnerCompanyId, string? OwnerCompany,
    Guid? UsingCompanyId, string? UsingCompany, Guid? OwnerAreaId, string? OwnerArea,
    Guid? CustodyId, Guid? WorkerId, string? Worker, string? WorkerArea, string? Office, DateTimeOffset? AssignedAtUtc,
    bool NeedsReconciliation, IReadOnlyList<CustodyHistoryItem> History, IReadOnlyList<OperationHistoryItem> Operations,
    bool HasMoreHistory)
{
    public EquipmentAvailability Availability => CustodyId.HasValue ? EquipmentAvailability.Occupied : EquipmentAvailability.Free;
}
