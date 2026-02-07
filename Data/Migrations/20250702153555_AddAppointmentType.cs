using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsikologProje_Void.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAppointmentType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsInPerson",
                table: "Appointments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "Appointments",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsInPerson",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "Appointments");
        }
    }
}
