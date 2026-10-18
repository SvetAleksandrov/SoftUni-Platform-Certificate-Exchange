using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftUni_Platform_Certificate_Exchange.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueCertificateReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CertificateReviews_CertificateSubmissionId",
                table: "CertificateReviews");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateReviews_CertificateSubmissionId",
                table: "CertificateReviews",
                column: "CertificateSubmissionId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CertificateReviews_CertificateSubmissionId",
                table: "CertificateReviews");

            migrationBuilder.CreateIndex(
                name: "IX_CertificateReviews_CertificateSubmissionId",
                table: "CertificateReviews",
                column: "CertificateSubmissionId");
        }
    }
}
