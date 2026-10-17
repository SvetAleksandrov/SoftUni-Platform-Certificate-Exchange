namespace SoftUni_Platform_Certificate_Exchange.Services.Interfaces;

// Услугата за проверка на сертификати
public interface ICertificateValidationService
{
    // Получава файла, името му и очаквания hostname
    CertificateValidationResult Validate(
        byte[] fileBytes,
        string fileName,
        string hostname);
}

// Резултат от техническата проверка
public record CertificateValidationResult(
    bool IsValid,
    string Message,
    string? Subject = null,
    string? Issuer = null,
    string? Thumbprint = null,
    DateTime? NotBeforeUtc = null,
    DateTime? NotAfterUtc = null
);
