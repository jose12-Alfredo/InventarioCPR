using InventarioCPR.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace InventarioCPR.Infrastructure.Persistence;

public sealed class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
{
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<EquipmentCategory> Categories => Set<EquipmentCategory>();
    public DbSet<EquipmentBrand> Brands => Set<EquipmentBrand>();
    public DbSet<CommercialModel> CommercialModels => Set<CommercialModel>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Area> Areas => Set<Area>();
    public DbSet<Site> Sites => Set<Site>();
    public DbSet<Location> Locations => Set<Location>();
    public DbSet<StaffMember> StaffMembers => Set<StaffMember>();
    public DbSet<TechnicalConfiguration> TechnicalConfigurations => Set<TechnicalConfiguration>();
    public DbSet<RamModule> RamModules => Set<RamModule>();
    public DbSet<StorageUnit> StorageUnits => Set<StorageUnit>();
    public DbSet<CategoryAttributeDefinition> CategoryAttributeDefinitions => Set<CategoryAttributeDefinition>();
    public DbSet<TechnicalAttributeValue> TechnicalAttributeValues => Set<TechnicalAttributeValue>();
    public DbSet<EquipmentInspection> Inspections => Set<EquipmentInspection>();
    public DbSet<TechnicalVerification> TechnicalVerifications => Set<TechnicalVerification>();
    public DbSet<EquipmentObservation> Observations => Set<EquipmentObservation>();
    public DbSet<EquipmentLocationObservation> LocationObservations => Set<EquipmentLocationObservation>();
    public DbSet<EquipmentDataMarker> DataMarkers => Set<EquipmentDataMarker>();
    public DbSet<PhotoEvidence> PhotoEvidence => Set<PhotoEvidence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<EquipmentCategory>(e =>
        {
            e.ToTable("equipment_categories", t => t.HasCheckConstraint("CK_categories_name", "length(btrim(\"Name\")) > 0"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });
        modelBuilder.Entity<EquipmentBrand>(e =>
        {
            e.ToTable("equipment_brands", t => t.HasCheckConstraint("CK_brands_name", "length(btrim(\"Name\")) > 0"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });
        modelBuilder.Entity<CommercialModel>(e =>
        {
            e.ToTable("commercial_models", t => t.HasCheckConstraint("CK_models_name", "length(btrim(\"Name\")) > 0"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(160).IsRequired();
            e.HasIndex(x => new { x.BrandId, x.CategoryId, x.Name }).IsUnique();
            e.HasAlternateKey(x => new { x.Id, x.BrandId, x.CategoryId });
            e.HasOne(x => x.Brand).WithMany().HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Department>(e =>
        {
            e.ToTable("departments", t => t.HasCheckConstraint("CK_departments_name", "length(btrim(\"Name\")) > 0"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });
        modelBuilder.Entity<Area>(e =>
        {
            e.ToTable("areas", t => t.HasCheckConstraint("CK_areas_name", "length(btrim(\"Name\")) > 0"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.HasIndex(x => new { x.DepartmentId, x.Name }).IsUnique();
            e.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Site>(e =>
        {
            e.ToTable("sites", t => t.HasCheckConstraint("CK_sites_name", "length(btrim(\"Name\")) > 0"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();
        });
        modelBuilder.Entity<Location>(e =>
        {
            e.ToTable("locations", t => t.HasCheckConstraint("CK_locations_name", "length(btrim(\"Name\")) > 0"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(160).IsRequired();
            e.HasIndex(x => new { x.SiteId, x.Name }).IsUnique();
            e.HasOne(x => x.Site).WithMany().HasForeignKey(x => x.SiteId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<StaffMember>(e =>
        {
            e.ToTable("staff_members", t => t.HasCheckConstraint("CK_staff_name", "length(btrim(\"Name\")) > 0"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.HasOne(x => x.Area).WithMany().HasForeignKey(x => x.AreaId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<Equipment>(e =>
        {
            e.ToTable("equipment", t =>
            {
                t.HasCheckConstraint("CK_equipment_code", "length(btrim(\"Code\")) > 0");
                t.HasCheckConstraint("CK_equipment_name", "length(btrim(\"Name\")) > 0");
                t.HasCheckConstraint("CK_equipment_model_brand", "\"CommercialModelId\" IS NULL OR \"BrandId\" IS NOT NULL");
                t.HasCheckConstraint("CK_equipment_serial", "(\"SerialAvailability\" = 'Recorded' AND nullif(btrim(\"SerialNumber\"), '') IS NOT NULL) OR (\"SerialAvailability\" IN ('NotRecorded', 'NotApplicable') AND \"SerialNumber\" IS NULL) OR \"SerialAvailability\" = 'PendingVerification'");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(80).IsRequired();
            e.Property(x => x.CodeNormalized).HasMaxLength(80).HasComputedColumnSql("upper(btrim(\"Code\"))", stored: true);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.SerialNumber).HasMaxLength(160);
            e.Property(x => x.SerialAvailability).HasConversion<string>().HasMaxLength(30).HasDefaultValue(DataAvailability.NotRecorded);
            e.HasIndex(x => x.CodeNormalized).IsUnique();
            e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Brand).WithMany().HasForeignKey(x => x.BrandId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.CommercialModel).WithMany()
                .HasForeignKey(x => new { x.CommercialModelId, x.BrandId, x.CategoryId })
                .HasPrincipalKey(x => new { x.Id, x.BrandId, x.CategoryId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<TechnicalConfiguration>(e =>
        {
            e.ToTable("technical_configurations", t =>
            {
                t.HasCheckConstraint("CK_technical_revision", "\"Revision\" > 0");
                t.HasCheckConstraint("CK_technical_ram", "\"TotalRamGb\" IS NULL OR \"TotalRamGb\" > 0");
                t.HasCheckConstraint("CK_technical_period", "\"EffectiveToUtc\" IS NULL OR \"EffectiveToUtc\" > \"EffectiveFromUtc\"");
            });
            e.HasKey(x => x.Id);
            e.HasAlternateKey(x => new { x.Id, x.CategoryId });
            e.HasIndex(x => new { x.EquipmentId, x.Revision }).IsUnique();
            e.HasIndex(x => x.EquipmentId).IsUnique().HasFilter("\"EffectiveToUtc\" IS NULL");
            e.Property(x => x.TotalRamGb).HasPrecision(12, 3);
            e.Property(x => x.ProcessorManufacturer).HasMaxLength(120);
            e.Property(x => x.ProcessorModel).HasMaxLength(160);
            e.Property(x => x.ProcessorDesignation).HasMaxLength(200);
            e.Property(x => x.OperatingSystem).HasMaxLength(160);
            e.Property(x => x.ChangeReason).HasMaxLength(1000);
            e.HasOne(x => x.Equipment).WithMany(x => x.TechnicalConfigurations).HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.RecordedBy).WithMany().HasForeignKey(x => x.RecordedById).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<RamModule>(e =>
        {
            e.ToTable("ram_modules", t => t.HasCheckConstraint("CK_ram_capacity", "\"CapacityGb\" > 0"));
            e.HasKey(x => x.Id);
            e.Property(x => x.CapacityGb).HasPrecision(12, 3);
            e.Property(x => x.Technology).HasMaxLength(80);
            e.Property(x => x.SerialNumber).HasMaxLength(160);
            e.HasIndex(x => new { x.TechnicalConfigurationId, x.Slot }).IsUnique().HasFilter("\"Slot\" IS NOT NULL");
            e.HasOne(x => x.TechnicalConfiguration).WithMany(x => x.RamModules).HasForeignKey(x => x.TechnicalConfigurationId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<StorageUnit>(e =>
        {
            e.ToTable("storage_units", t => t.HasCheckConstraint("CK_storage_capacity", "(\"Capacity\" IS NULL AND \"CapacityUnit\" IS NULL) OR (\"Capacity\" > 0 AND \"CapacityUnit\" IN ('GB', 'TB'))"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Capacity).HasPrecision(14, 3);
            e.Property(x => x.CapacityUnit).HasMaxLength(2);
            e.Property(x => x.Technology).HasMaxLength(80);
            e.Property(x => x.Interface).HasMaxLength(80);
            e.Property(x => x.SerialNumber).HasMaxLength(160);
            e.HasIndex(x => new { x.TechnicalConfigurationId, x.Position }).IsUnique().HasFilter("\"Position\" IS NOT NULL");
            e.HasOne(x => x.TechnicalConfiguration).WithMany(x => x.StorageUnits).HasForeignKey(x => x.TechnicalConfigurationId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<CategoryAttributeDefinition>(e =>
        {
            e.ToTable("category_attribute_definitions", t =>
            {
                t.HasCheckConstraint("CK_attribute_revision", "\"Revision\" > 0");
                t.HasCheckConstraint("CK_attribute_key", "length(btrim(\"Key\")) > 0");
            });
            e.HasKey(x => x.Id);
            e.HasAlternateKey(x => new { x.Id, x.CategoryId });
            e.Property(x => x.Key).HasMaxLength(80).IsRequired();
            e.Property(x => x.Label).HasMaxLength(160).IsRequired();
            e.Property(x => x.Unit).HasMaxLength(40);
            e.Property(x => x.ValueKind).HasConversion<string>().HasMaxLength(16);
            e.HasIndex(x => new { x.CategoryId, x.Key, x.Revision }).IsUnique();
            e.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<TechnicalAttributeValue>(e =>
        {
            e.ToTable("technical_attribute_values", t => t.HasCheckConstraint("CK_attribute_one_value", "(CASE WHEN \"TextValue\" IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN \"NumberValue\" IS NOT NULL THEN 1 ELSE 0 END + CASE WHEN \"BooleanValue\" IS NOT NULL THEN 1 ELSE 0 END) = 1"));
            e.HasKey(x => x.Id);
            e.Property(x => x.TextValue).HasMaxLength(500);
            e.Property(x => x.NumberValue).HasPrecision(18, 4);
            e.HasIndex(x => new { x.TechnicalConfigurationId, x.DefinitionId }).IsUnique();
            e.HasOne(x => x.TechnicalConfiguration).WithMany(x => x.AttributeValues)
                .HasForeignKey(x => new { x.TechnicalConfigurationId, x.CategoryId })
                .HasPrincipalKey(x => new { x.Id, x.CategoryId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Definition).WithMany().HasForeignKey(x => new { x.DefinitionId, x.CategoryId })
                .HasPrincipalKey(x => new { x.Id, x.CategoryId }).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<EquipmentInspection>(e =>
        {
            e.ToTable("equipment_inspections", t => t.HasCheckConstraint("CK_inspection_condition", "\"PhysicalCondition\" IS NOT NULL OR \"OperationalCondition\" IS NOT NULL"));
            e.HasKey(x => x.Id);
            e.HasAlternateKey(x => new { x.Id, x.EquipmentId });
            e.Property(x => x.PhysicalCondition).HasMaxLength(100);
            e.Property(x => x.OperationalCondition).HasMaxLength(100);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.HasIndex(x => new { x.EquipmentId, x.InspectedAtUtc });
            e.HasOne(x => x.Equipment).WithMany(x => x.Inspections).HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Inspector).WithMany().HasForeignKey(x => x.InspectorId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<TechnicalVerification>(e =>
        {
            e.ToTable("technical_verifications");
            e.HasKey(x => x.Id);
            e.Property(x => x.Notes).HasMaxLength(2000);
            e.HasIndex(x => new { x.EquipmentId, x.VerifiedAtUtc });
            e.HasOne(x => x.Equipment).WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Verifier).WithMany().HasForeignKey(x => x.VerifierId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<EquipmentObservation>(e =>
        {
            e.ToTable("equipment_observations", t => t.HasCheckConstraint("CK_observation_text", "length(btrim(\"Text\")) > 0"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Text).HasColumnType("text").IsRequired();
            e.Property(x => x.Source).HasMaxLength(80).IsRequired();
            e.HasIndex(x => new { x.EquipmentId, x.RecordedAtUtc });
            e.HasOne(x => x.Equipment).WithMany(x => x.Observations).HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Author).WithMany().HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<EquipmentLocationObservation>(e =>
        {
            e.ToTable("equipment_location_observations");
            e.HasKey(x => x.Id);
            e.Property(x => x.Notes).HasMaxLength(1000);
            e.HasIndex(x => new { x.EquipmentId, x.ObservedAtUtc });
            e.HasOne(x => x.Equipment).WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Location).WithMany().HasForeignKey(x => x.LocationId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<EquipmentDataMarker>(e =>
        {
            e.ToTable("equipment_data_markers", t =>
            {
                t.HasCheckConstraint("CK_marker_key", "length(btrim(\"FieldKey\")) > 0 AND \"FieldKey\" <> 'serial_number'");
                t.HasCheckConstraint("CK_marker_availability", "\"Availability\" IN ('NotApplicable', 'PendingVerification')");
            });
            e.HasKey(x => x.Id);
            e.Property(x => x.FieldKey).HasMaxLength(80).IsRequired();
            e.Property(x => x.Availability).HasConversion<string>().HasMaxLength(30);
            e.HasIndex(x => new { x.EquipmentId, x.FieldKey }).IsUnique();
            e.HasOne(x => x.Equipment).WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<PhotoEvidence>(e =>
        {
            e.ToTable("photo_evidence");
            e.HasKey(x => x.Id);
            e.Property(x => x.CloudinaryPublicId).HasMaxLength(255).IsRequired();
            e.HasIndex(x => x.CloudinaryPublicId).IsUnique();
            e.Property(x => x.EvidenceType).HasMaxLength(40).IsRequired();
            e.HasOne(x => x.Equipment).WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Inspection).WithMany().HasForeignKey(x => new { x.InspectionId, x.EquipmentId })
                .HasPrincipalKey(x => new { x.Id, x.EquipmentId }).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
