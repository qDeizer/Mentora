using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace PsikologProje_Void.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLocationModesAndNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LocationNote",
                table: "Appointments",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InPersonLocationMode",
                table: "AppointmentAutomationRoutines",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "profile");

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "AppointmentAutomationRoutines",
                type: "geography",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LocationNote",
                table: "AppointmentAutomationRoutines",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocationNote",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "InPersonLocationMode",
                table: "AppointmentAutomationRoutines");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "AppointmentAutomationRoutines");

            migrationBuilder.DropColumn(
                name: "LocationNote",
                table: "AppointmentAutomationRoutines");
        }
    }
}
