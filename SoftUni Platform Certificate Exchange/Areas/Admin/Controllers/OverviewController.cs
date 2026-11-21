using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SoftUni_Platform_Certificate_Exchange.Areas.Admin.Controllers;

// Контролер в административната MVC Area
[Area("Admin")]
[Authorize(Roles = "Admin")]
public class OverviewController : Controller
{
    // GET: /Admin/Overview
    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }
}
