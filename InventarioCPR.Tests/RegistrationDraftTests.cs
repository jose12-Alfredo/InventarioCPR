using InventarioCPR.Application.Inventory;
using InventarioCPR.Components.Inventory;
using Xunit;

namespace InventarioCPR.Tests;

public sealed class RegistrationDraftTests
{
    [Fact]
    public void UnknownRamRemainsNullAndManualTotalNeedsNoModules()
    {
        var draft = new TechnicalDraft();
        Assert.Null(draft.EffectiveRam);
        draft.TotalRamGb = 32;
        Assert.Equal(32, draft.EffectiveRam);
        Assert.Empty(draft.BuildRam());
    }

    [Fact]
    public void CompleteModulesCalculateTotalWithoutDestroyingTheManualSnapshot()
    {
        var draft = new TechnicalDraft { TotalRamGb = 32 };
        draft.RamModules.AddRange([new() { Capacity = 8 }, new() { Capacity = 8 }]);
        Assert.Equal(16, draft.EffectiveRam);
        draft.RamModulesComplete = false;
        Assert.Equal(32, draft.EffectiveRam);
    }

    [Fact]
    public void BlankRowsAreNotInventedAsZeroButPartialDistributionIsExplicit()
    {
        var draft = new TechnicalDraft { TotalRamGb = 16 };
        draft.RamModules.Add(new());
        Assert.Empty(draft.BuildRam());
        draft.RamModules.Add(new() { Capacity = 8 });
        Assert.Null(draft.EffectiveRam);
        Assert.Throws<InventoryValidationException>(() => draft.BuildRam());
        draft.RamModulesComplete = false;
        Assert.Single(draft.BuildRam());
        Assert.Equal(16, draft.EffectiveRam);
    }

    [Fact]
    public void KnownDiskTechnologyCanBeSavedWithoutInventingCapacity()
    {
        var draft = new TechnicalDraft();
        draft.StorageUnits.Add(new());
        draft.StorageUnits.Add(new() { Type = "SSD", Interface = "NVMe" });
        var disk = Assert.Single(draft.BuildStorage());
        Assert.Equal("SSD", disk.Technology);
        Assert.Null(disk.Capacity);
    }

    [Fact]
    public async Task ServerRejectsZeroRamAndInconsistentCompleteModulesBeforeOpeningAConnection()
    {
        var service = new InventoryService(null!, new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(), new StubInventoryAccess());
        await Assert.ThrowsAsync<InventoryValidationException>(() => service.SaveAsync(new EquipmentInput
            { Code = "REG_SIMPLE_INVALIDO", Name = "Equipo ficticio", Category = "laptop", TotalRamGb = 0 }));
        await Assert.ThrowsAsync<InventoryValidationException>(() => service.SaveAsync(new EquipmentInput
        {
            Code = "REG_SIMPLE_INVALIDO", Name = "Equipo ficticio", Category = "laptop", TotalRamGb = 32,
            RamModulesComplete = true, RamModules = [new() { CapacityGb = 8 }, new() { CapacityGb = 8 }]
        }));
    }
}
