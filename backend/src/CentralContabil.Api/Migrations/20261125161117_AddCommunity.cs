using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralContabil.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CommunityDiscussions",
                schema: "central",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorDisplayName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    AuthorPublicRole = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Title = table.Column<string>(type: "character varying(140)", maxLength: 140, nullable: false),
                    Content = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    Category = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EditedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsRemoved = table.Column<bool>(type: "boolean", nullable: false),
                    RemovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RemovedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityDiscussions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommunityModerationActions",
                schema: "central",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ActorId = table.Column<Guid>(type: "uuid", nullable: false),
                    TargetType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    TargetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Action = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityModerationActions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommunityReports",
                schema: "central",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReporterId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscussionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CommentId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reason = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Details = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityReports", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CommunityComments",
                schema: "central",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscussionId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorId = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthorDisplayName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    AuthorPublicRole = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Content = table.Column<string>(type: "character varying(3000)", maxLength: 3000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EditedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsRemoved = table.Column<bool>(type: "boolean", nullable: false),
                    RemovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RemovedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CommunityComments_CommunityDiscussions_DiscussionId",
                        column: x => x.DiscussionId,
                        principalSchema: "central",
                        principalTable: "CommunityDiscussions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CommunityUsefulReactions",
                schema: "central",
                columns: table => new
                {
                    DiscussionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CommunityUsefulReactions", x => new { x.DiscussionId, x.UserId });
                    table.ForeignKey(
                        name: "FK_CommunityUsefulReactions_CommunityDiscussions_DiscussionId",
                        column: x => x.DiscussionId,
                        principalSchema: "central",
                        principalTable: "CommunityDiscussions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityComments_AuthorId",
                schema: "central",
                table: "CommunityComments",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityComments_DiscussionId_CreatedAt",
                schema: "central",
                table: "CommunityComments",
                columns: new[] { "DiscussionId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityDiscussions_AuthorId",
                schema: "central",
                table: "CommunityDiscussions",
                column: "AuthorId");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityDiscussions_IsRemoved_Category_CreatedAt",
                schema: "central",
                table: "CommunityDiscussions",
                columns: new[] { "IsRemoved", "Category", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityModerationActions_TargetType_TargetId_CreatedAt",
                schema: "central",
                table: "CommunityModerationActions",
                columns: new[] { "TargetType", "TargetId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CommunityReports_Status_CreatedAt",
                schema: "central",
                table: "CommunityReports",
                columns: new[] { "Status", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CommunityComments",
                schema: "central");

            migrationBuilder.DropTable(
                name: "CommunityModerationActions",
                schema: "central");

            migrationBuilder.DropTable(
                name: "CommunityReports",
                schema: "central");

            migrationBuilder.DropTable(
                name: "CommunityUsefulReactions",
                schema: "central");

            migrationBuilder.DropTable(
                name: "CommunityDiscussions",
                schema: "central");
        }
    }
}
