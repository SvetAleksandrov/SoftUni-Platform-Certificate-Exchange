using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftUni_Platform_Certificate_Exchange.Migrations
{
    /// <inheritdoc />
    public partial class AddCertificateAuditEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CertificateAuditEvents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CertificateSubmissionId = table.Column<int>(type: "int", nullable: true),
                    OrganizationId = table.Column<int>(type: "int", nullable: false),
                    TargetHostname = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    PerformedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    EventType = table.Column<int>(type: "int", nullable: false),
                    Details = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CertificateAuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CertificateAuditEvents_CertificateSubmissions_CertificateSubmissionId",
                        column: x => x.CertificateSubmissionId,
                        principalTable: "CertificateSubmissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CertificateAuditEvents_CertificateSubmissionId",
                table: "CertificateAuditEvents",
                column: "CertificateSubmissionId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CertificateAuditEvents");
        }
    }
}
