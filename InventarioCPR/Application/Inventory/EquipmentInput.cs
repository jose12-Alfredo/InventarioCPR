namespace InventarioCPR.Application.Inventory;

public sealed class EquipmentInput
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string? Brand { get; set; }
    public string? Model { get; set; }
    public string? Serial { get; set; }
    public string SerialStatus { get; set; } = "unrecorded";
    public string? ProcessorManufacturer { get; set; }
    public string? ProcessorModel { get; set; }
    public string? ProcessorName { get; set; }
    public decimal? TotalRamGb { get; set; }
    public string? OperatingSystem { get; set; }
    public List<RamInput> RamModules { get; set; } = [];
    public List<StorageInput> StorageUnits { get; set; } = [];
    public string? Site { get; set; }
    public string? Location { get; set; }
    public string? PhysicalCondition { get; set; }
    public string? OperationalCondition { get; set; }
    public string? Inspector { get; set; }
    public bool VerifyNow { get; set; }
    public string? Verifier { get; set; }
    public string? Observation { get; set; }
    public List<string> PendingFields { get; set; } = [];
    public List<string> NotApplicableFields { get; set; } = [];
}

public sealed class RamInput
{
    public decimal? CapacityGb { get; set; }
}

public sealed class StorageInput
{
    public string? Technology { get; set; }
    public string? Interface { get; set; }
    public decimal? Capacity { get; set; }
    public string? Unit { get; set; }
}

public sealed record EquipmentRow(Guid Id, string Code, string Name, string Category,
    string? Brand, string? Model, string? PhysicalCondition, string? OperationalCondition);
public sealed record EquipmentPage(IReadOnlyList<EquipmentRow> Rows, int Total);
public sealed record ConditionOption(string Value, string Label);

public sealed class InventoryValidationException(string message) : Exception(message);
