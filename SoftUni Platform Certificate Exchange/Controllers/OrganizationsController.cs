namespace SoftUni_Platform_Certificate_Exchange.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftUni_Platform_Certificate_Exchange.Data;
using SoftUni_Platform_Certificate_Exchange.Models;
using SoftUni_Platform_Certificate_Exchange.ViewModels;


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
    public async Task<IActionResult> Index(
        string? organizationSearch,
        string? registrationSearch,
        string statusSearch = "all",
        string groupBy = "none",
        string sort = "organization_asc")
    {
        // 1. Подготвяме заявката, без да я изпълняваме
        var query = _context.Organizations
            .AsNoTracking()
            .AsQueryable();

        // 2. SEARCH: Organization
        organizationSearch = organizationSearch?.Trim() ?? "";

        if (!string.IsNullOrWhiteSpace(organizationSearch))
        {
            // Търсим само в колоната Name
            query = query.Where(o =>
                o.Name.Contains(organizationSearch));
        }

        // 3. SEARCH: Registration
        registrationSearch = registrationSearch?.Trim() ?? "";

        if (!string.IsNullOrWhiteSpace(registrationSearch))
        {
            // Търсим само в колоната RegistrationNumber
            query = query.Where(o =>
                o.RegistrationNumber != null &&
                o.RegistrationNumber.Contains(registrationSearch));
        }

        // 4. FILTER: Status
        statusSearch = statusSearch?.Trim().ToLowerInvariant() ?? "all";

        if (statusSearch == "active")
        {
            query = query.Where(o => o.IsActive);
        }
        else if (statusSearch == "inactive")
        {
            query = query.Where(o => !o.IsActive);
        }
        else
        {
            statusSearch = "all";
        }

        // 5. GROUP BY: Проверка на избраната колона
        groupBy = groupBy?.Trim().ToLowerInvariant() ?? "none";

        string[] allowedGroups =
        {
        "none",
        "organization",
        "registration",
        "status"
    };

        if (!allowedGroups.Contains(groupBy))
        {
            groupBy = "none";
        }

        // 6. SORTING: Проверка на избраната колона и посока
        sort = sort?.Trim().ToLowerInvariant() ?? "organization_asc";

        string[] allowedSorts =
        {
        "organization_asc",
        "organization_desc",
        "registration_asc",
        "registration_desc",
        "status_asc",
        "status_desc"
    };

        if (!allowedSorts.Contains(sort))
        {
            sort = "organization_asc";
        }

        // 7. Сортираме според избраната колона
        var orderedQuery = sort switch
        {
            "organization_desc" => query
                .OrderByDescending(o => o.Name),

            "registration_asc" => query
                .OrderBy(o => o.RegistrationNumber),

            "registration_desc" => query
                .OrderByDescending(o => o.RegistrationNumber),

            "status_asc" => query
                .OrderBy(o => o.IsActive),

            "status_desc" => query
                .OrderByDescending(o => o.IsActive),

            // По подразбиране: Organization A-Z
            _ => query.OrderBy(o => o.Name)
        };

        // 8. Изпълняваме SQL заявката
        // ID осигурява стабилна подредба при равни стойности
        var organizations = await orderedQuery
            .ThenBy(o => o.Id)
            .ToListAsync();

        // 9. Предаваме стойностите обратно към Razor View
        ViewBag.OrganizationSearch = organizationSearch;
        ViewBag.RegistrationSearch = registrationSearch;
        ViewBag.StatusSearch = statusSearch;
        ViewBag.GroupBy = groupBy;
        ViewBag.Sort = sort;

        // 10. Връщаме филтрираните резултати
        return View(organizations);
    }


    // GET: /Organizations/Create
    // Показва празния формуляр за нова организация
    [HttpGet]
    public IActionResult Create()
    {
        return View(new OrganizationCreateViewModel());
    }

    // POST: /Organizations/Create
    // Получава данните и създава организация в SQL Server
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        OrganizationCreateViewModel model)
    {
        // Проверяваме въведените данни
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // Премахваме излишните интервали
        var name = model.Name.Trim();

        // Проверяваме дължината след премахване на интервалите
        if (name.Length < 3)
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "Organization name must be at least 3 characters.");

            return View(model);
        }

        // Създаваме нов Entity обект
        var organization = new Organization
        {
            Name = name,
            RegistrationNumber = string.IsNullOrWhiteSpace(
                model.RegistrationNumber)
                    ? null
                    : model.RegistrationNumber.Trim(),
            IsActive = true
        };

        // Добавяме обекта за запис в базата данни
        _context.Organizations.Add(organization);

        // Изпълняваме INSERT заявката към SQL Server
        await _context.SaveChangesAsync();

        // Пренасочваме към списъка с организации
        return RedirectToAction(nameof(Index));
    }

    // GET: /Organizations/Edit/1
    // Зарежда организацията и показва формуляра
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        // Търсим организацията по Primary Key
        var organization = await _context.Organizations.FindAsync(id);

        // Връщаме 404, ако организацията не съществува
        if (organization == null)
            return NotFound();

        // Прехвърляме данните от Entity към ViewModel
        var model = new OrganizationEditViewModel
        {
            Id = organization.Id,
            Name = organization.Name,
            RegistrationNumber = organization.RegistrationNumber,
            IsActive = organization.IsActive
        };

        // Показваме формуляра с текущите стойности
        return View(model);
    }

    // POST: /Organizations/Edit/1
    // Записва направените промени в SQL Server
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id, OrganizationEditViewModel model)
    {
        // Проверяваме дали URL и формулярът сочат един запис
        if (id != model.Id)
            return BadRequest();

        // Проверяваме валидацията на въведените данни
        if (!ModelState.IsValid)
            return View(model);

        // Премахваме интервалите в началото и края
        var name = model.Name.Trim();

        if (name.Length < 3)
        {
            ModelState.AddModelError(
                nameof(model.Name),
                "Organization name must be at least 3 characters.");

            return View(model);
        }

        // Зареждаме съществуващия Entity от SQL Server
        var organization = await _context.Organizations.FindAsync(id);

        if (organization == null)
            return NotFound();

        // Актуализираме само разрешените полета
        organization.Name = name;
        organization.RegistrationNumber =
            string.IsNullOrWhiteSpace(model.RegistrationNumber)
                ? null
                : model.RegistrationNumber.Trim();

        organization.IsActive = model.IsActive;

        // EF Core автоматично открива променените стойности
        await _context.SaveChangesAsync();

        // Връщаме потребителя към списъка
        return RedirectToAction(nameof(Index));
    }

    // GET: /Organizations/Delete/1
    // Показва страницата за потвърждение
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        // Търсим организацията без проследяване на промени
        var organization = await _context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id);

        // Връщаме 404, ако организацията не съществува
        if (organization == null)
            return NotFound();

        // Проверяваме за свързани сертификати и потребители
        ViewBag.HasDependencies =
            await _context.CertificateSubmissions
                .AnyAsync(c => c.OrganizationId == id)
            ||
            await _context.Users
                .AnyAsync(u => u.OrganizationId == id);

        return View(organization);
    }

    // POST: /Organizations/Delete/1
    // Изтрива организацията след потвърждение
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        // Зареждаме организацията от базата данни
        var organization = await _context.Organizations.FindAsync(id);

        if (organization == null)
            return NotFound();

        // Повторна проверка за свързани записи
        var hasCertificates = await _context.CertificateSubmissions
            .AnyAsync(c => c.OrganizationId == id);

        var hasUsers = await _context.Users
            .AnyAsync(u => u.OrganizationId == id);

        if (hasCertificates || hasUsers)
        {
            TempData["Error"] =
                "Organization has related users or certificates.";

            return RedirectToAction(nameof(Delete), new { id });
        }

        // Маркираме организацията за изтриване
        _context.Organizations.Remove(organization);

        try
        {
            // Изпълняваме DELETE заявката към SQL Server
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Базата може да откаже изтриване при свързани записи
            TempData["Error"] =
                "Organization could not be deleted due to database constraints.";

            return RedirectToAction(nameof(Delete), new { id });
        }

        // Връщаме потребителя към списъка
        return RedirectToAction(nameof(Index));
    }

    // GET: /Organizations/Details/1
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        // Зареждаме избраната организация
        var organization = await _context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == id);

        if (organization == null)
            return NotFound();

        // Извличаме потребителите само на тази организация
        var users = await _context.Users
            .Where(u => u.OrganizationId == id)
            .AsNoTracking()
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Select(u => new OrganizationUserViewModel
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email ?? string.Empty
            })
            .ToListAsync();

        // Обединяваме организацията и потребителите
        var model = new OrganizationDetailsViewModel
        {
            Id = organization.Id,
            Name = organization.Name,
            RegistrationNumber = organization.RegistrationNumber,
            IsActive = organization.IsActive,
            Users = users
        };

        return View(model);
    }


}

