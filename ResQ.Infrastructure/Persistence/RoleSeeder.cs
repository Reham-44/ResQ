using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ResQ.Infrastructure.Identity;

namespace ResQ.Infrastructure.Persistence;

public static class RoleSeeder
{
    public static readonly string[] Roles = ["Citizen", "ResponseTeamMember", "Dispatcher", "Admin"];

    public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
    {
        var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        foreach (var role in Roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                {
                    var errors = string.Join("; ", result.Errors.Select(error => error.Description));
                    throw new InvalidOperationException($"Failed to create role '{role}': {errors}");
                }
            }
        }
    }
}
