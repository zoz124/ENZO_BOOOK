using BookNest.Core.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BookNest.Web.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class DashboardController : Controller
{
    private readonly IAdminDashboardService _dashboard;

    public DashboardController(IAdminDashboardService dashboard) => _dashboard = dashboard;

    public async Task<IActionResult> Index(CancellationToken cancellationToken)
        => View(await _dashboard.GetStatsAsync(cancellationToken));
}
