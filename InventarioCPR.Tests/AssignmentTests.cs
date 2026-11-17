using InventarioCPR.Application.Assignments;
using InventarioCPR.Application.Inventory;
using InventarioCPR.Application.Security;
using InventarioCPR.Infrastructure.Persistence;
using InventarioCPR.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Xunit;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using InventarioCPR.Infrastructure.Security;

namespace InventarioCPR.Tests;

public sealed class AssignmentLocalFactAttribute : FactAttribute
{
    public AssignmentLocalFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("INVENTARIOCPR_ASSIGNMENTS_LOCAL_POSTGRES")))
            Skip = "Requiere PostgreSQL local desechable; no usa Neon ni secretos.";
    }
}

public sealed class AssignmentTests
{
    [AssignmentLocalFact]
    public async Task MovementRequiresObservedConditionsAndReturnKeepsOriginalDeliveryFacts()
    {
        await using var f=await Fixture.Create();
        await Assert.ThrowsAsync<InventoryValidationException>(()=>f.Service.ExecuteAsync(f.Command(CustodyAction.Assign,f.A) with{DeliveryOperationalCondition=null}));
        await Assert.ThrowsAsync<InventoryValidationException>(()=>f.Service.ExecuteAsync(f.Command(CustodyAction.Assign,f.A) with{ConditionInspectorId=f.Access.Actor.Id}));
        await f.Service.ExecuteAsync(f.Command(CustodyAction.Assign,f.A));var s=(await f.Service.SummaryAsync(f.Equipment))!;
        await Assert.ThrowsAsync<InventoryValidationException>(()=>f.Service.ExecuteAsync(f.Command(CustodyAction.Transfer,f.B,s) with{ReturnPhysicalCondition=null}));
        await f.Service.ExecuteAsync(f.Command(CustodyAction.Return,null,s) with{ReturnPhysicalCondition="Dañado ficticio",ReturnOperationalCondition="No operativo"});
        var details=(await f.Inventory.GetAsync(f.Equipment))!;Assert.Equal("No operativo",details.Input.OperationalCondition);Assert.Equal("Dañado ficticio",details.Input.PhysicalCondition);
        using var db=f.Db();Assert.Equal(2,await db.Inspections.CountAsync());
        var observations=await db.Inspections.OrderBy(x=>x.InspectedAtUtc).ToListAsync();Assert.Equal("Funcional",observations[0].OperationalCondition);Assert.Equal("No operativo",observations[1].OperationalCondition);
        var history=(await f.Service.SummaryAsync(f.Equipment))!;Assert.Equal(2,history.Operations.Count);Assert.Contains("Funcional",history.Operations.Last().AfterJson);
        Assert.DoesNotContain((await f.Inventory.ListAsync(scope:new(FreeFunctional:true))).Rows,row=>row.Id==f.Equipment);
    }

    [AssignmentLocalFact]
    public async Task ExistingIdentityRolesResolveTrustedActorsAndRejectRevokedPermissions()
    {
        await using var f=await Fixture.Create();var services=new ServiceCollection().AddLogging();services.AddHttpContextAccessor();
        services.AddDbContext<AccessDbContext>(o=>o.UseNpgsql(f.Connection,n=>n.MigrationsHistoryTable("__AuthMigrationsHistory")));
        services.AddIdentity<AccessUser,IdentityRole<Guid>>().AddEntityFrameworkStores<AccessDbContext>().AddDefaultTokenProviders();services.AddAuthorization(InventoryPolicies.Configure);
        using var provider=services.BuildServiceProvider();using var scope=provider.CreateScope();await scope.ServiceProvider.GetRequiredService<AccessDbContext>().Database.MigrateAsync();
        var users=scope.ServiceProvider.GetRequiredService<UserManager<AccessUser>>();var roles=scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        foreach(var role in new[]{InventoryPolicies.Administrator,InventoryPolicies.Operator,InventoryPolicies.Viewer})Assert.True((await roles.CreateAsync(new(role))).Succeeded);
        foreach(var role in new[]{InventoryPolicies.Administrator,InventoryPolicies.Operator,InventoryPolicies.Viewer})
        {
            var user=new AccessUser{UserName="fixture-"+Guid.NewGuid().ToString("N"),Email=Guid.NewGuid()+"@example.invalid"};Assert.True((await users.CreateAsync(user)).Succeeded);Assert.True((await users.AddToRoleAsync(user,role)).Succeeded);
            var principal=await scope.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<AccessUser>>().CreateAsync(user);
            var access=new IdentityInventoryAccess(new Auth(principal),scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Authorization.IAuthorizationService>(),users,scope.ServiceProvider.GetRequiredService<SignInManager<AccessUser>>());
            Assert.True(await access.CanReadAsync());
            if(role==InventoryPolicies.Viewer){await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>access.RequireWriteActorAsync());}
            else{Assert.Equal(user.Id,(await access.RequireWriteActorAsync()).Id);Assert.True((await users.RemoveFromRoleAsync(user,role)).Succeeded);await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>access.RequireWriteActorAsync());}
        }
    }

    private sealed class Auth(ClaimsPrincipal principal):AuthenticationStateProvider { public override Task<AuthenticationState> GetAuthenticationStateAsync()=>Task.FromResult(new AuthenticationState(principal)); }

    [AssignmentLocalFact]
    public async Task ReviewedIdempotentScriptExecutesAndCanBeRepeatedOnlyInDisposableDatabase()
    {
        await using var f=await Fixture.Create();using var db=f.Db();var migrator=db.GetService<IMigrator>();
        await migrator.MigrateAsync("20261112200209_EquipmentDraftFinalization");
        var script=migrator.GenerateScript("20261112200209_EquipmentDraftFinalization","20261116181150_CompaniesAndEquipmentCustodies",MigrationsSqlGenerationOptions.Idempotent);
        Assert.DoesNotContain("INSERT INTO companies",script);Assert.DoesNotContain("access_users",script);Assert.DoesNotContain("security.",script);
        await db.Database.ExecuteSqlRawAsync(script);await db.Database.ExecuteSqlRawAsync(script);
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());Assert.Equal(3,await db.Equipment.CountAsync());Assert.Equal(2,await db.StaffMembers.CountAsync());
        Assert.Empty(await db.Companies.ToListAsync());Assert.Empty(await db.Custodies.ToListAsync());Assert.Empty(await db.Operations.ToListAsync());
        Assert.All(await db.Equipment.ToListAsync(),equipment=>Assert.True(equipment.NeedsCustodyReconciliation));
    }

    [AssignmentLocalFact]
    public async Task PaginationCountsAllRowsAndAuditFailureRollsBackClosedAndNewCustody()
    {
        await using var f=await Fixture.Create();
        using(var db=f.Db())
        {
            var category=await db.Categories.SingleAsync();
            for(var i=0;i<58;i++)db.Equipment.Add(new Equipment{Id=Guid.NewGuid(),Code="PAGED_"+i.ToString("D3"),Name="Activo paginado ficticio",Category=category,RegistrationState=EquipmentRegistrationState.Definitive,RequiresRegistrationPhoto=false});
            await db.SaveChangesAsync();
        }
        var page=await f.Inventory.ListAsync(page:1);Assert.Equal(25,page.Rows.Count);Assert.Equal(60,page.Total);Assert.Equal(60,page.Statistics!.Total);Assert.Equal(60,page.Statistics.Free);
        await f.Service.ExecuteAsync(f.Command(CustodyAction.Assign,f.A));var before=(await f.Service.SummaryAsync(f.Equipment))!;
        using(var db=f.Db())await db.Database.ExecuteSqlRawAsync("CREATE FUNCTION fail_custody_test_audit() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'Fallo controlado de auditoría local'; END $$; CREATE TRIGGER fail_custody_test_audit BEFORE INSERT ON equipment_operations FOR EACH ROW EXECUTE FUNCTION fail_custody_test_audit();");
        await Assert.ThrowsAsync<DbUpdateException>(()=>f.Service.ExecuteAsync(f.Command(CustodyAction.Transfer,f.B,before)));
        var after=(await f.Service.SummaryAsync(f.Equipment))!;Assert.Equal(before.CustodyId,after.CustodyId);Assert.Equal(before.Revision,after.Revision);Assert.Single(after.History);Assert.Single(after.Operations);Assert.Null(after.History.Single().ToUtc);
    }
    [AssignmentLocalFact]
    public async Task DatabaseUniquenessAuditGuardsAndMigrationPreserveLegacyRecords()
    {
        await using var f=await Fixture.Create();
        await f.Service.ExecuteAsync(f.Command(CustodyAction.Assign,f.A));
        await using(var db=f.Db())
        {
            var current=await db.Custodies.AsNoTracking().SingleAsync();
            db.Custodies.Add(new EquipmentCustody{Id=Guid.NewGuid(),EquipmentId=f.Equipment,WorkerId=f.B,WorkerAreaId=f.AreaB,Kind=CustodyKind.Individual,StartedAtUtc=DateTimeOffset.UtcNow,WorkerName="B ficticio",WorkerAreaName="Área ficticia",WorkerDepartmentName="Departamento ficticio"});
            var error=await Assert.ThrowsAsync<DbUpdateException>(()=>db.SaveChangesAsync());Assert.Contains(((PostgresException)error.InnerException!).SqlState,new[]{PostgresErrorCodes.UniqueViolation,PostgresErrorCodes.RaiseException});
        }
        await using(var db=f.Db())
        {
            await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlRawAsync("UPDATE equipment_operations SET \"ActorName\"='alteración'"));
            await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlRawAsync("DELETE FROM equipment_custodies"));
            await Assert.ThrowsAsync<PostgresException>(()=>db.Database.ExecuteSqlRawAsync("UPDATE equipment_custodies SET \"WorkerName\"='alteración'"));
            await Assert.ThrowsAsync<PostgresException>(()=>db.GetService<IMigrator>().MigrateAsync("20261112200209_EquipmentDraftFinalization"));
            Assert.Equal(3,await db.Equipment.CountAsync());Assert.Equal(1,await db.Custodies.CountAsync());
        }
        // Separate disposable database: actual upgrade path with legacy rows and no company preload.
        var options=new DbContextOptionsBuilder<InventoryDbContext>().UseNpgsql(f.Connection).Options;
        await using var db2=new InventoryDbContext(options);
        Assert.Empty(await db2.Companies.ToListAsync());
        Assert.All(await db2.Equipment.ToListAsync(),e=>Assert.Null(e.OwnerCompanyId));
        Assert.All(await db2.StaffMembers.ToListAsync(),worker=>Assert.True(worker.IsActive));
    }

    [AssignmentLocalFact]
    public async Task UpgradeMarksLegacyForReconciliationWithoutInventingCompaniesOrCustody()
    {
        await using var f=await Fixture.Create(legacy:true);using var db=f.Db();
        var old=await db.Equipment.SingleAsync(x=>x.Code=="LEGACY_CUSTODY_CHECK");
        Assert.True(old.NeedsCustodyReconciliation);Assert.Null(old.OwnerCompanyId);Assert.Null(old.UsingCompanyId);Assert.Null(old.OwnerAreaId);
        Assert.Equal(EquipmentRegistrationState.Definitive,old.RegistrationState);Assert.False(old.RequiresRegistrationPhoto);
        Assert.Empty(await db.Companies.ToListAsync());Assert.Empty(await db.Custodies.ToListAsync());
        var verifier=await db.StaffMembers.SingleAsync(x=>x.Name=="Verificador previo ficticio");Assert.True(verifier.IsActive);Assert.False(verifier.CanReceiveEquipment);
        Assert.Null(verifier.AreaId);Assert.Equal(1,(await f.Inventory.ListAsync()).Statistics!.ReconciliationPending);
    }

    [AssignmentLocalFact]
    public async Task DerivedAvailabilityTransferReturnHistoryAndStaffWithoutIdentity()
    {
        await using var f = await Fixture.Create();
        Assert.Equal(new InventoryStatistics(2,2,0,0), (await f.Inventory.ListAsync()).Statistics);
        var assign = f.Command(CustodyAction.Assign,f.A);
        var result = await f.Service.ExecuteAsync(assign);
        Assert.Equal(1,result.Revision);
        Assert.Equal(EquipmentAvailability.Occupied,(await f.Service.SummaryAsync(f.Equipment))!.Availability);
        var s = (await f.Service.SummaryAsync(f.Equipment))!;
        var transfer = f.Command(CustodyAction.Transfer,f.B,s);
        await f.Service.ExecuteAsync(transfer);
        var moved = (await f.Service.SummaryAsync(f.Equipment))!;
        Assert.Equal(EquipmentAvailability.Occupied,moved.Availability);
        Assert.Equal(f.B,moved.WorkerId);
        await f.Service.ExecuteAsync(f.Command(CustodyAction.Return,null,moved));
        var ended=(await f.Service.SummaryAsync(f.Equipment))!;
        Assert.Equal(EquipmentAvailability.Free,ended.Availability);
        Assert.Equal(2,ended.History.Count);
        Assert.All(ended.History,x=>Assert.NotNull(x.ToUtc));
        Assert.Equal(3,ended.Operations.Count);
        Assert.All(ended.Operations,x=>Assert.Equal("Operador ficticio",x.Actor));
        using var db=f.Db();Assert.False(await db.Database.SqlQueryRaw<bool>("SELECT to_regclass('public.access_users') IS NOT NULL AS \"Value\"").SingleAsync());
    }

    [AssignmentLocalFact]
    public async Task IndependentCompaniesAreasAndHistoricalLabelsSurviveRenamesAndInactivity()
    {
        await using var f=await Fixture.Create();
        var owner=await f.Company("Propietaria ficticia");var user=await f.Company("Usuaria ficticia");
        var classification=new AssetClassification(Guid.NewGuid(),f.Equipment,0,owner,user,f.AreaA,"Clasificación inicial");
        await f.Service.ClassifyAsync(classification);
        var s=(await f.Service.SummaryAsync(f.Equipment))!;
        await f.Service.ExecuteAsync(f.Command(CustodyAction.Assign,f.B,s));
        s=(await f.Service.SummaryAsync(f.Equipment))!;
        Assert.Equal("Área ficticia A",s.OwnerArea);Assert.Equal("Área ficticia B",s.WorkerArea);
        await f.Service.ClassifyAsync(new(Guid.NewGuid(),f.Equipment,s.Revision,owner,null,f.AreaA,"Cambiar solo usuaria"));
        var changed=(await f.Service.SummaryAsync(f.Equipment))!;
        Assert.Equal(owner,changed.OwnerCompanyId);Assert.Null(changed.UsingCompanyId);Assert.Equal(s.CustodyId,changed.CustodyId);
        var entry=(await f.Service.GetDirectoryEntryAsync(DirectoryKind.Company,owner))!;
        await f.Service.SaveDirectoryAsync(new(Guid.NewGuid(),owner,DirectoryKind.Company,"Propietaria renombrada",false,entry.Revision));
        var worker=(await f.Service.GetDirectoryEntryAsync(DirectoryKind.Worker,f.B))!;
        await f.Service.SaveDirectoryAsync(new(Guid.NewGuid(),f.B,DirectoryKind.Worker,"Trabajador renombrado",false,worker.Revision,f.AreaA,CanReceiveEquipment:false));
        var history=(await f.Service.SummaryAsync(f.Equipment))!;
        Assert.Equal("Propietaria ficticia",history.History.Single().OwnerCompany);
        Assert.Equal("Usuaria ficticia",history.History.Single().UsingCompany);
        Assert.Equal("Trabajador ficticio B",history.History.Single().Worker);
        Assert.Equal("Área ficticia B",history.History.Single().WorkerArea);
        Assert.Equal("Propietaria renombrada",history.OwnerCompany);
        await f.Service.ExecuteAsync(f.Command(CustodyAction.Return,null,history)); // Inactive workers retain history and can return.
        Assert.Single(await f.Service.CatalogHistoryAsync(DirectoryKind.Worker,f.B));
        Assert.DoesNotContain(await f.Service.OptionsAsync(DirectoryKind.Worker),x=>x.Id==f.B);
    }

    [AssignmentLocalFact]
    public async Task ConcurrentAssignmentsHaveOneWinnerAndRetriesDoNotCloseNewCustody()
    {
        await using var f=await Fixture.Create();
        async Task<bool> Attempt(CustodyCommand command){try{await f.Service.ExecuteAsync(command);return true;}catch(InventoryValidationException){return false;}}
        var successes=await Task.WhenAll(Attempt(f.Command(CustodyAction.Assign,f.A)),Attempt(f.Command(CustodyAction.Assign,f.B)));
        Assert.Single(successes,x=>x);
        var s=(await f.Service.SummaryAsync(f.Equipment))!;
        var close=f.Command(CustodyAction.Return,null,s);
        var result=await f.Service.ExecuteAsync(close);var retry=await f.Service.ExecuteAsync(close);Assert.Equal(result.OperationId,retry.OperationId);
        await Assert.ThrowsAsync<InventoryValidationException>(()=>f.Service.ExecuteAsync(close with{Note="Contenido diferente"}));
        s=(await f.Service.SummaryAsync(f.Equipment))!;await f.Service.ExecuteAsync(f.Command(CustodyAction.Assign,f.A,s));
        await f.Service.ExecuteAsync(close);Assert.NotNull((await f.Service.SummaryAsync(f.Equipment))!.CustodyId);
        using var db=f.Db();Assert.Equal(1,await db.Custodies.CountAsync(x=>x.EndedAtUtc==null));Assert.Equal(3,await db.Operations.CountAsync());
    }

    [AssignmentLocalFact]
    public async Task FailedTransferRollsBackAndSharedCustodyHasOnePrincipal()
    {
        await using var f=await Fixture.Create();await f.Service.ExecuteAsync(f.Command(CustodyAction.Assign,f.A));
        var s=(await f.Service.SummaryAsync(f.Equipment))!;
        await Assert.ThrowsAsync<InventoryValidationException>(()=>f.Service.ExecuteAsync(f.Command(CustodyAction.Transfer,Guid.NewGuid(),s)));
        Assert.Equal(s.CustodyId,(await f.Service.SummaryAsync(f.Equipment))!.CustodyId);
        await Assert.ThrowsAsync<InventoryValidationException>(()=>f.Service.ExecuteAsync(f.Command(CustodyAction.Transfer,f.B,s) with{Kind=CustodyKind.SharedOffice}));
        await f.Service.ExecuteAsync(f.Command(CustodyAction.Transfer,f.B,s) with{Kind=CustodyKind.SharedOffice,SharedLocationId=f.Office});
        var shared=(await f.Service.SummaryAsync(f.Equipment))!;Assert.Equal(EquipmentAvailability.Occupied,shared.Availability);Assert.Equal("Oficina ficticia",shared.Office);
        using var db=f.Db();Assert.Equal(1,await db.Custodies.CountAsync(x=>x.EndedAtUtc==null));
        var closed=await db.Custodies.SingleAsync(x=>x.EndedAtUtc!=null);closed.WorkerName="No permitido";
        await Assert.ThrowsAsync<InvalidOperationException>(()=>db.SaveChangesAsync());
    }

    [AssignmentLocalFact]
    public async Task CompanyScopedCountsCombinedFiltersAndFunctionalOnlyUseDefinitives()
    {
        await using var f=await Fixture.Create();var company=await f.Company("Empresa ficticia filtro");
        await f.Service.ClassifyAsync(new(Guid.NewGuid(),f.Equipment,0,company,null,f.AreaA,null));
        using(var db=f.Db())
        {
            db.Inspections.Add(new(){Id=Guid.NewGuid(),EquipmentId=f.Equipment,InspectorId=f.A,InspectedAtUtc=DateTimeOffset.UtcNow,OperationalCondition="Funcional"});
            db.Inspections.Add(new(){Id=Guid.NewGuid(),EquipmentId=f.Other,InspectorId=f.A,InspectedAtUtc=DateTimeOffset.UtcNow,OperationalCondition="Averiado"});await db.SaveChangesAsync();
        }
        var scope=new InventoryScope(OwnerCompanyId:company,OwnerAreaId:f.AreaA);
        var list=await f.Inventory.ListAsync("ACTIVO",scope:scope);Assert.Equal(1,list.Total);Assert.Equal(1,list.Statistics!.Free);
        Assert.Single((await f.Inventory.ListAsync(scope:new(FreeFunctional:true))).Rows);
        var summary=(await f.Service.SummaryAsync(f.Equipment))!;await f.Service.ExecuteAsync(f.Command(CustodyAction.Assign,f.B,summary));
        list=await f.Inventory.ListAsync(scope:scope with{Availability=EquipmentAvailability.Free});Assert.Empty(list.Rows);Assert.Equal(1,list.Statistics!.Occupied);
        Assert.Empty((await f.Inventory.ListAsync(scope:new(FreeFunctional:true))).Rows);
        Assert.Equal(1,(await f.Inventory.ListAsync(scope:scope with{WorkerId=f.B})).Total);
        Assert.Equal(1,(await f.Inventory.ListAsync(scope:new(WithoutOwnerCompany:true))).Total);
        Assert.Equal(1,(await f.Inventory.ListAsync(draftsOnly:true)).Total);
    }

    [AssignmentLocalFact]
    public async Task ReconciliationNeverCreatesThirdAvailabilityAndPreservesUnknownDeliveryDate()
    {
        await using var f=await Fixture.Create();using(var db=f.Db()){var e=await db.Equipment.SingleAsync(x=>x.Id==f.Equipment);e.NeedsCustodyReconciliation=true;await db.SaveChangesAsync();}
        var s=(await f.Service.SummaryAsync(f.Equipment))!;Assert.True(s.NeedsReconciliation);Assert.Equal(EquipmentAvailability.Free,s.Availability);
        await f.Service.ExecuteAsync(f.Command(CustodyAction.ReconcileAssigned,f.A,s));
        s=(await f.Service.SummaryAsync(f.Equipment))!;Assert.False(s.NeedsReconciliation);Assert.Null(s.History.Single().OriginalDeliveryAtUtc);Assert.True(s.History.Single().Reconciled);
        await f.Service.ExecuteAsync(f.Command(CustodyAction.Return,null,s));s=(await f.Service.SummaryAsync(f.Equipment))!;
        await f.Service.ExecuteAsync(f.Command(CustodyAction.ReconcileFree,null,s));Assert.Equal(EquipmentAvailability.Free,(await f.Service.SummaryAsync(f.Equipment))!.Availability);
        Assert.Equal(2,Enum.GetValues<EquipmentAvailability>().Length);
    }

    [AssignmentLocalFact]
    public async Task DraftsUnauthorizedInactiveWorkersAndStaleRequestsAreRejected()
    {
        await using var f=await Fixture.Create();
        await Assert.ThrowsAsync<InventoryValidationException>(()=>f.Service.ExecuteAsync(f.Command(CustodyAction.Assign,f.A) with{EquipmentId=f.Draft}));
        f.Access.Write=false;await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>f.Service.ExecuteAsync(f.Command(CustodyAction.Assign,f.A)));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>f.Company("No autorizada"));f.Access.Write=true;
        var worker=(await f.Service.GetDirectoryEntryAsync(DirectoryKind.Worker,f.A))!;await f.Service.SaveDirectoryAsync(new(Guid.NewGuid(),f.A,DirectoryKind.Worker,worker.Name,false,worker.Revision,f.AreaA,CanReceiveEquipment:true));
        await Assert.ThrowsAsync<InventoryValidationException>(()=>f.Service.ExecuteAsync(f.Command(CustodyAction.Assign,f.A)));
        await f.Service.ExecuteAsync(f.Command(CustodyAction.Assign,f.B));
        await Assert.ThrowsAsync<InventoryValidationException>(()=>f.Service.ExecuteAsync(f.Command(CustodyAction.Return,null)));
        var s=(await f.Service.SummaryAsync(f.Equipment))!;
        await Assert.ThrowsAsync<InventoryValidationException>(()=>f.Service.ExecuteAsync(f.Command(CustodyAction.Return,null,s) with{EffectiveAtUtc=DateTimeOffset.UtcNow.AddDays(-1)}));
    }

    [AssignmentLocalFact]
    public async Task GeneralCategoryDoesNotRequireComputerFieldsAndCompanyUsageCanChangeWhileFree()
    {
        await using var f=await Fixture.Create();using(var db=f.Db())
        {
            var category=new EquipmentCategory{Id=Guid.NewGuid(),Name="Mueble ficticio de arquitectura",TechnicalProfile="General"};db.Categories.Add(category);
            (await db.Equipment.SingleAsync(x=>x.Id==f.Other)).Category=category;await db.SaveChangesAsync();
        }
        var user=await f.Company("Usuaria ficticia independiente");
        await f.Service.ClassifyAsync(new(Guid.NewGuid(),f.Other,0,null,user,null,"Uso sin custodia"));
        var s=(await f.Service.SummaryAsync(f.Other))!;Assert.Null(s.OwnerCompanyId);Assert.Equal(user,s.UsingCompanyId);Assert.Equal(EquipmentAvailability.Free,s.Availability);
        Assert.Equal(1,(await f.Inventory.ListAsync(scope:new(UsingCompanyId:user))).Total);
        await f.Service.ExecuteAsync(f.Command(CustodyAction.Assign,f.A,s) with{EquipmentId=f.Other});Assert.Equal(EquipmentAvailability.Occupied,(await f.Service.SummaryAsync(f.Other))!.Availability);
    }

    internal sealed class TestAccess:IInventoryAccess
    {
        public bool Write=true;public InventoryActor Actor=new(Guid.NewGuid(),"Operador ficticio");
        public Task<bool> CanReadAsync()=>Task.FromResult(true);public Task<bool> CanWriteAsync()=>Task.FromResult(Write);public Task EnsureReadAsync()=>Task.CompletedTask;
        public Task EnsureWriteAsync()=>Write?Task.CompletedTask:throw new UnauthorizedAccessException();
        public Task<InventoryActor> RequireWriteActorAsync()=>Write?Task.FromResult(Actor):throw new UnauthorizedAccessException();
    }
    internal sealed class Fixture:IAsyncDisposable
    {
        public Guid Equipment=Guid.NewGuid(),Other=Guid.NewGuid(),Draft=Guid.NewGuid(),A=Guid.NewGuid(),B=Guid.NewGuid(),AreaA=Guid.NewGuid(),AreaB=Guid.NewGuid(),Office=Guid.NewGuid();
        public string Connection="";string admin="",database="";public TestAccess Access=new();public AssignmentService Service=null!;public InventoryService Inventory=null!;
        public InventoryDbContext Db()=>new(new DbContextOptionsBuilder<InventoryDbContext>().UseNpgsql(Connection).Options);
        public static async Task<Fixture> Create(bool legacy=false)
        {
            var f=new Fixture();var cs=new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("INVENTARIOCPR_ASSIGNMENTS_LOCAL_POSTGRES"));
            if(cs.Host is not("127.0.0.1" or "localhost" or "::1")||cs.Database!="postgres")throw new InvalidOperationException("Solo base desechable local.");
            cs.Pooling=false;f.admin=cs.ConnectionString;f.database="inventariocpr_custody_test_"+Guid.NewGuid().ToString("N");await using var c=new NpgsqlConnection(f.admin);await c.OpenAsync();await using(var cmd=new NpgsqlCommand($"CREATE DATABASE \"{f.database}\"",c))await cmd.ExecuteNonQueryAsync();cs.Database=f.database;f.Connection=cs.ConnectionString;
            using(var db=f.Db())
            {
                if(legacy)
                {
                    await db.GetService<IMigrator>().MigrateAsync("20261112200209_EquipmentDraftFinalization");
                    var categoryId=Guid.NewGuid();var legacyId=Guid.NewGuid();var verifierId=Guid.NewGuid();
                    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO equipment_categories (\"Id\",\"Name\") VALUES ({categoryId},'Anterior ficticia')");
                    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO equipment (\"Id\",\"Code\",\"Name\",\"CategoryId\",\"RegistrationState\",\"RequiresRegistrationPhoto\") VALUES ({legacyId},'LEGACY_CUSTODY_CHECK','Anterior sin asignación digital',{categoryId},'Definitive',false)");
                    await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO staff_members (\"Id\",\"Name\") VALUES ({verifierId},'Verificador previo ficticio')");
                }
                await db.Database.MigrateAsync();var department=new Department{Id=Guid.NewGuid(),Name="Departamento ficticio"};db.Departments.Add(department);db.Areas.AddRange(new Area{Id=f.AreaA,Name="Área ficticia A",Department=department},new Area{Id=f.AreaB,Name="Área ficticia B",Department=department});
                db.StaffMembers.AddRange(new StaffMember{Id=f.A,Name="Trabajador ficticio A",AreaId=f.AreaA,CanReceiveEquipment=true},new StaffMember{Id=f.B,Name="Trabajador ficticio B",AreaId=f.AreaB,CanReceiveEquipment=true});
                var category=new EquipmentCategory{Id=Guid.NewGuid(),Name="Monitor"};db.Categories.Add(category);
                db.Equipment.AddRange(new Equipment{Id=f.Equipment,Code="ACTIVO_A",Name="Monitor ficticio A",Category=category,RegistrationState=EquipmentRegistrationState.Definitive,RequiresRegistrationPhoto=false},new Equipment{Id=f.Other,Code="ACTIVO_B",Name="Monitor ficticio B",Category=category,RegistrationState=EquipmentRegistrationState.Definitive,RequiresRegistrationPhoto=false},new Equipment{Id=f.Draft,Code="DRAFT_TEST",Name="Borrador ficticio",Category=category});
                var site=new Site{Id=Guid.NewGuid(),Name="Sede ficticia"};db.Sites.Add(site);db.Locations.Add(new Location{Id=f.Office,Name="Oficina ficticia",Site=site});await db.SaveChangesAsync();
            }
            var factory=new PooledDbContextFactory<InventoryDbContext>(new DbContextOptionsBuilder<InventoryDbContext>().UseNpgsql(f.Connection).Options);
            var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{["Inventory:ProductionWritesEnabled"]="true"}).Build();f.Service=new(factory,f.Access,config);f.Inventory=new(factory,config,f.Access);return f;
        }
        public CustodyCommand Command(CustodyAction action,Guid? worker,AssignmentSummary? summary=null)=>new(Guid.NewGuid(),summary?.EquipmentId??Equipment,summary?.Revision??0,summary?.CustodyId,action,worker,CustodyKind.Individual,null,DateTimeOffset.UtcNow,"Operación ficticia",ConditionInspectorId:worker??A,DeliveryPhysicalCondition:"Buena ficticia",DeliveryOperationalCondition:"Funcional",ReturnPhysicalCondition:"Buena ficticia",ReturnOperationalCondition:"Funcional");
        public Task<Guid> Company(string name)=>Service.SaveDirectoryAsync(new(Guid.NewGuid(),Guid.NewGuid(),DirectoryKind.Company,name,true,0));
        public async ValueTask DisposeAsync(){await using var c=new NpgsqlConnection(admin);await c.OpenAsync();await using var cmd=new NpgsqlCommand($"DROP DATABASE \"{database}\" WITH (FORCE)",c);await cmd.ExecuteNonQueryAsync();}
    }
}
