namespace SoftUni_Platform_Certificate_Exchange.Services;

using SoftUni_Platform_Certificate_Exchange.Models;
using SoftUni_Platform_Certificate_Exchange.Services.Interfaces;

public class CertificateReviewRules : ICertificateReviewRules
{
    // Само подадени сертификати могат да бъдат преглеждани
    public bool CanReview(CertificateStatus status)
    {
        return status == CertificateStatus.Submitted ||
               status == CertificateStatus.UnderReview;
    }

    // Определяме новия статус според решението
    public CertificateStatus GetResultingStatus(
        CertificateReviewDecision decision)
    {
        return decision switch
        {
            CertificateReviewDecision.Approved =>
                CertificateStatus.Approved,

            CertificateReviewDecision.Rejected =>
                CertificateStatus.Rejected,

            _ => throw new ArgumentOutOfRangeException(
                nameof(decision), "Invalid review decision.")
        };
    }
}
