    
namespace SoftUni_Platform_Certificate_Exchange.ViewModels;

// Информация за организацията и нейните потребители
public class OrganizationDetailsViewModel
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? RegistrationNumber { get; set; }

    public bool IsActive { get; set; }

    // Списък с потребителите на организацията
    public List<OrganizationUserViewModel> Users { get; set; } = new();
}

// Информация за един потребител в списъка
public class OrganizationUserViewModel
{
    public string Id { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}
