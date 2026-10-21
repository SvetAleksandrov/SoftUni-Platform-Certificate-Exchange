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
using System.Text.RegularExpressions;


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

    // Logger за технически събития и грешки
    private readonly ILogger<CertificatesController> _logger;

    // Dependency Injection на всички необходими услуги
    public CertificatesController(
        ApplicationDbContext context,
        ICertificateValidationService validator,
        UserManager<ApplicationUser> userManager,
        ILogger<CertificatesController> logger)
    {
        _context = context;
        _validator = validator;
        _userManager = userManager;
        _logger = logger;
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

    [HttpGet]
    public async Task<IActionResult> Index(
    string? organizationSearch,
    string? hostnameSearch,
    DateTime? validFromDate,
    DateTime? validToDate,
    string statusSearch = "all",
    DateTime? submittedDate = null,
    string groupBy = "none",
    string sort = "submitted_desc")
    {
        // 1. Подготвяме заявката към базата данни
        var query = _context.CertificateSubmissions
            .Include(c => c.Organization)
            .AsNoTracking()
            .AsQueryable();

        // 2. SECURITY: Client вижда само собствената организация
        if (!User.IsInRole("Admin"))
        {
            var organizationId = await GetClientOrganizationIdAsync();

            if (organizationId == null)
                return Forbid();

            query = query.Where(c =>
                c.OrganizationId == organizationId.Value);
        }

        // 3. SEARCH: Organization
        organizationSearch = organizationSearch?.Trim() ?? "";

        if (!string.IsNullOrWhiteSpace(organizationSearch))
        {
            query = query.Where(c =>
                c.Organization.Name.Contains(organizationSearch));
        }

        // 4. SEARCH: Hostname
        hostnameSearch = hostnameSearch?.Trim() ?? "";

        if (!string.IsNullOrWhiteSpace(hostnameSearch))
        {
            query = query.Where(c =>
                c.TargetHostname.Contains(hostnameSearch));
        }

        // 5. VALID TO: филтрираме по датата на изтичане (UTC)

        if (validFromDate.HasValue)
        {
            var start = validFromDate.Value.Date;
            var end = start.AddDays(1);

            query = query.Where(c =>
                c.NotBeforeUtc >= start &&
                c.NotBeforeUtc < end);
        }


        // 6 VALID TO: филтрираме по датата на изтичане (UTC)
        if (validToDate.HasValue)
        {
            var start = validToDate.Value.Date;
            var end = start.AddDays(1);

            query = query.Where(c =>
                c.NotAfterUtc >= start &&
                c.NotAfterUtc < end);
        }


        // 7. FILTER: Status - използваме CertificateStatus enum
        statusSearch = statusSearch?.Trim() ?? "all";

        if (!statusSearch.Equals(
            "all", StringComparison.OrdinalIgnoreCase))
        {
            // Приемаме само действителни стойности от enum
            if (Enum.TryParse<CertificateStatus>(
                    statusSearch, true, out var selectedStatus)
                && Enum.IsDefined(selectedStatus))
            {
                query = query.Where(c =>
                    c.Status == selectedStatus);

                statusSearch = selectedStatus.ToString();
            }
            else
            {
                statusSearch = "all";
            }
        }
        else
        {
            statusSearch = "all";
        }


        // 8 SUBMITTED AT: всички заявки от избрания UTC ден
        if (submittedDate.HasValue)
        {
            var start = submittedDate.Value.Date;
            var end = start.AddDays(1);

            query = query.Where(c =>
                c.CreatedAtUtc >= start &&
                c.CreatedAtUtc < end);
        }


        // 9. GROUP BY: Валидираме разрешените колони
        groupBy = groupBy?.Trim().ToLowerInvariant() ?? "none";

        string[] allowedGroups =
        {
        "none",
        "organization",
        "hostname",
        "validfrom",
        "validto",
        "status",
        "submitted"
    };

        if (!allowedGroups.Contains(groupBy))
            groupBy = "none";

        // 10. SORTING: Валидираме разрешените критерии
        sort = sort?.Trim().ToLowerInvariant() ?? "submitted_desc";

        string[] allowedSorts =
        {
        "organization_asc", "organization_desc",
        "hostname_asc", "hostname_desc",
        "validfrom_asc", "validfrom_desc",
        "validto_asc", "validto_desc",
        "status_asc", "status_desc",
        "submitted_asc", "submitted_desc"
    };

        if (!allowedSorts.Contains(sort))
            sort = "submitted_desc";

        // 11. Сортираме в SQL Server
        var orderedQuery = sort switch
        {
            "organization_asc" => query
                .OrderBy(c => c.Organization.Name),

            "organization_desc" => query
                .OrderByDescending(c => c.Organization.Name),

            "hostname_asc" => query
                .OrderBy(c => c.TargetHostname),

            "hostname_desc" => query
                .OrderByDescending(c => c.TargetHostname),

            "validfrom_asc" => query
                .OrderBy(c => c.NotBeforeUtc),

            "validfrom_desc" => query
                .OrderByDescending(c => c.NotBeforeUtc),

            "validto_asc" => query
                .OrderBy(c => c.NotAfterUtc),

            "validto_desc" => query
                .OrderByDescending(c => c.NotAfterUtc),

            "status_asc" => query
                .OrderBy(c => c.Status),

            "status_desc" => query
                .OrderByDescending(c => c.Status),

            "submitted_asc" => query
                .OrderBy(c => c.CreatedAtUtc),

            // По подразбиране: най-новите заявки първо
            _ => query.OrderByDescending(c => c.CreatedAtUtc)
        };

        // 12. Изпълняваме заявката
        // ID осигурява стабилен ред при равни стойности
        var certificates = await orderedQuery
            .ThenBy(c => c.Id)
            .ToListAsync();

        // 13. Връщаме критериите обратно към Razor
        ViewBag.OrganizationSearch = organizationSearch;
        ViewBag.HostnameSearch = hostnameSearch;


        // Запазваме избраните дати за Razor View
        ViewBag.ValidFromDate =
            validFromDate?.ToString("yyyy-MM-dd") ?? "";

        ViewBag.ValidToDate =
            validToDate?.ToString("yyyy-MM-dd") ?? "";

        ViewBag.SubmittedDate =
            submittedDate?.ToString("yyyy-MM-dd") ?? "";

        ViewBag.GroupBy = groupBy;
        ViewBag.Sort = sort;

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

    // GET: /Certificates/Download/1
    // Изтегля публичния сертификат при разрешен достъп
    [HttpGet]
    public async Task<IActionResult> Download(int id)
    {
        // Подготвяме заявката към SQL Server
        var query = _context.CertificateSubmissions
            .AsNoTracking()
            .AsQueryable();

        // Client може да изтегля само сертификати на своята организация
        if (!User.IsInRole("Admin"))
        {
            var organizationId = await GetClientOrganizationIdAsync();

            if (organizationId == null)
                return Forbid();

            // Филтрираме данните по организация
            query = query.Where(c =>
                c.OrganizationId == organizationId.Value);
        }

        // Търсим сертификата само сред разрешените записи
        var certificate = await query
            .FirstOrDefaultAsync(c => c.Id == id);

        if (certificate == null)
            return NotFound();

        // Проверяваме дали има записан файл
        if (string.IsNullOrWhiteSpace(certificate.StorageKey))
            return NotFound();

        // Допускаме само генерирано GUID име и разрешено разширение
        if (!Regex.IsMatch(
            certificate.StorageKey,
            @"\A[a-fA-F0-9]{32}\.(cer|crt|pem)\z",
            RegexOptions.IgnoreCase))
        {
            return NotFound();
        }

        // Използваме същата защитена директория като при Upload
        var storagePath = Path.Combine(
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData),
            "SecureCertPortal",
            "Certificates");

        // Създаваме пълния път до файла
        var filePath = Path.Combine(
            storagePath,
            certificate.StorageKey);

        // Проверяваме дали файлът съществува
        if (!System.IO.File.Exists(filePath))
            return NotFound();

        // Генерираме безопасно име за изтеглянето
        var downloadName = $"certificate-{certificate.Id}" +
            Path.GetExtension(certificate.StorageKey);

        // Забраняваме кеширане на отговора
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";

        // Отваряме файла само за четене
        // ASP.NET Core ще освободи stream-а след изпращането
        var stream = System.IO.File.OpenRead(filePath);

        // Връщаме файла като download, без директен публичен URL
        return File(
            stream,
            "application/octet-stream",
            downloadName);
    }

    // Намира сертификат, до който потребителят има достъп
    private async Task<CertificateSubmission?> FindEditableCertificateAsync(int id)
    {
        var query = _context.CertificateSubmissions.AsQueryable();

        // Client има достъп само до собствената си организация
        if (!User.IsInRole("Admin"))
        {
            var organizationId = await GetClientOrganizationIdAsync();

            if (organizationId == null)
                return null;

            query = query.Where(c =>
                c.OrganizationId == organizationId.Value);
        }

        return await query.FirstOrDefaultAsync(c => c.Id == id);
    }

    // GET: /Certificates/Edit/1
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        // Зареждаме сертификата с проверка за достъп
        var certificate = await FindEditableCertificateAsync(id);

        if (certificate == null)
            return NotFound();

        // Позволяваме редакция само преди обработване
        if (certificate.Status != CertificateStatus.Draft &&
            certificate.Status != CertificateStatus.Submitted)
            return Conflict("Certificate cannot be edited.");

        // Проверяваме дали вече има решение
        if (await _context.CertificateReviews.AnyAsync(
            r => r.CertificateSubmissionId == id))
            return Conflict("Certificate has already been reviewed.");

        // Подаваме hostname само за визуализация
        ViewBag.Hostname = certificate.TargetHostname;

        var model = new CertificateEditViewModel
        {
            Id = certificate.Id,
            Description = certificate.Description
        };

        return View(model);
    }

    // POST: /Certificates/Edit/1
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id, CertificateEditViewModel model)
    {
        // Проверяваме дали ID съвпадат
        if (id != model.Id)
            return BadRequest();

        // Зареждаме разрешения за редакция сертификат
        var certificate = await FindEditableCertificateAsync(id);

        if (certificate == null)
            return NotFound();

        // Не допускаме промяна след обработване
        if (certificate.Status != CertificateStatus.Draft &&
            certificate.Status != CertificateStatus.Submitted)
            return Conflict("Certificate cannot be edited.");

        // Допълнителна проверка за съществуващо решение
        if (await _context.CertificateReviews.AnyAsync(
            r => r.CertificateSubmissionId == id))
            return Conflict("Certificate has already been reviewed.");

        // Проверяваме Data Annotations
        if (!ModelState.IsValid)
        {
            ViewBag.Hostname = certificate.TargetHostname;
            return View(model);
        }

        // Променяме само разрешеното поле
        certificate.Description =
            string.IsNullOrWhiteSpace(model.Description)
                ? null
                : model.Description.Trim();

        // EF Core изпълнява UPDATE в SQL Server
        await _context.SaveChangesAsync();

        // Връщаме се към детайлите
        return RedirectToAction(nameof(Details), new { id });
    }

    // GET: /Certificates/Delete/1
    // Показва страницата за потвърждение
    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        // Зареждаме само сертификат, до който имаме достъп
        var certificate = await FindEditableCertificateAsync(id);

        if (certificate == null)
            return NotFound();

        // Изтриваме само Draft или Submitted
        if (certificate.Status != CertificateStatus.Draft &&
            certificate.Status != CertificateStatus.Submitted)
            return Conflict("Certificate cannot be deleted.");

        // Не позволяваме изтриване при записано решение
        bool hasReview = await _context.CertificateReviews
            .AnyAsync(r => r.CertificateSubmissionId == id);

        if (hasReview)
            return Conflict("Certificate has review history.");

        return View(certificate);
    }

    // POST: /Certificates/Delete/1
    // Изтрива сертификата след потвърждение
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        // Проверяваме достъпа до сертификата
        var certificate = await FindEditableCertificateAsync(id);

        if (certificate == null)
            return NotFound();

        // Изтриваме само необработени сертификати
        int deletedRows = await _context.CertificateSubmissions
            .Where(c => c.Id == id &&
                c.OrganizationId == certificate.OrganizationId &&
                (c.Status == CertificateStatus.Draft ||
                 c.Status == CertificateStatus.Submitted) &&
                !_context.CertificateReviews.Any(r =>
                    r.CertificateSubmissionId == c.Id))
            .ExecuteDeleteAsync();

        // Ако сертификатът е променен, отказваме изтриването
        if (deletedRows != 1)
            return Conflict("Certificate was changed or reviewed.");

        // Проверяваме името, генерирано при Upload
        var storageKey = certificate.StorageKey;

        if (!string.IsNullOrWhiteSpace(storageKey) &&
            System.Text.RegularExpressions.Regex.IsMatch(
                storageKey,
                @"\A[a-fA-F0-9]{32}\.(cer|crt|pem)\z",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase))
        {
            // Използваме същата директория като при Upload
            var storagePath = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "SecureCertPortal",
                "Certificates");

            var filePath = Path.Combine(storagePath, storageKey);

            try
            {
                // Премахваме физическия файл, ако съществува
                System.IO.File.Delete(filePath);
            }
            catch (Exception ex) when (
                ex is IOException or UnauthorizedAccessException)
            {
                // Файлът остава за последващо администраторско почистване
                _logger.LogWarning(ex,
                    "File cleanup failed for certificate {Id}", id);
            }
        }

        // Показваме съобщение след изтриването
        TempData["Success"] = "Certificate submission deleted.";

        return RedirectToAction(nameof(Index));
    }


}
