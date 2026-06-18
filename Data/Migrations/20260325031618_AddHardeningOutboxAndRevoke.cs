using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsikologProje_Void.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHardeningOutboxAndRevoke : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClinicalNoteShares_SharedWithDoctorId",
                table: "ClinicalNoteShares");

            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedAtUtc",
                table: "ClinicalNoteShares",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RevokedByPatientId",
                table: "ClinicalNoteShares",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CancelledAtUtc",
                table: "Appointments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CancelledReason",
                table: "Appointments",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "EmailOutboxMessages",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    To = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                    Subject = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    HtmlBody = table.Column<string>(type: "nvarchar(max)", maxLength: 16000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    MaxRetryCount = table.Column<int>(type: "int", nullable: false),
                    NextAttemptAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ProcessingStartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailOutboxMessages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNoteShares_RevokedByPatientId",
                table: "ClinicalNoteShares",
                column: "RevokedByPatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNoteShares_SharedWithDoctorId_RevokedAtUtc",
                table: "ClinicalNoteShares",
                columns: new[] { "SharedWithDoctorId", "RevokedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EmailOutboxMessages_Status_NextAttemptAtUtc",
                table: "EmailOutboxMessages",
                columns: new[] { "Status", "NextAttemptAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_ClinicalNoteShares_AspNetUsers_RevokedByPatientId",
                table: "ClinicalNoteShares",
                column: "RevokedByPatientId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ClinicalNoteShares_AspNetUsers_RevokedByPatientId",
                table: "ClinicalNoteShares");

            migrationBuilder.DropTable(
                name: "EmailOutboxMessages");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalNoteShares_RevokedByPatientId",
                table: "ClinicalNoteShares");

            migrationBuilder.DropIndex(
                name: "IX_ClinicalNoteShares_SharedWithDoctorId_RevokedAtUtc",
                table: "ClinicalNoteShares");

            migrationBuilder.DropColumn(
                name: "RevokedAtUtc",
                table: "ClinicalNoteShares");

            migrationBuilder.DropColumn(
                name: "RevokedByPatientId",
                table: "ClinicalNoteShares");

            migrationBuilder.DropColumn(
                name: "CancelledAtUtc",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "CancelledReason",
                table: "Appointments");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNoteShares_SharedWithDoctorId",
                table: "ClinicalNoteShares",
                column: "SharedWithDoctorId");
        }
    }
}
