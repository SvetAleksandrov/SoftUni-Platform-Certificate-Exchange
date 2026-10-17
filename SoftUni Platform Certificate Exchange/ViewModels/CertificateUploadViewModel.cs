namespace SoftUni_Platform_Certificate_Exchange.ViewModels;

using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

public class CertificateUploadViewModel
{
    // Организацията, която предоставя сертификата
    [Range(1, int.MaxValue, ErrorMessage = "Select an organization.")]
    [Display(Name = "Client Organization")]
    public int OrganizationId { get; set; }

    // DNS адрес на сървъра, например api.example.com
    [Required(ErrorMessage = "Hostname is required.")]
    [StringLength(253)]
    [Display(Name = "Target Hostname")]
    public string TargetHostname { get; set; } = string.Empty;

    // Допълнително описание на сертификата
    [StringLength(500)]
    public string? Description { get; set; }

    // Файлът, който потребителят избира за качване
    [Required(ErrorMessage = "Certificate file is required.")]
    [Display(Name = "SSL/TLS Certificate")]
    public IFormFile? CertificateFile { get; set; }
}
