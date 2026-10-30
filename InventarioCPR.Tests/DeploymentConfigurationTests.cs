using InventarioCPR.Application.Inventory;
using InventarioCPR.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Npgsql;
using Xunit;

namespace InventarioCPR.Tests;

public sealed class DeploymentConfigurationTests
{
    [Fact]
    public void ProductionConnectionRequiresDedicatedKeyAndEnforcesTls()
    {
        var missing = new ConfigurationBuilder().Build();
        Assert.Throws<InvalidOperationException>(() => NeonConnection.Runtime(missing, isDevelopment: false));

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Database"] =
                "Host=example.invalid;Database=inventory;Username=test;Password=placeholder;SSL Mode=Disable"
        }).Build();

        var connection = new NpgsqlConnectionStringBuilder(NeonConnection.Runtime(configuration, isDevelopment: false));
        Assert.Equal(SslMode.VerifyFull, connection.SslMode);
        Assert.True(connection.Pooling);
        Assert.Equal(20, connection.MaxPoolSize);
    }

    [Fact]
    public void ProductionWritesRequireExplicitOptIn()
    {
        var disabled = new InventoryService(null!, new ConfigurationBuilder().Build(), new StubInventoryAccess());
        Assert.False(disabled.CanWrite);

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Inventory:ProductionWritesEnabled"] = "true"
        }).Build();
        var enabled = new InventoryService(null!, configuration, new StubInventoryAccess());
        Assert.True(enabled.CanWrite);
    }
}
