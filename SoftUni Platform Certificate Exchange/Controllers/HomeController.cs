
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftUni_Platform_Certificate_Exchange.Data;
using SoftUni_Platform_Certificate_Exchange.Models;
using SoftUni_Platform_Certificate_Exchange.ViewModels;

namespace SoftUni_Platform_Certificate_Exchange.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    // Dependency Injection
    public HomeController(
        ILogger<HomeController> logger,
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _logger = logger;
        _context = context;
        _userManager = userManager;
    }

    // GET: /
    // Публична Home страница или Dashboard според ролята
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var model = new HomeDashboardViewModel();

        // 1. Посетител без Login - публична Welcome страница
        if (User.Identity?.IsAuthenticated != true)
            return View(model);

        // 2. Получаваме текущия Identity потребител
        var user = await _userManager.GetUserAsync(User);

        if (user == null)
            return Challenge();

        // Използваме собственото име или имейла
        model.DisplayName = string.IsNullOrWhiteSpace(user.FirstName)
            ? user.Email ?? "User"
            : user.FirstName;

        // Подготвяме заявката за сертификати
        var query = _context.CertificateSubmissions
            .AsNoTracking()
            .AsQueryable();

        // 3. ADMIN DASHBOARD
        if (User.IsInRole("Admin"))
        {
            model.IsAdmin = true;

            // Всички организации
            model.OrganizationCount =
                await _context.Organizations.CountAsync();

            // Потребители, свързани с организация
            model.AssignedUsersCount = await _context.Users
                .CountAsync(u => u.OrganizationId != null);
        }
        // 4. CLIENT DASHBOARD
        else if (User.IsInRole("Client"))
        {
            // Организацията идва от Identity, не от URL
            if (user.OrganizationId is not int organizationId)
                return Forbid();

            // Проверяваме дали организацията е активна
            var organizationName = await _context.Organizations
                .Where(o => o.Id == organizationId && o.IsActive)
                .Select(o => o.Name)
                .FirstOrDefaultAsync();

            if (organizationName == null)
                return Forbid();

            model.OrganizationName = organizationName;

            // Клиентът вижда само своите сертификати
            query = query.Where(c =>
                c.OrganizationId == organizationId);
        }
        else
        {
            // Непозната роля няма достъп до Dashboard
            return Forbid();
        }

        // 5. Общ брой сертификати
        model.CertificateCount = await query.CountAsync();

        // 6. Чакащи обработка
        model.PendingCount = await query.CountAsync(c =>
            c.Status == CertificateStatus.Submitted ||
            c.Status == CertificateStatus.UnderReview);

        // 7. Одобрени сертификати
        model.ApprovedCount = await query.CountAsync(c =>
            c.Status == CertificateStatus.Approved);

        // 8. Отхвърлени сертификати
        model.RejectedCount = await query.CountAsync(c =>
            c.Status == CertificateStatus.Rejected);

        // 9. Последните пет заявки
        model.RecentCertificates = await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .ThenByDescending(c => c.Id)
            .Take(5)
            .Select(c => new DashboardCertificateViewModel
            {
                Id = c.Id,
                OrganizationName = c.Organization.Name,
                Hostname = c.TargetHostname,
                Status = c.Status,
                CreatedAtUtc = c.CreatedAtUtc,
                ValidToUtc = c.NotAfterUtc
            })
            .ToListAsync();

        return View(model);
    }

    // GET: /Home/Privacy
    [HttpGet]
    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    // Стандартната MVC страница за грешки
    [ResponseCache(
        Duration = 0,
        Location = ResponseCacheLocation.None,
        NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id
                        ?? HttpContext.TraceIdentifier
        });
    }
}
