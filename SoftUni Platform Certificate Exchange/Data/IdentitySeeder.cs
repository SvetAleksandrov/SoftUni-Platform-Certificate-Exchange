namespace SoftUni_Platform_Certificate_Exchange.Data;

using Microsoft.AspNetCore.Identity;
using SoftUni_Platform_Certificate_Exchange.Models;

public static class IdentitySeeder
{
    // Създава първоначален администратор в базата данни
    public static async Task SeedAdminAsync(
        IServiceProvider services,
        IConfiguration configuration)
    {
        // Създаваме отделен Dependency Injection scope
        using var scope = services.CreateScope();

        // Получаваме услугите за роли и потребители
        var roleManager = scope.ServiceProvider
            .GetRequiredService<RoleManager<IdentityRole>>();

        var userManager = scope.ServiceProvider
            .GetRequiredService<UserManager<ApplicationUser>>();

        // Четем тестовите данни от User Secrets
        var email = configuration["SeedAdmin:Email"];
        var password = configuration["SeedAdmin:Password"];

        // Не позволяваме създаване с липсващи данни
        if (string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Admin credentials are missing from User Secrets.");
        }

        // Създаваме роля Admin, ако не съществува
        if (!await roleManager.RoleExistsAsync("Admin"))
        {
            var result = await roleManager.CreateAsync(
                new IdentityRole("Admin"));

            if (!result.Succeeded)
                throw new InvalidOperationException(
                    string.Join("; ", result.Errors.Select(e => e.Description)));
        }


        // Създаваме роля Client, ако все още не съществува
        if (!await roleManager.RoleExistsAsync("Client"))
        {
            var result = await roleManager.CreateAsync(
                new IdentityRole("Client"));

            // Проверяваме дали ролята е създадена успешно
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    string.Join("; ",
                        result.Errors.Select(e => e.Description)));
            }
        }


        // Проверяваме дали потребителят вече съществува
        var admin = await userManager.FindByEmailAsync(email);

        if (admin == null)
        {
            // Създаваме нов администратор
            admin = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Administrator"
            };

            // Identity съхранява хеш на паролата
            var result = await userManager.CreateAsync(admin, password);

            if (!result.Succeeded)
                throw new InvalidOperationException(
                    string.Join("; ", result.Errors.Select(e => e.Description)));
        }

        // Добавяме потребителя към роля Admin
        if (!await userManager.IsInRoleAsync(admin, "Admin"))
        {
            var result = await userManager.AddToRoleAsync(admin, "Admin");

            if (!result.Succeeded)
                throw new InvalidOperationException(
                    string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }
}
