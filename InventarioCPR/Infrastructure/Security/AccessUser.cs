using Microsoft.AspNetCore.Identity;

namespace InventarioCPR.Infrastructure.Security;

// An access account is not a staff member or the current custodian of equipment.
public sealed class AccessUser : IdentityUser<Guid>
{
    public bool IsActive { get; set; } = true;
}
