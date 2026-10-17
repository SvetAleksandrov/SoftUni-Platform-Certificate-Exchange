namespace SoftUni_Platform_Certificate_Exchange.Models;
using System.ComponentModel.DataAnnotations;

public class CertificateReview
{
    // Уникален идентификатор (Primary Key)
    public int Id { get; set; }

    // Идентификатор на преглеждания сертификат (Foreign Key)
    public int CertificateSubmissionId { get; set; }

    // Връзка към сертификата (Navigation Property)
    public CertificateSubmission CertificateSubmission { get; set; } = null!;

    // Идентификатор на служителя, извършил прегледа
    [Required]
    public string ReviewedByUserId { get; set; } = string.Empty;

    // Решение на служителя: Approved или Rejected
    public CertificateReviewDecision Decision { get; set; }

    // Коментар или причина за отказ
    [StringLength(1000)]
    public string? Comment { get; set; }

    // Дата и час на извършване на прегледа (UTC)
    public DateTime ReviewedAtUtc { get; set; } = DateTime.UtcNow;
}

// Възможни решения при преглед на сертификата
public enum CertificateReviewDecision
{
    Approved,
    Rejected
}
