using System.Security.Claims;
using InventarioCPR.Application.Security;
using InventarioCPR.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace InventarioCPR.Application.Photos;

public sealed record PhotoActor(Guid Id, string Name, bool CanWrite);
public interface IPhotoAccess
{
    Task<PhotoActor> AuthorizeAsync(ClaimsPrincipal principal, bool write);
}

public sealed class PhotoAccess(IAuthorizationService authorization, SignInManager<AccessUser> signIn,
    UserManager<AccessUser> users, IConfiguration configuration) : IPhotoAccess
{
    public async Task<PhotoActor> AuthorizeAsync(ClaimsPrincipal principal, bool write)
    {
        if (principal.Identity?.IsAuthenticated != true ||
            !(await authorization.AuthorizeAsync(principal, InventoryPolicies.Read)).Succeeded)
            throw new PhotoException("Inicie sesión con un perfil autorizado.", 403);
        var user = await signIn.ValidateSecurityStampAsync(principal);
        if (user?.IsActive != true) throw new PhotoException("La sesión ya no está autorizada.", 403);
        var roles = await users.GetRolesAsync(user);
        if (!roles.Any(role => role is InventoryPolicies.Administrator or InventoryPolicies.Operator or InventoryPolicies.Viewer))
            throw new PhotoException("Su perfil no permite consultar evidencias.", 403);
        var canWrite = (await authorization.AuthorizeAsync(principal, InventoryPolicies.Write)).Succeeded &&
            roles.Any(role => role is InventoryPolicies.Administrator or InventoryPolicies.Operator) &&
            (configuration["NeonDevelopmentApp:BranchConfirmed"] == "test-migracion-inicial" || configuration.GetValue<bool>("Inventory:ProductionWritesEnabled"));
        if (write && !canWrite) throw new PhotoException("Su perfil o entorno no permite modificar fotografías.", 403);
        return new PhotoActor(user.Id, user.UserName ?? user.Id.ToString(), canWrite);
    }
}
