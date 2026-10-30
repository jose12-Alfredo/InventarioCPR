using InventarioCPR.Application.Inventory;
using InventarioCPR.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Xunit;

namespace InventarioCPR.Tests;

public sealed class InventoryServiceTests
{
    [Fact]
    public async Task RejectsInvalidInputsBeforeOpeningAnyDatabaseConnection()
    {
        var service = new InventoryService(null!, new ConfigurationBuilder().Build(), new StubInventoryAccess());
        await Assert.ThrowsAsync<InventoryValidationException>(() => service.SaveAsync(
            new EquipmentInput { Code = "  ", Name = "Prueba", Category = "laptop" }));
        await Assert.ThrowsAsync<InventoryValidationException>(() => service.SaveAsync(
            new EquipmentInput { Code = "EQ_1", Name = "Prueba", Category = "laptop", Model = "Sin marca" }));
        await Assert.ThrowsAsync<InventoryValidationException>(() => service.SaveAsync(
            new EquipmentInput { Code = "EQ_1", Name = "Prueba", Category = "laptop",
                TotalRamGb = -1 }));
        await Assert.ThrowsAsync<InventoryValidationException>(() => service.SaveAsync(
            new EquipmentInput { Code = "EQ_1", Name = "Prueba", Category = "monitor",
                StorageUnits = [new StorageInput { Capacity = 1, Unit = "TB" }] }));
        await Assert.ThrowsAsync<InventoryValidationException>(() => service.SaveAsync(
            new EquipmentInput { Code = "EQ_1", Name = "Prueba", Category = "laptop",
                PendingFields = ["location"], NotApplicableFields = ["location"] }));
    }

    [Fact]
    public async Task ValidInputCannotWriteBeforeDevelopmentBranchIsConfirmed()
    {
        var service = new InventoryService(null!, new ConfigurationBuilder().Build(), new StubInventoryAccess());
        Assert.False(service.CanWrite);
        await Assert.ThrowsAsync<InventoryValidationException>(() => service.SaveAsync(
            new EquipmentInput { Code = "EQ_1", Name = "Equipo ficticio", Category = "laptop" }));
    }

    // This test intentionally leaves one clearly labeled fictitious expediente in the isolated branch
    // so that the user can reopen it after restarting the application. Never run against production.
    [DatabaseFact(persistsData: true)]
    public async Task RegistrationSurvivesNewContextAndTechnicalEditKeepsHistory()
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets("InventarioCPR-local").Build();
        var raw = configuration.GetConnectionString("NeonDevelopmentApp")
            ?? throw new InvalidOperationException("Configure NeonDevelopmentApp en User Secrets.");
        var expected = configuration["NeonMigrationTest:ExpectedHost"];
        var app = new NpgsqlConnectionStringBuilder(raw) { SslMode = SslMode.VerifyFull };
        var host = (app.Host ?? "").Replace("-pooler.", ".", StringComparison.OrdinalIgnoreCase);
        if (!string.Equals(expected?.Replace("-pooler.", ".", StringComparison.OrdinalIgnoreCase), host,
                StringComparison.OrdinalIgnoreCase) || app.Database != "neondb")
            throw new InvalidOperationException("El endpoint de development no es el aprobado.");
        var main = configuration.GetConnectionString("NeonDirect");
        if (!string.IsNullOrWhiteSpace(main) &&
            (new NpgsqlConnectionStringBuilder(main).Host ?? "").Replace("-pooler.", ".") == host)
            throw new InvalidOperationException("El endpoint de desarrollo coincide con production.");

        var options = new DbContextOptionsBuilder<InventoryDbContext>().UseNpgsql(app.ConnectionString).Options;
        var firstFactory = new PooledDbContextFactory<InventoryDbContext>(options);
        var first = new InventoryService(firstFactory, configuration, new StubInventoryAccess());
        var code = "PRUEBA_" + Guid.NewGuid().ToString("N")[..12];
        var draft = new EquipmentInput { Code = code, Category = "laptop", Name = "Laptop ficticia de prueba",
            SerialStatus = "pending", PendingFields = ["storage"] };
        var id = await first.SaveAsync(draft);

        // A fresh factory/context must retrieve the committed record, not an in-memory draft.
        var secondFactory = new PooledDbContextFactory<InventoryDbContext>(options);
        var reopened = new InventoryService(secondFactory, configuration, new StubInventoryAccess());
        var persisted = await reopened.GetAsync(id);
        Assert.NotNull(persisted);
        Assert.Equal(code, persisted.Input.Code);
        Assert.Null(persisted.Input.Serial);
        Assert.Contains("storage", persisted.Input.PendingFields);
        Assert.Empty(persisted.History);

        await Assert.ThrowsAsync<InventoryValidationException>(() => reopened.SaveAsync(
            new EquipmentInput { Code = code.ToLowerInvariant(), Category = "laptop", Name = "Duplicado ficticio" }));

        draft.TotalRamGb = 16;
        draft.StorageUnits =
        [
            new StorageInput { Technology = "SSD", Interface = "NVMe", Capacity = 512, Unit = "GB" },
            new StorageInput { Technology = "HDD", Interface = "SATA", Capacity = 1, Unit = "TB" }
        ];
        draft.PendingFields.Clear();
        draft.Observation = "Especificaciones ficticias registradas en desarrollo.";
        await reopened.SaveAsync(draft, id);
        draft.TotalRamGb = 32;
        draft.RamModules = [new RamInput { CapacityGb = 16 }, new RamInput { CapacityGb = 16 }];
        draft.Observation = null;
        await reopened.SaveAsync(draft, id);

        draft.Site = "Sede ficticia " + code;
        draft.Location = "Laboratorio de prueba";
        draft.PhysicalCondition = "Inspección ficticia";
        draft.OperationalCondition = "Comprobación ficticia";
        draft.Inspector = "Inspector ficticio " + code;
        draft.VerifyNow = true;
        draft.Verifier = draft.Inspector;
        draft.Observation = "Observación ficticia posterior.";
        await reopened.SaveAsync(draft, id);

        var latest = await new InventoryService(secondFactory, configuration, new StubInventoryAccess()).GetAsync(id);
        Assert.NotNull(latest);
        Assert.Equal(32, latest.Input.TotalRamGb);
        Assert.Equal(2, latest.Input.StorageUnits.Count);
        Assert.Equal(2, latest.Input.RamModules.Count);
        Assert.Equal(draft.Site, latest.Input.Site);
        Assert.Equal(draft.Location, latest.Input.Location);
        Assert.Equal(draft.PhysicalCondition, latest.Input.PhysicalCondition);
        Assert.Equal(draft.OperationalCondition, latest.Input.OperationalCondition);
        Assert.Contains(latest.History, x => x.Contains("Última verificación:"));
        Assert.Contains(latest.History, x => x.Contains("Observación ficticia posterior."));
        Assert.Contains(latest.History, x => x.StartsWith("Revisión 1"));
        Assert.Contains(latest.History, x => x.StartsWith("Revisión 2"));
        Assert.Contains(latest.History, x => x.Contains("RAM: 16 GB"));
        Assert.DoesNotContain(latest.History, x => x.Contains("16,000 GB"));
        Assert.Contains((await reopened.ListAsync(code[..10].ToLowerInvariant(), "laptop", "physical")).Rows, x => x.Id == id);
        Assert.Contains((await reopened.ListAsync(code, "laptop", "physical:" + draft.PhysicalCondition)).Rows, x => x.Id == id);
        Assert.Contains(await reopened.GetConditionOptionsAsync(), x => x.Value == "physical:" + draft.PhysicalCondition);
        Assert.DoesNotContain((await reopened.ListAsync(code, "laptop", "unrecorded")).Rows, x => x.Id == id);
        await using var db = await secondFactory.CreateDbContextAsync();
        Assert.Equal(2, await db.TechnicalConfigurations.CountAsync(x => x.EquipmentId == id));
        Assert.Equal(1, await db.TechnicalConfigurations.CountAsync(x => x.EquipmentId == id && x.EffectiveToUtc == null));
    }
}
