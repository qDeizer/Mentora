using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsikologProje_Void.Data.Migrations
{
    /// <inheritdoc />
    public partial class VNext2_GlobalLocation_Notes_PrivateOffers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DoctorPatientConnectionStates_AspNetUsers_DoctorId",
                table: "DoctorPatientConnectionStates");

            migrationBuilder.DropForeignKey(
                name: "FK_DoctorPatientConnectionStates_AspNetUsers_PatientId",
                table: "DoctorPatientConnectionStates");

            migrationBuilder.AddColumn<bool>(
                name: "ClinicalNoteCommentInAppEnabled",
                table: "UserNotificationPreferences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ClinicalNoteShareInAppEnabled",
                table: "UserNotificationPreferences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "InAppEnabled",
                table: "UserNotificationPreferences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IncomingRequestInAppEnabled",
                table: "UserNotificationPreferences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PrivateOfferInAppEnabled",
                table: "UserNotificationPreferences",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "ClinicalNotes",
                type: "nvarchar(max)",
                maxLength: 50000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(4000)",
                oldMaxLength: 4000);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivateOffer",
                table: "Appointments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "OfferExpiresAtUtc",
                table: "Appointments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfferNoteFromDoctor",
                table: "Appointments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OfferRespondedAtUtc",
                table: "Appointments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OfferResponseNoteFromPatient",
                table: "Appointments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OfferStatus",
                table: "Appointments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "TargetPatientId",
                table: "Appointments",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ClinicalNoteAccessRules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicalNoteId = table.Column<int>(type: "int", nullable: false),
                    DoctorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RuleType = table.Column<int>(type: "int", nullable: false),
                    CreatedByPatientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalNoteAccessRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicalNoteAccessRules_AspNetUsers_CreatedByPatientId",
                        column: x => x.CreatedByPatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicalNoteAccessRules_AspNetUsers_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicalNoteAccessRules_ClinicalNotes_ClinicalNoteId",
                        column: x => x.ClinicalNoteId,
                        principalTable: "ClinicalNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClinicalNoteComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicalNoteId = table.Column<int>(type: "int", nullable: false),
                    DoctorId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    PatientId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalNoteComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicalNoteComments_AspNetUsers_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicalNoteComments_AspNetUsers_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicalNoteComments_ClinicalNotes_ClinicalNoteId",
                        column: x => x.ClinicalNoteId,
                        principalTable: "ClinicalNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClinicalNoteLocks",
                columns: table => new
                {
                    ClinicalNoteId = table.Column<int>(type: "int", nullable: false),
                    LockedByDoctorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    IsLockedForPatient = table.Column<bool>(type: "bit", nullable: false),
                    LockedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalNoteLocks", x => x.ClinicalNoteId);
                    table.ForeignKey(
                        name: "FK_ClinicalNoteLocks_AspNetUsers_LockedByDoctorId",
                        column: x => x.LockedByDoctorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicalNoteLocks_ClinicalNotes_ClinicalNoteId",
                        column: x => x.ClinicalNoteId,
                        principalTable: "ClinicalNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: false),
                    DeepLink = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Type = table.Column<int>(type: "int", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReadAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_TargetPatientId_OfferStatus_StartTime",
                table: "Appointments",
                columns: new[] { "TargetPatientId", "OfferStatus", "StartTime" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNoteAccessRules_ClinicalNoteId_DoctorId_RuleType",
                table: "ClinicalNoteAccessRules",
                columns: new[] { "ClinicalNoteId", "DoctorId", "RuleType" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNoteAccessRules_CreatedByPatientId",
                table: "ClinicalNoteAccessRules",
                column: "CreatedByPatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNoteAccessRules_DoctorId_RuleType_RevokedAtUtc",
                table: "ClinicalNoteAccessRules",
                columns: new[] { "DoctorId", "RuleType", "RevokedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNoteComments_ClinicalNoteId_CreatedAtUtc",
                table: "ClinicalNoteComments",
                columns: new[] { "ClinicalNoteId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNoteComments_DoctorId",
                table: "ClinicalNoteComments",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNoteComments_PatientId",
                table: "ClinicalNoteComments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNoteLocks_LockedByDoctorId",
                table: "ClinicalNoteLocks",
                column: "LockedByDoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead_CreatedAtUtc",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead", "CreatedAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_AspNetUsers_TargetPatientId",
                table: "Appointments",
                column: "TargetPatientId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorPatientConnectionStates_AspNetUsers_DoctorId",
                table: "DoctorPatientConnectionStates",
                column: "DoctorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorPatientConnectionStates_AspNetUsers_PatientId",
                table: "DoctorPatientConnectionStates",
                column: "PatientId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_AspNetUsers_TargetPatientId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_DoctorPatientConnectionStates_AspNetUsers_DoctorId",
                table: "DoctorPatientConnectionStates");

            migrationBuilder.DropForeignKey(
                name: "FK_DoctorPatientConnectionStates_AspNetUsers_PatientId",
                table: "DoctorPatientConnectionStates");

            migrationBuilder.DropTable(
                name: "ClinicalNoteAccessRules");

            migrationBuilder.DropTable(
                name: "ClinicalNoteComments");

            migrationBuilder.DropTable(
                name: "ClinicalNoteLocks");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_TargetPatientId_OfferStatus_StartTime",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ClinicalNoteCommentInAppEnabled",
                table: "UserNotificationPreferences");

            migrationBuilder.DropColumn(
                name: "ClinicalNoteShareInAppEnabled",
                table: "UserNotificationPreferences");

            migrationBuilder.DropColumn(
                name: "InAppEnabled",
                table: "UserNotificationPreferences");

            migrationBuilder.DropColumn(
                name: "IncomingRequestInAppEnabled",
                table: "UserNotificationPreferences");

            migrationBuilder.DropColumn(
                name: "PrivateOfferInAppEnabled",
                table: "UserNotificationPreferences");

            migrationBuilder.DropColumn(
                name: "IsPrivateOffer",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "OfferExpiresAtUtc",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "OfferNoteFromDoctor",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "OfferRespondedAtUtc",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "OfferResponseNoteFromPatient",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "OfferStatus",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "TargetPatientId",
                table: "Appointments");

            migrationBuilder.AlterColumn<string>(
                name: "Content",
                table: "ClinicalNotes",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldMaxLength: 50000);

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorPatientConnectionStates_AspNetUsers_DoctorId",
                table: "DoctorPatientConnectionStates",
                column: "DoctorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorPatientConnectionStates_AspNetUsers_PatientId",
                table: "DoctorPatientConnectionStates",
                column: "PatientId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
