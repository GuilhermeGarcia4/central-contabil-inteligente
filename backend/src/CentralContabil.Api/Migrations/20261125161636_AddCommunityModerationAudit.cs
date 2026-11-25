using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CentralContabil.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCommunityModerationAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActorDisplayName",
                schema: "central",
                table: "CommunityModerationActions",
                type: "character varying(80)",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityReports_ReporterId_CommentId",
                schema: "central",
                table: "CommunityReports",
                columns: new[] { "ReporterId", "CommentId" },
                unique: true,
                filter: "\"Status\" = 'Pending' AND \"CommentId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_CommunityReports_ReporterId_DiscussionId",
                schema: "central",
                table: "CommunityReports",
                columns: new[] { "ReporterId", "DiscussionId" },
                unique: true,
                filter: "\"Status\" = 'Pending' AND \"DiscussionId\" IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CommunityReports_ExactlyOneTarget",
                schema: "central",
                table: "CommunityReports",
                sql: "(\"DiscussionId\" IS NULL) <> (\"CommentId\" IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CommunityReports_ReporterId_CommentId",
                schema: "central",
                table: "CommunityReports");

            migrationBuilder.DropIndex(
                name: "IX_CommunityReports_ReporterId_DiscussionId",
                schema: "central",
                table: "CommunityReports");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CommunityReports_ExactlyOneTarget",
                schema: "central",
                table: "CommunityReports");

            migrationBuilder.DropColumn(
                name: "ActorDisplayName",
                schema: "central",
                table: "CommunityModerationActions");
        }
    }
}
