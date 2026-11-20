using Microsoft.AspNetCore.Mvc;

namespace BookNest.Web.Controllers;

/// <summary>T-04: component styleguide, available in Development only.</summary>
public class DevController : Controller
{
    private readonly IWebHostEnvironment _environment;

    public DevController(IWebHostEnvironment environment) => _environment = environment;

    [HttpGet]
    public IActionResult Styleguide()
        => _environment.IsDevelopment() ? View() : NotFound();
}
