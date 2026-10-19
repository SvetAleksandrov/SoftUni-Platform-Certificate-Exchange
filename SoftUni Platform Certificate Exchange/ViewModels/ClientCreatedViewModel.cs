namespace SoftUni_Platform_Certificate_Exchange.ViewModels;

public class ClientCreatedViewModel
{
    // Имейл на новия потребител
    public string Email { get; set; } = string.Empty;

    // Име на клиентската организация
    public string OrganizationName { get; set; } = string.Empty;

    // Временна парола - само за еднократно показване
    public string TemporaryPassword { get; set; } = string.Empty;
}
