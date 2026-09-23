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
                await roleManager.CreateAsync(new IdentityRole(role));
        }
    }
}
