using System.Security.Claims;
using InventarioCPR.Application.Inventory;
using InventarioCPR.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InventarioCPR.Tests;

internal sealed class StubInventoryAccess(bool canRead = true, bool canWrite = true) : IInventoryAccess
{
    public Task<bool> CanReadAsync() => Task.FromResult(canRead);
    public Task<bool> CanWriteAsync() => Task.FromResult(canWrite);
    public Task EnsureReadAsync() => canRead ? Task.CompletedTask : Task.FromException(new UnauthorizedAccessException());
    public Task EnsureWriteAsync() => canWrite ? Task.CompletedTask : Task.FromException(new UnauthorizedAccessException());
}

public sealed class InventoryAuthorizationTests
{
    private static ClaimsPrincipal Principal(string? role) => role is null ? new ClaimsPrincipal(new ClaimsIdentity()) :
        new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, role)], "test"));

    [Fact]
    public async Task ProvisionalPoliciesRequireAuthenticationAndSeparateReadWriteAdministration()
    {
        Assert.Equal("Sistemas", InventoryPolicies.Operator);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(InventoryPolicies.Configure);
        await using var provider = services.BuildServiceProvider();
        var auth = provider.GetRequiredService<IAuthorizationService>();
        Assert.False((await auth.AuthorizeAsync(Principal(null), InventoryPolicies.Read)).Succeeded);
        var viewer = Principal(InventoryPolicies.Viewer);
        Assert.True((await auth.AuthorizeAsync(viewer, InventoryPolicies.Read)).Succeeded);
        Assert.False((await auth.AuthorizeAsync(viewer, InventoryPolicies.Write)).Succeeded);
        Assert.False((await auth.AuthorizeAsync(viewer, InventoryPolicies.ManageUsers)).Succeeded);
        Assert.True((await auth.AuthorizeAsync(Principal(InventoryPolicies.Operator), InventoryPolicies.Write)).Succeeded);
        Assert.False((await auth.AuthorizeAsync(Principal(InventoryPolicies.Operator), InventoryPolicies.ManageUsers)).Succeeded);
        Assert.True((await auth.AuthorizeAsync(Principal(InventoryPolicies.Administrator), InventoryPolicies.ManageUsers)).Succeeded);
    }

    [Fact]
    public async Task InventoryServiceRejectsUnauthorizedReadsAndWritesBeforeConnecting()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["NeonDevelopmentApp:BranchConfirmed"] = "test-migracion-inicial"
        }).Build();
        var untrusted = new InventoryService(null!, config, new StubInventoryAccess(false, false));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => untrusted.ListAsync());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => untrusted.GetAsync(Guid.NewGuid()));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => untrusted.SaveAsync(
            new EquipmentInput { Code = "EQ_PRUEBA", Category = "laptop", Name = "Equipo ficticio" }));
        var viewer = new InventoryService(null!, config, new StubInventoryAccess(true, false));
        Assert.False(await viewer.CanWriteAsync());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => viewer.SaveAsync(
            new EquipmentInput { Code = "EQ_PRUEBA", Category = "laptop", Name = "Equipo ficticio" }));
    }
}
