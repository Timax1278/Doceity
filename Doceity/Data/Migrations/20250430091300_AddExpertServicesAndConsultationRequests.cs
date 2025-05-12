using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doceity.Data.Migrations
{
    public partial class AddExpertServicesAndConsultationRequests : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Availabilities");

            migrationBuilder.DropTable(
                name: "ExpertProfiles");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Availabilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpertUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    AvailableDate = table.Column<DateTime>(type: "date", nullable: false),
                    EndTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    StartTime = table.Column<TimeSpan>(type: "time", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Availabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Availabilities_AspNetUsers_ExpertUserId",
                        column: x => x.ExpertUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExpertProfiles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Bio = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    JobTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Specializations = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    WebsiteUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    YearsOfExperience = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpertProfiles", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_ExpertProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Availabilities_AvailableDate",
                table: "Availabilities",
                column: "AvailableDate");

            migrationBuilder.CreateIndex(
                name: "IX_Availabilities_ExpertUserId",
                table: "Availabilities",
                column: "ExpertUserId");
        }
    }
}
