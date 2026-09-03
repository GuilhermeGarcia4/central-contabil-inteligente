using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralContabil.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialPlanningV5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "InstallmentCount",
                schema: "central",
                table: "FinancialTransactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InstallmentNumber",
                schema: "central",
                table: "FinancialTransactions",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InstallmentPlanId",
                schema: "central",
                table: "FinancialTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FinancialGoals",
                schema: "central",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrentAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TargetDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsCompleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialGoals", x => x.Id);
                    table.CheckConstraint("CK_FinancialGoals_Amounts", "\"TargetAmount\" > 0 AND \"CurrentAmount\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "FinancialInstallmentPlans",
                schema: "central",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InstallmentCount = table.Column<int>(type: "integer", nullable: false),
                    FirstDueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PaymentMethod = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialInstallmentPlans", x => x.Id);
                    table.CheckConstraint("CK_FinancialInstallmentPlans_Values", "\"TotalAmount\" > 0 AND \"InstallmentCount\" >= 2");
                    table.ForeignKey(
                        name: "FK_FinancialInstallmentPlans_FinancialCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "central",
                        principalTable: "FinancialCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyBudgets",
                schema: "central",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    PlannedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyBudgets", x => x.Id);
                    table.CheckConstraint("CK_MonthlyBudgets_PlannedAmount_Positive", "\"PlannedAmount\" > 0");
                    table.ForeignKey(
                        name: "FK_MonthlyBudgets_FinancialCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "central",
                        principalTable: "FinancialCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialGoalContributions",
                schema: "central",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoalId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialGoalContributions", x => x.Id);
                    table.CheckConstraint("CK_FinancialGoalContributions_Amount_Positive", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_FinancialGoalContributions_FinancialGoals_GoalId",
                        column: x => x.GoalId,
                        principalSchema: "central",
                        principalTable: "FinancialGoals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_InstallmentPlanId_InstallmentNumber",
                schema: "central",
                table: "FinancialTransactions",
                columns: new[] { "InstallmentPlanId", "InstallmentNumber" },
                unique: true,
                filter: "\"InstallmentPlanId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialGoalContributions_GoalId",
                schema: "central",
                table: "FinancialGoalContributions",
                column: "GoalId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialGoalContributions_UserId_Date",
                schema: "central",
                table: "FinancialGoalContributions",
                columns: new[] { "UserId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialGoals_UserId_IsCompleted",
                schema: "central",
                table: "FinancialGoals",
                columns: new[] { "UserId", "IsCompleted" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialInstallmentPlans_CategoryId",
                schema: "central",
                table: "FinancialInstallmentPlans",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialInstallmentPlans_UserId_FirstDueDate",
                schema: "central",
                table: "FinancialInstallmentPlans",
                columns: new[] { "UserId", "FirstDueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyBudgets_CategoryId",
                schema: "central",
                table: "MonthlyBudgets",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_MonthlyBudgets_UserId_CategoryId_Year_Month",
                schema: "central",
                table: "MonthlyBudgets",
                columns: new[] { "UserId", "CategoryId", "Year", "Month" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialTransactions_FinancialInstallmentPlans_Installment~",
                schema: "central",
                table: "FinancialTransactions",
                column: "InstallmentPlanId",
                principalSchema: "central",
                principalTable: "FinancialInstallmentPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinancialTransactions_FinancialInstallmentPlans_Installment~",
                schema: "central",
                table: "FinancialTransactions");

            migrationBuilder.DropTable(
                name: "FinancialGoalContributions",
                schema: "central");

            migrationBuilder.DropTable(
                name: "FinancialInstallmentPlans",
                schema: "central");

            migrationBuilder.DropTable(
                name: "MonthlyBudgets",
                schema: "central");

            migrationBuilder.DropTable(
                name: "FinancialGoals",
                schema: "central");

            migrationBuilder.DropIndex(
                name: "IX_FinancialTransactions_InstallmentPlanId_InstallmentNumber",
                schema: "central",
                table: "FinancialTransactions");

            migrationBuilder.DropColumn(
                name: "InstallmentCount",
                schema: "central",
                table: "FinancialTransactions");

            migrationBuilder.DropColumn(
                name: "InstallmentNumber",
                schema: "central",
                table: "FinancialTransactions");

            migrationBuilder.DropColumn(
                name: "InstallmentPlanId",
                schema: "central",
                table: "FinancialTransactions");
        }
    }
}
