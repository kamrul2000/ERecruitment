using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERecruitment.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTenantThemeSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TenantThemeSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CompanyName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FaviconUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrimaryColor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SecondaryColor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BackgroundColor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FontFamily = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Template = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CustomCss = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantThemeSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TenantThemeSettings_TenantId",
                table: "TenantThemeSettings",
                column: "TenantId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TenantThemeSettings");
        }
    }
}
