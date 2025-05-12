using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doceity.Data.Migrations
{
    public partial class AddOriginalAvailabilityAndIsBookedWithRestrictFK : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OriginalAvailabilityId",
                table: "ConsultationRequests",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsBooked",
                table: "Availabilities",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationRequests_OriginalAvailabilityId",
                table: "ConsultationRequests",
                column: "OriginalAvailabilityId");

            migrationBuilder.AddForeignKey(
                name: "FK_ConsultationRequests_Availabilities_OriginalAvailabilityId",
                table: "ConsultationRequests",
                column: "OriginalAvailabilityId",
                principalTable: "Availabilities",
                principalColumn: "AvailabilityId",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ConsultationRequests_Availabilities_OriginalAvailabilityId",
                table: "ConsultationRequests");

            migrationBuilder.DropIndex(
                name: "IX_ConsultationRequests_OriginalAvailabilityId",
                table: "ConsultationRequests");

            migrationBuilder.DropColumn(
                name: "OriginalAvailabilityId",
                table: "ConsultationRequests");

            migrationBuilder.DropColumn(
                name: "IsBooked",
                table: "Availabilities");
        }
    }
}
