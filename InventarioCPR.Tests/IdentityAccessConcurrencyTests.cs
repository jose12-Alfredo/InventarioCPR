using System.Security.Claims;
using InventarioCPR.Application.Security;
using InventarioCPR.Infrastructure.Persistence;
using InventarioCPR.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InventarioCPR.Tests;

public sealed class IdentityAccessConcurrencyTests
{
    [DatabaseFact]
    public async Task OverlappingCircuitPermissionChecksSafelyUseTheScopedIdentityContext()
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets("InventarioCPR-local").Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(InventoryPolicies.Configure);
        services.AddDbContext<AccessDbContext>(options => options.UseNpgsql(NeonConnection.DevelopmentApp(configuration)));
        services.AddIdentity<AccessUser, IdentityRole<Guid>>().AddEntityFrameworkStores<AccessDbContext>();
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AccessUser>>();
        var account = await users.Users.AsNoTracking().SingleAsync();
        Assert.True(account.IsActive);
        var principal = await scope.ServiceProvider.GetRequiredService<IUserClaimsPrincipalFactory<AccessUser>>().CreateAsync(account);
        var access = new IdentityInventoryAccess(new CircuitState(principal), scope.ServiceProvider.GetRequiredService<IAuthorizationService>(),
            users, scope.ServiceProvider.GetRequiredService<SignInManager<AccessUser>>());
        // Test principal is created by Identity from the existing account; no password/account/role changes.
        // Real browser tests separately exercise the user's manually authenticated cookie.
        var checks = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => access.CanReadAsync()));
        Assert.All(checks, value => Assert.True(value));
    }

    private sealed class CircuitState(ClaimsPrincipal principal) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() => Task.FromResult(new AuthenticationState(principal));
    }
}
