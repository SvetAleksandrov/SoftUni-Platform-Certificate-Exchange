namespace SoftUni_Platform_Certificate_Exchange.Tests;

using SoftUni_Platform_Certificate_Exchange.Models;
using SoftUni_Platform_Certificate_Exchange.Services;
using Xunit;

public class CertificateReviewRulesTests
{
    private readonly CertificateReviewRules _rules = new();

    // Проверяваме кои статуси позволяват Review
    [Theory]
    [InlineData(CertificateStatus.Draft, false)]
    [InlineData(CertificateStatus.Submitted, true)]
    [InlineData(CertificateStatus.UnderReview, true)]
    [InlineData(CertificateStatus.Approved, false)]
    [InlineData(CertificateStatus.Rejected, false)]
    public void CanReview_ReturnsExpectedResult(
        CertificateStatus status,
        bool expected)
    {
        var result = _rules.CanReview(status);

        Assert.Equal(expected, result);
    }

    // Проверяваме резултата при Approved и Rejected
    [Theory]
    [InlineData(
        CertificateReviewDecision.Approved,
        CertificateStatus.Approved)]
    [InlineData(
        CertificateReviewDecision.Rejected,
        CertificateStatus.Rejected)]
    public void GetResultingStatus_ReturnsExpectedStatus(
        CertificateReviewDecision decision,
        CertificateStatus expected)
    {
        var result = _rules.GetResultingStatus(decision);

        Assert.Equal(expected, result);
    }

    // Невалидно решение трябва да бъде отхвърлено
    [Fact]
    public void GetResultingStatus_InvalidDecision_ThrowsException()
    {
        var invalidDecision =
            (CertificateReviewDecision)999;

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _rules.GetResultingStatus(invalidDecision));
    }
}
