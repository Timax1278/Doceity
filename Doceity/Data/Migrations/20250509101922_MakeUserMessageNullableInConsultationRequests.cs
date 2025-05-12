using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doceity.Data.Migrations
{
    public partial class MakeUserMessageNullableInConsultationRequests : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserMessage",
                table: "ConsultationRequests",
                type: "nvarchar(1500)",
                maxLength: 1500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1500)",
                oldMaxLength: 1500);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UserMessage",
                table: "ConsultationRequests",
                type: "nvarchar(1500)",
                maxLength: 1500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(1500)",
                oldMaxLength: 1500,
                oldNullable: true);
        }
    }
}
