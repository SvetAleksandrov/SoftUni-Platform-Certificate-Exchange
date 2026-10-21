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
    string? phoneSearch,
    string? departmentSearch,
    string? jobTitleSearch,
    string? organizationSearch,
    string groupBy = "none",
    string sort = "lastname_asc")
    {
        // Подготвяме заявката към Identity потребителите
        var query = _context.Users.AsNoTracking().AsQueryable();

        // Премахваме излишните интервали
        firstNameSearch = firstNameSearch?.Trim() ?? "";
        lastNameSearch = lastNameSearch?.Trim() ?? "";
        emailSearch = emailSearch?.Trim() ?? "";
        phoneSearch = phoneSearch?.Trim() ?? "";
        departmentSearch = departmentSearch?.Trim() ?? "";
        jobTitleSearch = jobTitleSearch?.Trim() ?? "";
        organizationSearch = organizationSearch?.Trim() ?? "";

        // Независими филтри - комбинират се чрез AND
        if (firstNameSearch.Length > 0)
            query = query.Where(u =>
                u.FirstName.Contains(firstNameSearch));

        if (lastNameSearch.Length > 0)
            query = query.Where(u =>
                u.LastName.Contains(lastNameSearch));

        if (emailSearch.Length > 0)
            query = query.Where(u =>
                u.Email != null && u.Email.Contains(emailSearch));

        if (phoneSearch.Length > 0)
            query = query.Where(u =>
                u.PhoneNumber != null &&
                u.PhoneNumber.Contains(phoneSearch));

        if (departmentSearch.Length > 0)
            query = query.Where(u =>
                u.Department != null &&
                u.Department.Contains(departmentSearch));

        if (jobTitleSearch.Length > 0)
            query = query.Where(u =>
                u.JobTitle != null &&
                u.JobTitle.Contains(jobTitleSearch));

        if (organizationSearch.Length > 0)
        {
            // Специална обработка за вътрешни потребители
            bool matchesInternal = "Internal / System".Contains(
                organizationSearch,
                StringComparison.OrdinalIgnoreCase);

            query = query.Where(u =>
                (u.Organization != null &&
                 u.Organization.Name.Contains(organizationSearch)) ||
                (matchesInternal && u.OrganizationId == null));
        }

        // Разрешени стойности за Group By
        groupBy = groupBy?.Trim().ToLowerInvariant() ?? "none";

        string[] allowedGroups =
        {
        "none", "firstname", "lastname", "email",
        "phone", "department", "jobtitle", "organization"
    };

        if (!allowedGroups.Contains(groupBy))
            groupBy = "none";

        // Разрешени стойности за Sorting
        sort = sort?.Trim().ToLowerInvariant() ?? "lastname_asc";

        string[] allowedSorts =
        {
        "firstname_asc", "firstname_desc",
        "lastname_asc", "lastname_desc",
        "email_asc", "email_desc",
        "phone_asc", "phone_desc",
        "department_asc", "department_desc",
        "jobtitle_asc", "jobtitle_desc",
        "organization_asc", "organization_desc"
    };

        if (!allowedSorts.Contains(sort))
            sort = "lastname_asc";

        // Сортиране в SQL Server
        var orderedQuery = sort switch
        {
            "firstname_asc" => query.OrderBy(u => u.FirstName),
            "firstname_desc" => query.OrderByDescending(u => u.FirstName),

            "lastname_desc" => query.OrderByDescending(u => u.LastName),

            "email_asc" => query.OrderBy(u => u.Email),
            "email_desc" => query.OrderByDescending(u => u.Email),

            "phone_asc" => query.OrderBy(u => u.PhoneNumber),
            "phone_desc" => query.OrderByDescending(u => u.PhoneNumber),

            "department_asc" => query.OrderBy(u => u.Department),
            "department_desc" => query.OrderByDescending(u => u.Department),

            "jobtitle_asc" => query.OrderBy(u => u.JobTitle),
            "jobtitle_desc" => query.OrderByDescending(u => u.JobTitle),

            "organization_asc" => query.OrderBy(u =>
                u.Organization != null
                    ? u.Organization.Name : "Internal / System"),

            "organization_desc" => query.OrderByDescending(u =>
                u.Organization != null
                    ? u.Organization.Name : "Internal / System"),

            _ => query.OrderBy(u => u.LastName)
        };

        // Извличаме колоните за таблицата
        var users = await orderedQuery
            .ThenBy(u => u.Id)
            .Select(u => new UserListItemViewModel
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email ?? "",
                PhoneNumber = u.PhoneNumber,
                Department = u.Department,
                JobTitle = u.JobTitle,
                OrganizationId = u.OrganizationId,
                OrganizationName = u.Organization != null
                    ? u.Organization.Name : null
            })
            .ToListAsync();

        // Възстановяваме стойностите в Razor View
        ViewBag.FirstNameSearch = firstNameSearch;
        ViewBag.LastNameSearch = lastNameSearch;
        ViewBag.EmailSearch = emailSearch;
        ViewBag.PhoneSearch = phoneSearch;
        ViewBag.DepartmentSearch = departmentSearch;
        ViewBag.JobTitleSearch = jobTitleSearch;
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
            PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber)
                 ? null : model.PhoneNumber.Trim(),

            Department = string.IsNullOrWhiteSpace(model.Department)
                 ? null : model.Department.Trim(),

            JobTitle = string.IsNullOrWhiteSpace(model.JobTitle)
                 ? null : model.JobTitle.Trim(),

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

    // GET: /Users/Edit/{id}
    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return NotFound();

        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
            return NotFound();

        // Не позволяваме редакция на Admin акаунти
        if (!await _userManager.IsInRoleAsync(user, "Client"))
            return Forbid();

        var model = new UserEditViewModel
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email ?? "",
            PhoneNumber = user.PhoneNumber,
            Department = user.Department,
            JobTitle = user.JobTitle,
            OrganizationId = user.OrganizationId ?? 0
        };

        await LoadOrganizationsAsync();

        return View(model);
    }

    // POST: /Users/Edit/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        string id, UserEditViewModel model)
    {
        if (id != model.Id)
            return BadRequest();

        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
            return NotFound();

        // Само Client акаунти могат да се редактират тук
        if (!await _userManager.IsInRoleAsync(user, "Client"))
            return Forbid();

        if (!ModelState.IsValid)
        {
            await LoadOrganizationsAsync();
            return View(model);
        }

        // Организацията трябва да съществува и да е активна
        bool organizationExists = await _context.Organizations
            .AnyAsync(o =>
                o.Id == model.OrganizationId && o.IsActive);

        if (!organizationExists)
        {
            ModelState.AddModelError(
                nameof(model.OrganizationId),
                "Invalid organization.");

            await LoadOrganizationsAsync();
            return View(model);
        }

        // Не прехвърляме потребител с вече подадени сертификати
        // към друга организация
        if (user.OrganizationId != model.OrganizationId &&
            await _context.CertificateSubmissions.AnyAsync(
                c => c.UploadedByUserId == user.Id))
        {
            ModelState.AddModelError(
                nameof(model.OrganizationId),
                "Cannot change organization after certificate submissions.");

            await LoadOrganizationsAsync();
            return View(model);
        }

        // Проверяваме за дублиращ се имейл
        var email = model.Email.Trim();
        var existingUser = await _userManager.FindByEmailAsync(email);

        if (existingUser != null && existingUser.Id != user.Id)
        {
            ModelState.AddModelError(
                nameof(model.Email),
                "Email is already in use.");

            await LoadOrganizationsAsync();
            return View(model);
        }

        // Проверяваме дали се променя имейлът
        bool emailChanged = !string.Equals(
            user.Email, email, StringComparison.OrdinalIgnoreCase);

        // Актуализираме разрешените данни
        user.FirstName = model.FirstName.Trim();
        user.LastName = model.LastName.Trim();
        user.Email = email;
        user.UserName = email;

        user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber)
            ? null : model.PhoneNumber.Trim();

        user.Department = string.IsNullOrWhiteSpace(model.Department)
            ? null : model.Department.Trim();

        user.JobTitle = string.IsNullOrWhiteSpace(model.JobTitle)
            ? null : model.JobTitle.Trim();

        user.OrganizationId = model.OrganizationId;

        // При смяна на имейл инвалидиране на старите сесии
        if (emailChanged)
            user.SecurityStamp = Guid.NewGuid().ToString("N");

        // Identity валидира и актуализира потребителя
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            await LoadOrganizationsAsync();
            return View(model);
        }

        TempData["Success"] = "Client user updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    // GET: /Users/Delete/{id}
    [HttpGet]
    public async Task<IActionResult> Delete(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return NotFound();

        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
            return NotFound();

        // Забраняваме изтриването на Admin и други роли
        if (!await _userManager.IsInRoleAsync(user, "Client"))
            return Forbid();

        // Проверяваме историята на сертификатите
        bool hasSubmissions = await _context.CertificateSubmissions
            .AnyAsync(c => c.UploadedByUserId == user.Id);

        bool hasReviews = await _context.CertificateReviews
            .AnyAsync(r => r.ReviewedByUserId == user.Id);

        // Зареждаме името на организацията
        string organizationName = "N/A";

        if (user.OrganizationId is int organizationId)
        {
            organizationName = await _context.Organizations
                .Where(o => o.Id == organizationId)
                .Select(o => o.Name)
                .FirstOrDefaultAsync() ?? "N/A";
        }

        var model = new UserDeleteViewModel
        {
            Id = user.Id,
            FullName = $"{user.FirstName} {user.LastName}",
            Email = user.Email ?? "",
            OrganizationName = organizationName,
            HasCertificateHistory = hasSubmissions || hasReviews
        };

        return View(model);
    }

    // POST: /Users/Delete/{id}
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return NotFound();

        var user = await _userManager.FindByIdAsync(id);

        if (user == null)
            return NotFound();

        // Само Client акаунти могат да се изтриват
        if (!await _userManager.IsInRoleAsync(user, "Client"))
            return Forbid();

        // Проверяваме историята отново непосредствено преди Delete
        bool hasSubmissions = await _context.CertificateSubmissions
            .AnyAsync(c => c.UploadedByUserId == user.Id);

        bool hasReviews = await _context.CertificateReviews
            .AnyAsync(r => r.ReviewedByUserId == user.Id);

        if (hasSubmissions || hasReviews)
        {
            TempData["Error"] =
                "User cannot be deleted because certificate history exists.";

            return RedirectToAction(nameof(Index));
        }

        // Изтриваме потребителя чрез Identity
        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            TempData["Error"] =
                "User deletion failed. Please try again.";

            return RedirectToAction(nameof(Index));
        }

        TempData["Success"] = "Client user deleted successfully.";

        return RedirectToAction(nameof(Index));
    }



}
