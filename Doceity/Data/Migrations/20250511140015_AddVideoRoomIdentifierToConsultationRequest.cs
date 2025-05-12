using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doceity.Data.Migrations
{
    public partial class AddVideoRoomIdentifierToConsultationRequest : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VideoRoomIdentifier",
                table: "ConsultationRequests",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VideoRoomIdentifier",
                table: "ConsultationRequests");
        }
    }
}
