namespace InventarioCPR.Infrastructure.Persistence.Models;

public sealed class EquipmentCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
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
}

public sealed class Area
{
    public Guid Id { get; set; }
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;
    public string Name { get; set; } = null!;
}

public sealed class Site
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
}

public sealed class Location
{
    public Guid Id { get; set; }
    public Guid SiteId { get; set; }
    public Site Site { get; set; } = null!;
    public string Name { get; set; } = null!;
}

// A staff record is an identifiable verifier, not a current custodian/assignment.
public sealed class StaffMember
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid? AreaId { get; set; }
    public Area? Area { get; set; }
}
