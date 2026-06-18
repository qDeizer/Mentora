using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using PsikologProje_Void.Data;

#nullable disable

namespace PsikologProje_Void.Data.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260618090000_SchoolDemoThemeAndRequests")]
    public partial class SchoolDemoThemeAndRequests : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ThemePreference",
                table: "AspNetUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "system");

            migrationBuilder.AddColumn<string>(
                name: "LayoutDensity",
                table: "AspNetUsers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "comfortable");

            migrationBuilder.AddColumn<string>(
                name: "MeetingLink",
                table: "Appointments",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReasonForVisit",
                table: "AppointmentRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PreviousSupportInfo",
                table: "AppointmentRequests",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UrgencyLevel",
                table: "AppointmentRequests",
                type: "nvarchar(40)",
                maxLength: 40,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Expectations",
                table: "AppointmentRequests",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql("UPDATE Specialties SET Name = N'Çocuk Psikolojisi' WHERE Id = 1;");
            migrationBuilder.Sql("UPDATE Specialties SET Name = N'Anksiyete Bozuklukları' WHERE Id = 4;");
            migrationBuilder.Sql("UPDATE Specialties SET Name = N'Çift Terapisi' WHERE Id = 6;");
            migrationBuilder.Sql("UPDATE Specialties SET Name = N'Yeme Bozuklukları' WHERE Id = 7;");
            migrationBuilder.Sql("UPDATE Specialties SET Name = N'Bağımlılık Tedavisi' WHERE Id = 8;");
            migrationBuilder.Sql("UPDATE Specialties SET Name = N'Kişilik Bozuklukları' WHERE Id = 9;");
            migrationBuilder.Sql("UPDATE Specialties SET Name = N'Yaşlı Psikolojisi' WHERE Id = 10;");
            migrationBuilder.Sql("UPDATE Specialties SET Name = N'Kariyer Danışmanlığı' WHERE Id = 12;");
            migrationBuilder.Sql("UPDATE Specialties SET Name = N'Stres Yönetimi' WHERE Id = 13;");
            migrationBuilder.Sql("UPDATE Specialties SET Name = N'Öfke Yönetimi' WHERE Id = 14;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE Specialties SET Name = N'Cocuk Psikolojisi' WHERE Id = 1;");
            migrationBuilder.Sql("UPDATE Specialties SET Name = N'Anksiyete Bozukluklari' WHERE Id = 4;");
            migrationBuilder.Sql("UPDATE Specialties SET Name = N'Cift Terapisi' WHERE Id = 6;");
            migrationBuilder.Sql("UPDATE Specialties SET Name = N'Yeme Bozukluklari' WHERE Id = 7;");
            migrationBuilder.Sql("UPDATE Specialties SET Name = N'Bagimlilik Tedavisi' WHERE Id = 8;");
            migrationBuilder.Sql("UPDATE Specialties SET Name = N'Kisilik Bozukluklari' WHERE Id = 9;");
            migrationBuilder.Sql("UPDATE Specialties SET Name = N'Yasli Psikolojisi' WHERE Id = 10;");
            migrationBuilder.Sql("UPDATE Specialties SET Name = N'Kariyer Danismanligi' WHERE Id = 12;");
            migrationBuilder.Sql("UPDATE Specialties SET Name = N'Stres Yonetimi' WHERE Id = 13;");
            migrationBuilder.Sql("UPDATE Specialties SET Name = N'Ofke Yönetimi' WHERE Id = 14;");

            migrationBuilder.DropColumn("ThemePreference", "AspNetUsers");
            migrationBuilder.DropColumn("LayoutDensity", "AspNetUsers");
            migrationBuilder.DropColumn("MeetingLink", "Appointments");
            migrationBuilder.DropColumn("ReasonForVisit", "AppointmentRequests");
            migrationBuilder.DropColumn("PreviousSupportInfo", "AppointmentRequests");
            migrationBuilder.DropColumn("UrgencyLevel", "AppointmentRequests");
            migrationBuilder.DropColumn("Expectations", "AppointmentRequests");
        }
    }
}
