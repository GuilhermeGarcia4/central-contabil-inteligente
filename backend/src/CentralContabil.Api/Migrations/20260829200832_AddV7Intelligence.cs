using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralContabil.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddV7Intelligence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AssistantConversations",
                schema: "central",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssistantConversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserPreferences",
                schema: "central",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExplanationProfile = table.Column<int>(type: "integer", nullable: false),
                    AlertBills = table.Column<bool>(type: "boolean", nullable: false),
                    AlertInvoices = table.Column<bool>(type: "boolean", nullable: false),
                    AlertBudget = table.Column<bool>(type: "boolean", nullable: false),
                    AlertGoals = table.Column<bool>(type: "boolean", nullable: false),
                    AlertInstallments = table.Column<bool>(type: "boolean", nullable: false),
                    AlertWeeklySummary = table.Column<bool>(type: "boolean", nullable: false),
                    AlertMonthlySummary = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserPreferences", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AssistantMessages",
                schema: "central",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                    SourcesJson = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssistantMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssistantMessages_AssistantConversations_ConversationId",
                        column: x => x.ConversationId,
                        principalSchema: "central",
                        principalTable: "AssistantConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AssistantConversations_UserId_CreatedAt",
                schema: "central",
                table: "AssistantConversations",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AssistantMessages_ConversationId_CreatedAt",
                schema: "central",
                table: "AssistantMessages",
                columns: new[] { "ConversationId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_UserPreferences_UserId",
                schema: "central",
                table: "UserPreferences",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssistantMessages",
                schema: "central");

            migrationBuilder.DropTable(
                name: "UserPreferences",
                schema: "central");

            migrationBuilder.DropTable(
                name: "AssistantConversations",
                schema: "central");
        }
    }
}
