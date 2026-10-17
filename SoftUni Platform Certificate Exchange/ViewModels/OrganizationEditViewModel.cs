namespace SoftUni_Platform_Certificate_Exchange.ViewModels;

using System.ComponentModel.DataAnnotations;

public class OrganizationEditViewModel
{
    // ID на организацията, която редактираме
    public int Id { get; set; }

    // Задължително име между 3 и 150 символа
    [Required(ErrorMessage = "Organization name is required.")]
    [StringLength(150, MinimumLength = 3)]
    [Display(Name = "Organization Name")]
    public string Name { get; set; } = string.Empty;

    // Незадължителен регистрационен номер
    [StringLength(30)]
    [Display(Name = "Registration Number")]
    public string? RegistrationNumber { get; set; }

    // Дали организацията е активна
    [Display(Name = "Active Organization")]
    public bool IsActive { get; set; }
}
