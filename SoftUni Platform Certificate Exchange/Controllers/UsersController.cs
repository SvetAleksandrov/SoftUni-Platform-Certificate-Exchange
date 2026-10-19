namespace SoftUni_Platform_Certificate_Exchange.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SoftUni_Platform_Certificate_Exchange.Data;
using SoftUni_Platform_Certificate_Exchange.Models;
using SoftUni_Platform_Certificate_Exchange.ViewModels;

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
    public async Task<IActionResult> Index()
    {
        // Извличаме потребителите от Identity таблицата
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.LastName)
            .ThenBy(u => u.FirstName)
            .Select(u => new UserListItemViewModel
            {
                Id = u.Id,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Email = u.Email ?? string.Empty,
                OrganizationId = u.OrganizationId,

                // EF Core извлича името на свързаната организация
                OrganizationName = u.Organization != null
                    ? u.Organization.Name
                    : null
            })
            .ToListAsync();

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
        // Проверяваме валидацията на формуляра
        if (!ModelState.IsValid)
        {
            await LoadOrganizationsAsync();
            return View(model);
        }

        // Проверяваме дали организацията е активна
        bool organizationExists = await _context.Organizations
            .AnyAsync(o => o.Id == model.OrganizationId && o.IsActive);

        if (!organizationExists)
        {
            ModelState.AddModelError(
                nameof(model.OrganizationId), "Invalid organization.");

            await LoadOrganizationsAsync();
            return View(model);
        }

        // Проверяваме за вече регистриран имейл
        var email = model.Email.Trim();

        if (await _userManager.FindByEmailAsync(email) != null)
        {
            ModelState.AddModelError(
                nameof(model.Email), "Email already exists.");

            await LoadOrganizationsAsync();
            return View(model);
        }

        // Създаваме потребител от тип ApplicationUser
        var user = new ApplicationUser
        {
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            Email = email,
            UserName = email,
            OrganizationId = model.OrganizationId,

            // Само за курсовата версия без email verification
            EmailConfirmed = true
        };

        // Identity създава потребителя и хешира паролата
        var result = await _userManager.CreateAsync(
            user, model.Password);

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
            // Опит за премахване при неуспешно добавяне на роля
            var deleteResult = await _userManager.DeleteAsync(user);

            if (!deleteResult.Succeeded)
                throw new InvalidOperationException(
                    "User rollback failed after role assignment error.");

            ModelState.AddModelError(
                "", "Client role could not be assigned.");

            await LoadOrganizationsAsync();
            return View(model);
        }

        // Връщаме се към организациите
        TempData["Success"] = "Client user created successfully.";

        return RedirectToAction("Index", "Organizations");
    }
}
