using InventarioCPR.Infrastructure.Persistence;
using InventarioCPR.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using System.Globalization;
using InventarioCPR.Application.Photos;
using InventarioCPR.Application.Security;
using InventarioCPR.Application.Assignments;

namespace InventarioCPR.Application.Inventory;

public sealed record EquipmentDetails(Guid Id, EquipmentInput Input, IReadOnlyList<string> History, string? PhotoUrl,
    bool IsDraft = false, int RegistrationRevision = 0, bool RequiresPhoto = false, AssignmentSummary? Assignment = null);

public sealed partial class InventoryService(IDbContextFactory<InventoryDbContext> contexts, IConfiguration configuration,
    IInventoryAccess access, IPhotoStorage? photoStorage = null)
{
    public const int PageSize = 25;
    public bool CanWrite => configuration["NeonDevelopmentApp:BranchConfirmed"] == "test-migracion-inicial" ||
        configuration.GetValue<bool>("Inventory:ProductionWritesEnabled");
    public async Task<bool> CanWriteAsync() => CanWrite && await access.CanWriteAsync();
    private static readonly IReadOnlyDictionary<string, string> Categories = new Dictionary<string, string>
    {
        ["desktop"] = "Computadora de escritorio", ["laptop"] = "Laptop", ["monitor"] = "Monitor",
        ["camera"] = "Cámara", ["external-disk"] = "Disco externo", ["peripheral"] = "Periférico",
        ["recording"] = "Equipo de grabación", ["other"] = "Otro dispositivo"
    };

    private static readonly HashSet<string> MarkerKeys =
    ["location", "physical_condition", "operational_condition", "verification", "ram", "storage", "operating_system"];

    public async Task<IReadOnlyList<ConditionOption>> GetConditionOptionsAsync(CancellationToken cancellationToken = default)
    {
        await access.EnsureReadAsync();
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var physical = await db.Equipment.AsNoTracking().Where(x => x.RegistrationState == EquipmentRegistrationState.Definitive).Select(x => x.Inspections
            .OrderByDescending(i => i.InspectedAtUtc).ThenByDescending(i => i.Id).Select(i => i.PhysicalCondition).FirstOrDefault())
            .Distinct().ToListAsync(cancellationToken);
        var operational = await db.Equipment.AsNoTracking().Where(x => x.RegistrationState == EquipmentRegistrationState.Definitive).Select(x => x.Inspections
            .OrderByDescending(i => i.InspectedAtUtc).ThenByDescending(i => i.Id).Select(i => i.OperationalCondition).FirstOrDefault())
            .Distinct().ToListAsync(cancellationToken);
        return physical.Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x)
            .Select(x => new ConditionOption("physical:" + x, "Física · " + x))
            .Concat(operational.Where(x => !string.IsNullOrWhiteSpace(x)).OrderBy(x => x)
                .Select(x => new ConditionOption("operational:" + x, "Operativa · " + x)))
            .ToList();
    }

    public async Task<EquipmentPage> ListAsync(
        string? search = null, string? category = null, string? condition = null,
        int page = 0,
        CancellationToken cancellationToken = default, bool draftsOnly = false, InventoryScope? scope = null)
    {
        await access.EnsureReadAsync();
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        await using var snapshot = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.RepeatableRead, cancellationToken);
        scope ??= new InventoryScope();
        if (scope.Availability is { } availability && !Enum.IsDefined(availability)) throw new InventoryValidationException("Disponibilidad inválida.");
        var equipment = db.Equipment.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            equipment = equipment.Where(x => x.Code.ToLower().Contains(term) || x.Name.ToLower().Contains(term) ||
                x.Category.Name.ToLower().Contains(term) ||
                (x.Brand != null && x.Brand.Name.ToLower().Contains(term)) ||
                (x.CommercialModel != null && x.CommercialModel.Name.ToLower().Contains(term)));
        }
        if (!string.IsNullOrWhiteSpace(category) && Categories.TryGetValue(category, out var categoryName))
            equipment = equipment.Where(x => x.Category.Name == categoryName);
        if (scope.OwnerCompanyId is { } owner) equipment = equipment.Where(x => x.OwnerCompanyId == owner);
        if (scope.WithoutOwnerCompany) equipment = equipment.Where(x => x.OwnerCompanyId == null);
        if (scope.UsingCompanyId is { } userCompany) equipment = equipment.Where(x => x.UsingCompanyId == userCompany);
        if (scope.WithoutUsingCompany) equipment = equipment.Where(x => x.UsingCompanyId == null);
        if (scope.OwnerAreaId is { } ownerArea) equipment = equipment.Where(x => x.OwnerAreaId == ownerArea);
        if (scope.WorkerId is { } worker) equipment = equipment.Where(x => db.Custodies.Any(c => c.EquipmentId == x.Id && c.WorkerId == worker && c.EndedAtUtc == null));
        if (scope.FreeFunctional) equipment = equipment.Where(x =>
            x.Inspections.OrderByDescending(i => i.InspectedAtUtc).ThenByDescending(i => i.Id).Select(i => i.OperationalCondition == null ? "" : i.OperationalCondition.Trim().ToLower()).FirstOrDefault() == "funcional" ||
            x.Inspections.OrderByDescending(i => i.InspectedAtUtc).ThenByDescending(i => i.Id).Select(i => i.OperationalCondition == null ? "" : i.OperationalCondition.Trim().ToLower()).FirstOrDefault() == "operativo");
        equipment = condition switch
        {
            "physical" => equipment.Where(x => x.Inspections.Any(i => i.PhysicalCondition != null)),
            "operational" => equipment.Where(x => x.Inspections.Any(i => i.OperationalCondition != null)),
            "unrecorded" => equipment.Where(x => !x.Inspections.Any()),
            _ => equipment
        };
        if (condition?.StartsWith("physical:", StringComparison.Ordinal) == true)
        {
            var value = condition["physical:".Length..];
            equipment = equipment.Where(x => x.Inspections.OrderByDescending(i => i.InspectedAtUtc).ThenByDescending(i => i.Id)
                .Select(i => i.PhysicalCondition).FirstOrDefault() == value);
        }
        else if (condition?.StartsWith("operational:", StringComparison.Ordinal) == true)
        {
            var value = condition["operational:".Length..];
            equipment = equipment.Where(x => x.Inspections.OrderByDescending(i => i.InspectedAtUtc).ThenByDescending(i => i.Id)
                .Select(i => i.OperationalCondition).FirstOrDefault() == value);
        }
        // Same PostgreSQL repeatable-read snapshot: counts cannot disagree during concurrent custody changes.
        var statsBase = equipment.Where(x => x.RegistrationState == EquipmentRegistrationState.Definitive);
        equipment = equipment.Where(x => x.RegistrationState == (draftsOnly ? EquipmentRegistrationState.Draft : EquipmentRegistrationState.Definitive));
        var statsTotal = await statsBase.CountAsync(cancellationToken);
        var statsOccupied = await statsBase.CountAsync(x => db.Custodies.Any(c => c.EquipmentId == x.Id && c.EndedAtUtc == null), cancellationToken);
        var reconcile = await statsBase.CountAsync(x => x.NeedsCustodyReconciliation, cancellationToken);
        if (!draftsOnly && (scope.Availability == EquipmentAvailability.Free || scope.FreeFunctional)) equipment = equipment.Where(x => !db.Custodies.Any(c => c.EquipmentId == x.Id && c.EndedAtUtc == null));
        else if (!draftsOnly && scope.Availability == EquipmentAvailability.Occupied) equipment = equipment.Where(x => db.Custodies.Any(c => c.EquipmentId == x.Id && c.EndedAtUtc == null));
        var total = await equipment.CountAsync(cancellationToken);
        var photosEnabled = configuration.GetValue<bool>("Inventory:PhotosEnabled") &&
            configuration.GetValue("Security:EnableIdentity", true) && !(await db.Database.GetPendingMigrationsAsync(cancellationToken)).Any();
        var rowData = await equipment.OrderBy(x => x.CodeNormalized).Skip(Math.Max(0, page) * PageSize).Take(PageSize)
            .Select(x => new
            {
                x.Id,
                x.Code,
                x.Name,
                OwnerArea = x.OwnerArea != null ? x.OwnerArea.Name : null,
                OwnerCompany = x.OwnerCompany != null ? x.OwnerCompany.Name : null,
                UsingCompany = x.UsingCompany != null ? x.UsingCompany.Name : null,
                Responsible = db.Custodies.Where(c => c.EquipmentId == x.Id && c.EndedAtUtc == null).Select(c => c.Worker.Name).FirstOrDefault(),
                Occupied = db.Custodies.Any(c => c.EquipmentId == x.Id && c.EndedAtUtc == null),
                Category = x.Category.Name,
                Brand = x.Brand != null ? x.Brand.Name : null,
                Model = x.CommercialModel != null ? x.CommercialModel.Name : null,
                PhysicalCondition = x.Inspections.OrderByDescending(i => i.InspectedAtUtc).ThenByDescending(i => i.Id).Select(i => i.PhysicalCondition).FirstOrDefault(),
                OperationalCondition = x.Inspections.OrderByDescending(i => i.InspectedAtUtc).ThenByDescending(i => i.Id).Select(i => i.OperationalCondition).FirstOrDefault(),
                PhotoId = photosEnabled ? db.PhotoPrincipalPeriods.Where(p => p.EquipmentId == x.Id && p.ToUtc == null)
                    .Select(p => (Guid?)p.PhotoId).FirstOrDefault() : null
            })
            .ToListAsync(cancellationToken);
        var rows = rowData.Select(x => new EquipmentRow(x.Id,
            draftsOnly && x.Code == $"~BORRADOR-{x.Id:N}" ? "Código pendiente" : x.Code, x.Name, x.Category, x.Brand, x.Model,
            x.PhysicalCondition, x.OperationalCondition, x.PhotoId is Guid photo ? PhotoRules.ContentUrl(x.Id, photo, true) : null,
            x.OwnerArea, x.Responsible, x.Occupied, x.OwnerCompany, x.UsingCompany)).ToList();
        await snapshot.CommitAsync(cancellationToken);
        return new EquipmentPage(rows, total, new InventoryStatistics(statsTotal, statsTotal - statsOccupied, statsOccupied, reconcile));
    }

    public async Task<EquipmentDetails?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await access.EnsureReadAsync();
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        var equipment = await db.Equipment.AsNoTracking()
            .Include(x => x.Category).Include(x => x.Brand).Include(x => x.CommercialModel)
            .Include(x => x.TechnicalConfigurations).ThenInclude(x => x.RamModules)
            .Include(x => x.TechnicalConfigurations).ThenInclude(x => x.StorageUnits)
            .Include(x => x.Observations).Include(x => x.Inspections).ThenInclude(x => x.Inspector)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (equipment is null) return null;
        if (equipment.RegistrationState == EquipmentRegistrationState.Draft && equipment.DraftPayload is not null)
            return new EquipmentDetails(id, ReadDraft(equipment), [], null, true, equipment.RegistrationRevision, true);
        var latest = equipment.TechnicalConfigurations.SingleOrDefault(x => x.EffectiveToUtc is null);
        var inspection = equipment.Inspections.OrderByDescending(x => x.InspectedAtUtc).ThenByDescending(x => x.Id).FirstOrDefault();
        var location = await db.LocationObservations.AsNoTracking().Include(x => x.Location).ThenInclude(x => x.Site)
            .Where(x => x.EquipmentId == id).OrderByDescending(x => x.ObservedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var verification = await db.TechnicalVerifications.AsNoTracking().Include(x => x.Verifier)
            .Where(x => x.EquipmentId == id).OrderByDescending(x => x.VerifiedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        var markers = await db.DataMarkers.AsNoTracking().Where(x => x.EquipmentId == id)
            .ToListAsync(cancellationToken);
        Guid? photoId = null;
        if (configuration.GetValue<bool>("Inventory:PhotosEnabled") && configuration.GetValue("Security:EnableIdentity", true) &&
            !(await db.Database.GetPendingMigrationsAsync(cancellationToken)).Any())
            photoId = await db.PhotoPrincipalPeriods.AsNoTracking().Where(x => x.EquipmentId == id && x.ToUtc == null)
                .Select(x => (Guid?)x.PhotoId).SingleOrDefaultAsync(cancellationToken);
        var input = new EquipmentInput
        {
            Code = equipment.Code, Name = equipment.Name,
            Category = Categories.FirstOrDefault(x => x.Value == equipment.Category.Name).Key ?? "other",
            Brand = equipment.Brand?.Name, Model = equipment.CommercialModel?.Name,
            Serial = equipment.SerialNumber,
            SerialStatus = equipment.SerialAvailability switch
            {
                DataAvailability.NotApplicable => "not-applicable",
                DataAvailability.PendingVerification => "pending",
                _ => "unrecorded"
            },
            ProcessorManufacturer = latest?.ProcessorManufacturer,
            ProcessorModel = latest?.ProcessorModel,
            ProcessorName = latest?.ProcessorDesignation,
            TotalRamGb = latest?.TotalRamGb,
            RamModulesComplete = latest?.RamModulesComplete ?? false,
            ExpectedTechnicalRevision = latest?.Revision ?? 0,
            OperatingSystem = latest?.OperatingSystem,
            RamModules = latest?.RamModules.OrderBy(x => x.Slot)
                .Select(x => new RamInput { OriginalId = x.Id, CapacityGb = x.CapacityGb }).ToList() ?? [],
            StorageUnits = latest?.StorageUnits.OrderBy(x => x.Position)
                .Select(x => new StorageInput { OriginalId = x.Id, Technology = x.Technology, Interface = x.Interface,
                    Capacity = x.Capacity, Unit = x.CapacityUnit }).ToList() ?? [],
            Site = location?.Location.Site.Name, Location = location?.Location.Name,
            PhysicalCondition = inspection?.PhysicalCondition, OperationalCondition = inspection?.OperationalCondition,
            Inspector = equipment.RegistrationState == EquipmentRegistrationState.Draft ? inspection?.Inspector.Name : null,
            Observation = null,
            PendingFields = markers.Where(x => x.Availability == DataAvailability.PendingVerification)
                .Select(x => x.FieldKey).ToList(),
            NotApplicableFields = markers.Where(x => x.Availability == DataAvailability.NotApplicable)
                .Select(x => x.FieldKey).ToList(),
            Verifier = verification?.Verifier.Name
        };
        var history = equipment.TechnicalConfigurations.OrderByDescending(x => x.Revision)
            .Select(x => $"Revisión {x.Revision} · {x.EffectiveFromUtc:yyyy-MM-dd HH:mm} UTC · RAM: " +
                (x.TotalRamGb is null ? "no registrada" :
                    $"{x.TotalRamGb.Value.ToString("0.###", CultureInfo.GetCultureInfo("es-BO"))} GB") +
                $" · discos: {x.StorageUnits.Count}").ToList();
        history.AddRange(equipment.Observations.OrderByDescending(x => x.RecordedAtUtc)
            .Select(x => $"Nota ({x.Source}, {x.RecordedAtUtc:yyyy-MM-dd HH:mm} UTC): {x.Text}"));
        if (verification is not null)
            history.Add($"Última verificación: {verification.VerifiedAtUtc:yyyy-MM-dd HH:mm} UTC · {verification.Verifier.Name}");
        return new EquipmentDetails(id, input, history, photoId is Guid photo ? PhotoRules.ContentUrl(id, photo) : null,
            equipment.RegistrationState == EquipmentRegistrationState.Draft, equipment.RegistrationRevision, equipment.RequiresRegistrationPhoto,
            equipment.RegistrationState == EquipmentRegistrationState.Definitive ? await AssignmentService.ReadSummaryAsync(db, id, 0, cancellationToken) : null);
    }

    public async Task<Guid> SaveAsync(EquipmentInput input, Guid? id = null, CancellationToken cancellationToken = default)
    {
        if (id is null) return await SaveDraftAsync(input, Guid.NewGuid(), 0, cancellationToken);
        await access.EnsureWriteAsync();
        await using var check = await contexts.CreateDbContextAsync(cancellationToken);
        var existing = await check.Equipment.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new InventoryValidationException("El equipo ya no existe.");
        if (existing.RegistrationState == EquipmentRegistrationState.Draft)
            throw new InventoryValidationException("Guarde el borrador con su revisión vigente y utilice Finalizar registro.");
        return await SaveDefinitiveAsync(input, id.Value, false, null, cancellationToken);
    }

    private async Task<Guid> SaveDefinitiveAsync(EquipmentInput input, Guid id, bool finalize, int? draftRevision, CancellationToken cancellationToken)
    {
        Validate(input);
        if (!CanWrite)
            throw new InventoryValidationException("Las escrituras de inventario no están habilitadas para este entorno.");
        await access.EnsureWriteAsync();
        await using var db = await contexts.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            Equipment equipment;
            {
                // Serialize edits to the same expediente before reading its current revision.
                await using (var lockCommand = db.Database.GetDbConnection().CreateCommand())
                {
                    lockCommand.Transaction = db.Database.CurrentTransaction!.GetDbTransaction();
                    lockCommand.CommandText = "SELECT 1 FROM equipment WHERE \"Id\" = @id FOR UPDATE";
                    var parameter = lockCommand.CreateParameter();
                    parameter.ParameterName = "id";
                    parameter.Value = id;
                    lockCommand.Parameters.Add(parameter);
                    await lockCommand.ExecuteScalarAsync(cancellationToken);
                }
                equipment = await db.Equipment.Include(x => x.Category)
                    .Include(x => x.TechnicalConfigurations).ThenInclude(x => x.RamModules)
                    .Include(x => x.TechnicalConfigurations).ThenInclude(x => x.StorageUnits)
                    .Include(x => x.TechnicalConfigurations).ThenInclude(x => x.AttributeValues)
                    .FirstOrDefaultAsync(x => x.Id == id, cancellationToken)
                    ?? throw new InventoryValidationException("El equipo ya no existe.");
                if (finalize && equipment.RegistrationState == EquipmentRegistrationState.Definitive) return equipment.Id;
                if (equipment.RegistrationState == EquipmentRegistrationState.Draft && !finalize)
                    throw new InventoryValidationException("Utilice Finalizar registro para este borrador.");
                if (finalize && equipment.RegistrationRevision != draftRevision)
                    throw new InventoryValidationException("El borrador cambió en otra sesión. Vuelva a abrirlo antes de finalizar.");
                if (equipment.Category.Name != Categories[input.Category])
                {
                    if (!finalize) throw new InventoryValidationException("No cambie la categoría de un expediente existente.");
                    equipment.Category = await RegistrationCategoryAsync(db, input.Category, cancellationToken);
                }
                if (equipment.RequiresRegistrationPhoto)
                    await ConfirmRegistrationPhotoAsync(db, equipment.Id, cancellationToken);
            }

            equipment.Code = input.Code.Trim();
            equipment.Name = input.Name.Trim();
            equipment.SerialNumber = Clean(input.Serial);
            equipment.SerialAvailability = equipment.SerialNumber is not null && input.SerialStatus != "pending" ? DataAvailability.Recorded :
                input.SerialStatus switch
                {
                    "pending" => DataAvailability.PendingVerification,
                    "not-applicable" => DataAvailability.NotApplicable,
                    _ => DataAvailability.NotRecorded
                };
            if (Clean(input.Brand) is { } brandName)
            {
                var brand = await db.Brands.FirstOrDefaultAsync(x => x.Name == brandName, cancellationToken);
                if (brand is null)
                {
                    brand = new EquipmentBrand { Id = Guid.NewGuid(), Name = brandName };
                    db.Brands.Add(brand);
                }
                equipment.Brand = brand;
                if (Clean(input.Model) is { } modelName)
                {
                    var model = await db.CommercialModels.FirstOrDefaultAsync(x => x.BrandId == brand.Id &&
                        x.CategoryId == equipment.Category.Id && x.Name == modelName, cancellationToken);
                    if (model is null)
                    {
                        model = new CommercialModel { Id = Guid.NewGuid(), Name = modelName, Brand = brand,
                            Category = equipment.Category };
                        db.CommercialModels.Add(model);
                    }
                    equipment.CommercialModel = model;
                }
                else equipment.CommercialModel = null;
            }
            else
            {
                equipment.CommercialModel = null;
                equipment.Brand = null;
            }

            var now = DateTimeOffset.UtcNow;
            var current = equipment.TechnicalConfigurations.SingleOrDefault(x => x.EffectiveToUtc is null);
            if (input.UpdateTechnicalData && input.ExpectedTechnicalRevision is { } expected &&
                expected != (current?.Revision ?? 0))
                throw new InventoryValidationException("La ficha técnica cambió mientras editaba. Vuelva a abrir el expediente antes de guardar.");
            var mappedRam = input.UpdateTechnicalData ? MapRam(input.RamModules, current) : [];
            var mappedStorage = input.UpdateTechnicalData ? MapStorage(input.StorageUnits, current) : [];
            if (input.UpdateTechnicalData && HasTechnicalChange(current, input, mappedRam, mappedStorage))
            {
                if (current is not null)
                {
                    current.EffectiveToUtc = now;
                    // Free the partial unique index before inserting the replacement, in the same transaction.
                    await db.SaveChangesAsync(cancellationToken);
                }
                var revision = new TechnicalConfiguration
                {
                    Id = Guid.NewGuid(), Equipment = equipment, Category = equipment.Category,
                    Revision = (equipment.TechnicalConfigurations.Count == 0 ? 0 :
                        equipment.TechnicalConfigurations.Max(x => x.Revision)) + 1,
                    EffectiveFromUtc = now,
                    ProcessorManufacturer = Clean(input.ProcessorManufacturer),
                    ProcessorModel = Clean(input.ProcessorModel),
                    ProcessorDesignation = Clean(input.ProcessorName),
                    TotalRamGb = input.TotalRamGb,
                    RamModulesComplete = input.RamModulesComplete ?? current?.RamModulesComplete ?? false,
                    OperatingSystem = Clean(input.OperatingSystem),
                    RamModules = mappedRam,
                    StorageUnits = mappedStorage,
                    AttributeValues = current?.AttributeValues.Select(x => new TechnicalAttributeValue
                    {
                        Id = Guid.NewGuid(), CategoryId = x.CategoryId, DefinitionId = x.DefinitionId,
                        TextValue = x.TextValue, NumberValue = x.NumberValue, BooleanValue = x.BooleanValue
                    }).ToList() ?? []
                };
                db.TechnicalConfigurations.Add(revision);
            }

            if (Clean(input.Site) is { } siteName && Clean(input.Location) is { } locationName)
            {
                var last = await db.LocationObservations.Include(x => x.Location).ThenInclude(x => x.Site)
                    .Where(x => x.EquipmentId == equipment.Id).OrderByDescending(x => x.ObservedAtUtc)
                    .FirstOrDefaultAsync(cancellationToken);
                if (last?.Location.Name != locationName || last.Location.Site.Name != siteName)
                {
                    var site = await db.Sites.FirstOrDefaultAsync(x => x.Name == siteName, cancellationToken);
                    if (site is null) { site = new Site { Id = Guid.NewGuid(), Name = siteName }; db.Sites.Add(site); }
                    var location = await db.Locations.FirstOrDefaultAsync(x => x.SiteId == site.Id && x.Name == locationName, cancellationToken);
                    if (location is null)
                    {
                        location = new Location { Id = Guid.NewGuid(), Site = site, Name = locationName };
                        db.Locations.Add(location);
                    }
                    db.LocationObservations.Add(new EquipmentLocationObservation
                        { Id = Guid.NewGuid(), Equipment = equipment, Location = location, ObservedAtUtc = now });
                }
            }
            else if (await db.LocationObservations.AnyAsync(x => x.EquipmentId == equipment.Id, cancellationToken))
                throw new InventoryValidationException("Una ubicación constatada no puede borrarse sin un movimiento o corrección histórica.");

            if (Clean(input.PhysicalCondition) is not null || Clean(input.OperationalCondition) is not null)
            {
                var last = await db.Inspections.Where(x => x.EquipmentId == equipment.Id)
                    .OrderByDescending(x => x.InspectedAtUtc).FirstOrDefaultAsync(cancellationToken);
                if (last?.PhysicalCondition != Clean(input.PhysicalCondition) ||
                    last?.OperationalCondition != Clean(input.OperationalCondition))
                {
                    if (Clean(input.Inspector) is null)
                        throw new InventoryValidationException("Indique la persona que inspeccionó las condiciones.");
                    var inspector = await GetStaffAsync(db, Clean(input.Inspector)!, cancellationToken);
                    db.Inspections.Add(new EquipmentInspection { Id = Guid.NewGuid(), Equipment = equipment,
                        Inspector = inspector, InspectedAtUtc = now,
                        PhysicalCondition = Clean(input.PhysicalCondition),
                        OperationalCondition = Clean(input.OperationalCondition) });
                }
            }
            else if (await db.Inspections.AnyAsync(x => x.EquipmentId == equipment.Id, cancellationToken))
                throw new InventoryValidationException("Una condición constatada no puede borrarse sin una nueva inspección.");

            if (input.VerifyNow)
            {
                var verifier = await GetStaffAsync(db, Clean(input.Verifier)!, cancellationToken);
                db.TechnicalVerifications.Add(new TechnicalVerification
                    { Id = Guid.NewGuid(), Equipment = equipment, Verifier = verifier, VerifiedAtUtc = now });
            }
            if (Clean(input.Observation) is { } text)
                db.Observations.Add(new EquipmentObservation { Id = Guid.NewGuid(), Equipment = equipment,
                    Text = text, Source = "Formulario", RecordedAtUtc = now });

            var existingMarkers = await db.DataMarkers.Where(x => x.EquipmentId == equipment.Id)
                .ToListAsync(cancellationToken);
            foreach (var marker in existingMarkers.Where(x => !input.PendingFields.Contains(x.FieldKey) &&
                !input.NotApplicableFields.Contains(x.FieldKey)))
                db.DataMarkers.Remove(marker);
            foreach (var marker in existingMarkers.Where(x => input.PendingFields.Contains(x.FieldKey)))
                marker.Availability = DataAvailability.PendingVerification;
            foreach (var marker in existingMarkers.Where(x => input.NotApplicableFields.Contains(x.FieldKey)))
                marker.Availability = DataAvailability.NotApplicable;
            foreach (var field in input.PendingFields.Where(x => existingMarkers.All(m => m.FieldKey != x)))
                db.DataMarkers.Add(new EquipmentDataMarker { Id = Guid.NewGuid(), Equipment = equipment,
                    FieldKey = field, Availability = DataAvailability.PendingVerification });
            foreach (var field in input.NotApplicableFields.Where(x => existingMarkers.All(m => m.FieldKey != x)))
                db.DataMarkers.Add(new EquipmentDataMarker { Id = Guid.NewGuid(), Equipment = equipment,
                    FieldKey = field, Availability = DataAvailability.NotApplicable });

            if (finalize)
            {
                equipment.RegistrationState = EquipmentRegistrationState.Definitive;
                equipment.DraftPayload = null;
                equipment.FinalizedAtUtc = now;
                equipment.RegistrationRevision++;
                equipment.RegistrationUpdatedAtUtc = now;
            }
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return equipment.Id;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new InventoryValidationException("Ya existe un código o un valor de catálogo equivalente. Revise los datos e inténtelo de nuevo.");
        }
    }

    private static async Task<StaffMember> GetStaffAsync(InventoryDbContext db, string name, CancellationToken ct)
    {
        var added = db.StaffMembers.Local.FirstOrDefault(x => x.Name == name);
        if (added is not null) return added;
        var staff = await db.StaffMembers.FirstOrDefaultAsync(x => x.Name == name, ct);
        if (staff is not null) return staff;
        staff = new StaffMember { Id = Guid.NewGuid(), Name = name };
        db.StaffMembers.Add(staff);
        return staff;
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static List<RamModule> MapRam(IReadOnlyList<RamInput> input, TechnicalConfiguration? old)
    {
        var sources = old?.RamModules.ToDictionary(x => x.Id) ?? [];
        var used = input.Where(x => x.OriginalId.HasValue).Select(x => x.OriginalId!.Value).ToList();
        if (used.Distinct().Count() != used.Count || used.Any(id => !sources.ContainsKey(id)))
            throw new InventoryValidationException("Los módulos de RAM cambiaron. Vuelva a abrir la ficha técnica.");
        var occupied = used.Select(id => sources[id].Slot).Where(x => x.HasValue).Select(x => x!.Value).ToHashSet();
        return input.Select(x =>
        {
            var source = x.OriginalId is { } id ? sources[id] : null;
            int? slot = source is not null ? source.Slot : NextPosition(occupied);
            return new RamModule { Id = Guid.NewGuid(), Slot = slot, CapacityGb = x.CapacityGb!.Value,
                Technology = source?.Technology, SerialNumber = source?.SerialNumber };
        }).ToList();
    }

    private static List<StorageUnit> MapStorage(IReadOnlyList<StorageInput> input, TechnicalConfiguration? old)
    {
        var sources = old?.StorageUnits.ToDictionary(x => x.Id) ?? [];
        var used = input.Where(x => x.OriginalId.HasValue).Select(x => x.OriginalId!.Value).ToList();
        if (used.Distinct().Count() != used.Count || used.Any(id => !sources.ContainsKey(id)))
            throw new InventoryValidationException("Los discos cambiaron. Vuelva a abrir la ficha técnica.");
        var occupied = used.Select(id => sources[id].Position).Where(x => x.HasValue).Select(x => x!.Value).ToHashSet();
        return input.Select(x =>
        {
            var source = x.OriginalId is { } id ? sources[id] : null;
            int? position = source is not null ? source.Position : NextPosition(occupied);
            return new StorageUnit { Id = Guid.NewGuid(), Position = position, Capacity = x.Capacity,
                CapacityUnit = x.Capacity is null ? null : x.Unit, Technology = Clean(x.Technology),
                Interface = Clean(x.Interface), SerialNumber = source?.SerialNumber };
        }).ToList();
    }

    private static int NextPosition(HashSet<int> occupied)
    {
        var next = 1;
        while (occupied.Contains(next)) next++;
        occupied.Add(next);
        return next;
    }

    private static bool HasTechnicalChange(TechnicalConfiguration? old, EquipmentInput input,
        IReadOnlyList<RamModule> ram, IReadOnlyList<StorageUnit> storage)
    {
        if (old is null)
            return Clean(input.ProcessorManufacturer) is not null || Clean(input.ProcessorModel) is not null ||
                Clean(input.ProcessorName) is not null || input.TotalRamGb is not null ||
                Clean(input.OperatingSystem) is not null || input.RamModules.Count > 0 || input.StorageUnits.Count > 0;
        return old.ProcessorManufacturer != Clean(input.ProcessorManufacturer) ||
            old.ProcessorModel != Clean(input.ProcessorModel) || old.ProcessorDesignation != Clean(input.ProcessorName) ||
             old.TotalRamGb != input.TotalRamGb || old.OperatingSystem != Clean(input.OperatingSystem) ||
            old.RamModulesComplete != (input.RamModulesComplete ?? old.RamModulesComplete) ||
            !old.RamModules.OrderBy(x => x.Slot).Select(x => (x.CapacityGb, x.Slot, x.Technology, x.SerialNumber))
                .SequenceEqual(ram.OrderBy(x => x.Slot).Select(x => (x.CapacityGb, x.Slot, x.Technology, x.SerialNumber))) ||
            !old.StorageUnits.OrderBy(x => x.Position).Select(x => (x.Technology, x.Interface, x.Capacity, x.CapacityUnit, x.Position, x.SerialNumber))
                .SequenceEqual(storage.OrderBy(x => x.Position).Select(x => (x.Technology, x.Interface, x.Capacity, x.CapacityUnit, x.Position, x.SerialNumber)));
    }

    private static void Validate(EquipmentInput x)
    {
        static void Length(string? value, int max, string label)
        {
            if (value?.Trim().Length > max)
                throw new InventoryValidationException($"{label}: máximo {max} caracteres.");
        }
        if (string.IsNullOrWhiteSpace(x.Code) || string.IsNullOrWhiteSpace(x.Name) || !Categories.ContainsKey(x.Category))
            throw new InventoryValidationException("Código, categoría y nombre descriptivo son obligatorios.");
        Length(x.Code, 80, "Código"); Length(x.Name, 200, "Nombre");
        Length(x.Brand, 120, "Marca"); Length(x.Model, 160, "Modelo"); Length(x.Serial, 160, "Serie");
        Length(x.ProcessorManufacturer, 120, "Fabricante del procesador");
        Length(x.ProcessorModel, 160, "Modelo del procesador"); Length(x.ProcessorName, 200, "Procesador");
        Length(x.OperatingSystem, 160, "Sistema operativo");
        Length(x.Site, 120, "Sede"); Length(x.Location, 160, "Ubicación");
        Length(x.PhysicalCondition, 100, "Condición física");
        Length(x.OperationalCondition, 100, "Condición operativa");
        Length(x.Inspector, 200, "Inspector"); Length(x.Verifier, 200, "Verificador");
        if (Clean(x.Model) is not null && Clean(x.Brand) is null)
            throw new InventoryValidationException("Indique una marca al registrar un modelo.");
        if (Clean(x.Serial) is not null && x.SerialStatus == "not-applicable")
            throw new InventoryValidationException("Una serie informada no puede marcarse «No aplica».");
        if (x.SerialStatus is not ("unrecorded" or "pending" or "not-applicable"))
            throw new InventoryValidationException("Estado del número de serie no válido.");
        if ((Clean(x.Site) is null) != (Clean(x.Location) is null))
            throw new InventoryValidationException("Para registrar ubicación indique sede y lugar juntos.");
        if (x.VerifyNow && Clean(x.Verifier) is null)
            throw new InventoryValidationException("Indique quién realiza la verificación ahora.");
        if (Clean(x.Observation)?.Length > 4000)
            throw new InventoryValidationException("La observación no puede superar 4000 caracteres.");
        if (x.PendingFields.Distinct().Count() != x.PendingFields.Count ||
            x.PendingFields.Any(field => !MarkerKeys.Contains(field)) ||
            x.NotApplicableFields.Distinct().Count() != x.NotApplicableFields.Count ||
            x.NotApplicableFields.Any(field => !MarkerKeys.Contains(field)) ||
            x.NotApplicableFields.Intersect(x.PendingFields).Any())
            throw new InventoryValidationException("Datos pendientes no válidos.");
        if ((x.NotApplicableFields.Contains("location") && Clean(x.Location) is not null) ||
            (x.NotApplicableFields.Contains("physical_condition") && Clean(x.PhysicalCondition) is not null) ||
            (x.NotApplicableFields.Contains("operational_condition") && Clean(x.OperationalCondition) is not null) ||
            (x.NotApplicableFields.Contains("verification") && x.VerifyNow) ||
            (x.NotApplicableFields.Contains("ram") && (x.TotalRamGb is not null || x.RamModules.Count > 0)) ||
            (x.NotApplicableFields.Contains("storage") && x.StorageUnits.Count > 0) ||
            (x.NotApplicableFields.Contains("operating_system") && Clean(x.OperatingSystem) is not null))
            throw new InventoryValidationException("Un dato informado no puede marcarse «No aplica».");
        if (x.TotalRamGb is <= 0 or > 100000 || x.RamModules.Count > 32 || x.StorageUnits.Count > 32 ||
            x.RamModules.Any(m => m.CapacityGb is null or <= 0 or > 100000) ||
            x.StorageUnits.Any(d => d.Capacity is <= 0 or > 100000 ||
                (d.Capacity is not null && d.Unit is not ("GB" or "TB"))))
            throw new InventoryValidationException("Revise capacidades de RAM y almacenamiento; deben ser positivas y tener unidad válida.");
        if (x.UpdateTechnicalData && x.RamModulesComplete == true && x.RamModules.Count > 0 && x.TotalRamGb != x.RamModules.Sum(m => m.CapacityGb))
            throw new InventoryValidationException("La RAM total debe coincidir con la suma de los módulos completos.");
        foreach (var disk in x.StorageUnits)
        {
            Length(disk.Technology, 80, "Tipo de disco"); Length(disk.Interface, 80, "Interfaz");
        }
        if (x.Category is not ("desktop" or "laptop") &&
            (x.TotalRamGb is not null || x.RamModules.Count > 0 || x.StorageUnits.Count > 0 ||
             Clean(x.ProcessorManufacturer) is not null || Clean(x.ProcessorModel) is not null ||
             Clean(x.ProcessorName) is not null || Clean(x.OperatingSystem) is not null))
            throw new InventoryValidationException("La ficha técnica enviada no corresponde a esta categoría.");
    }
}
