namespace SoftUni_Platform_Certificate_Exchange.Data;

using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SoftUni_Platform_Certificate_Exchange.Models;

// Основен клас за комуникация с базата данни
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    // Получава настройките за SQL Server чрез Dependency Injection
    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {

    }

    // Таблица за клиентските организации
    public DbSet<Organization> Organizations { get; set; } = null!;

    // Таблица за качените SSL сертификати
    public DbSet<CertificateSubmission> CertificateSubmissions { get; set; } = null!;

    // Таблица за одобренията и отказите
    public DbSet<CertificateReview> CertificateReviews { get; set; } = null!;


    // Конфигурираме връзките между Entity Models
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Запазваме стандартните настройки на ASP.NET Identity
        base.OnModelCreating(builder);

        // Забраняваме изтриване на организация с сертификати
        builder.Entity<CertificateSubmission>()
            .HasOne(c => c.Organization)
            .WithMany()
            .HasForeignKey(c => c.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Забраняваме изтриване на организация с потребители
        builder.Entity<ApplicationUser>()
            .HasOne(u => u.Organization)
            .WithMany()
            .HasForeignKey(u => u.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Един сертификат може да има само едно решение
        builder.Entity<CertificateReview>()
            .HasIndex(r => r.CertificateSubmissionId)
            .IsUnique();

    }

}
