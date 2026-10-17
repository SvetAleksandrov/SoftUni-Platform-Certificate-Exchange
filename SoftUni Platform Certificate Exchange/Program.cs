using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SoftUni_Platform_Certificate_Exchange.Data;
using SoftUni_Platform_Certificate_Exchange.Models;

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

// Проверяваме правата за достъп
app.UseAuthorization();

// Определяме стандартния MVC маршрут
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Стартираме приложението
app.Run();
