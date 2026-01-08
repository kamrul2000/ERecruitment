using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERecruitment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddApplicationCvSnapshotAndSalary : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedSalary",
                table: "JobApplications",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResumeContentTypeSnapshot",
                table: "JobApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResumeFileNameSnapshot",
                table: "JobApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ResumeSizeSnapshot",
                table: "JobApplications",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResumeUrlSnapshot",
                table: "JobApplications",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SalaryCurrency",
                table: "JobApplications",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ExpectedSalary",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "ResumeContentTypeSnapshot",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "ResumeFileNameSnapshot",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "ResumeSizeSnapshot",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "ResumeUrlSnapshot",
                table: "JobApplications");

            migrationBuilder.DropColumn(
                name: "SalaryCurrency",
                table: "JobApplications");
        }
    }
}
