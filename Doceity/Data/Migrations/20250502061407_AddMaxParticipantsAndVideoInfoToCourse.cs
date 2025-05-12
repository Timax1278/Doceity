using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doceity.Data.Migrations
{
    public partial class AddMaxParticipantsAndVideoInfoToCourse : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VideoRoomInfo",
                table: "Courses");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Courses",
                newName: "CourseId");

            migrationBuilder.AddColumn<int>(
                name: "MaxParticipants",
                table: "Courses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VideoMeetingInfo",
                table: "Courses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxParticipants",
                table: "Courses");

            migrationBuilder.DropColumn(
                name: "VideoMeetingInfo",
                table: "Courses");

            migrationBuilder.RenameColumn(
                name: "CourseId",
                table: "Courses",
                newName: "Id");

            migrationBuilder.AddColumn<string>(
                name: "VideoRoomInfo",
                table: "Courses",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);
        }
    }
}
