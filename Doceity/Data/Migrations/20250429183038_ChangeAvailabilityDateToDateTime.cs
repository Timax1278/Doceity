using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doceity.Data.Migrations
{
    public partial class ChangeAvailabilityDateToDateTime : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DayOfWeek",
                table: "Availabilities");

            migrationBuilder.AddColumn<DateTime>(
                name: "AvailableDate",
                table: "Availabilities",
                type: "date",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    StartDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatorExpertId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VideoRoomInfo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Courses_AspNetUsers_CreatorExpertId",
                        column: x => x.CreatorExpertId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Availabilities_AvailableDate",
                table: "Availabilities",
                column: "AvailableDate");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CreatorExpertId",
                table: "Courses",
                column: "CreatorExpertId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_StartDateTime",
                table: "Courses",
                column: "StartDateTime");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropIndex(
                name: "IX_Availabilities_AvailableDate",
                table: "Availabilities");

            migrationBuilder.DropColumn(
                name: "AvailableDate",
                table: "Availabilities");

            migrationBuilder.AddColumn<int>(
                name: "DayOfWeek",
                table: "Availabilities",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
