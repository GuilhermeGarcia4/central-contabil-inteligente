using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace CentralContabil.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPersonalFinanceModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FinancialCategories",
                schema: "central",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Icon = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FinancialCategoryPreferences",
                schema: "central",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    NormalizedText = table.Column<string>(type: "character varying(180)", maxLength: 180, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    UsageCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialCategoryPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FinancialCategoryPreferences_FinancialCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "central",
                        principalTable: "FinancialCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinancialRecurrences",
                schema: "central",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    PaymentMethod = table.Column<int>(type: "integer", nullable: false),
                    Frequency = table.Column<int>(type: "integer", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    NextOccurrence = table.Column<DateOnly>(type: "date", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialRecurrences", x => x.Id);
                    table.CheckConstraint("CK_FinancialRecurrences_Amount_Positive", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_FinancialRecurrences_FinancialCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "central",
                        principalTable: "FinancialCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FinancialTransactions",
                schema: "central",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CategoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    TransactionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PaymentMethod = table.Column<int>(type: "integer", nullable: false),
                    IsRecurring = table.Column<bool>(type: "boolean", nullable: false),
                    RecurrenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialTransactions", x => x.Id);
                    table.CheckConstraint("CK_FinancialTransactions_Amount_Positive", "\"Amount\" > 0");
                    table.ForeignKey(
                        name: "FK_FinancialTransactions_FinancialCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "central",
                        principalTable: "FinancialCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FinancialTransactions_FinancialRecurrences_RecurrenceId",
                        column: x => x.RecurrenceId,
                        principalSchema: "central",
                        principalTable: "FinancialRecurrences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.InsertData(
                schema: "central",
                table: "FinancialCategories",
                columns: new[] { "Id", "CreatedAt", "Icon", "IsActive", "IsDefault", "Name", "Type", "UserId" },
                values: new object[,]
                {
                    { new Guid("40000000-0000-0000-0000-000000000001"), new DateTimeOffset(new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "home", true, true, "Moradia", 1, null },
                    { new Guid("40000000-0000-0000-0000-000000000002"), new DateTimeOffset(new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "food", true, true, "Alimentação", 1, null },
                    { new Guid("40000000-0000-0000-0000-000000000003"), new DateTimeOffset(new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "transport", true, true, "Transporte", 1, null },
                    { new Guid("40000000-0000-0000-0000-000000000004"), new DateTimeOffset(new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "health", true, true, "Saúde", 1, null },
                    { new Guid("40000000-0000-0000-0000-000000000005"), new DateTimeOffset(new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "education", true, true, "Educação", 1, null },
                    { new Guid("40000000-0000-0000-0000-000000000006"), new DateTimeOffset(new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "leisure", true, true, "Lazer", 1, null },
                    { new Guid("40000000-0000-0000-0000-000000000007"), new DateTimeOffset(new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "shopping", true, true, "Compras", 1, null },
                    { new Guid("40000000-0000-0000-0000-000000000008"), new DateTimeOffset(new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "subscription", true, true, "Assinaturas", 1, null },
                    { new Guid("40000000-0000-0000-0000-000000000009"), new DateTimeOffset(new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "bill", true, true, "Contas", 1, null },
                    { new Guid("40000000-0000-0000-0000-000000000010"), new DateTimeOffset(new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "tax", true, true, "Impostos", 1, null },
                    { new Guid("40000000-0000-0000-0000-000000000011"), new DateTimeOffset(new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "debt", true, true, "Dívidas", 1, null },
                    { new Guid("40000000-0000-0000-0000-000000000012"), new DateTimeOffset(new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "pet", true, true, "Pets", 1, null },
                    { new Guid("40000000-0000-0000-0000-000000000013"), new DateTimeOffset(new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "gift", true, true, "Presentes", 1, null },
                    { new Guid("40000000-0000-0000-0000-000000000014"), new DateTimeOffset(new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "other", true, true, "Outros", 1, null },
                    { new Guid("40000000-0000-0000-0000-000000000015"), new DateTimeOffset(new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "salary", true, true, "Salário", 0, null },
                    { new Guid("40000000-0000-0000-0000-000000000016"), new DateTimeOffset(new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "freelance", true, true, "Freelance", 0, null },
                    { new Guid("40000000-0000-0000-0000-000000000017"), new DateTimeOffset(new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "benefit", true, true, "Benefícios", 0, null },
                    { new Guid("40000000-0000-0000-0000-000000000018"), new DateTimeOffset(new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "investment", true, true, "Investimentos", 0, null },
                    { new Guid("40000000-0000-0000-0000-000000000019"), new DateTimeOffset(new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "sale", true, true, "Vendas", 0, null },
                    { new Guid("40000000-0000-0000-0000-000000000020"), new DateTimeOffset(new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "refund", true, true, "Reembolso", 0, null },
                    { new Guid("40000000-0000-0000-0000-000000000021"), new DateTimeOffset(new DateTime(2026, 8, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "other", true, true, "Outras receitas", 0, null }
                });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialCategories_Type_IsActive",
                schema: "central",
                table: "FinancialCategories",
                columns: new[] { "Type", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialCategories_UserId_Type_Name",
                schema: "central",
                table: "FinancialCategories",
                columns: new[] { "UserId", "Type", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialCategoryPreferences_CategoryId",
                schema: "central",
                table: "FinancialCategoryPreferences",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialCategoryPreferences_UserId_Type_NormalizedText",
                schema: "central",
                table: "FinancialCategoryPreferences",
                columns: new[] { "UserId", "Type", "NormalizedText" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialRecurrences_CategoryId",
                schema: "central",
                table: "FinancialRecurrences",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialRecurrences_UserId_IsActive_NextOccurrence",
                schema: "central",
                table: "FinancialRecurrences",
                columns: new[] { "UserId", "IsActive", "NextOccurrence" });

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_CategoryId",
                schema: "central",
                table: "FinancialTransactions",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_RecurrenceId_TransactionDate",
                schema: "central",
                table: "FinancialTransactions",
                columns: new[] { "RecurrenceId", "TransactionDate" },
                unique: true,
                filter: "\"RecurrenceId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_UserId",
                schema: "central",
                table: "FinancialTransactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialTransactions_UserId_TransactionDate",
                schema: "central",
                table: "FinancialTransactions",
                columns: new[] { "UserId", "TransactionDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FinancialCategoryPreferences",
                schema: "central");

            migrationBuilder.DropTable(
                name: "FinancialTransactions",
                schema: "central");

            migrationBuilder.DropTable(
                name: "FinancialRecurrences",
                schema: "central");

            migrationBuilder.DropTable(
                name: "FinancialCategories",
                schema: "central");
        }
    }
}
