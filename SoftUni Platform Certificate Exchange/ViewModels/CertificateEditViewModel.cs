namespace SoftUni_Platform_Certificate_Exchange.ViewModels;

using System.ComponentModel.DataAnnotations;

public class CertificateEditViewModel
{
    // ID на сертификата, който редактираме
    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    // Позволяваме редакция само на описанието
    [StringLength(500, ErrorMessage = "Maximum 500 characters.")]
    [Display(Name = "Certificate Description")]
    public string? Description { get; set; }
}
