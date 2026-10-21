
namespace SoftUni_Platform_Certificate_Exchange.ViewModels;

public class UserDeleteViewModel
{
    public string Id { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string OrganizationName { get; set; } = string.Empty;

    // Потребители с история не могат да бъдат изтрити
    public bool HasCertificateHistory { get; set; }
}
