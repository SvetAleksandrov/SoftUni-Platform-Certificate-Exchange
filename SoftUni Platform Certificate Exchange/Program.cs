using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SoftUni_Platform_Certificate_Exchange.Data;
using SoftUni_Platform_Certificate_Exchange.Models;
using SoftUni_Platform_Certificate_Exchange.Services;
using SoftUni_Platform_Certificate_Exchange.Services.Interfaces;


// Създаваме конфигурацията на приложението
var builder = WebApplication.CreateBuilder(args);

// Регистрираме MVC Controllers и Views
builder.Services.AddControllersWithViews();

// Четем Connection String от appsettings.json
var connectionString = builder.Configuration
    .GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string not found.");

// Регистрираме Entity Framework Core със SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Регистрираме Identity за потребители и роли
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Конфигурираме адресите за Login и отказан достъп
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Регистрираме услугата чрез Dependency Injection
builder.Services.AddScoped<
    ICertificateValidationService,
    CertificateValidationService>();

// Създаваме приложението
var app = builder.Build();

// Създаваме тестов Admin само в Development среда
if (app.Environment.IsDevelopment())
{
    await IdentitySeeder.SeedAdminAsync(
        app.Services,
        app.Configuration);
}


// Обработка на грешки извън Development среда
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Пренасочване към HTTPS
app.UseHttpsRedirection();

// Позволяваме зареждане на CSS, JS и изображения
app.UseStaticFiles();

// Активираме Routing
app.UseRouting();

// Проверяваме самоличността на потребителя
app.UseAuthentication();


// Проверяваме дали текущият потребител трябва да смени паролата
app.Use(async (context, next) =>
{
    // Позволяваме страницата за смяна и Logout
    var path = context.Request.Path.Value ?? "";

    bool allowedPath =
        path.Equals("/Account/ChangePassword",
            StringComparison.OrdinalIgnoreCase) ||
        path.Equals("/Account/Logout",
            StringComparison.OrdinalIgnoreCase);

    // Проверяваме само автентикирани потребители
    if (context.User.Identity?.IsAuthenticated == true &&
        !allowedPath)
    {
        // Получаваме UserManager от Dependency Injection
        var userManager = context.RequestServices
            .GetRequiredService<UserManager<ApplicationUser>>();

        // Зареждаме актуалния потребител от базата
        var user = await userManager.GetUserAsync(context.User);

        // Блокираме всички други маршрути до смяната
        if (user?.MustChangePassword == true)
        {
            context.Response.Redirect("/Account/ChangePassword");
            return;
        }
    }

    // Продължаваме нормалната обработка на заявката
    await next();
});


// Проверяваме правата за достъп
app.UseAuthorization();

// Определяме стандартния MVC маршрут
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Стартираме приложението
app.Run();
