namespace SoftUni_Platform_Certificate_Exchange.Models;

using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

// Наследяваме IdentityUser за готовите функции на ASP.NET Identity
public class ApplicationUser : IdentityUser
{
    // Собствено име на потребителя
    public string FirstName { get; set; } = string.Empty;

    // Фамилия на потребителя
    public string LastName { get; set; } = string.Empty;


    // Задължителна смяна на паролата при първоначален Login
    // true  = потребителят трябва да смени паролата
    // false = потребителят може да използва приложението
    public bool MustChangePassword { get; set; } = false;


    // Отдел, в който работи потребителят
    [StringLength(100)]
    public string? Department { get; set; }

    // Длъжност на потребителя
    [StringLength(100)]
    public string? JobTitle { get; set; }


    // Идентификатор на клиентската организация (Foreign Key)
    // Nullable, защото вътрешните служители нямат клиентска организация
    public int? OrganizationId { get; set; }

    // Връзка към организацията (Navigation Property)
    public Organization? Organization { get; set; }
}
