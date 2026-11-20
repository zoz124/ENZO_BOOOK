using System.Reflection;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace BookNest.ArchitectureTests;

[Trait("Category", "Guard")]
public class AdminAuthorizationTests
{
    [Fact]
    public void AdminControllers_MustRequireAdminRole()
    {
        var controllers = typeof(Program).Assembly.GetTypes()
            .Where(t => typeof(Controller).IsAssignableFrom(t)
                        && t is { IsAbstract: false }
                        && (t.Namespace?.Contains(".Areas.Admin", StringComparison.Ordinal) ?? false))
            .ToList();

        controllers.Should().NotBeEmpty("the admin area should have controllers");

        foreach (var controller in controllers)
        {
            var attribute = controller.GetCustomAttribute<AuthorizeAttribute>();

            attribute.Should().NotBeNull($"{controller.Name} is missing [Authorize]");
            attribute!.Roles.Should().Contain("Admin", $"{controller.Name} must require the Admin role");
        }
    }

    [Fact]
    public void CustomerOnlyControllers_MustRequireAuthentication()
    {
        var guarded = new[] { "CheckoutController", "OrdersController", "ProfileController", "WishlistController" };

        // Same-named controllers exist in the Admin area, so scope this to the storefront assembly namespace.
        var controllers = typeof(Program).Assembly.GetTypes()
            .Where(t => typeof(Controller).IsAssignableFrom(t)
                        && guarded.Contains(t.Name)
                        && !(t.Namespace?.Contains(".Areas.Admin", StringComparison.Ordinal) ?? false))
            .ToList();

        controllers.Should().HaveCount(guarded.Length);

        foreach (var controller in controllers)
        {
            controller.GetCustomAttribute<AuthorizeAttribute>()
                .Should().NotBeNull($"{controller.Name} must require a signed-in user");
        }
    }
}
