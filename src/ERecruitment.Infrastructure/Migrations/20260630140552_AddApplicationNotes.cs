using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERecruitment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationNotes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobApplicationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AuthorUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AuthorEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Kind = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Body = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TechnicalScore = table.Column<int>(type: "int", nullable: true),
                    CommunicationScore = table.Column<int>(type: "int", nullable: true),
                    CultureFitScore = table.Column<int>(type: "int", nullable: true),
                    Recommendation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ApplicationNotes_JobApplications_JobApplicationId",
                        column: x => x.JobApplicationId,
                        principalTable: "JobApplications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationNotes_JobApplicationId",
                table: "ApplicationNotes",
                column: "JobApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationNotes_TenantId_JobApplicationId_CreatedAt",
                table: "ApplicationNotes",
                columns: new[] { "TenantId", "JobApplicationId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationNotes");
        }
    }
}
