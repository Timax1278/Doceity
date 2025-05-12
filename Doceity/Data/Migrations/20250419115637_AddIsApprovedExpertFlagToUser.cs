using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doceity.Data.Migrations
{
    public partial class AddIsApprovedExpertFlagToUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpertProfile_AspNetUsers_UserId",
                table: "ExpertProfile");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExpertProfile",
                table: "ExpertProfile");

            migrationBuilder.RenameTable(
                name: "ExpertProfile",
                newName: "ExpertProfiles");

            migrationBuilder.AddColumn<bool>(
                name: "IsApprovedExpert",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExpertProfiles",
                table: "ExpertProfiles",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpertProfiles_AspNetUsers_UserId",
                table: "ExpertProfiles",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ExpertProfiles_AspNetUsers_UserId",
                table: "ExpertProfiles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ExpertProfiles",
                table: "ExpertProfiles");

            migrationBuilder.DropColumn(
                name: "IsApprovedExpert",
                table: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "ExpertProfiles",
                newName: "ExpertProfile");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ExpertProfile",
                table: "ExpertProfile",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ExpertProfile_AspNetUsers_UserId",
                table: "ExpertProfile",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
