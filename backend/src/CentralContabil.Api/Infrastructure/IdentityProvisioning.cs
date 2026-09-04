using CentralContabil.Api.Modules.Shared.Domain;
using Microsoft.AspNetCore.Identity;

namespace CentralContabil.Api.Infrastructure;

public static class ApplicationRoles
{
    public const string User = "User";
    public const string Admin = "Admin";

    public static readonly IReadOnlyList<string> Required = [User, Admin];
}

public sealed class IdentityStructuralSeed(
    RoleManager<IdentityRole<Guid>> roles,
    ILogger<IdentityStructuralSeed> logger)
{
    public async Task SeedAsync()
    {
        foreach (var roleName in ApplicationRoles.Required)
        {
            if (await roles.RoleExistsAsync(roleName)) continue;

            var result = await roles.CreateAsync(new IdentityRole<Guid>(roleName));
            if (!result.Succeeded)
            {
                var errors = IdentityErrors(result);
                logger.LogError("Failed to create required identity role {Role}. Errors: {Errors}", roleName, errors);
                throw new InvalidOperationException($"Failed to create required identity role '{roleName}': {errors}");
            }

            logger.LogInformation("Required identity role created: {Role}", roleName);
        }
    }

    private static string IdentityErrors(IdentityResult result) =>
        string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
}

public static class UserRoleProvisioning
{
    public static async Task EnsureRoleAsync(
        UserManager<ApplicationUser> users,
        ApplicationUser user,
        string roleName)
    {
        if (await users.IsInRoleAsync(user, roleName)) return;

        var result = await users.AddToRoleAsync(user, roleName);
        if (result.Succeeded) return;

        var errors = string.Join("; ", result.Errors.Select(error => $"{error.Code}: {error.Description}"));
        throw new InvalidOperationException($"Failed to assign required identity role '{roleName}': {errors}");
    }
}
