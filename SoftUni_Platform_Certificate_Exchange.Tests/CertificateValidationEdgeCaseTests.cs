namespace SoftUni_Platform_Certificate_Exchange.Tests;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using SoftUni_Platform_Certificate_Exchange.Services;
using Xunit;

public class CertificateValidationEdgeCaseTests
{
    private readonly CertificateValidationService _service = new();

    // Изтекъл сертификат не трябва да бъде приеман
    [Fact]
    public void Validate_ExpiredCertificate_ReturnsInvalid()
    {
        var file = CreateCertificate(-30, -1);

        var result = _service.Validate(
            file, "server.cer", "example.com");

        Assert.False(result.IsValid);
        Assert.Equal(
            "Certificate is not currently valid.",
            result.Message);
    }

    // Сертификат с бъдеща начална дата не е валиден
    [Fact]
    public void Validate_FutureCertificate_ReturnsInvalid()
    {
        var file = CreateCertificate(1, 30);

        var result = _service.Validate(
            file, "server.cer", "example.com");

        Assert.False(result.IsValid);
        Assert.Equal(
            "Certificate is not currently valid.",
            result.Message);
    }

    // Client Authentication EKU не е достатъчен за TLS сървър
    [Fact]
    public void Validate_WrongEku_ReturnsInvalid()
    {
        var file = CreateCertificate(
            -1, 30, allowServerAuth: false);

        var result = _service.Validate(
            file, "server.cer", "example.com");

        Assert.False(result.IsValid);
        Assert.Equal(
            "Certificate is not valid for server authentication.",
            result.Message);
    }

    // Проверяваме валиден сертификат в PEM формат
    [Fact]
    public void Validate_ValidPem_ReturnsValid()
    {
        var der = CreateCertificate(-1, 30);

        var pem = "-----BEGIN CERTIFICATE-----\n"
            + Convert.ToBase64String(
                der, Base64FormattingOptions.InsertLineBreaks)
            + "\n-----END CERTIFICATE-----\n";

        var file = Encoding.UTF8.GetBytes(pem);

        var result = _service.Validate(
            file, "server.pem", "example.com");

        Assert.True(result.IsValid, result.Message);
        Assert.NotNull(result.Thumbprint);
    }

    // Генерираме публичен сертификат за тестовете
    private static byte[] CreateCertificate(
        int validFromDays,
        int validToDays,
        bool allowServerAuth = true)
    {
        using var rsa = RSA.Create(2048);

        var request = new CertificateRequest(
            "CN=example.com",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName("example.com");

        request.CertificateExtensions.Add(san.Build());

        // Server Authentication или Client Authentication
        var ekuOid = allowServerAuth
            ? "1.3.6.1.5.5.7.3.1"
            : "1.3.6.1.5.5.7.3.2";

        var usages = new OidCollection();
        usages.Add(new Oid(ekuOid));

        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                usages, critical: false));

        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(validFromDays),
            DateTimeOffset.UtcNow.AddDays(validToDays));

        return certificate.Export(X509ContentType.Cert);
    }
}
