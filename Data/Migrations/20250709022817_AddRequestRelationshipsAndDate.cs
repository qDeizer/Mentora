using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsikologProje_Void.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRequestRelationshipsAndDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "PatientId",
                table: "AppointmentRequests",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "DoctorId",
                table: "AppointmentRequests",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "AppointmentRequests",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

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

            migrationBuilder.AddForeignKey(
                name: "FK_AppointmentRequests_Appointments_AppointmentId",
                table: "AppointmentRequests",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_AppointmentRequests_AspNetUsers_DoctorId",
                table: "AppointmentRequests",
                column: "DoctorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_AppointmentRequests_AspNetUsers_PatientId",
                table: "AppointmentRequests",
                column: "PatientId",
                principalTable: "AspNetUsers",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppointmentRequests_Appointments_AppointmentId",
                table: "AppointmentRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_AppointmentRequests_AspNetUsers_DoctorId",
                table: "AppointmentRequests");

            migrationBuilder.DropForeignKey(
                name: "FK_AppointmentRequests_AspNetUsers_PatientId",
                table: "AppointmentRequests");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentRequests_AppointmentId",
                table: "AppointmentRequests");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentRequests_DoctorId",
                table: "AppointmentRequests");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentRequests_PatientId",
                table: "AppointmentRequests");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "AppointmentRequests");

            migrationBuilder.AlterColumn<string>(
                name: "PatientId",
                table: "AppointmentRequests",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.AlterColumn<string>(
                name: "DoctorId",
                table: "AppointmentRequests",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
