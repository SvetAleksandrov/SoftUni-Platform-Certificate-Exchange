using System.ComponentModel.DataAnnotations;

namespace SoftUni_Platform_Certificate_Exchange.Models;

// Видове действия със сертификат
public enum CertificateAuditEventType
{
    Submitted = 1,
    Approved = 2,
    Rejected = 3
}

// История на действията със сертификатите
public class CertificateAuditEvent
{
    public int Id { get; set; }

    // Връзка към сертификата, ако още съществува
    public int? CertificateSubmissionId { get; set; }

    public CertificateSubmission? CertificateSubmission { get; set; }

    // Организация, към която се отнася събитието
    public int OrganizationId { get; set; }

    // Запазваме hostname за историческа справка
    [Required]
    [MaxLength(255)]
    public string TargetHostname { get; set; } = string.Empty;

    // Identity ID на потребителя, извършил действието
    [Required]
    [MaxLength(450)]
    public string PerformedByUserId { get; set; } = string.Empty;

    // Тип на извършеното действие
    public CertificateAuditEventType EventType { get; set; }

    // Допълнителна информация
    [MaxLength(1000)]
    public string? Details { get; set; }

    // Дата и час в UTC
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
