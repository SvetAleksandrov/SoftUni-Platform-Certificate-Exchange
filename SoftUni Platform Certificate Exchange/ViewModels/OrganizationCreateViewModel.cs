
using System.ComponentModel.DataAnnotations;

namespace SoftUni_Platform_Certificate_Exchange.ViewModels;

public class OrganizationCreateViewModel
{
    // Задължително име между 3 и 150 символа
    [Required(ErrorMessage = "Organization name is required.")]
    [StringLength(150, MinimumLength = 3)]
    [Display(Name = "Organization Name")]
    public string Name { get; set; } = string.Empty;

    // Незадължителен регистрационен номер
    [StringLength(30)]
    [Display(Name = "Registration Number")]
    public string? RegistrationNumber { get; set; }
}
