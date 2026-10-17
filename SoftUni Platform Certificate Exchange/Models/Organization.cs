namespace SoftUni_Platform_Certificate_Exchange.Models;

using System.ComponentModel.DataAnnotations;
public class Organization
{
    public int Id { get; set; }

    [Required]
    [StringLength(150, MinimumLength = 3)]
    public string Name { get; set; } = string.Empty;

    [StringLength(30)]
    public string? RegistrationNumber { get; set; }

    public bool IsActive { get; set; } = true;
}
