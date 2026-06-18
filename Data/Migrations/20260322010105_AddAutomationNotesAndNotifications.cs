using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsikologProje_Void.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomationNotesAndNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Appointments_DoctorId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_PatientId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentRequests_AppointmentId",
                table: "AppointmentRequests");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentRequests_DoctorId",
                table: "AppointmentRequests");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentRequests_PatientId",
                table: "AppointmentRequests");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Appointments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DoctorReminderSentAtUtc",
                table: "Appointments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PatientReminderSentAtUtc",
                table: "Appointments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Appointments",
                type: "rowversion",
                rowVersion: true,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Appointments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "AppointmentAutomationRoutines",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DaysOfWeek = table.Column<int>(type: "int", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    DurationInMinutes = table.Column<int>(type: "int", nullable: false),
                    IsOnline = table.Column<bool>(type: "bit", nullable: false),
                    IsInPerson = table.Column<bool>(type: "bit", nullable: false),
                    MinPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    MaxPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ActiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    ActiveUntil = table.Column<DateOnly>(type: "date", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    PausedUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentAutomationRoutines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentAutomationRoutines_AspNetUsers_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClinicalNotes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AuthorDoctorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AppointmentId = table.Column<int>(type: "int", nullable: true),
                    Content = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicalNotes_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ClinicalNotes_AspNetUsers_AuthorDoctorId",
                        column: x => x.AuthorDoctorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicalNotes_AspNetUsers_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserNotificationPreferences",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmailEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AppointmentReminderEnabled = table.Column<bool>(type: "bit", nullable: false),
                    RequestStatusEmailsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    ReminderMinutesBefore = table.Column<int>(type: "int", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserNotificationPreferences", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_UserNotificationPreferences_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AppointmentAutomationRoutineSpecialties",
                columns: table => new
                {
                    RoutineId = table.Column<int>(type: "int", nullable: false),
                    SpecialtyId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentAutomationRoutineSpecialties", x => new { x.RoutineId, x.SpecialtyId });
                    table.ForeignKey(
                        name: "FK_AppointmentAutomationRoutineSpecialties_AppointmentAutomationRoutines_RoutineId",
                        column: x => x.RoutineId,
                        principalTable: "AppointmentAutomationRoutines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AppointmentAutomationRoutineSpecialties_Specialties_SpecialtyId",
                        column: x => x.SpecialtyId,
                        principalTable: "Specialties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClinicalNoteShares",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClinicalNoteId = table.Column<int>(type: "int", nullable: false),
                    SharedByPatientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SharedWithDoctorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SharedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClinicalNoteShares", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClinicalNoteShares_AspNetUsers_SharedByPatientId",
                        column: x => x.SharedByPatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicalNoteShares_AspNetUsers_SharedWithDoctorId",
                        column: x => x.SharedWithDoctorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ClinicalNoteShares_ClinicalNotes_ClinicalNoteId",
                        column: x => x.ClinicalNoteId,
                        principalTable: "ClinicalNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Cocuk Psikolojisi");

            migrationBuilder.UpdateData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "Anksiyete Bozukluklari");

            migrationBuilder.UpdateData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: 6,
                column: "Name",
                value: "Cift Terapisi");

            migrationBuilder.UpdateData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: 7,
                column: "Name",
                value: "Yeme Bozukluklari");

            migrationBuilder.UpdateData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: 8,
                column: "Name",
                value: "Bagimlilik Tedavisi");

            migrationBuilder.UpdateData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: 9,
                column: "Name",
                value: "Kisilik Bozukluklari");

            migrationBuilder.UpdateData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: 10,
                column: "Name",
                value: "Yasli Psikolojisi");

            migrationBuilder.UpdateData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: 12,
                column: "Name",
                value: "Kariyer Danismanligi");

            migrationBuilder.UpdateData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: 13,
                column: "Name",
                value: "Stres Yonetimi");

            migrationBuilder.UpdateData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: 14,
                column: "Name",
                value: "Ofke Yonetimi");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId_StartTime_EndTime",
                table: "Appointments",
                columns: new[] { "DoctorId", "StartTime", "EndTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PatientId_StartTime_EndTime",
                table: "Appointments",
                columns: new[] { "PatientId", "StartTime", "EndTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRequests_AppointmentId_PatientId",
                table: "AppointmentRequests",
                columns: new[] { "AppointmentId", "PatientId" });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRequests_DoctorId_Status",
                table: "AppointmentRequests",
                columns: new[] { "DoctorId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRequests_PatientId_Status",
                table: "AppointmentRequests",
                columns: new[] { "PatientId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentAutomationRoutines_DoctorId_IsEnabled",
                table: "AppointmentAutomationRoutines",
                columns: new[] { "DoctorId", "IsEnabled" });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentAutomationRoutines_PausedUntilUtc",
                table: "AppointmentAutomationRoutines",
                column: "PausedUntilUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentAutomationRoutineSpecialties_SpecialtyId",
                table: "AppointmentAutomationRoutineSpecialties",
                column: "SpecialtyId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNotes_AppointmentId",
                table: "ClinicalNotes",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNotes_AuthorDoctorId_CreatedAtUtc",
                table: "ClinicalNotes",
                columns: new[] { "AuthorDoctorId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNotes_PatientId_CreatedAtUtc",
                table: "ClinicalNotes",
                columns: new[] { "PatientId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNoteShares_ClinicalNoteId_SharedWithDoctorId",
                table: "ClinicalNoteShares",
                columns: new[] { "ClinicalNoteId", "SharedWithDoctorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNoteShares_SharedByPatientId",
                table: "ClinicalNoteShares",
                column: "SharedByPatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ClinicalNoteShares_SharedWithDoctorId",
                table: "ClinicalNoteShares",
                column: "SharedWithDoctorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentAutomationRoutineSpecialties");

            migrationBuilder.DropTable(
                name: "ClinicalNoteShares");

            migrationBuilder.DropTable(
                name: "UserNotificationPreferences");

            migrationBuilder.DropTable(
                name: "AppointmentAutomationRoutines");

            migrationBuilder.DropTable(
                name: "ClinicalNotes");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_DoctorId_StartTime_EndTime",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_PatientId_StartTime_EndTime",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentRequests_AppointmentId_PatientId",
                table: "AppointmentRequests");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentRequests_DoctorId_Status",
                table: "AppointmentRequests");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentRequests_PatientId_Status",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "DoctorReminderSentAtUtc",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "PatientReminderSentAtUtc",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Appointments");

            migrationBuilder.UpdateData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: 1,
                column: "Name",
                value: "Çocuk Psikolojisi");

            migrationBuilder.UpdateData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: 4,
                column: "Name",
                value: "Anksiyete Bozuklukları");

            migrationBuilder.UpdateData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: 6,
                column: "Name",
                value: "Çift Terapisi");

            migrationBuilder.UpdateData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: 7,
                column: "Name",
                value: "Yeme Bozuklukları");

            migrationBuilder.UpdateData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: 8,
                column: "Name",
                value: "Bağımlılık Tedavisi");

            migrationBuilder.UpdateData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: 9,
                column: "Name",
                value: "Kişilik Bozuklukları");

            migrationBuilder.UpdateData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: 10,
                column: "Name",
                value: "Yaşlı Psikolojisi");

            migrationBuilder.UpdateData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: 12,
                column: "Name",
                value: "Kariyer Danışmanlığı");

            migrationBuilder.UpdateData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: 13,
                column: "Name",
                value: "Stres Yönetimi");

            migrationBuilder.UpdateData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: 14,
                column: "Name",
                value: "Öfke Yönetimi");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId",
                table: "Appointments",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PatientId",
                table: "Appointments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRequests_AppointmentId",
                table: "AppointmentRequests",
                column: "AppointmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRequests_DoctorId",
                table: "AppointmentRequests",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentRequests_PatientId",
                table: "AppointmentRequests",
                column: "PatientId");
        }
    }
}
