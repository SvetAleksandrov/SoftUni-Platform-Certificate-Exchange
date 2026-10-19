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
