using System.Net;
using BookNest.Core.Interfaces.IServices;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace BookNest.IntegrationTests;

[Trait("Category", "Mvc")]
public class SmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");

            // The layout reads the cart on every page; stubbing keeps this test DB-free.
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<ICatalogService>();
                services.AddScoped<ICatalogService, StubCatalogService>();

                services.RemoveAll<ICartService>();
                services.AddScoped<ICartService, StubCartService>();
            });
        });
    }

    [Fact]
    public async Task Application_StartsSuccessfully()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/");

        // Assert
        response.Should().BeSuccessful("Application should start without errors");
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Home/Index")]
    [InlineData("/books")]
    [InlineData("/cart")]
    [InlineData("/account/login")]
    [InlineData("/account/register")]
    public async Task PublicEndpoints_ReturnSuccess(string url)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync(url);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            $"public endpoint {url} should be accessible");
    }

    [Fact]
    public async Task PublicPage_RendersEnglishBrandShell()
    {
        var client = _factory.CreateClient();

        var html = await client.GetStringAsync("/");

        // The storefront is English/LTR and branded Enzo Book.
        html.Should().Contain("dir=\"ltr\"");
        html.Should().Contain("lang=\"en\"");
        html.Should().Contain("Enzo Book");
        html.Should().Contain("Bestsellers");
        html.Should().Contain("Free shipping on orders over");
    }

    [Fact]
    public async Task Responses_CarrySecurityHeaders()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/");

        response.Headers.Should().ContainKey("X-Content-Type-Options");
        response.Headers.Should().ContainKey("X-Frame-Options");
        response.Headers.Should().ContainKey("Referrer-Policy");
        response.Headers.Should().ContainKey("Content-Security-Policy");
        response.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");
    }

    [Theory]
    [InlineData("/admin")]
    [InlineData("/admin/books")]
    [InlineData("/admin/orders")]
    public async Task AdminRoutes_Anonymous_RedirectToLogin(string url)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync(url);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location!.OriginalString.Should().Contain("/account/login");
    }

    [Fact]
    public async Task Styleguide_IsNotAvailableOutsideDevelopment()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/dev/styleguide");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound,
            "the component styleguide must not exist in deployed environments");
    }
}
