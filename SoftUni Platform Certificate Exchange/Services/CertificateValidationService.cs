namespace SoftUni_Platform_Certificate_Exchange.Services;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using SoftUni_Platform_Certificate_Exchange.Services.Interfaces;

public class CertificateValidationService : ICertificateValidationService
{
    // Максимален размер на сертификата: 256 KB
    private const int MaxFileSize = 256 * 1024;

    public CertificateValidationResult Validate(
        byte[] fileBytes,
        string fileName,
        string hostname)
    {
        // Проверяваме дали файлът е празен или твърде голям
        if (fileBytes.Length == 0 || fileBytes.Length > MaxFileSize)
            return new(false, "Invalid file size. Maximum is 256 KB.");

        // Допускаме само публични сертификати
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        if (extension is not (".cer" or ".crt" or ".pem"))
            return new(false, "Only .cer, .crt and .pem are allowed.");

        if (string.IsNullOrWhiteSpace(hostname))
            return new(false, "Hostname is required.");

        try
        {
            // Разпознаваме текстов PEM или бинарен DER формат
            var text = Encoding.UTF8.GetString(fileBytes);
            bool isPem = text.Contains("-----BEGIN", StringComparison.Ordinal);

            X509Certificate2 certificate;

            if (isPem)
            {
                // Отхвърляме частни ключове и множество PEM блокове
                if (!text.Contains("-----BEGIN CERTIFICATE-----") ||
                    !text.Contains("-----END CERTIFICATE-----") ||
                    text.Contains("PRIVATE KEY", StringComparison.OrdinalIgnoreCase) ||
                    text.Split("-----BEGIN ").Length != 2 ||
                    text.Split("-----END ").Length != 2)
                {
                    return new(false, "Expected one public PEM certificate.");
                }

                // Зареждаме публичния сертификат от PEM
                certificate = X509Certificate2.CreateFromPem(text);
            }
            else
            {
                // PEM файловете трябва да съдържат PEM текст
                if (extension == ".pem")
                    return new(false, "Invalid PEM certificate.");

                // Отхвърляме PFX, PKCS#12 и други формати
                if (X509Certificate2.GetCertContentType(fileBytes)
                    != X509ContentType.Cert)
                {
                    return new(false, "Expected a public DER certificate.");
                }

                // Зареждаме бинарния DER сертификат (.NET 8)
                certificate = new X509Certificate2(fileBytes);
            }

            // Освобождаваме криптографските ресурси след проверката
            using (certificate)
            {
                // Не приемаме сертификати с частен ключ
                if (certificate.HasPrivateKey)
                    return new(false, "Private keys are not allowed.");

                // Преобразуваме датите в UTC
                var validFrom = certificate.NotBefore.ToUniversalTime();
                var validTo = certificate.NotAfter.ToUniversalTime();

                // Проверяваме периода на валидност
                if (DateTime.UtcNow < validFrom ||
                    DateTime.UtcNow > validTo)
                {
                    return new(false, "Certificate is not currently valid.");
                }

                // Проверяваме DNS SAN, без fallback към Common Name
                if (!certificate.MatchesHostname(
                    hostname.Trim(),
                    allowWildcards: true,
                    allowCommonName: false))
                {
                    return new(false, "Certificate does not match hostname.");
                }

                // Ако има EKU, трябва да позволява TLS server authentication
                var eku = certificate.Extensions
                    .OfType<X509EnhancedKeyUsageExtension>()
                    .FirstOrDefault();

                if (eku != null &&
                    !eku.EnhancedKeyUsages.Cast<Oid>().Any(o =>
                        o.Value == "1.3.6.1.5.5.7.3.1" ||
                        o.Value == "2.5.29.37.0"))
                {
                    return new(false, "Certificate is not valid for server authentication.");
                }

                // Връщаме успешно проверените метаданни
                return new CertificateValidationResult(
                    IsValid: true,
                    Message: "Basic certificate checks passed.",
                    Subject: certificate.Subject,
                    Issuer: certificate.Issuer,
                    Thumbprint: certificate.Thumbprint,
                    NotBeforeUtc: validFrom,
                    NotAfterUtc: validTo
                );
            }
        }
        catch (CryptographicException)
        {
            // Файлът съдържа невалиден сертификат
            return new(false, "Invalid X.509 certificate content.");
        }
        catch (ArgumentException)
        {
            // Невалиден hostname или параметър
            return new(false, "Invalid certificate or hostname.");
        }
    }
}
