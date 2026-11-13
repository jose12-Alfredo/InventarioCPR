using InventarioCPR.Application.Inventory;
using InventarioCPR.Infrastructure.Persistence;
using InventarioCPR.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Xunit;

namespace InventarioCPR.Tests;

public sealed class RegistrationDatabaseFactAttribute : FactAttribute
{
    public RegistrationDatabaseFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("INVENTARIOCPR_RUN_REGISTRATION_TESTS") != "test-migracion-inicial")
            Skip = "Requiere la rama aislada autorizada; conserva un equipo ficticio de preservación técnica.";
    }
}

public sealed class RegistrationPreservationTests
{
    [RegistrationDatabaseFact]
    public async Task HiddenMetadataPartialRamAndExcelSourceSurviveEditingAndNewTechnicalRevisions()
    {
        var config = new ConfigurationBuilder().AddUserSecrets("InventarioCPR-local").Build();
        var raw = NeonConnection.DevelopmentApp(config);
        if (config["NeonDevelopmentApp:BranchConfirmed"] != "test-migracion-inicial" ||
            new NpgsqlConnectionStringBuilder(raw).Host?.Replace("-pooler.", ".") != "ep-raspy-dream-b476uukh.c-6.us-east-2.aws.neon.tech")
            throw new InvalidOperationException("La conexión no es la rama de pruebas aprobada.");
        var options = new DbContextOptionsBuilder<InventoryDbContext>().UseNpgsql(raw).Options;
        var factory = new PooledDbContextFactory<InventoryDbContext>(options);
        var id = Guid.NewGuid();
        await using (var seed = await factory.CreateDbContextAsync())
        {
            var category = await seed.Categories.SingleAsync(x => x.Name == "Laptop");
            var equipment = new Equipment { Id = id, Code = "REG_SIMPLE_META_" + id.ToString("N")[..12], Name = "Laptop ficticia de preservación", Category = category };
            var definition = new CategoryAttributeDefinition { Id = Guid.NewGuid(), CategoryId = category.Id, Key = "fixture_" + id.ToString("N"), Label = "Atributo ficticio histórico", Revision = 1, ValueKind = TechnicalValueKind.Text };
            seed.Equipment.Add(equipment);
            seed.CategoryAttributeDefinitions.Add(definition);
            seed.TechnicalConfigurations.Add(new TechnicalConfiguration
            {
                Id = Guid.NewGuid(), Equipment = equipment, Category = category, Revision = 1,
                EffectiveFromUtc = DateTimeOffset.UtcNow, ProcessorManufacturer = "Fabricante ficticio",
                ProcessorModel = "Modelo CPU avanzado ficticio", ProcessorDesignation = "Procesador ficticio original",
                TotalRamGb = 32, RamModulesComplete = false, OperatingSystem = "SO ficticio",
                RamModules = [new() { Id = Guid.NewGuid(), Slot = 4, CapacityGb = 8, Technology = "DDR4", SerialNumber = "RAM-FICTICIA-4" }, new() { Id = Guid.NewGuid(), Slot = 2, CapacityGb = 8, Technology = "DDR4", SerialNumber = "RAM-FICTICIA-2" }],
                StorageUnits = [new() { Id = Guid.NewGuid(), Position = 3, Technology = "SSD", Interface = "NVMe", Capacity = 512, CapacityUnit = "GB", SerialNumber = "SSD-FICTICIO" }, new() { Id = Guid.NewGuid(), Position = 5, Technology = "HDD", Interface = "SATA", Capacity = 1, CapacityUnit = "TB", SerialNumber = "HDD-FICTICIO" }],
                AttributeValues = [new() { Id = Guid.NewGuid(), CategoryId = category.Id, DefinitionId = definition.Id, TextValue = "Valor ficticio que el formulario no edita" }]
            });
            seed.Observations.Add(new EquipmentObservation { Id = Guid.NewGuid(), Equipment = equipment, Source = "Excel", Text = "Procedencia ficticia original intacta", RecordedAtUtc = DateTimeOffset.UtcNow });
            seed.DataMarkers.Add(new EquipmentDataMarker { Id = Guid.NewGuid(), Equipment = equipment, FieldKey = "ram", Availability = DataAvailability.PendingVerification });
            await seed.SaveChangesAsync();
        }
        var service = new InventoryService(factory, config, new StubInventoryAccess());
        var initial = (await service.GetAsync(id))!.Input;
        Assert.False(initial.RamModulesComplete);
        Assert.Equal(32, initial.TotalRamGb);
        initial.UpdateTechnicalData = false;
        initial.Name = "Laptop ficticia con identidad editada";
        await service.SaveAsync(initial, id);
        await using (var check = await factory.CreateDbContextAsync())
            Assert.Equal(1, await check.TechnicalConfigurations.CountAsync(x => x.EquipmentId == id));

        var update = (await service.GetAsync(id))!.Input;
        update.ProcessorName = "Procesador ficticio actualizado";
        await service.SaveAsync(update, id);
        initial.UpdateTechnicalData = true;
        await Assert.ThrowsAsync<InventoryValidationException>(() => service.SaveAsync(initial, id)); // Stale technical revision.
        var latest = (await service.GetAsync(id))!.Input;
        Assert.Equal("Fabricante ficticio", latest.ProcessorManufacturer);
        Assert.Equal("Modelo CPU avanzado ficticio", latest.ProcessorModel);
        Assert.Equal(32, latest.TotalRamGb);
        Assert.False(latest.RamModulesComplete);
        latest.RamModules.Add(new() { CapacityGb = 16 });
        latest.RamModulesComplete = true;
        await service.SaveAsync(latest, id);
        await using var verified = await factory.CreateDbContextAsync();
        var revisions = await verified.TechnicalConfigurations.Where(x => x.EquipmentId == id)
            .Include(x => x.RamModules).Include(x => x.StorageUnits).Include(x => x.AttributeValues).OrderBy(x => x.Revision).ToListAsync();
        Assert.Equal(3, revisions.Count);
        var current = revisions[^1];
        Assert.True(current.RamModulesComplete);
        Assert.Contains(current.RamModules, x => x.Slot == 4 && x.SerialNumber == "RAM-FICTICIA-4" && x.Technology == "DDR4");
        Assert.Contains(current.RamModules, x => x.Slot == 2 && x.SerialNumber == "RAM-FICTICIA-2");
        Assert.Contains(current.RamModules, x => x.Slot == 1 && x.CapacityGb == 16);
        Assert.Contains(current.StorageUnits, x => x.Position == 3 && x.SerialNumber == "SSD-FICTICIO");
        Assert.Contains(current.StorageUnits, x => x.Position == 5 && x.SerialNumber == "HDD-FICTICIO");
        Assert.All(revisions, revision => Assert.Equal("Valor ficticio que el formulario no edita", Assert.Single(revision.AttributeValues).TextValue));
        Assert.Null(current.EffectiveToUtc);
        Assert.All(revisions.Take(2), revision => Assert.NotNull(revision.EffectiveToUtc));
        Assert.Equal("Procedencia ficticia original intacta", (await verified.Observations.SingleAsync(x => x.EquipmentId == id)).Text);
        Assert.Contains("ram", (await service.GetAsync(id))!.Input.PendingFields);
    }
}
