using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERecruitment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Job : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "JobPostings",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "JobPostings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "JobPostings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "JobPostings",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "JobApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobPostings_TenantId_Title",
                table: "JobPostings",
                columns: new[] { "TenantId", "Title" });

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_CandidateId",
                table: "JobApplications",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_JobPostingId",
                table: "JobApplications",
                column: "JobPostingId");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_TenantId_CandidateId_JobPostingId",
                table: "JobApplications",
                columns: new[] { "TenantId", "CandidateId", "JobPostingId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_JobApplications_Candidates_CandidateId",
                table: "JobApplications",
                column: "CandidateId",
                principalTable: "Candidates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_JobApplications_JobPostings_JobPostingId",
                table: "JobApplications",
                column: "JobPostingId",
                principalTable: "JobPostings",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobApplications_Candidates_CandidateId",
                table: "JobApplications");

            migrationBuilder.DropForeignKey(
                name: "FK_JobApplications_JobPostings_JobPostingId",
                table: "JobApplications");

            migrationBuilder.DropIndex(
                name: "IX_JobPostings_TenantId_Title",
                table: "JobPostings");

            migrationBuilder.DropIndex(
                name: "IX_JobApplications_CandidateId",
                table: "JobApplications");

            migrationBuilder.DropIndex(
                name: "IX_JobApplications_JobPostingId",
                table: "JobApplications");

            migrationBuilder.DropIndex(
                name: "IX_JobApplications_TenantId_CandidateId_JobPostingId",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "JobPostings");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "JobApplications");

            migrationBuilder.AlterColumn<string>(
                name: "Title",
                table: "JobPostings",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
