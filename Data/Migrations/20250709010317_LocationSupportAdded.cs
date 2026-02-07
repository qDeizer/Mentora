using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace PsikologProje_Void.Data.Migrations
{
    /// <inheritdoc />
    public partial class LocationSupportAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Appointments");

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "AspNetUsers",
                type: "geography",
                nullable: true);

            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "Appointments",
                type: "geography",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "Appointments");

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "AspNetUsers",
                type: "float(18)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "AspNetUsers",
                type: "float(18)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Appointments",
                type: "float",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Appointments",
                type: "float",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
