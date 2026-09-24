using FluentValidation.TestHelper;
using ResQ.API.Controllers;
using ResQ.Application.Features.Auth.Commands.Register;
using ResQ.Application.Features.Emergencies.Commands.CreateEmergency;
using Microsoft.AspNetCore.Authorization;

namespace ResQ.Tests;

public sealed class RegistrationAuthorizationTests
{
    private readonly RegisterCommandValidator _validator = new();

    [Theory]
    [InlineData("Admin")]
    [InlineData("Dispatcher")]
    [InlineData("ResponseTeamMember")]
    public void Public_registration_rejects_privileged_roles(string role)
    {
        var result = _validator.TestValidate(new RegisterCommand("user@example.com", "Password123!", "Test User", role));
        result.ShouldHaveValidationErrorFor(command => command.Role);
    }

    [Fact]
    public void Public_registration_accepts_citizen_role()
    {
        var result = _validator.TestValidate(new RegisterCommand("user@example.com", "Password123!", "Test User", "Citizen"));
        result.ShouldNotHaveValidationErrorFor(command => command.Role);
    }

    [Fact]
    public void Citizen_id_is_not_json_bindable()
    {
        var property = typeof(CreateEmergencyCommand).GetProperty(nameof(CreateEmergencyCommand.CitizenId))!;
        Assert.Contains(property.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonIgnoreAttribute), false), _ => true);
    }

    [Fact]
    public void Dashboard_filter_implements_hangfire_authorization_contract()
    {
        Assert.True(typeof(ResQ.API.Security.AdminDashboardAuthorizationFilter)
            .GetInterfaces().Contains(typeof(Hangfire.Dashboard.IDashboardAuthorizationFilter)));
    }

    [Fact]
    public void Emergency_creation_requires_the_citizen_role()
    {
        var method = typeof(EmergenciesController).GetMethod(nameof(EmergenciesController.Create))!;
        var authorize = method.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>();
        Assert.Contains(authorize, attribute => attribute.Roles == "Citizen");
    }

    [Theory]
    [InlineData(nameof(DispatchController.Assign), "Dispatcher,Admin")]
    [InlineData(nameof(DispatchController.Reassign), "Dispatcher,Admin")]
    [InlineData(nameof(DispatchController.Accept), "ResponseTeamMember")]
    [InlineData(nameof(DispatchController.Close), "Dispatcher,Admin")]
    public void Dispatch_routes_declare_expected_roles(string actionName, string roles)
    {
        var method = typeof(DispatchController).GetMethod(actionName)!;
        var authorize = method.GetCustomAttributes(typeof(AuthorizeAttribute), true).Cast<AuthorizeAttribute>();
        Assert.Contains(authorize, attribute => attribute.Roles == roles);
    }
}
