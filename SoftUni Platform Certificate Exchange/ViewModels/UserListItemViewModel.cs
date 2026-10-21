
namespace SoftUni_Platform_Certificate_Exchange.ViewModels;

// Данни за един потребител в административния списък
public class UserListItemViewModel
{
    // Уникален Identity идентификатор
    public string Id { get; set; } = string.Empty;

    // Имена на потребителя
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    // Имейл за вход в приложението
    public string Email { get; set; } = string.Empty;

    // Организацията може да е null за вътрешни служители
    public int? OrganizationId { get; set; }

    // Име на организацията за визуализация
    public string? OrganizationName { get; set; }


    // Телефон от ASP.NET Identity
    public string? PhoneNumber { get; set; }

    // Име на отдела
    public string? Department { get; set; }

    // Име на позицията
    public string? JobTitle { get; set; }

}
