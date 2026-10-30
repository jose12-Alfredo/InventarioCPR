using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace InventarioCPR.Infrastructure.Security;

// Separate migration history keeps Identity tables independent of the approved inventory schema.
public sealed class AccessDbContext(DbContextOptions<AccessDbContext> options)
    : IdentityDbContext<AccessUser, IdentityRole<Guid>, Guid>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<AccessUser>().ToTable("access_users");
        builder.Entity<IdentityRole<Guid>>().ToTable("access_roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("access_user_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("access_user_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("access_user_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("access_user_tokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("access_role_claims");
    }
}
