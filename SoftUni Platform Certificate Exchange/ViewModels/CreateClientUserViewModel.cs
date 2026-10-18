namespace SoftUni_Platform_Certificate_Exchange.ViewModels;

using System.ComponentModel.DataAnnotations;

public class CreateClientUserViewModel
{
    // Организация, към която ще принадлежи потребителят
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

    // Имейл за вход в приложението
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    // Начална тестова парола
    [Required]
    [StringLength(100, MinimumLength = 12)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    // Потвърждение на паролата
    [Required]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
