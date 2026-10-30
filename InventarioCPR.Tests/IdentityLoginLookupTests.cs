using InventarioCPR.Infrastructure.Persistence;
using InventarioCPR.Infrastructure.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace InventarioCPR.Tests;

public sealed class IdentityLoginLookupTests
{
    [DatabaseFact]
    public async Task ExistingAdministratorCanBeFoundByEmailWithoutTreatingEmailAsUserName()
    {
        var configuration = new ConfigurationBuilder().AddUserSecrets("InventarioCPR-local").Build();
        var connection = NeonConnection.DevelopmentApp(configuration);
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<AccessDbContext>(options => options.UseNpgsql(connection));
        services.AddIdentityCore<AccessUser>().AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AccessDbContext>();
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AccessDbContext>();
        var account = await db.Users.AsNoTracking().SingleAsync();
        Assert.NotNull(account.Email);
        Assert.NotEqual(account.UserName, account.Email);
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AccessUser>>();
        Assert.Null(await users.FindByNameAsync(account.Email));
        Assert.Equal(account.Id, (await users.FindByEmailAsync(account.Email))?.Id);
        Assert.Equal(account.Id, (await users.FindByNameAsync(account.UserName!))?.Id);
    }
}
