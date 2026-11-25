namespace SoftUni_Platform_Certificate_Exchange.Services.Interfaces;

using SoftUni_Platform_Certificate_Exchange.Models;


// Бизнес правила за преглед на сертификати
public interface ICertificateReviewRules
{
    /// Determines whether a certificate with the specified status can be reviewed.<param name="status">The current status of the certificate.</param>
    /// True if the certificate can be reviewed; otherwise, false.
    bool CanReview(CertificateStatus status);

    /// <summary>
    /// Determines the resulting certificate status based on the review decision.The resulting status of the certificate after the decision is applied.
    CertificateStatus GetResultingStatus(
        CertificateReviewDecision decision);
}
