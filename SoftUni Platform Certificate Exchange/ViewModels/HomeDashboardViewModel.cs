namespace SoftUni_Platform_Certificate_Exchange.ViewModels;

using SoftUni_Platform_Certificate_Exchange.Models;


// Данни за началния Dashboard
public class HomeDashboardViewModel
{
    // Показвано име на потребителя
    public string DisplayName { get; set; } = string.Empty;

    // Определя кой Dashboard да се покаже
    public bool IsAdmin { get; set; }

    // Организацията на Client потребителя
    public string? OrganizationName { get; set; }

    // Административни показатели
    public int OrganizationCount { get; set; }
    public int AssignedUsersCount { get; set; }

    // Показатели за сертификатите
    public int CertificateCount { get; set; }
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }

    // Последните пет сертификата
    public List<DashboardCertificateViewModel> RecentCertificates
    { get; set; } = new();
}

// Обобщени данни за един сертификат
public class DashboardCertificateViewModel
{
    public int Id { get; set; }

    public string OrganizationName { get; set; } = string.Empty;

    public string Hostname { get; set; } = string.Empty;

    public CertificateStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ValidToUtc { get; set; }
}
