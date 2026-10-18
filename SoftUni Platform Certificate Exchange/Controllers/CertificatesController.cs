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


// Достъп до Controller за Admin и Client
[Authorize(Roles = "Admin,Client")]
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

    // Връща организацията на текущия Client потребител
    private async Task<int?> GetClientOrganizationIdAsync()
    {
        // Взимаме потребителя от ASP.NET Identity
        var user = await _userManager.GetUserAsync(User);

        // Клиентът задължително трябва да има организация
        if (user?.OrganizationId is not int organizationId)
            return null;

        // Проверяваме дали организацията е активна
        bool isActive = await _context.Organizations
            .AsNoTracking()
            .AnyAsync(o => o.Id == organizationId && o.IsActive);

        // Неактивните организации нямат достъп
        return isActive ? organizationId : null;
    }



    // GET: /Certificates
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        // Подготвяме заявката към SQL Server
        var query = _context.CertificateSubmissions
            .Include(c => c.Organization)
            .AsNoTracking()
            .AsQueryable();

        // При Client филтрираме по неговата организация
        if (!User.IsInRole("Admin"))
        {
            var organizationId = await GetClientOrganizationIdAsync();

            if (organizationId == null)
                return Forbid();

            // Клиентът вижда само своите сертификати
            query = query.Where(c =>
                c.OrganizationId == organizationId.Value);
        }

        // Изпълняваме SQL заявката
        var certificates = await query
            .OrderByDescending(c => c.CreatedAtUtc)
            .ToListAsync();

        return View(certificates);
    }


    // GET: /Certificates/Details/1
    // Показва информацията за конкретен сертификат
    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {

        // Подготвяме заявката за конкретния сертификат
        var query = _context.CertificateSubmissions
            .Include(c => c.Organization)
            .AsNoTracking()
            .AsQueryable();

        // Ограничаваме Client до собствената организация
        if (!User.IsInRole("Admin"))
        {
            var organizationId = await GetClientOrganizationIdAsync();

            if (organizationId == null)
                return Forbid();

            query = query.Where(c =>
                c.OrganizationId == organizationId.Value);
        }

        // Търсим сертификата само сред разрешените записи
        var certificate = await query
            .FirstOrDefaultAsync(c => c.Id == id);


        // Връщаме HTTP 404, ако не съществува
        if (certificate == null)
            return NotFound();

        // Извличаме решението за конкретния сертификат
        var review = await _context.CertificateReviews
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.CertificateSubmissionId == id);

        // Предаваме решението към Razor View
        ViewBag.Review = review;

        // Намираме имейла на служителя, взел решението
        ViewBag.ReviewerEmail = review == null
            ? null
            : await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == review.ReviewedByUserId)
                .Select(u => u.Email)
                .FirstOrDefaultAsync();


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
    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var model = new CertificateUploadViewModel();

        if (User.IsInRole("Admin"))
        {
            // Admin може да избира клиентска организация
            await LoadOrganizationsAsync();
        }
        else
        {
            // Client използва само собствената си организация
            var organizationId = await GetClientOrganizationIdAsync();

            if (organizationId == null)
                return Forbid();

            model.OrganizationId = organizationId.Value;

            // Зареждаме името на организацията за визуализация
            ViewBag.ClientOrganizationName = await _context.Organizations
                .Where(o => o.Id == organizationId.Value)
                .Select(o => o.Name)
                .FirstOrDefaultAsync();
        }

        return View(model);
    }

    // POST: /Certificates/Create
    // Получава файла, валидира го и записва сертификата
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(512 * 1024)]
    public async Task<IActionResult> Create(
        CertificateUploadViewModel model)
    {

        // За Client определяме организацията от Identity
        if (!User.IsInRole("Admin"))
        {
            var organizationId = await GetClientOrganizationIdAsync();

            if (organizationId == null)
                return Forbid();

            // Игнорираме OrganizationId, изпратен от браузъра
            model.OrganizationId = organizationId.Value;

            // Премахваме резултата от валидацията на входното ID
            // Реалното ID вече е проверено на сървъра
            ModelState.Remove(nameof(model.OrganizationId));

            // Запазваме името за повторно показване при грешка
            ViewBag.ClientOrganizationName = await _context.Organizations
                .Where(o => o.Id == organizationId.Value)
                .Select(o => o.Name)
                .FirstOrDefaultAsync();
        }

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

    // Само Admin може да отваря Review страницата
    // GET: /Certificates/Review/1
    // Показва формуляра за одобрение или отказ
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> Review(int id)
    {
        // Зареждаме сертификата заедно с организацията
        var certificate = await _context.CertificateSubmissions
            .Include(c => c.Organization)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id);

        // Проверяваме дали сертификатът съществува
        if (certificate == null)
            return NotFound();

        // Позволяваме преглед само на подадени сертификати
        if (certificate.Status != CertificateStatus.Submitted &&
            certificate.Status != CertificateStatus.UnderReview)
        {
            return BadRequest("Certificate is not available for review.");
        }

        // Предаваме сертификата за визуализация във View
        ViewBag.Certificate = certificate;

        // Създаваме ViewModel за формуляра
        var model = new CertificateReviewViewModel
        {
            CertificateSubmissionId = certificate.Id
        };

        return View(model);
    }

    // Само Admin може да отваря Review страницата
    // POST: /Certificates/Review/1
    // Обработва и записва решението на служителя
    [Authorize(Roles = "Admin")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Review(
        int id, CertificateReviewViewModel model)
    {
        // Проверяваме дали ID в URL и формуляра съвпадат
        if (id != model.CertificateSubmissionId)
            return BadRequest();

        // Зареждаме сертификата от SQL Server
        var certificate = await _context.CertificateSubmissions
            .Include(c => c.Organization)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (certificate == null)
            return NotFound();

        // Не позволяваме повторно одобрение или отказ
        if (certificate.Status != CertificateStatus.Submitted &&
            certificate.Status != CertificateStatus.UnderReview)
        {
            return Conflict("Certificate has already been processed.");
        }

        // Проверяваме за вече съществуващо решение
        bool reviewExists = await _context.CertificateReviews
            .AnyAsync(r => r.CertificateSubmissionId == id);

        if (reviewExists)
            return Conflict("A review already exists.");

        // Проверяваме дали решението е валидна Enum стойност
        if (model.Decision.HasValue &&
            !Enum.IsDefined(model.Decision.Value))
        {
            ModelState.AddModelError(
                nameof(model.Decision), "Invalid decision.");
        }

        // Проверяваме Data Annotations и бизнес валидацията
        if (!ModelState.IsValid)
        {
            ViewBag.Certificate = certificate;
            return View(model);
        }

        // Получаваме ID на текущия служител
        var userId = _userManager.GetUserId(User);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized();

        // Създаваме записа за одобрение или отказ
        var review = new CertificateReview
        {
            CertificateSubmissionId = certificate.Id,
            ReviewedByUserId = userId,
            Decision = model.Decision!.Value,
            Comment = model.Comment?.Trim(),
            ReviewedAtUtc = DateTime.UtcNow
        };

        // Актуализираме бизнес статуса на сертификата
        certificate.Status = model.Decision.Value ==
            CertificateReviewDecision.Approved
                ? CertificateStatus.Approved
                : CertificateStatus.Rejected;

        // Добавяме решението в базата
        _context.CertificateReviews.Add(review);

        // Записваме решението и новия статус в SQL Server
        await _context.SaveChangesAsync();

        // Отваряме актуализираните детайли на сертификата
        return RedirectToAction(
            nameof(Details),
            new { id = certificate.Id });
    }


}
