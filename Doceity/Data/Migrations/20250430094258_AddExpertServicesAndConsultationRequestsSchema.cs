using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Doceity.Data.Migrations
{
    public partial class AddExpertServicesAndConsultationRequestsSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpertServices",
                columns: table => new
                {
                    ExpertServiceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpertUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    EstimatedDurationMinutes = table.Column<int>(type: "int", nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpertServices", x => x.ExpertServiceId);
                    table.ForeignKey(
                        name: "FK_ExpertServices_AspNetUsers_ExpertUserId",
                        column: x => x.ExpertUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConsultationRequests",
                columns: table => new
                {
                    ConsultationRequestId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestingUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ExpertUserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RequestedExpertServiceId = table.Column<int>(type: "int", nullable: true),
                    ProposedDateTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserMessage = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: false),
                    RequestTimestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ResponseTimestamp = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpertResponseMessage = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConsultationRequests", x => x.ConsultationRequestId);
                    table.ForeignKey(
                        name: "FK_ConsultationRequests_AspNetUsers_ExpertUserId",
                        column: x => x.ExpertUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsultationRequests_AspNetUsers_RequestingUserId",
                        column: x => x.RequestingUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ConsultationRequests_ExpertServices_RequestedExpertServiceId",
                        column: x => x.RequestedExpertServiceId,
                        principalTable: "ExpertServices",
                        principalColumn: "ExpertServiceId",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationRequests_ExpertUserId",
                table: "ConsultationRequests",
                column: "ExpertUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationRequests_RequestedExpertServiceId",
                table: "ConsultationRequests",
                column: "RequestedExpertServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ConsultationRequests_RequestingUserId",
                table: "ConsultationRequests",
                column: "RequestingUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ExpertServices_ExpertUserId",
                table: "ExpertServices",
                column: "ExpertUserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConsultationRequests");

            migrationBuilder.DropTable(
                name: "ExpertServices");
        }
    }
}
