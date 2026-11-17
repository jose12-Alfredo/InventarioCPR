using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using InventarioCPR.Application.Inventory;
using InventarioCPR.Application.Security;
using InventarioCPR.Infrastructure.Persistence;
using InventarioCPR.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace InventarioCPR.Application.Assignments;

public sealed partial class AssignmentService(IDbContextFactory<InventoryDbContext> contexts, IInventoryAccess access, IConfiguration configuration)
{
    public bool WritesEnabled => configuration["NeonDevelopmentApp:BranchConfirmed"] == "test-migracion-inicial" || configuration.GetValue<bool>("Inventory:ProductionWritesEnabled");
    public async Task<bool> CanWriteAsync() => WritesEnabled && await access.CanWriteAsync();
    private async Task<InventoryActor> ActorAsync()
    {
        if (!WritesEnabled) throw new InventoryValidationException("Las escrituras no están habilitadas en este entorno.");
        return await access.RequireWriteActorAsync();
    }
    private static void Require(bool valid, string message) { if (!valid) throw new InventoryValidationException(message); }
    private static string? Clean(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
    private static string Json(object value) => JsonSerializer.Serialize(value);
    private static string Hash(object command) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Json(command))));
    private static async Task RequestLock(InventoryDbContext db, Guid request, CancellationToken ct)
    {
        Require(request != Guid.Empty, "Falta el identificador de la operación.");
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT pg_advisory_xact_lock(hashtextextended({request.ToString()}, 0))", ct);
    }
    private static async Task<EquipmentOperation?> Receipt(InventoryDbContext db, Guid request, string hash, InventoryActor actor, CancellationToken ct)
    {
        var prior = await db.Operations.AsNoTracking().SingleOrDefaultAsync(x => x.RequestId == request, ct);
        if (prior is not null) Require(prior.ActorId == actor.Id && prior.RequestHash == hash, "El identificador ya corresponde a otra solicitud. Actualice antes de continuar.");
        return prior;
    }
    private static EquipmentOperation Audit(Guid request, string hash, InventoryActor actor, string entity, Guid id,
        string action, object? before, object after, string? note = null, DateTimeOffset? effective = null,
        Guid? source = null, Guid? destination = null, string? oldName = null, string? newName = null) => new()
    {
        Id = Guid.NewGuid(), RequestId = request, RequestHash = hash, EntityType = entity, EntityId = id,
        EquipmentId = entity == "Equipment" ? id : null, Action = action, ActorId = actor.Id, ActorName = actor.Name,
        RecordedAtUtc = DateTimeOffset.UtcNow, EffectiveAtUtc = effective, Note = Clean(note),
        BeforeJson = before is null ? null : Json(before), AfterJson = Json(after),
        SourceCustodyId = source, DestinationCustodyId = destination, PreviousName = oldName, NewName = newName
    };
    private static async Task<Equipment> LockedEquipment(InventoryDbContext db, Guid id, CancellationToken ct)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM equipment WHERE \"Id\"={id} FOR UPDATE", ct);
        var equipment = await db.Equipment.Include(x => x.OwnerCompany).Include(x => x.UsingCompany).Include(x => x.OwnerArea).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InventoryValidationException("No se encontró el equipo.");
        Require(equipment.RegistrationState == EquipmentRegistrationState.Definitive, "Las custodias y la clasificación se gestionan después de finalizar el borrador.");
        // Hold catalog labels stable while taking historical snapshots, including unchanged/inactive relations.
        foreach (var companyId in new[] { equipment.OwnerCompanyId, equipment.UsingCompanyId }.Where(x => x.HasValue).Distinct().OrderBy(x => x))
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM companies WHERE \"Id\"={companyId!.Value} FOR SHARE",ct);
        if(equipment.OwnerAreaId is{} areaId)await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM areas WHERE \"Id\"={areaId} FOR SHARE",ct);
        if(equipment.OwnerCompany is not null)await db.Entry(equipment.OwnerCompany).ReloadAsync(ct);
        if(equipment.UsingCompany is not null)await db.Entry(equipment.UsingCompany).ReloadAsync(ct);
        if(equipment.OwnerArea is not null)await db.Entry(equipment.OwnerArea).ReloadAsync(ct);
        return equipment;
    }
    private static async Task CompanyActive(InventoryDbContext db, Guid? id, CancellationToken ct)
    {
        if (id is null) return;
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM companies WHERE \"Id\"={id.Value} FOR SHARE", ct);
        Require(await db.Companies.AnyAsync(x => x.Id == id && x.IsActive, ct), "Seleccione una empresa activa; los datos históricos no se borran.");
    }
    private static async Task<Area> AreaActive(InventoryDbContext db, Guid id, CancellationToken ct)
    {
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM areas WHERE \"Id\"={id} FOR SHARE", ct);
        var area = await db.Areas.Include(x => x.Department).SingleOrDefaultAsync(x => x.Id == id, ct)
            ?? throw new InventoryValidationException("Seleccione un área válida.");
        await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM departments WHERE \"Id\"={area.DepartmentId} FOR SHARE", ct);
        await db.Entry(area.Department).ReloadAsync(ct);
        Require(area.IsActive && area.Department.IsActive, "El área y su departamento deben estar activos.");
        return area;
    }

    public async Task<OperationResult> ClassifyAsync(AssetClassification command, CancellationToken ct = default)
    {
        var actor = await ActorAsync();var hash = Hash(command);
        Require(command.EquipmentId != Guid.Empty, "Seleccione un equipo válido.");
        Require(command.Note?.Length is not > 4000, "La observación supera 4000 caracteres.");
        await using var db = await contexts.CreateDbContextAsync(ct);await using var tx = await db.Database.BeginTransactionAsync(ct);
        await RequestLock(db, command.RequestId, ct);
        var receipt = await Receipt(db, command.RequestId, hash, actor, ct);
        if (receipt is not null) return new(receipt.Id, command.EquipmentId, (await db.Equipment.AsNoTracking().SingleAsync(x => x.Id == command.EquipmentId, ct)).OperationsRevision);
        var equipment = await LockedEquipment(db, command.EquipmentId, ct);
        Require(equipment.OperationsRevision == command.ExpectedRevision, "El equipo cambió. Actualice antes de guardar su clasificación.");
        var changedCompanies = new List<Guid?>();
        if(command.OwnerCompanyId != equipment.OwnerCompanyId) changedCompanies.Add(command.OwnerCompanyId);
        if(command.UsingCompanyId != equipment.UsingCompanyId) changedCompanies.Add(command.UsingCompanyId);
        foreach (var company in changedCompanies.Where(x => x.HasValue).Distinct().OrderBy(x => x)) await CompanyActive(db, company, ct);
        if (command.OwnerAreaId is { } areaId && command.OwnerAreaId != equipment.OwnerAreaId) await AreaActive(db, areaId, ct);
        var before = await Classification(db, equipment, ct);
        equipment.OwnerCompanyId = command.OwnerCompanyId;equipment.UsingCompanyId = command.UsingCompanyId;equipment.OwnerAreaId = command.OwnerAreaId;
        equipment.OperationsRevision++;
        var after = await Classification(db, equipment, ct);
        var op = Audit(command.RequestId, hash, actor, "Equipment", equipment.Id, "Classification", before, after, command.Note);
        db.Operations.Add(op);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
        return new(op.Id, equipment.Id, equipment.OperationsRevision);
    }

    private static async Task<object> Classification(InventoryDbContext db, Equipment e, CancellationToken ct) => new
    {
        e.OwnerCompanyId, OwnerCompany = e.OwnerCompanyId is { } owner ? await db.Companies.Where(x => x.Id == owner).Select(x => x.Name).SingleAsync(ct) : null,
        e.UsingCompanyId, UsingCompany = e.UsingCompanyId is { } user ? await db.Companies.Where(x => x.Id == user).Select(x => x.Name).SingleAsync(ct) : null,
        e.OwnerAreaId, OwnerArea = e.OwnerAreaId is { } area ? await db.Areas.Where(x => x.Id == area).Select(x => x.Name).SingleAsync(ct) : null
    };

    public async Task<OperationResult> ExecuteAsync(CustodyCommand command, CancellationToken ct = default)
    {
        var actor = await ActorAsync();var hash = Hash(command);
        Require(Enum.IsDefined(command.Action) && Enum.IsDefined(command.Kind), "Operación o modalidad inválida.");
        Require(command.Note?.Length is not > 4000, "La observación supera 4000 caracteres.");
        Require(command.EffectiveAtUtc.Offset == TimeSpan.Zero && command.EffectiveAtUtc <= DateTimeOffset.UtcNow, "Indique una fecha efectiva UTC que no sea futura.");
        await using var db = await contexts.CreateDbContextAsync(ct);await using var tx = await db.Database.BeginTransactionAsync(ct);
        await RequestLock(db, command.RequestId, ct);
        var receipt = await Receipt(db, command.RequestId, hash, actor, ct);
        if (receipt is not null) return new(receipt.Id, command.EquipmentId, (await db.Equipment.AsNoTracking().SingleAsync(x => x.Id == command.EquipmentId, ct)).OperationsRevision);
        var equipment = await LockedEquipment(db, command.EquipmentId, ct);
        var current = await db.Custodies.SingleOrDefaultAsync(x => x.EquipmentId == equipment.Id && x.EndedAtUtc == null, ct);
        Require(equipment.OperationsRevision == command.ExpectedRevision && current?.Id == command.ExpectedCustodyId, "La custodia cambió en otra sesión. Actualice antes de continuar.");
        var opens = command.Action is CustodyAction.Assign or CustodyAction.Transfer or CustodyAction.ReconcileAssigned;
        var closes = command.Action is CustodyAction.Return or CustodyAction.Transfer;
        var reconciles = command.Action is CustodyAction.ReconcileAssigned or CustodyAction.ReconcileFree;
        var effective = reconciles ? DateTimeOffset.UtcNow : command.EffectiveAtUtc;
        Require(reconciles || command.OriginalDeliveryAtUtc is null, "La fecha original solo corresponde a conciliación.");
        Require(closes ? current is not null : current is null, closes ? "No existe una custodia vigente que cerrar." : "El equipo ya está ocupado; use Transferir o Liberar.");
        if (current is not null) Require(command.EffectiveAtUtc >= current.StartedAtUtc, "La devolución o transferencia no puede ser anterior a la entrega.");
        var lastEnd = await db.Custodies.Where(x => x.EquipmentId == equipment.Id && x.EndedAtUtc != null).MaxAsync(x => x.EndedAtUtc, ct);
        if (opens && lastEnd is not null) Require(effective >= lastEnd, "La fecha se solapa con la historia registrada.");
        StaffMember? inspector = null;
        if (!reconciles)
        {
            Require(command.ConditionInspectorId.HasValue,"Seleccione la persona que comprobó las condiciones de entrega o devolución.");
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM staff_members WHERE \"Id\"={command.ConditionInspectorId!.Value} FOR SHARE",ct);
            inspector = await db.StaffMembers.SingleOrDefaultAsync(x=>x.Id==command.ConditionInspectorId,ct);
            Require(inspector is { IsActive: true },"La persona que inspeccionó debe existir y estar activa; no es necesariamente el usuario Identity.");
            static bool Condition(string? value) => !string.IsNullOrWhiteSpace(value) && value.Trim().Length<=100;
            if(opens)Require(Condition(command.DeliveryPhysicalCondition)&&Condition(command.DeliveryOperationalCondition),"Indique condición física y operativa observadas al entregar.");
            if(closes)Require(Condition(command.ReturnPhysicalCondition)&&Condition(command.ReturnOperationalCondition),"Indique condición física y operativa observadas al recibir la devolución.");
        }
        EquipmentCustody? next = null;
        if (opens)
        {
            Require(command.WorkerId.HasValue, "Seleccione el trabajador o custodio principal.");
            await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM staff_members WHERE \"Id\"={command.WorkerId!.Value} FOR SHARE", ct);
            var worker = await db.StaffMembers.SingleOrDefaultAsync(x => x.Id == command.WorkerId, ct);
            Require(worker is { IsActive: true, CanReceiveEquipment: true, AreaId: not null }, "El trabajador debe estar activo, habilitado para recibir equipos y tener área.");
            var workerArea = await AreaActive(db, worker!.AreaId!.Value, ct);
            Location? office = null;
            if (command.Kind == CustodyKind.SharedOffice)
            {
                Require(command.SharedLocationId.HasValue, "Una custodia compartida necesita oficina y custodio principal.");
                await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM locations WHERE \"Id\"={command.SharedLocationId!.Value} FOR SHARE", ct);
                office = await db.Locations.Include(x => x.Site).SingleOrDefaultAsync(x => x.Id == command.SharedLocationId, ct);
                if (office is not null)
                {
                    await db.Database.ExecuteSqlInterpolatedAsync($"SELECT 1 FROM sites WHERE \"Id\"={office.SiteId} FOR SHARE", ct);
                    await db.Entry(office.Site).ReloadAsync(ct);
                }
                Require(office is { IsActive: true } && office.Site.IsActive, "Seleccione una oficina/ubicación activa.");
            }
            else Require(command.SharedLocationId is null, "La ubicación compartida solo corresponde a la modalidad compartida.");
            if (current is not null) Require(current.WorkerId != worker.Id || current.Kind != command.Kind || current.SharedLocationId != command.SharedLocationId, "El destino ya es el custodio actual.");
            Require(command.OriginalDeliveryAtUtc is null || (reconciles && command.OriginalDeliveryAtUtc.Value.Offset == TimeSpan.Zero && command.OriginalDeliveryAtUtc <= effective), "La fecha original solo se informa durante conciliación y no puede ser futura respecto de la confirmación.");
            next = new EquipmentCustody
            {
                Id = Guid.NewGuid(), EquipmentId = equipment.Id, WorkerId = worker.Id, WorkerAreaId = workerArea.Id,
                Kind = command.Kind, SharedLocationId = office?.Id, StartedAtUtc = effective,
                IsInitialReconciliation = reconciles, OriginalDeliveryAtUtc = reconciles ? command.OriginalDeliveryAtUtc : effective,
                WorkerName = worker.Name, WorkerAreaName = workerArea.Name, WorkerDepartmentName = workerArea.Department.Name,
                OwnerCompanyName = equipment.OwnerCompany?.Name, UsingCompanyName = equipment.UsingCompany?.Name,
                OwnerAreaName = equipment.OwnerArea?.Name, SharedLocationName = office?.Name, SharedSiteName = office?.Site.Name
            };
        }
        if (closes)
        {
            current!.EndedAtUtc = command.EffectiveAtUtc;current.Closure = command.Action == CustodyAction.Return ? CustodyClosure.Return : CustodyClosure.Transfer;
            await db.SaveChangesAsync(ct); // Release the partial unique index, still within the same transaction.
        }
        if (next is not null) { db.Custodies.Add(next);await db.SaveChangesAsync(ct); }
        if (reconciles) { equipment.NeedsCustodyReconciliation = false;equipment.CustodyReviewedAtUtc = DateTimeOffset.UtcNow; }
        equipment.OperationsRevision++;
        Guid? inspectionId = null;
        if(inspector is not null)
        {
            inspectionId=Guid.NewGuid();
            db.Inspections.Add(new EquipmentInspection
            {
                Id=inspectionId.Value,EquipmentId=equipment.Id,InspectorId=inspector.Id,InspectedAtUtc=command.EffectiveAtUtc,
                PhysicalCondition=Clean(opens?command.DeliveryPhysicalCondition:command.ReturnPhysicalCondition),
                OperationalCondition=Clean(opens?command.DeliveryOperationalCondition:command.ReturnOperationalCondition),
                Notes="Inspección de "+command.Action+" · operación "+command.RequestId
            });
        }
        var classification = await Classification(db, equipment, ct);
        var lastInspection = await db.Inspections.AsNoTracking().Where(x => x.EquipmentId == equipment.Id)
            .OrderByDescending(x => x.InspectedAtUtc).ThenByDescending(x => x.Id)
            .Select(x => new { x.Id, x.InspectedAtUtc, x.PhysicalCondition, x.OperationalCondition }).FirstOrDefaultAsync(ct);
        var op = Audit(command.RequestId, hash, actor, "Equipment", equipment.Id, command.Action.ToString(),
            current is null ? null : new { CustodyId = current.Id, current.WorkerId, current.WorkerName, current.WorkerAreaId, current.WorkerAreaName },
            new { CustodyId = next?.Id, WorkerId = next?.WorkerId, Worker = next?.WorkerName, WorkerAreaId = next?.WorkerAreaId,
                WorkerArea = next?.WorkerAreaName, OfficeId = next?.SharedLocationId, Office = next?.SharedLocationName, classification,
                Availability = next is null ? "Libre" : "Ocupado", LastRecordedInspection = lastInspection,
                RecordedInspectionId=inspectionId,InspectorId=inspector?.Id,Inspector=inspector?.Name,
                DeliveryPhysicalCondition=opens&&!reconciles?Clean(command.DeliveryPhysicalCondition):null,DeliveryOperationalCondition=opens&&!reconciles?Clean(command.DeliveryOperationalCondition):null,
                ReturnPhysicalCondition=closes?Clean(command.ReturnPhysicalCondition):null,ReturnOperationalCondition=closes?Clean(command.ReturnOperationalCondition):null,equipment.OperationsRevision },
            command.Note, effective, current?.Id, next?.Id);
        db.Operations.Add(op);await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
        return new(op.Id, equipment.Id, equipment.OperationsRevision);
    }
}
