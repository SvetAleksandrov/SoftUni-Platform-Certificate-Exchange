namespace SoftUni_Platform_Certificate_Exchange.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftUni_Platform_Certificate_Exchange.Data;

// Засега само Admin има достъп до сертификатите
[Authorize(Roles = "Admin")]
public class CertificatesController : Controller
{
    // Достъп до базата данни чрез Entity Framework
    private readonly ApplicationDbContext _context;

    // Dependency Injection на ApplicationDbContext
    public CertificatesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Certificates
    // Показва всички сертификати
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Извличаме сертификатите заедно с организациите
        var certificates = await _context.CertificateSubmissions
            .Include(c => c.Organization)
            .AsNoTracking()
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync();

        // Предаваме списъка към Index.cshtml
        return View(certificates);
    }

    // GET: /Certificates/Details/1
    // Показва информацията за конкретен сертификат
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        // Търсим сертификата по ID
        var certificate = await _context.CertificateSubmissions
            .Include(c => c.Organization)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        // Връщаме HTTP 404, ако не съществува
        if (certificate == null)
            return NotFound();

        // Предаваме сертификата към Details.cshtml
        return View(certificate);
    }
}
