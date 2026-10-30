using System.Security.Claims;
using InventarioCPR.Infrastructure.Security;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace InventarioCPR.Application.Security;

// Provisional profiles: business must approve names and the final permission matrix before activation.
public static class InventoryPolicies
{
    public const string Read = "Inventory.Read";
    public const string Write = "Inventory.Write";
    public const string ManageUsers = "Users.Manage";
    public const string Administrator = "Administrador";
    public const string Operator = "Sistemas";
    public const string Viewer = "Consulta";

    public static void Configure(AuthorizationOptions options)
    {
        options.AddPolicy(Read, policy => policy.RequireAuthenticatedUser()
            .RequireRole(Administrator, Operator, Viewer));
        options.AddPolicy(Write, policy => policy.RequireAuthenticatedUser()
            .RequireRole(Administrator, Operator));
        options.AddPolicy(ManageUsers, policy => policy.RequireAuthenticatedUser()
            .RequireRole(Administrator));
    }
}

public interface IInventoryAccess
{
    Task<bool> CanReadAsync();
    Task<bool> CanWriteAsync();
    Task EnsureReadAsync();
    Task EnsureWriteAsync();
}

// The current local-only fictitious-data mode is retained until the Identity migration is approved.
public sealed class LocalTestInventoryAccess(IWebHostEnvironment environment) : IInventoryAccess
{
    public Task<bool> CanReadAsync() => Task.FromResult(environment.IsDevelopment());
    public Task<bool> CanWriteAsync() => CanReadAsync();
    public async Task EnsureReadAsync() { if (!await CanReadAsync()) throw new UnauthorizedAccessException(); }
    public async Task EnsureWriteAsync() { if (!await CanWriteAsync()) throw new UnauthorizedAccessException(); }
}

public sealed class IdentityInventoryAccess(
    AuthenticationStateProvider authenticationState,
    IAuthorizationService authorization,
    UserManager<AccessUser> users,
    SignInManager<AccessUser> signIn) : IInventoryAccess
{
    private async Task<bool> AllowedAsync(string policy)
    {
        var principal = (await authenticationState.GetAuthenticationStateAsync()).User;
        if (principal.Identity?.IsAuthenticated != true ||
            !(await authorization.AuthorizeAsync(principal, policy)).Succeeded)
            return false;
        var account = await signIn.ValidateSecurityStampAsync(principal);
        if (account?.IsActive != true) return false;
        var currentRoles = await users.GetRolesAsync(account);
        return policy switch
        {
            InventoryPolicies.Read => currentRoles.Any(x => x is InventoryPolicies.Administrator or InventoryPolicies.Operator or InventoryPolicies.Viewer),
            InventoryPolicies.Write => currentRoles.Any(x => x is InventoryPolicies.Administrator or InventoryPolicies.Operator),
            InventoryPolicies.ManageUsers => currentRoles.Contains(InventoryPolicies.Administrator),
            _ => false
        };
    }

    public Task<bool> CanReadAsync() => AllowedAsync(InventoryPolicies.Read);
    public Task<bool> CanWriteAsync() => AllowedAsync(InventoryPolicies.Write);
    public async Task EnsureReadAsync() { if (!await CanReadAsync()) throw new UnauthorizedAccessException(); }
    public async Task EnsureWriteAsync() { if (!await CanWriteAsync()) throw new UnauthorizedAccessException(); }
}
