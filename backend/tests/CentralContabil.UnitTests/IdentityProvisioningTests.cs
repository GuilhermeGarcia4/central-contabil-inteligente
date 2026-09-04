using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Shared.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CentralContabil.UnitTests;

public sealed class IdentityProvisioningTests
{
    [Fact]
    public async Task Missing_required_roles_are_created()
    {
        await using var services = CreateServices();

        await RunStructuralSeedAsync(services);

        using var scope = services.CreateScope();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        Assert.True(await roles.RoleExistsAsync(ApplicationRoles.User));
        Assert.True(await roles.RoleExistsAsync(ApplicationRoles.Admin));
        Assert.Equal(2, await roles.Roles.CountAsync());
        Assert.Equal("USER", (await roles.FindByNameAsync(ApplicationRoles.User))!.NormalizedName);
        Assert.Equal("ADMIN", (await roles.FindByNameAsync(ApplicationRoles.Admin))!.NormalizedName);
    }

    [Fact]
    public async Task Existing_roles_are_not_duplicated()
    {
        await using var services = CreateServices();
        using (var scope = services.CreateScope())
        {
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            Assert.True((await roles.CreateAsync(new IdentityRole<Guid>(ApplicationRoles.User))).Succeeded);
        }

        await RunStructuralSeedAsync(services);

        using var verificationScope = services.CreateScope();
        var db = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Single(await db.Roles.Where(role => role.NormalizedName == "USER").ToListAsync());
        Assert.Single(await db.Roles.Where(role => role.NormalizedName == "ADMIN").ToListAsync());
    }

    [Fact]
    public async Task Structural_seed_can_run_multiple_times()
    {
        await using var services = CreateServices();

        await RunStructuralSeedAsync(services);
        await RunStructuralSeedAsync(services);
        await RunStructuralSeedAsync(services);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(2, await db.Roles.CountAsync());
    }

    [Fact]
    public async Task New_Google_user_receives_User_without_Admin()
    {
        await using var services = CreateServices();
        await RunStructuralSeedAsync(services);
        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await CreateUserAsync(users, "new-google@example.test");
        Assert.True((await users.AddLoginAsync(user, new UserLoginInfo("Google", "google-new", "Google"))).Succeeded);

        await UserRoleProvisioning.EnsureRoleAsync(users, user, ApplicationRoles.User);

        Assert.True(await users.IsInRoleAsync(user, ApplicationRoles.User));
        Assert.False(await users.IsInRoleAsync(user, ApplicationRoles.Admin));
    }

    [Fact]
    public async Task Existing_Google_user_without_role_receives_User()
    {
        await using var services = CreateServices();
        await RunStructuralSeedAsync(services);
        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await CreateUserAsync(users, "partial-google@example.test");
        Assert.True((await users.AddLoginAsync(user, new UserLoginInfo("Google", "google-partial", "Google"))).Succeeded);

        var existing = await users.FindByLoginAsync("Google", "google-partial");
        await UserRoleProvisioning.EnsureRoleAsync(users, existing!, ApplicationRoles.User);

        Assert.True(await users.IsInRoleAsync(existing!, ApplicationRoles.User));
        Assert.False(await users.IsInRoleAsync(existing!, ApplicationRoles.Admin));
    }

    [Fact]
    public async Task Existing_user_with_User_does_not_receive_duplicate_role()
    {
        await using var services = CreateServices();
        await RunStructuralSeedAsync(services);
        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await CreateUserAsync(users, "existing-role@example.test");

        await UserRoleProvisioning.EnsureRoleAsync(users, user, ApplicationRoles.User);
        await UserRoleProvisioning.EnsureRoleAsync(users, user, ApplicationRoles.User);

        Assert.Single(await db.UserRoles.Where(item => item.UserId == user.Id).ToListAsync());
        Assert.False(await users.IsInRoleAsync(user, ApplicationRoles.Admin));
    }

    [Fact]
    public async Task Traditional_registration_receives_User_without_Admin()
    {
        await using var services = CreateServices();
        await RunStructuralSeedAsync(services);
        using var scope = services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            UserName = "traditional@example.test",
            Email = "traditional@example.test",
            DisplayName = "Traditional"
        };
        Assert.True((await users.CreateAsync(user, "Strong!Pass123")).Succeeded);

        await UserRoleProvisioning.EnsureRoleAsync(users, user, ApplicationRoles.User);

        Assert.True(await users.IsInRoleAsync(user, ApplicationRoles.User));
        Assert.False(await users.IsInRoleAsync(user, ApplicationRoles.Admin));
    }

    private static ServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        var databaseName = Guid.NewGuid().ToString();
        services.AddLogging();
        services.AddDbContext<AppDbContext>(options => options.UseInMemoryDatabase(databaseName));
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<AppDbContext>();
        services.AddScoped<IdentityStructuralSeed>();
        return services.BuildServiceProvider();
    }

    private static async Task RunStructuralSeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IdentityStructuralSeed>().SeedAsync();
    }

    private static async Task<ApplicationUser> CreateUserAsync(
        UserManager<ApplicationUser> users,
        string email)
    {
        var user = new ApplicationUser { UserName = email, Email = email, DisplayName = email };
        Assert.True((await users.CreateAsync(user)).Succeeded);
        return user;
    }
}
