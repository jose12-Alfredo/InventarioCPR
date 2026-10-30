namespace InventarioCPR.Infrastructure.Persistence.Models;

public enum DataAvailability { NotRecorded, Recorded, NotApplicable, PendingVerification }
public enum TechnicalValueKind { Text, Number, Boolean }

// Each revision is a complete snapshot. Do not update previous revisions when components change.
public sealed class TechnicalConfiguration
{
    public Guid Id { get; set; }
    public Guid EquipmentId { get; set; }
    public Equipment Equipment { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public EquipmentCategory Category { get; set; } = null!;
    public int Revision { get; set; }
    public DateTimeOffset EffectiveFromUtc { get; set; }
    public DateTimeOffset? EffectiveToUtc { get; set; }
    public Guid? RecordedById { get; set; }
    public StaffMember? RecordedBy { get; set; }
    public string? ChangeReason { get; set; }
    public string? ProcessorManufacturer { get; set; }
    public string? ProcessorModel { get; set; }
    public string? ProcessorDesignation { get; set; }
    public decimal? TotalRamGb { get; set; }
    public bool RamModulesComplete { get; set; }
    public string? OperatingSystem { get; set; }
    public ICollection<RamModule> RamModules { get; set; } = new List<RamModule>();
    public ICollection<StorageUnit> StorageUnits { get; set; } = new List<StorageUnit>();
    public ICollection<TechnicalAttributeValue> AttributeValues { get; set; } = new List<TechnicalAttributeValue>();
}

public sealed class RamModule
{
    public Guid Id { get; set; }
    public Guid TechnicalConfigurationId { get; set; }
    public TechnicalConfiguration TechnicalConfiguration { get; set; } = null!;
    public int? Slot { get; set; }
    public decimal CapacityGb { get; set; }
    public string? Technology { get; set; }
    public string? SerialNumber { get; set; }
}

public sealed class StorageUnit
{
    public Guid Id { get; set; }
    public Guid TechnicalConfigurationId { get; set; }
    public TechnicalConfiguration TechnicalConfiguration { get; set; } = null!;
    public int? Position { get; set; }
    public decimal? Capacity { get; set; }
    public string? CapacityUnit { get; set; } // GB or TB as originally reported; normalization policy is pending.
    public string? Technology { get; set; } // SSD, HDD, etc.; distinct from interface.
    public string? Interface { get; set; } // NVMe, SATA, USB, etc.
    public string? SerialNumber { get; set; }
}

// Definitions are versioned, never overwritten after values reference them.
public sealed class CategoryAttributeDefinition
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public EquipmentCategory Category { get; set; } = null!;
    public string Key { get; set; } = null!;
    public int Revision { get; set; }
    public string Label { get; set; } = null!;
    public TechnicalValueKind ValueKind { get; set; }
    public string? Unit { get; set; }
}

public sealed class TechnicalAttributeValue
{
    public Guid Id { get; set; }
    public Guid TechnicalConfigurationId { get; set; }
    public TechnicalConfiguration TechnicalConfiguration { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public Guid DefinitionId { get; set; }
    public CategoryAttributeDefinition Definition { get; set; } = null!;
    public string? TextValue { get; set; }
    public decimal? NumberValue { get; set; }
    public bool? BooleanValue { get; set; }
}
