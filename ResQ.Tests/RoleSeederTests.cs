using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using ResQ.Infrastructure.Identity;
using ResQ.Infrastructure.Persistence;

namespace ResQ.Tests;

public sealed class RoleSeederTests
{
    [Fact]
    public async Task SeedRoles_is_idempotent_for_existing_roles()
    {
        await using var db = TestDatabase.Create();
        using var provider = CreateServices(db).BuildServiceProvider();

        using (var scope = provider.CreateScope())
            await RoleSeeder.SeedRolesAsync(scope.ServiceProvider);
        using (var scope = provider.CreateScope())
            await RoleSeeder.SeedRolesAsync(scope.ServiceProvider);

        Assert.Equal(RoleSeeder.Roles.Length, db.Roles.Count());
    }

    [Fact]
    public async Task SeedRoles_fails_with_identity_error_descriptions_when_creation_fails()
    {
        await using var db = TestDatabase.Create();
        using var provider = CreateServices(db, rejectRoles: true).BuildServiceProvider();
        using var scope = provider.CreateScope();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => RoleSeeder.SeedRolesAsync(scope.ServiceProvider));

        Assert.Contains(RoleSeeder.Roles[0], exception.Message);
        Assert.Contains("Role creation rejected by test validator", exception.Message);
    }

    private static IServiceCollection CreateServices(ResQDbContext db, bool rejectRoles = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(db);
        services.AddIdentityCore<ApplicationUser>()
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ResQDbContext>();

        if (rejectRoles)
            services.AddScoped<IRoleValidator<IdentityRole>, RejectRoleValidator>();

        return services;
    }

    private sealed class RejectRoleValidator : IRoleValidator<IdentityRole>
    {
        public Task<IdentityResult> ValidateAsync(RoleManager<IdentityRole> manager, IdentityRole role) =>
            Task.FromResult(IdentityResult.Failed(new IdentityError
            {
                Description = "Role creation rejected by test validator"
            }));
    }
}
