using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralContabil.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEmbeddingProviderMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmbeddingDimensions",
                schema: "central",
                table: "KnowledgeChunks",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmbeddingModel",
                schema: "central",
                table: "KnowledgeChunks",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmbeddingProvider",
                schema: "central",
                table: "KnowledgeChunks",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeChunks_EmbeddingProvider_EmbeddingModel_EmbeddingD~",
                schema: "central",
                table: "KnowledgeChunks",
                columns: new[] { "EmbeddingProvider", "EmbeddingModel", "EmbeddingDimensions" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KnowledgeChunks_EmbeddingProvider_EmbeddingModel_EmbeddingD~",
                schema: "central",
                table: "KnowledgeChunks");

            migrationBuilder.DropColumn(
                name: "EmbeddingDimensions",
                schema: "central",
                table: "KnowledgeChunks");

            migrationBuilder.DropColumn(
                name: "EmbeddingModel",
                schema: "central",
                table: "KnowledgeChunks");

            migrationBuilder.DropColumn(
                name: "EmbeddingProvider",
                schema: "central",
                table: "KnowledgeChunks");
        }
    }
}
