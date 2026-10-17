namespace SoftUni_Platform_Certificate_Exchange.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoftUni_Platform_Certificate_Exchange.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using SoftUni_Platform_Certificate_Exchange.Models;
using SoftUni_Platform_Certificate_Exchange.ViewModels;
using SoftUni_Platform_Certificate_Exchange.Services.Interfaces;


// Засега само Admin има достъп до сертификатите
[Authorize(Roles = "Admin")]
public class CertificatesController : Controller
{
    // Достъп до базата данни
    private readonly ApplicationDbContext _context;

    // Услуга за проверка на SSL/TLS сертификати
    private readonly ICertificateValidationService _validator;

    // Управление на Identity потребители
    private readonly UserManager<ApplicationUser> _userManager;

    // Dependency Injection на необходимите услуги
    public CertificatesController(
        ApplicationDbContext context,
        ICertificateValidationService validator,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _validator = validator;
        _userManager = userManager;
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

    // Зарежда активните организации за падащото меню
    private async Task LoadOrganizationsAsync()
    {
        ViewBag.Organizations = new SelectList(
            await _context.Organizations
                .Where(o => o.IsActive)
                .OrderBy(o => o.Name)
                .AsNoTracking()
                .ToListAsync(),
            "Id",
            "Name");
    }

    // GET: /Certificates/Create
    // Показва формата за качване на сертификат
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        await LoadOrganizationsAsync();

        return View(new CertificateUploadViewModel());
    }

    // POST: /Certificates/Create
    // Получава файла, валидира го и записва сертификата
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(512 * 1024)]
    public async Task<IActionResult> Create(
        CertificateUploadViewModel model)
    {
        // Проверяваме полетата от формуляра
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
                nameof(model.OrganizationId),
                "Invalid organization.");

            await LoadOrganizationsAsync();
            return View(model);
        }

        // Получаваме качения файл
        var file = model.CertificateFile!;

        // Максимален размер: 256 KB
        if (file.Length == 0 || file.Length > 256 * 1024)
        {
            ModelState.AddModelError(
                nameof(model.CertificateFile),
                "Maximum certificate size is 256 KB.");

            await LoadOrganizationsAsync();
            return View(model);
        }

        // Прочитаме съдържанието на файла
        using var stream = new MemoryStream();
        await file.CopyToAsync(stream);
        byte[] fileBytes = stream.ToArray();

        // Извикваме услугата за проверка на сертификата
        var validation = _validator.Validate(
            fileBytes,
            file.FileName,
            model.TargetHostname.Trim());

        // Прекратяваме, ако сертификатът е невалиден
        if (!validation.IsValid)
        {
            ModelState.AddModelError(
                nameof(model.CertificateFile),
                validation.Message);

            await LoadOrganizationsAsync();
            return View(model);
        }

        // Получаваме ID на текущия потребител
        var userId = _userManager.GetUserId(User);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Използваме защитена локална папка извън wwwroot
        var storagePath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "SecureCertPortal",
            "Certificates");

        // Създаваме папката, ако не съществува
        Directory.CreateDirectory(storagePath);

        // Генерираме уникално име, независимо от името на клиента
        var storageKey = Guid.NewGuid().ToString("N") +
            Path.GetExtension(file.FileName).ToLowerInvariant();

        var filePath = Path.Combine(storagePath, storageKey);

        // Създаваме Entity обекта за SQL Server
        var certificate = new CertificateSubmission
        {
            OrganizationId = model.OrganizationId,
            UploadedByUserId = userId,
            TargetHostname = model.TargetHostname.Trim(),
            Description = model.Description,
            OriginalFileName = Path.GetFileName(file.FileName),
            StorageKey = storageKey,
            Subject = validation.Subject,
            Issuer = validation.Issuer,
            Thumbprint = validation.Thumbprint,
            NotBeforeUtc = validation.NotBeforeUtc,
            NotAfterUtc = validation.NotAfterUtc,
            Status = CertificateStatus.Submitted
        };

        try
        {
            // Съхраняваме реалния файл извън публичната директория
            await System.IO.File.WriteAllBytesAsync(filePath, fileBytes);

            // Записваме метаданните в SQL Server
            _context.CertificateSubmissions.Add(certificate);
            await _context.SaveChangesAsync();
        }
        catch
        {
            // Премахваме файла при неуспешен запис
            if (System.IO.File.Exists(filePath))
                System.IO.File.Delete(filePath);

            throw;
        }

        // Отваряме детайлите на новия сертификат
        return RedirectToAction(
            nameof(Details),
            new { id = certificate.Id });
    }

}
