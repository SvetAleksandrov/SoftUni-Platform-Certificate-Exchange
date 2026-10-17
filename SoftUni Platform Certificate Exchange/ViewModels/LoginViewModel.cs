namespace SoftUni_Platform_Certificate_Exchange.ViewModels;

using System.ComponentModel.DataAnnotations;

public class LoginViewModel
{
    // Задължителен имейл с проверка на формата
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    // Задължителна парола, скрита в HTML формуляра
    [Required]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    // Дали потребителят иска да остане вписан
    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }
}
