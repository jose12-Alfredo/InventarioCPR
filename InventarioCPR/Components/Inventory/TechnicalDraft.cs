using InventarioCPR.Application.Inventory;

namespace InventarioCPR.Components.Inventory;

// State of the existing modal, not another registration form. Category drafts survive step changes.
public sealed class TechnicalDraft
{
    public string? ProcessorManufacturer { get; set; }
    public string? ProcessorModel { get; set; }
    public string? ProcessorName { get; set; }
    public decimal? TotalRamGb { get; set; }
    public bool RamModulesComplete { get; set; } = true;
    public bool IsDirty { get; set; }
    public int? ExpectedRevision { get; set; }
    public string RamState { get; set; } = "unrecorded";
    public string? OperatingSystem { get; set; }
    public string OsState { get; set; } = "unrecorded";
    public string? Notes { get; set; }
    public List<RamDraft> RamModules { get; } = [];
    public List<StorageDraft> StorageUnits { get; } = [];
    public string StorageState { get; set; } = "unrecorded";
    public bool CalculatesRam => RamModulesComplete && RamModules.Any(x => x.Capacity.HasValue || x.OriginalId.HasValue);
    public decimal? EffectiveRam => CalculatesRam
        ? RamModules.All(x => x.Capacity is > 0) ? RamModules.Sum(x => x.Capacity) : null
        : TotalRamGb;

    public List<RamInput> BuildRam(bool allowIncomplete = false)
    {
        if (!allowIncomplete && CalculatesRam && RamModules.Any(x => x.Capacity is null))
            throw new InventoryValidationException("Complete los módulos de RAM o desmarque «He registrado todos los módulos».");
        return RamModules.Where(x => allowIncomplete || x.OriginalId.HasValue || x.Capacity.HasValue)
            .Select(x => new RamInput { OriginalId = x.OriginalId, CapacityGb = x.Capacity }).ToList();
    }

    public List<StorageInput> BuildStorage() => StorageUnits
        .Where(x => x.OriginalId.HasValue || x.Capacity.HasValue || !string.IsNullOrWhiteSpace(x.Type) || !string.IsNullOrWhiteSpace(x.Interface))
        .Select(x => new StorageInput { OriginalId = x.OriginalId, Technology = x.Type,
            Interface = x.Interface, Capacity = x.Capacity, Unit = x.Unit }).ToList();
}

public sealed class RamDraft
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid? OriginalId { get; set; }
    public decimal? Capacity { get; set; }
}

public sealed class StorageDraft
{
    public Guid Id { get; } = Guid.NewGuid();
    public Guid? OriginalId { get; set; }
    public string Type { get; set; } = "";
    public string Interface { get; set; } = "";
    public decimal? Capacity { get; set; }
    public string Unit { get; set; } = "GB";
}
