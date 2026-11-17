using InventarioCPR.Application.Inventory;
using InventarioCPR.Infrastructure.Persistence;
using InventarioCPR.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace InventarioCPR.Application.Assignments;

public sealed partial class AssignmentService
{
    public async Task<IReadOnlyList<DirectoryOption>> InspectorOptionsAsync(CancellationToken ct=default)
    {
        await access.EnsureReadAsync();await using var db=await contexts.CreateDbContextAsync(ct);
        return await db.StaffMembers.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.Name).Take(250)
            .Select(x=>new DirectoryOption(x.Id,x.Name,true,x.PersonnelCode)).ToListAsync(ct);
    }
    public async Task<IReadOnlyList<DirectoryOption>> EquipmentOptionsAsync(string? search, CancellationToken ct=default)
    {
        await access.EnsureReadAsync();await using var db=await contexts.CreateDbContextAsync(ct);
        var query=db.Equipment.AsNoTracking().Where(x=>x.RegistrationState==EquipmentRegistrationState.Definitive);
        if(Clean(search)is{}term)query=query.Where(x=>x.Code.ToLower().Contains(term.ToLower())||x.Name.ToLower().Contains(term.ToLower()));
        return await query.OrderBy(x=>x.CodeNormalized).Take(50).Select(x=>new DirectoryOption(x.Id,x.Code+" · "+x.Name,true,null)).ToListAsync(ct);
    }
    public async Task<AssignmentSummary?> SummaryAsync(Guid id,int page=0,CancellationToken ct=default)
    {
        await access.EnsureReadAsync();await using var db=await contexts.CreateDbContextAsync(ct);return await ReadSummaryAsync(db,id,page,ct);
    }
    public static async Task<AssignmentSummary?> ReadSummaryAsync(InventoryDbContext db,Guid id,int page,CancellationToken ct)
    {
        var e=await db.Equipment.AsNoTracking().Include(x=>x.OwnerCompany).Include(x=>x.UsingCompany).Include(x=>x.OwnerArea).SingleOrDefaultAsync(x=>x.Id==id,ct);
        if(e is null)return null;
        var current=await db.Custodies.AsNoTracking().Include(x=>x.Worker).ThenInclude(x=>x.Area).Include(x=>x.SharedLocation).SingleOrDefaultAsync(x=>x.EquipmentId==id&&x.EndedAtUtc==null,ct);
        page=Math.Max(0,page);
        var histories=await db.Custodies.AsNoTracking().Where(x=>x.EquipmentId==id).OrderByDescending(x=>x.StartedAtUtc).ThenByDescending(x=>x.Id).Skip(page*25).Take(26)
            .Select(x=>new CustodyHistoryItem(x.Id,x.WorkerId,x.WorkerName,x.WorkerAreaName,x.WorkerDepartmentName,x.Kind,x.SharedLocationName,x.StartedAtUtc,x.EndedAtUtc,x.Closure==null?null:x.Closure.ToString(),x.OwnerCompanyName,x.UsingCompanyName,x.OwnerAreaName,x.IsInitialReconciliation,x.OriginalDeliveryAtUtc)).ToListAsync(ct);
        var ops=await db.Operations.AsNoTracking().Where(x=>x.EquipmentId==id).OrderByDescending(x=>x.RecordedAtUtc).ThenByDescending(x=>x.Id).Skip(page*25).Take(26)
            .Select(x=>new OperationHistoryItem(x.Id,x.Action,x.ActorName,x.RecordedAtUtc,x.EffectiveAtUtc,x.Note,x.BeforeJson,x.AfterJson)).ToListAsync(ct);
        return new(id,e.OperationsRevision,e.OwnerCompanyId,e.OwnerCompany?.Name,e.UsingCompanyId,e.UsingCompany?.Name,e.OwnerAreaId,e.OwnerArea?.Name,
            current?.Id,current?.WorkerId,current?.Worker.Name,current?.Worker.Area?.Name,current?.SharedLocation?.Name,current?.StartedAtUtc,e.NeedsCustodyReconciliation,histories.Take(25).ToList(),ops.Take(25).ToList(),histories.Count>25||ops.Count>25);
    }
}
