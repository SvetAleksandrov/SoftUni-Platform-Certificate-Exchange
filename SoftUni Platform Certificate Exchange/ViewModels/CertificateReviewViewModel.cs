namespace SoftUni_Platform_Certificate_Exchange.ViewModels;

using System.ComponentModel.DataAnnotations;
using SoftUni_Platform_Certificate_Exchange.Models;

// Модел за формуляра за одобрение или отказ
public class CertificateReviewViewModel : IValidatableObject
{
    // Идентификатор на сертификата
    [Range(1, int.MaxValue)]
    public int CertificateSubmissionId { get; set; }

    // Решение на служителя - Approved или Rejected
    [Required(ErrorMessage = "Please select a decision.")]
    public CertificateReviewDecision? Decision { get; set; }

    // Коментар към решението
    [StringLength(1000)]
    [Display(Name = "Review Comment")]
    public string? Comment { get; set; }

    // Допълнителна бизнес валидация
    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        // При отказ коментарът е задължителен
        if (Decision == CertificateReviewDecision.Rejected &&
            string.IsNullOrWhiteSpace(Comment))
        {
            yield return new ValidationResult(
                "A comment is required when rejecting a certificate.",
                new[] { nameof(Comment) });
        }
    }
}
