using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsikologProje_Void.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicalNoteVisibilityDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultClinicalNoteVisibility",
                table: "UserNotificationPreferences",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Visibility",
                table: "ClinicalNotes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNotes_PatientId_Visibility_CreatedAtUtc",
                table: "ClinicalNotes",
                columns: new[] { "PatientId", "Visibility", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ClinicalNotes_PatientId_Visibility_CreatedAtUtc",
                table: "ClinicalNotes");

            migrationBuilder.DropColumn(
                name: "DefaultClinicalNoteVisibility",
                table: "UserNotificationPreferences");

            migrationBuilder.DropColumn(
                name: "Visibility",
                table: "ClinicalNotes");
        }
    }
}
