using InventarioCPR.Application.Inventory;
using InventarioCPR.Infrastructure.Persistence;
using InventarioCPR.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace InventarioCPR.Application.Assignments;

public sealed partial class AssignmentService
{
    public async Task<IReadOnlyList<DirectoryOption>> OptionsAsync(DirectoryKind kind, bool includeInactive = false, string? search = null, CancellationToken ct = default)
    {
        await access.EnsureReadAsync();await using var db = await contexts.CreateDbContextAsync(ct);
        IQueryable<DirectoryOption> query = kind switch
        {
            DirectoryKind.Company => db.Companies.Select(x => new DirectoryOption { Id=x.Id,Name=x.Name,Active=x.IsActive,Context=x.LegalName }),
            DirectoryKind.Department => db.Departments.Select(x => new DirectoryOption { Id=x.Id,Name=x.Name,Active=x.IsActive }),
            DirectoryKind.Area => db.Areas.Select(x => new DirectoryOption { Id=x.Id,Name=x.Name,Active=x.IsActive && x.Department.IsActive,Context=x.Department.Name }),
            DirectoryKind.Worker => db.StaffMembers.Select(x => new DirectoryOption { Id=x.Id,Name=x.Name,Active=x.IsActive && x.CanReceiveEquipment && x.Area != null && x.Area.IsActive && x.Area.Department.IsActive,
                Context=x.Area != null ? x.Area.Name + " · " + (x.PersonnelCode ?? "Sin código de personal") : "Área no registrada" }),
            DirectoryKind.Site => db.Sites.Select(x => new DirectoryOption { Id=x.Id,Name=x.Name,Active=x.IsActive }),
            DirectoryKind.Location => db.Locations.Select(x => new DirectoryOption { Id=x.Id,Name=x.Name,Active=x.IsActive && x.Site.IsActive,Context=x.Site.Name }),
            _ => throw new InventoryValidationException("Catálogo inválido.")
        };
        if (!includeInactive) query = query.Where(x => x.Active);
        if (Clean(search) is { } term) query = query.Where(x => x.Name.ToLower().Contains(term.ToLower()) || (x.Context != null && x.Context.ToLower().Contains(term.ToLower())));
        return await query.OrderBy(x => x.Name).ThenBy(x => x.Id).Take(250).AsNoTracking().ToListAsync(ct);
    }

    public async Task<DirectoryEntry?> GetDirectoryEntryAsync(DirectoryKind kind, Guid id, CancellationToken ct = default)
    {
        await access.EnsureReadAsync();await using var db = await contexts.CreateDbContextAsync(ct);
        return kind switch
        {
            DirectoryKind.Company => await db.Companies.Where(x => x.Id == id).Select(x => new DirectoryEntry(x.Id,x.Name,x.IsActive,x.Revision,null,x.LegalName,x.TaxIdentifier,false,null)).SingleOrDefaultAsync(ct),
            DirectoryKind.Department => await db.Departments.Where(x => x.Id == id).Select(x => new DirectoryEntry(x.Id,x.Name,x.IsActive,x.Revision,null,null,null,false,null)).SingleOrDefaultAsync(ct),
            DirectoryKind.Area => await db.Areas.Where(x => x.Id == id).Select(x => new DirectoryEntry(x.Id,x.Name,x.IsActive,x.Revision,x.DepartmentId,null,null,false,null)).SingleOrDefaultAsync(ct),
            DirectoryKind.Worker => await db.StaffMembers.Where(x => x.Id == id).Select(x => new DirectoryEntry(x.Id,x.Name,x.IsActive,x.Revision,x.AreaId,null,null,x.CanReceiveEquipment,x.PersonnelCode)).SingleOrDefaultAsync(ct),
            DirectoryKind.Site => await db.Sites.Where(x => x.Id == id).Select(x => new DirectoryEntry(x.Id,x.Name,x.IsActive,x.Revision,null,null,null,false,null)).SingleOrDefaultAsync(ct),
            DirectoryKind.Location => await db.Locations.Where(x => x.Id == id).Select(x => new DirectoryEntry(x.Id,x.Name,x.IsActive,x.Revision,x.SiteId,null,null,false,null)).SingleOrDefaultAsync(ct),
            _ => null
        };
    }

    public async Task<Guid> SaveDirectoryAsync(DirectoryEdit input, CancellationToken ct = default)
    {
        var actor = await ActorAsync();var hash = Hash(input);
        Require(input.Id != Guid.Empty && Enum.IsDefined(input.Kind), "Identificador o catálogo inválido.");
        var nameLimit=input.Kind==DirectoryKind.Worker?200:input.Kind==DirectoryKind.Company?160:120;
        Require(Clean(input.Name) is { } validName && validName.Length<=nameLimit, $"Indique un nombre de hasta {nameLimit} caracteres.");
        Require(input.LegalName?.Length is not > 240 && input.TaxIdentifier?.Length is not > 80 && input.PersonnelCode?.Length is not > 80, "Revise la longitud de los datos del catálogo.");
        await using var db = await contexts.CreateDbContextAsync(ct);await using var tx = await db.Database.BeginTransactionAsync(ct);
        await RequestLock(db, input.RequestId, ct);
        if (await Receipt(db, input.RequestId, hash, actor, ct) is not null) return input.Id;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({input.Kind.ToString()+input.Id.ToString()}, 1))",ct);
        var table = input.Kind switch { DirectoryKind.Company => "companies", DirectoryKind.Department => "departments", DirectoryKind.Area => "areas", DirectoryKind.Worker => "staff_members", DirectoryKind.Site => "sites", DirectoryKind.Location => "locations", _ => throw new InventoryValidationException("Catálogo inválido.") };
        // table is selected exclusively from the closed server-side enum above.
        var lockSql = "SELECT 1 FROM " + table + " WHERE \"Id\" = @id FOR UPDATE";
        await db.Database.ExecuteSqlRawAsync(lockSql, [new NpgsqlParameter("id",input.Id)], ct);
        object? before;object after;string? oldName;var nextRevision=input.ExpectedRevision+1;
        switch (input.Kind)
        {
            case DirectoryKind.Company:
                var company=await db.Companies.SingleOrDefaultAsync(x=>x.Id==input.Id,ct);
                Require(company?.Revision == input.ExpectedRevision || (company is null && input.ExpectedRevision==0),"La empresa cambió. Vuelva a cargarla.");
                before=company is null?null:new{company.Name,company.LegalName,company.TaxIdentifier,company.IsActive};oldName=company?.Name;
                if(company is null){company=new Company{Id=input.Id};db.Companies.Add(company);}
                company.Name=input.Name.Trim();company.LegalName=Clean(input.LegalName);company.TaxIdentifier=Clean(input.TaxIdentifier);company.IsActive=input.IsActive;company.Revision=nextRevision;
                after=new{company.Name,company.LegalName,company.TaxIdentifier,company.IsActive};break;
            case DirectoryKind.Department:
                var department=await db.Departments.SingleOrDefaultAsync(x=>x.Id==input.Id,ct);
                Require(department?.Revision==input.ExpectedRevision||(department is null&&input.ExpectedRevision==0),"El departamento cambió. Actualice.");
                before=department is null?null:new{department.Name,department.IsActive};oldName=department?.Name;
                if(department is null){department=new Department{Id=input.Id};db.Departments.Add(department);}
                department.Name=input.Name.Trim();department.IsActive=input.IsActive;department.Revision=nextRevision;after=new{department.Name,department.IsActive};break;
            case DirectoryKind.Area:
                Require(input.ParentId.HasValue,"Seleccione el departamento del área.");
                var area=await db.Areas.SingleOrDefaultAsync(x=>x.Id==input.Id,ct);
                await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM departments WHERE \"Id\"={input.ParentId!.Value} FOR SHARE",ct);
                Require((!input.IsActive&&area?.DepartmentId==input.ParentId)||await db.Departments.AnyAsync(x=>x.Id==input.ParentId&&x.IsActive,ct),"Seleccione un departamento activo.");
                Require(area?.Revision==input.ExpectedRevision||(area is null&&input.ExpectedRevision==0),"El área cambió. Actualice.");
                before=area is null?null:new{area.Name,area.DepartmentId,area.IsActive};oldName=area?.Name;
                if(area is null){area=new Area{Id=input.Id};db.Areas.Add(area);}
                area.Name=input.Name.Trim();area.DepartmentId=input.ParentId.Value;area.IsActive=input.IsActive;area.Revision=nextRevision;after=new{area.Name,area.DepartmentId,area.IsActive};break;
            case DirectoryKind.Worker:
                var worker=await db.StaffMembers.SingleOrDefaultAsync(x=>x.Id==input.Id,ct);
                if(input.ParentId.HasValue && (worker?.AreaId!=input.ParentId||(input.IsActive&&input.CanReceiveEquipment)))await AreaActive(db,input.ParentId.Value,ct);
                Require(!input.CanReceiveEquipment||input.ParentId.HasValue,"Para recibir equipos el trabajador necesita área.");
                Require(worker?.Revision==input.ExpectedRevision||(worker is null&&input.ExpectedRevision==0),"El trabajador cambió. Actualice.");
                before=worker is null?null:new{worker.Name,worker.AreaId,worker.IsActive,worker.CanReceiveEquipment,worker.PersonnelCode};oldName=worker?.Name;
                if(worker is null){worker=new StaffMember{Id=input.Id};db.StaffMembers.Add(worker);}
                worker.Name=input.Name.Trim();worker.AreaId=input.ParentId;worker.IsActive=input.IsActive;worker.CanReceiveEquipment=input.CanReceiveEquipment;worker.PersonnelCode=Clean(input.PersonnelCode);worker.Revision=nextRevision;
                after=new{worker.Name,worker.AreaId,worker.IsActive,worker.CanReceiveEquipment,worker.PersonnelCode};break;
            case DirectoryKind.Site:
                var site=await db.Sites.SingleOrDefaultAsync(x=>x.Id==input.Id,ct);
                Require(site?.Revision==input.ExpectedRevision||(site is null&&input.ExpectedRevision==0),"La sede cambió. Actualice.");
                before=site is null?null:new{site.Name,site.IsActive};oldName=site?.Name;
                if(site is null){site=new Site{Id=input.Id};db.Sites.Add(site);}
                site.Name=input.Name.Trim();site.IsActive=input.IsActive;site.Revision=nextRevision;after=new{site.Name,site.IsActive};break;
            default:
                Require(input.ParentId.HasValue,"Seleccione la sede de la ubicación/oficina.");
                var location=await db.Locations.SingleOrDefaultAsync(x=>x.Id==input.Id,ct);
                await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM sites WHERE \"Id\"={input.ParentId!.Value} FOR SHARE",ct);
                Require((!input.IsActive&&location?.SiteId==input.ParentId)||await db.Sites.AnyAsync(x=>x.Id==input.ParentId&&x.IsActive,ct),"Seleccione una sede activa.");
                Require(location?.Revision==input.ExpectedRevision||(location is null&&input.ExpectedRevision==0),"La ubicación cambió. Actualice.");
                before=location is null?null:new{location.Name,location.SiteId,location.IsActive};oldName=location?.Name;
                if(location is null){location=new Location{Id=input.Id};db.Locations.Add(location);}
                location.Name=input.Name.Trim();location.SiteId=input.ParentId.Value;location.IsActive=input.IsActive;location.Revision=nextRevision;after=new{location.Name,location.SiteId,location.IsActive};break;
        }
        db.Operations.Add(Audit(input.RequestId,hash,actor,input.Kind.ToString(),input.Id,"CatalogSave",before,after,oldName:oldName,newName:input.Name.Trim()));
        try{await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);}
        catch(DbUpdateException error)when(error.InnerException is PostgresException{SqlState:PostgresErrorCodes.UniqueViolation}){throw new InventoryValidationException("Ya existe ese nombre/código en el catálogo. Use la entrada existente.");}
        return input.Id;
    }

    public async Task<IReadOnlyList<OperationHistoryItem>> CatalogHistoryAsync(DirectoryKind kind,Guid id,CancellationToken ct=default)
    {
        await access.EnsureReadAsync();await using var db=await contexts.CreateDbContextAsync(ct);
        return await db.Operations.AsNoTracking().Where(x=>x.EntityType==kind.ToString()&&x.EntityId==id).OrderByDescending(x=>x.RecordedAtUtc).Take(100)
            .Select(x=>new OperationHistoryItem(x.Id,x.Action,x.ActorName,x.RecordedAtUtc,x.EffectiveAtUtc,x.Note,x.BeforeJson,x.AfterJson)).ToListAsync(ct);
    }
}
