using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsikologProje_Void.Data.Migrations
{
    /// <inheritdoc />
    public partial class VNext3_AdminAndDigestAndProfileRateLimit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmailDigestMode",
                table: "UserNotificationPreferences",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "TwoFactorViaEmailEnabled",
                table: "UserNotificationPreferences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsLockedForPatient",
                table: "ClinicalNoteComments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProfileChangeBlockedUntilUtc",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ProfileChangeCountInWindow",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "ProfileChangeWindowStartUtc",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EmailDigestMode",
                table: "UserNotificationPreferences");

            migrationBuilder.DropColumn(
                name: "TwoFactorViaEmailEnabled",
                table: "UserNotificationPreferences");

            migrationBuilder.DropColumn(
                name: "IsLockedForPatient",
                table: "ClinicalNoteComments");

            migrationBuilder.DropColumn(
                name: "ProfileChangeBlockedUntilUtc",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProfileChangeCountInWindow",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ProfileChangeWindowStartUtc",
                table: "AspNetUsers");
        }
    }
}
