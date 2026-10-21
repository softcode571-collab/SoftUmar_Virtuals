using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SoftUmar_Virtuals.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PlatformSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PlatformSetting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UsdToNgnRate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MarkupPercentage = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlatformSetting", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlatformSetting");
        }
    }
}
