namespace InventarioCPR.Infrastructure.Persistence.Models;

public sealed class EquipmentCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    // Metadata for future forms; existing computer technical editing remains unchanged.
    public string TechnicalProfile { get; set; } = "General";
}

public sealed class EquipmentBrand
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
}

public sealed class CommercialModel
{
    public Guid Id { get; set; }
    public Guid BrandId { get; set; }
    public EquipmentBrand Brand { get; set; } = null!;
    public Guid CategoryId { get; set; }
    public EquipmentCategory Category { get; set; } = null!;
    public string Name { get; set; } = null!;
}

public sealed class Department
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public int Revision { get; set; }
}

public sealed class Area
{
    public Guid Id { get; set; }
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public int Revision { get; set; }
}

public sealed class Site
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public int Revision { get; set; }
}

public sealed class Location
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public Site Site { get; set; } = null!;
    public string Name { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public int Revision { get; set; }
}

// A person is not an Identity account. Custody always comes from a custody period, never this row.
public sealed class StaffMember
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid? AreaId { get; set; }
    public Area? Area { get; set; }
    public bool IsActive { get; set; } = true;
    // Legacy verifier/inspector records must be explicitly reviewed before receiving equipment.
    public bool CanReceiveEquipment { get; set; }
    public string? PersonnelCode { get; set; }
    public int Revision { get; set; }
}
