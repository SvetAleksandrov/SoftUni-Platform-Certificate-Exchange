namespace SoftUni_Platform_Certificate_Exchange.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SoftUni_Platform_Certificate_Exchange.Data;
using SoftUni_Platform_Certificate_Exchange.Models;
using SoftUni_Platform_Certificate_Exchange.ViewModels;
using SoftUni_Platform_Certificate_Exchange.Services;


// Само Admin може да създава потребители
[Authorize(Roles = "Admin")]
public class UsersController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    // Dependency Injection
    public UsersController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    // GET: /Users
    // Показва всички потребители и техните организации

    [HttpGet]
    public async Task<IActionResult> Index(
        string? firstNameSearch,
        string? lastNameSearch,
        string? emailSearch,
        string? organizationSearch,
        string groupBy = "none",
        string sort = "lastname_asc")
    {
        // 1. Подготвяме SQL заявката, без да я изпълняваме
        var query = _context.Users
            .AsNoTracking()
            .AsQueryable();

        // 2. SEARCH: First Name
        firstNameSearch = firstNameSearch?.Trim() ?? "";

        if (!string.IsNullOrWhiteSpace(firstNameSearch))
        {
            query = query.Where(u =>
                u.FirstName.Contains(firstNameSearch));
        }

        // 3. SEARCH: Last Name
        lastNameSearch = lastNameSearch?.Trim() ?? "";

        if (!string.IsNullOrWhiteSpace(lastNameSearch))
        {
            query = query.Where(u =>
                u.LastName.Contains(lastNameSearch));
        }

        // 4. SEARCH: Email
        emailSearch = emailSearch?.Trim() ?? "";

        if (!string.IsNullOrWhiteSpace(emailSearch))
        {
            query = query.Where(u =>
                u.Email != null &&
                u.Email.Contains(emailSearch));
        }

        // 5. SEARCH: Organization
        organizationSearch = organizationSearch?.Trim() ?? "";

        if (!string.IsNullOrWhiteSpace(organizationSearch))
        {
            // Разпознаваме и вътрешните потребители
            bool matchesInternal = "Internal / System".Contains(
                organizationSearch,
                StringComparison.OrdinalIgnoreCase);

            query = query.Where(u =>
                (u.Organization != null &&
                 u.Organization.Name.Contains(organizationSearch)) ||
                (matchesInternal && u.OrganizationId == null));
        }

        // 6. GROUP BY: Разрешените колони
        groupBy = groupBy?.Trim().ToLowerInvariant() ?? "none";

        string[] allowedGroups =
        {
        "none",
        "firstname",
        "lastname",
        "email",
        "organization"
    };

        if (!allowedGroups.Contains(groupBy))
            groupBy = "none";

        // 7. SORTING: Разрешените колони и посоки
        sort = sort?.Trim().ToLowerInvariant() ?? "lastname_asc";

        string[] allowedSorts =
        {
        "firstname_asc", "firstname_desc",
        "lastname_asc", "lastname_desc",
        "email_asc", "email_desc",
        "organization_asc", "organization_desc"
    };

        if (!allowedSorts.Contains(sort))
            sort = "lastname_asc";

        // 8. Сортираме данните в SQL Server
        var orderedQuery = sort switch
        {
            "firstname_asc" => query
                .OrderBy(u => u.FirstName),

            "firstname_desc" => query
                .OrderByDescending(u => u.FirstName),

            "lastname_desc" => query
                .OrderByDescending(u => u.LastName),

            "email_asc" => query
                .OrderBy(u => u.Email),

            "email_desc" => query
                .OrderByDescending(u => u.Email),

            "organization_asc" => query
                .OrderBy(u => u.Organization != null
                    ? u.Organization.Name : "Internal / System"),

            "organization_desc" => query
                .OrderByDescending(u => u.Organization != null
                    ? u.Organization.Name : "Internal / System"),

            // По подразбиране: Last Name A-Z
            _ => query.OrderBy(u => u.LastName)
        };

        // 9. Извличаме само необходимите данни
        var users = await orderedQuery
            .ThenBy(u => u.Id)
            .Select(u => new UserListItemViewModel
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email ?? string.Empty,
                OrganizationId = u.OrganizationId,
                OrganizationName = u.Organization != null
                    ? u.Organization.Name
                    : null
            })
            .ToListAsync();

        // 10. Връщаме филтрите към Razor View
        ViewBag.FirstNameSearch = firstNameSearch;
        ViewBag.LastNameSearch = lastNameSearch;
        ViewBag.EmailSearch = emailSearch;
        ViewBag.OrganizationSearch = organizationSearch;

        ViewBag.GroupBy = groupBy;
        ViewBag.Sort = sort;

        return View(users);
    }


    // Зареждаме активните организации
    private async Task LoadOrganizationsAsync()
    {
        var organizations = await _context.Organizations
            .Where(o => o.IsActive)
            .OrderBy(o => o.Name)
            .AsNoTracking()
            .ToListAsync();

        ViewBag.Organizations = new SelectList(
            organizations, "Id", "Name");
    }

    // GET: /Users/Create
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadOrganizationsAsync();
        return View(new CreateClientUserViewModel());
    }


    // POST: /Users/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateClientUserViewModel model)
    {
        // Проверяваме данните от формуляра
        if (!ModelState.IsValid)
        {
            await LoadOrganizationsAsync();
            return View(model);
        }

        // Проверяваме клиентската организация
        var organization = await _context.Organizations
            .AsNoTracking()
            .FirstOrDefaultAsync(o =>
                o.Id == model.OrganizationId && o.IsActive);

        if (organization == null)
        {
            ModelState.AddModelError(
                nameof(model.OrganizationId), "Invalid organization.");

            await LoadOrganizationsAsync();
            return View(model);
        }

        // Проверяваме дали имейлът вече съществува
        var email = model.Email.Trim();

        if (await _userManager.FindByEmailAsync(email) != null)
        {
            ModelState.AddModelError(
                nameof(model.Email), "Email already exists.");

            await LoadOrganizationsAsync();
            return View(model);
        }

        // Генерираме случайна временна парола
        var temporaryPassword = TemporaryPasswordGenerator.Generate();

        // Създаваме клиентски потребител
        var user = new ApplicationUser
        {
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            Email = email,
            UserName = email,
            OrganizationId = organization.Id,

            // Тестов режим без потвърждение на имейл
            EmailConfirmed = true,

            // Задължителна смяна при първи Login
            MustChangePassword = true
        };

        // Identity съхранява хеш, а не текстовата парола
        var result = await _userManager.CreateAsync(
            user, temporaryPassword);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            await LoadOrganizationsAsync();
            return View(model);
        }

        // Присвояваме ролята Client
        var roleResult = await _userManager.AddToRoleAsync(
            user, "Client");

        if (!roleResult.Succeeded)
        {
            // Премахваме създадения акаунт при неуспешна роля
            var rollback = await _userManager.DeleteAsync(user);

            if (!rollback.Succeeded)
                throw new InvalidOperationException(
                    "User rollback failed.");

            ModelState.AddModelError(
                "", "Client role could not be assigned.");

            await LoadOrganizationsAsync();
            return View(model);
        }

        // Забраняваме HTTP кеширане на страницата с паролата
        Response.Headers["Cache-Control"] = "no-store, no-cache";
        Response.Headers["Pragma"] = "no-cache";

        // Показваме временната парола само в този отговор
        return View("Created", new ClientCreatedViewModel
        {
            Email = user.Email!,
            OrganizationName = organization.Name,
            TemporaryPassword = temporaryPassword
        });
    }

}
