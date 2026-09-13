using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralContabil.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialDashboardV8 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinancialCategoryChartPreferences",
                schema: "central",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChartColor = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialCategoryChartPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialCategoryChartPreferences_FinancialCategories_Categ~",
                        column: x => x.CategoryId,
                        principalSchema: "central",
                        principalTable: "FinancialCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialCategoryChartPreferences_CategoryId",
                schema: "central",
                table: "FinancialCategoryChartPreferences",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialCategoryChartPreferences_UserId_CategoryId",
                schema: "central",
                table: "FinancialCategoryChartPreferences",
                columns: new[] { "UserId", "CategoryId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinancialCategoryChartPreferences",
                schema: "central");
        }
    }
}
