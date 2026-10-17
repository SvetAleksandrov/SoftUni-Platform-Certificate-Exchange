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


}

