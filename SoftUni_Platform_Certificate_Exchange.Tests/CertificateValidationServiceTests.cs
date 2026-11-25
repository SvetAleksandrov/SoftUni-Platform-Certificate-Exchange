namespace SoftUni_Platform_Certificate_Exchange.Tests;

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using SoftUni_Platform_Certificate_Exchange.Services;
using Xunit;

public class CertificateValidationServiceTests
{
    private readonly CertificateValidationService _service = new();

    // Празен файл не трябва да бъде приеман
    [Fact]
    public void Validate_EmptyFile_ReturnsInvalid()
    {
        var result = _service.Validate(
            Array.Empty<byte>(),
            "server.cer",
            "example.com");

        Assert.False(result.IsValid);
        Assert.Equal(
            "Invalid file size. Maximum is 256 KB.",
            result.Message);
    }

    // Максималният размер е 256 KB
    [Fact]
    public void Validate_OversizedFile_ReturnsInvalid()
    {
        var file = new byte[256 * 1024 + 1];

        var result = _service.Validate(
            file,
            "server.cer",
            "example.com");

        Assert.False(result.IsValid);
    }

    // Неподдържаните разширения се отхвърлят
    [Theory]
    [InlineData("server.txt")]
    [InlineData("server.pfx")]
    [InlineData("server.pdf")]
    public void Validate_InvalidExtension_ReturnsInvalid(
        string fileName)
    {
        var result = _service.Validate(
            new byte[] { 1 },
            fileName,
            "example.com");

        Assert.False(result.IsValid);
        Assert.Equal(
            "Only .cer, .crt and .pem are allowed.",
            result.Message);
    }

    // Hostname е задължителен
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Validate_MissingHostname_ReturnsInvalid(
        string hostname)
    {
        var result = _service.Validate(
            new byte[] { 1 },
            "server.cer",
            hostname);

        Assert.False(result.IsValid);
        Assert.Equal("Hostname is required.", result.Message);
    }

    // PEM файлът трябва да съдържа PEM сертификат
    [Fact]
    public void Validate_InvalidPem_ReturnsInvalid()
    {
        var file = Encoding.UTF8.GetBytes(
            "This is not a certificate.");

        var result = _service.Validate(
            file,
            "server.pem",
            "example.com");

        Assert.False(result.IsValid);
        Assert.Equal("Invalid PEM certificate.", result.Message);
    }

    // Не допускаме файлове с частни ключове
    [Fact]
    public void Validate_PrivateKeyPem_ReturnsInvalid()
    {
        var file = Encoding.UTF8.GetBytes(
            "-----BEGIN PRIVATE KEY-----\n" +
            "invalid\n" +
            "-----END PRIVATE KEY-----");

        var result = _service.Validate(
            file,
            "server.pem",
            "example.com");

        Assert.False(result.IsValid);
        Assert.Equal(
            "Expected one public PEM certificate.",
            result.Message);
    }

    // Не допускаме множество PEM блокове
    [Fact]
    public void Validate_MultiplePemBlocks_ReturnsInvalid()
    {
        const string block =
            "-----BEGIN CERTIFICATE-----\n" +
            "invalid\n" +
            "-----END CERTIFICATE-----\n";

        var file = Encoding.UTF8.GetBytes(block + block);

        var result = _service.Validate(
            file,
            "server.pem",
            "example.com");

        Assert.False(result.IsValid);
    }

    // Валиден публичен сертификат се приема
    [Fact]
    public void Validate_ValidCertificate_ReturnsValid()
    {
        var file = CreateTestCertificate();

        var result = _service.Validate(
            file,
            "server.cer",
            "example.com");

        Assert.True(result.IsValid, result.Message);
        Assert.NotNull(result.Thumbprint);
        Assert.NotNull(result.NotAfterUtc);
    }

    // Сертификат за друг hostname се отхвърля
    [Fact]
    public void Validate_WrongHostname_ReturnsInvalid()
    {
        var file = CreateTestCertificate();

        var result = _service.Validate(
            file,
            "server.cer",
            "other.example.com");

        Assert.False(result.IsValid);
        Assert.Equal(
            "Certificate does not match hostname.",
            result.Message);
    }

    // Генерираме временен X.509 сертификат само за теста
    private static byte[] CreateTestCertificate()
    {
        using var rsa = RSA.Create(2048);

        var request = new CertificateRequest(
            "CN=example.com",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        // DNS Subject Alternative Name
        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName("example.com");

        request.CertificateExtensions.Add(san.Build());

        // TLS Server Authentication
        var usages = new OidCollection();
        usages.Add(new Oid("1.3.6.1.5.5.7.3.1"));

        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                usages,
                critical: false));

        using var certificate = request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1),
            DateTimeOffset.UtcNow.AddDays(30));

        // Експортираме само публичния сертификат
        return certificate.Export(X509ContentType.Cert);
    }
}
