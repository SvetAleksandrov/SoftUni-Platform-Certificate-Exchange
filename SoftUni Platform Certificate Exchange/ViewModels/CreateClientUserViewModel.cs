namespace SoftUni_Platform_Certificate_Exchange.ViewModels;

using System.ComponentModel.DataAnnotations;

public class CreateClientUserViewModel
{
    // Клиентската организация
    [Range(1, int.MaxValue, ErrorMessage = "Select an organization.")]
    public int OrganizationId { get; set; }

    // Собствено име
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string FirstName { get; set; } = string.Empty;

    // Фамилия
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string LastName { get; set; } = string.Empty;

    // Имейл за вход
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}