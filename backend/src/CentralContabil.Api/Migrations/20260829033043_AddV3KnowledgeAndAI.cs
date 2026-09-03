using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralContabil.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddV3KnowledgeAndAI : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS vector;");
            migrationBuilder.CreateTable(
                name: "AIRequests",
                schema: "central",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    Model = table.Column<string>(type: "text", nullable: false),
                    InputTokens = table.Column<int>(type: "integer", nullable: true),
                    OutputTokens = table.Column<int>(type: "integer", nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Intent = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeDocuments",
                schema: "central",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    DocumentType = table.Column<int>(type: "integer", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: true),
                    SourceId = table.Column<Guid>(type: "uuid", nullable: true),
                    LegalReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    Content = table.Column<string>(type: "text", nullable: false),
                    ContentHash = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Jurisdiction = table.Column<string>(type: "text", nullable: false),
                    OriginUrl = table.Column<string>(type: "text", nullable: true),
                    IsOfficial = table.Column<bool>(type: "boolean", nullable: false),
                    ValidFrom = table.Column<DateOnly>(type: "date", nullable: true),
                    ValidUntil = table.Column<DateOnly>(type: "date", nullable: true),
                    LastVerifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    NeedsReviewAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastIndexedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UnansweredQuestions",
                schema: "central",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NormalizedQuestion = table.Column<string>(type: "text", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    FirstAskedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastAskedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UnansweredQuestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeChunks",
                schema: "central",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    KnowledgeDocumentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChunkIndex = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    TokenEstimate = table.Column<int>(type: "integer", nullable: false),
                    EmbeddingStatus = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeChunks_KnowledgeDocuments_KnowledgeDocumentId",
                        column: x => x.KnowledgeDocumentId,
                        principalSchema: "central",
                        principalTable: "KnowledgeDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIRequests_CreatedAt",
                schema: "central",
                table: "AIRequests",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeChunks_KnowledgeDocumentId_ChunkIndex",
                schema: "central",
                table: "KnowledgeChunks",
                columns: new[] { "KnowledgeDocumentId", "ChunkIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocuments_ArticleId",
                schema: "central",
                table: "KnowledgeDocuments",
                column: "ArticleId",
                unique: true,
                filter: "\"ArticleId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocuments_ContentHash",
                schema: "central",
                table: "KnowledgeDocuments",
                column: "ContentHash");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocuments_Status_ValidFrom_ValidUntil",
                schema: "central",
                table: "KnowledgeDocuments",
                columns: new[] { "Status", "ValidFrom", "ValidUntil" });

            migrationBuilder.CreateIndex(
                name: "IX_UnansweredQuestions_NormalizedQuestion",
                schema: "central",
                table: "UnansweredQuestions",
                column: "NormalizedQuestion",
                unique: true);

            migrationBuilder.Sql("ALTER TABLE central.\"KnowledgeChunks\" ADD COLUMN \"Embedding\" vector(384) NULL;");
            migrationBuilder.Sql("CREATE INDEX \"IX_KnowledgeChunks_Embedding_Hnsw\" ON central.\"KnowledgeChunks\" USING hnsw (\"Embedding\" vector_cosine_ops);");
            migrationBuilder.Sql("CREATE INDEX \"IX_KnowledgeDocuments_FullText\" ON central.\"KnowledgeDocuments\" USING gin (to_tsvector('portuguese', coalesce(\"Title\", '') || ' ' || coalesce(\"Content\", '')));");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIRequests",
                schema: "central");

            migrationBuilder.DropTable(
                name: "KnowledgeChunks",
                schema: "central");

            migrationBuilder.DropTable(
                name: "UnansweredQuestions",
                schema: "central");

            migrationBuilder.DropTable(
                name: "KnowledgeDocuments",
                schema: "central");
        }
    }
}
