namespace SoftUni_Platform_Certificate_Exchange.Models;

using System.ComponentModel.DataAnnotations;

public class CertificateSubmission
{
    // Уникален идентификатор (Primary Key)
    public int Id { get; set; }

    // Идентификатор на клиентската организация (Foreign Key)
    public int OrganizationId { get; set; }

    // Връзка към организацията (Navigation Property)
    public Organization Organization { get; set; } = null!;

    // Идентификатор на потребителя, качил сертификата
    public string UploadedByUserId { get; set; } = string.Empty;

    // DNS адрес, за който е предназначен сертификатът
    [Required]
    [StringLength(253)]
    public string TargetHostname { get; set; } = string.Empty;

    // Допълнително описание от клиента
    [StringLength(500)]
    public string? Description { get; set; }

    // Оригинално име на качения файл
    public string? OriginalFileName { get; set; }

    // Уникално име за съхранение на файла
    public string? StorageKey { get; set; }

    // Идентификатор (thumbprint) на сертификата
    public string? Thumbprint { get; set; }

    // На кого е издаден сертификатът
    public string? Subject { get; set; }

    // Кой е издал сертификата
    public string? Issuer { get; set; }

    // Начална дата на валидност
    public DateTime? NotBeforeUtc { get; set; }

    // Крайна дата на валидност
    public DateTime? NotAfterUtc { get; set; }

    // Дата на създаване на заявката
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    // Текущ бизнес статус
    public CertificateStatus Status { get; set; }
        = CertificateStatus.Draft;
}

// Възможни статуси на заявката
public enum CertificateStatus
{
    Draft,
    Submitted,
    UnderReview,
    Approved,
    Rejected
}
