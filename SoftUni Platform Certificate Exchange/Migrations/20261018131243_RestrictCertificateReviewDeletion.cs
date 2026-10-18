using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftUni_Platform_Certificate_Exchange.Migrations
{
    /// <inheritdoc />
    public partial class RestrictCertificateReviewDeletion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CertificateReviews_CertificateSubmissions_CertificateSubmissionId",
                table: "CertificateReviews");

            migrationBuilder.AddForeignKey(
                name: "FK_CertificateReviews_CertificateSubmissions_CertificateSubmissionId",
                table: "CertificateReviews",
                column: "CertificateSubmissionId",
                principalTable: "CertificateSubmissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CertificateReviews_CertificateSubmissions_CertificateSubmissionId",
                table: "CertificateReviews");

            migrationBuilder.AddForeignKey(
                name: "FK_CertificateReviews_CertificateSubmissions_CertificateSubmissionId",
                table: "CertificateReviews",
                column: "CertificateSubmissionId",
                principalTable: "CertificateSubmissions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
