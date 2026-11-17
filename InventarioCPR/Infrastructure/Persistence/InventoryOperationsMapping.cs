using InventarioCPR.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace InventarioCPR.Infrastructure.Persistence;

public sealed partial class InventoryDbContext
{
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<EquipmentCustody> Custodies => Set<EquipmentCustody>();
    public DbSet<EquipmentOperation> Operations => Set<EquipmentOperation>();

    private void ProtectOperationsHistory()
    {
        foreach (var entry in ChangeTracker.Entries<EquipmentOperation>())
            if (entry.State is EntityState.Modified or EntityState.Deleted)
                throw new InvalidOperationException("La auditoría de operaciones es permanente y no se modifica ni elimina.");
        foreach (var entry in ChangeTracker.Entries<EquipmentCustody>())
        {
            if (entry.State == EntityState.Deleted || (entry.State == EntityState.Modified &&
                (entry.Property(x => x.EndedAtUtc).OriginalValue is not null || entry.Entity.EndedAtUtc is null ||
                 entry.Entity.Closure is null || entry.Entity.EndedAtUtc < entry.Entity.StartedAtUtc ||
                 entry.Properties.Any(p => p.IsModified && p.Metadata.Name is not (nameof(EquipmentCustody.EndedAtUtc) or nameof(EquipmentCustody.Closure))))))
                throw new InvalidOperationException("Solo se permite cerrar una custodia vigente; su responsable e historia se conservan.");
        }
        foreach (var entry in ChangeTracker.Entries().Where(x => x.State == EntityState.Deleted))
            if (entry.Entity is Company or StaffMember or Area or Department or Location or Site)
                throw new InvalidOperationException("Los catálogos se desactivan, no se eliminan.");
    }

    private static void ConfigureOperations(ModelBuilder model)
    {
        model.Entity<StaffMember>().Property(x => x.IsActive).HasDefaultValue(true);
        model.Entity<Area>().Property(x => x.IsActive).HasDefaultValue(true);
        model.Entity<Department>().Property(x => x.IsActive).HasDefaultValue(true);
        model.Entity<Site>().Property(x => x.IsActive).HasDefaultValue(true);
        model.Entity<Location>().Property(x => x.IsActive).HasDefaultValue(true);
        model.Entity<Company>(e =>
        {
            e.ToTable("companies", t => t.HasCheckConstraint("CK_company_name", "length(btrim(\"Name\")) > 0 AND \"Revision\" >= 0"));
            e.HasKey(x => x.Id);e.Property(x => x.Name).HasMaxLength(160).IsRequired();
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.NameNormalized).HasComputedColumnSql("upper(btrim(\"Name\"))", stored: true);
            e.HasIndex(x => x.NameNormalized).IsUnique();e.Property(x => x.LegalName).HasMaxLength(240);e.Property(x => x.TaxIdentifier).HasMaxLength(80);
        });
        model.Entity<Equipment>(e =>
        {
            e.HasOne(x => x.OwnerCompany).WithMany().HasForeignKey(x => x.OwnerCompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.UsingCompany).WithMany().HasForeignKey(x => x.UsingCompanyId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.OwnerArea).WithMany().HasForeignKey(x => x.OwnerAreaId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x => new { x.OwnerCompanyId, x.RegistrationState, x.CodeNormalized });
            e.HasIndex(x => new { x.UsingCompanyId, x.RegistrationState });
        });
        model.Entity<StaffMember>().Property(x => x.PersonnelCode).HasMaxLength(80);
        model.Entity<StaffMember>().HasIndex(x => x.PersonnelCode).IsUnique().HasFilter("\"PersonnelCode\" IS NOT NULL");
        model.Entity<EquipmentCategory>().Property(x => x.TechnicalProfile).HasMaxLength(40);
        model.Entity<EquipmentCustody>(e =>
        {
            e.ToTable("equipment_custodies", t =>
            {
                t.HasCheckConstraint("CK_custody_kind", "(\"Kind\"='Individual' AND \"SharedLocationId\" IS NULL) OR (\"Kind\"='SharedOffice' AND \"SharedLocationId\" IS NOT NULL)");
                t.HasCheckConstraint("CK_custody_period", "(\"EndedAtUtc\" IS NULL AND \"Closure\" IS NULL) OR (\"EndedAtUtc\" IS NOT NULL AND \"EndedAtUtc\">=\"StartedAtUtc\" AND \"Closure\" IN ('Return','Transfer'))");
                t.HasCheckConstraint("CK_custody_original_date", "\"OriginalDeliveryAtUtc\" IS NULL OR \"OriginalDeliveryAtUtc\" <= \"StartedAtUtc\"");
            });
            e.HasKey(x => x.Id);e.HasAlternateKey(x => new { x.Id, x.EquipmentId });
            e.Property(x => x.Kind).HasConversion<string>().HasMaxLength(30);e.Property(x => x.Closure).HasConversion<string>().HasMaxLength(30);
            e.HasIndex(x => x.EquipmentId).IsUnique().HasFilter("\"EndedAtUtc\" IS NULL");
            e.HasIndex(x => new { x.WorkerId, x.EquipmentId }).HasFilter("\"EndedAtUtc\" IS NULL");
            e.HasIndex(x => new { x.EquipmentId, x.StartedAtUtc });
            e.HasOne(x => x.Equipment).WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.Worker).WithMany().HasForeignKey(x => x.WorkerId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.WorkerArea).WithMany().HasForeignKey(x => x.WorkerAreaId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne(x => x.SharedLocation).WithMany().HasForeignKey(x => x.SharedLocationId).OnDelete(DeleteBehavior.Restrict);
            foreach (var p in new[] { "WorkerName", "WorkerAreaName", "WorkerDepartmentName", "OwnerCompanyName", "UsingCompanyName", "OwnerAreaName", "SharedLocationName", "SharedSiteName" }) e.Property(p).HasMaxLength(240);
        });
        model.Entity<EquipmentOperation>(e =>
        {
            e.ToTable("equipment_operations");e.HasKey(x => x.Id);e.HasIndex(x => x.RequestId).IsUnique();
            e.Property(x => x.RequestHash).HasMaxLength(64).IsRequired();e.Property(x => x.EntityType).HasMaxLength(30).IsRequired();
            e.Property(x => x.Action).HasMaxLength(40).IsRequired();e.Property(x => x.ActorName).HasMaxLength(256).IsRequired();
            e.Property(x => x.Note).HasMaxLength(4000);e.Property(x => x.PreviousName).HasMaxLength(240);e.Property(x => x.NewName).HasMaxLength(240);
            e.HasIndex(x => new { x.EntityType, x.EntityId, x.RecordedAtUtc });e.HasIndex(x => new { x.EquipmentId, x.RecordedAtUtc });
            e.HasOne<Equipment>().WithMany().HasForeignKey(x => x.EquipmentId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<EquipmentCustody>().WithMany().HasForeignKey(x => new { x.SourceCustodyId, x.EquipmentId }).HasPrincipalKey(x => new { x.Id, x.EquipmentId }).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<EquipmentCustody>().WithMany().HasForeignKey(x => new { x.DestinationCustodyId, x.EquipmentId }).HasPrincipalKey(x => new { x.Id, x.EquipmentId }).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
