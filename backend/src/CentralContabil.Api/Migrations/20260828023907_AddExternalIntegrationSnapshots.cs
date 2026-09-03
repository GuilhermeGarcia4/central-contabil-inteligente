using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralContabil.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalIntegrationSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExternalEntitySnapshots",
                schema: "central",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    ExternalKey = table.Column<string>(type: "text", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false),
                    RetrievedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalEntitySnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExternalProviders",
                schema: "central",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    BaseUrl = table.Column<string>(type: "text", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastSuccessAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastFailureAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExternalProviders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExternalEntitySnapshots_ExpiresAt",
                schema: "central",
                table: "ExternalEntitySnapshots",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_ExternalEntitySnapshots_Provider_EntityType_ExternalKey",
                schema: "central",
                table: "ExternalEntitySnapshots",
                columns: new[] { "Provider", "EntityType", "ExternalKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExternalProviders_Name",
                schema: "central",
                table: "ExternalProviders",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExternalEntitySnapshots",
                schema: "central");

            migrationBuilder.DropTable(
                name: "ExternalProviders",
                schema: "central");
        }
    }
}
