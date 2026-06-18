using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PsikologProje_Void.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorPatientConnectionState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DoctorPatientConnectionStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    PatientId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DisconnectedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DisconnectedByUserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorPatientConnectionStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorPatientConnectionStates_AspNetUsers_DisconnectedByUserId",
                        column: x => x.DisconnectedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DoctorPatientConnectionStates_AspNetUsers_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_DoctorPatientConnectionStates_AspNetUsers_PatientId",
                        column: x => x.PatientId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPatientConnectionStates_DisconnectedByUserId",
                table: "DoctorPatientConnectionStates",
                column: "DisconnectedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPatientConnectionStates_DoctorId_DisconnectedAtUtc",
                table: "DoctorPatientConnectionStates",
                columns: new[] { "DoctorId", "DisconnectedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPatientConnectionStates_DoctorId_PatientId",
                table: "DoctorPatientConnectionStates",
                columns: new[] { "DoctorId", "PatientId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DoctorPatientConnectionStates_PatientId_DisconnectedAtUtc",
                table: "DoctorPatientConnectionStates",
                columns: new[] { "PatientId", "DisconnectedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoctorPatientConnectionStates");
        }
    }
}
