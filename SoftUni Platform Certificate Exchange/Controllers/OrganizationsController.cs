namespace SoftUni_Platform_Certificate_Exchange.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftUni_Platform_Certificate_Exchange.Data;

// Достъп само за потребители с роля Admin
[Authorize(Roles = "Admin")]
public class OrganizationsController : Controller
{
    // Поле за достъп до базата данни
    private readonly ApplicationDbContext _context;

    // Dependency Injection - получаваме DbContext
    public OrganizationsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // HTTP GET: /Organizations или /Organizations/Index
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Извличаме всички организации от SQL Server
        var organizations = await _context.Organizations
            .AsNoTracking()          // Само четене, без проследяване
            .OrderBy(o => o.Name)   // Сортиране по име
            .ToListAsync();         // Асинхронно изпълнение на заявката

        // Предаваме резултата към Razor View
        return View(organizations);
    }
}
