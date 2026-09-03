using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralContabil.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddFinancialV6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "AccountId",
                schema: "central",
                table: "FinancialTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CreditCardId",
                schema: "central",
                table: "FinancialTransactions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CreditCards",
                schema: "central",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Bank = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Limit = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    ClosingDay = table.Column<int>(type: "integer", nullable: false),
                    DueDay = table.Column<int>(type: "integer", nullable: false),
                    Last4 = table.Column<string>(type: "character varying(4)", maxLength: 4, nullable: true),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CreditCards", x => x.Id);
                    table.CheckConstraint("CK_CreditCards_Values", "\"Limit\" >= 0 AND \"ClosingDay\" BETWEEN 1 AND 31 AND \"DueDay\" BETWEEN 1 AND 31");
                });

            migrationBuilder.CreateTable(
                name: "Debts",
                schema: "central",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    OriginalAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrentAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    InstallmentCount = table.Column<int>(type: "integer", nullable: true),
                    InterestRate = table.Column<decimal>(type: "numeric(9,4)", precision: 9, scale: 4, nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Institution = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsPaid = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Debts", x => x.Id);
                    table.CheckConstraint("CK_Debts_Values", "\"OriginalAmount\" > 0 AND \"CurrentAmount\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "EmergencyReserves",
                schema: "central",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CurrentAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TargetMonths = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmergencyReserves", x => x.Id);
                    table.CheckConstraint("CK_EmergencyReserves_Values", "\"TargetAmount\" > 0 AND \"CurrentAmount\" >= 0");
                });

            migrationBuilder.CreateTable(
                name: "FinancialAccounts",
                schema: "central",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Color = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialAccounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledTransactions",
                schema: "central",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledTransactions", x => x.Id);
                    table.CheckConstraint("CK_ScheduledTransactions_Amount_Positive", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_ScheduledTransactions_FinancialAccounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "central",
                        principalTable: "FinancialAccounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_ScheduledTransactions_FinancialCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "central",
                        principalTable: "FinancialCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_AccountId",
                schema: "central",
                table: "FinancialTransactions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_CreditCardId",
                schema: "central",
                table: "FinancialTransactions",
                column: "CreditCardId");

            migrationBuilder.CreateIndex(
                name: "IX_CreditCards_UserId_IsActive",
                schema: "central",
                table: "CreditCards",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_Debts_UserId_IsPaid",
                schema: "central",
                table: "Debts",
                columns: new[] { "UserId", "IsPaid" });

            migrationBuilder.CreateIndex(
                name: "IX_EmergencyReserves_UserId",
                schema: "central",
                table: "EmergencyReserves",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialAccounts_UserId_IsActive",
                schema: "central",
                table: "FinancialAccounts",
                columns: new[] { "UserId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTransactions_AccountId",
                schema: "central",
                table: "ScheduledTransactions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTransactions_CategoryId",
                schema: "central",
                table: "ScheduledTransactions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTransactions_UserId_DueDate",
                schema: "central",
                table: "ScheduledTransactions",
                columns: new[] { "UserId", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledTransactions_UserId_Status",
                schema: "central",
                table: "ScheduledTransactions",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialTransactions_CreditCards_CreditCardId",
                schema: "central",
                table: "FinancialTransactions",
                column: "CreditCardId",
                principalSchema: "central",
                principalTable: "CreditCards",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_FinancialTransactions_FinancialAccounts_AccountId",
                schema: "central",
                table: "FinancialTransactions",
                column: "AccountId",
                principalSchema: "central",
                principalTable: "FinancialAccounts",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FinancialTransactions_CreditCards_CreditCardId",
                schema: "central",
                table: "FinancialTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_FinancialTransactions_FinancialAccounts_AccountId",
                schema: "central",
                table: "FinancialTransactions");

            migrationBuilder.DropTable(
                name: "CreditCards",
                schema: "central");

            migrationBuilder.DropTable(
                name: "Debts",
                schema: "central");

            migrationBuilder.DropTable(
                name: "EmergencyReserves",
                schema: "central");

            migrationBuilder.DropTable(
                name: "ScheduledTransactions",
                schema: "central");

            migrationBuilder.DropTable(
                name: "FinancialAccounts",
                schema: "central");

            migrationBuilder.DropIndex(
                name: "IX_FinancialTransactions_AccountId",
                schema: "central",
                table: "FinancialTransactions");

            migrationBuilder.DropIndex(
                name: "IX_FinancialTransactions_CreditCardId",
                schema: "central",
                table: "FinancialTransactions");

            migrationBuilder.DropColumn(
                name: "AccountId",
                schema: "central",
                table: "FinancialTransactions");

            migrationBuilder.DropColumn(
                name: "CreditCardId",
                schema: "central",
                table: "FinancialTransactions");
        }
    }
}
