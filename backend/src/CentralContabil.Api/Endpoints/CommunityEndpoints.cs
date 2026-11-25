using System.Security.Claims;
using CentralContabil.Api.Modules.Community.Application;
namespace CentralContabil.Api.Endpoints;

public static class CommunityEndpoints
{
    public static IEndpointRouteBuilder MapCommunityApi(this IEndpointRouteBuilder app)
    {
        var community = app.MapGroup("/api/v1/community").WithTags("Comunidade");

        community.MapGet("", async (string? q, string? category, int? page, int? pageSize, ClaimsPrincipal user, CommunityService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.ListAsync(q, category, page ?? 1, pageSize ?? 20, ViewerId(user), ct)); }
            catch (ArgumentException ex) { return Results.BadRequest(new { title = ex.Message }); }
        }).AllowAnonymous().RequireRateLimiting("public");

        community.MapGet("/{id:guid}", async (Guid id, ClaimsPrincipal user, CommunityService service, CancellationToken ct) =>
        {
            var item = await service.GetAsync(id, ViewerId(user), ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).AllowAnonymous().RequireRateLimiting("public");

        community.MapPost("", async (CreateDiscussionRequest body, ClaimsPrincipal user, CommunityService service, CancellationToken ct) =>
        {
            try
            {
                var item = await service.CreateAsync(user, body.Title, body.Content, body.Category, ct);
                return Results.Created($"/api/v1/community/{item.Id}", item);
            }
            catch (ArgumentException ex) { return Results.BadRequest(new { title = ex.Message }); }
        }).RequireAuthorization().RequireRateLimiting("community-write");

        community.MapPut("/{id:guid}", async (Guid id, CreateDiscussionRequest body, ClaimsPrincipal user, CommunityService service, CancellationToken ct) =>
        {
            try
            {
                var item = await service.UpdateAsync(id, user, body.Title, body.Content, body.Category, ct);
                return item is null ? Results.NotFound() : Results.Ok(item);
            }
            catch (ArgumentException ex) { return Results.BadRequest(new { title = ex.Message }); }
        }).RequireAuthorization().RequireRateLimiting("community-write");

        community.MapDelete("/{id:guid}", async (Guid id, ClaimsPrincipal user, CommunityService service, CancellationToken ct) =>
            await service.RemoveOwnDiscussionAsync(id, user, ct) ? Results.NoContent() : Results.NotFound())
            .RequireAuthorization().RequireRateLimiting("community-write");

        community.MapPost("/{id:guid}/comments", async (Guid id, CommentRequest body, ClaimsPrincipal user, CommunityService service, CancellationToken ct) =>
        {
            try { return Results.Created($"/api/v1/community/{id}", await service.AddCommentAsync(id, user, body.Content, ct)); }
            catch (ArgumentException ex) { return Results.BadRequest(new { title = ex.Message }); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
        }).RequireAuthorization().RequireRateLimiting("community-write");

        community.MapPut("/comments/{id:guid}", async (Guid id, CommentRequest body, ClaimsPrincipal user, CommunityService service, CancellationToken ct) =>
        {
            try
            {
                var item = await service.UpdateCommentAsync(id, user, body.Content, ct);
                return item is null ? Results.NotFound() : Results.Ok(item);
            }
            catch (ArgumentException ex) { return Results.BadRequest(new { title = ex.Message }); }
        }).RequireAuthorization().RequireRateLimiting("community-write");

        community.MapDelete("/comments/{id:guid}", async (Guid id, ClaimsPrincipal user, CommunityService service, CancellationToken ct) =>
            await service.RemoveOwnCommentAsync(id, user, ct) ? Results.NoContent() : Results.NotFound())
            .RequireAuthorization().RequireRateLimiting("community-write");

        community.MapPost("/{id:guid}/useful", async (Guid id, ClaimsPrincipal user, CommunityService service, CancellationToken ct) =>
        {
            var isUseful = await service.ToggleUsefulAsync(id, user, ct);
            return isUseful is null ? Results.NotFound() : Results.Ok(new { isUseful = isUseful.Value });
        })
            .RequireAuthorization().RequireRateLimiting("community-write");

        community.MapPost("/{id:guid}/reports", async (Guid id, ReportRequest body, ClaimsPrincipal user, CommunityService service, CancellationToken ct) =>
        {
            try { return Results.Accepted(value: new { id = await service.ReportDiscussionAsync(id, user, body.Reason, body.Details, ct), status = "Pending" }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { title = ex.Message }); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { title = ex.Message }); }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException) { return Results.Conflict(new { title = "Você já denunciou este conteúdo; a equipe irá analisá-lo." }); }
        }).RequireAuthorization().RequireRateLimiting("community-report");

        community.MapPost("/comments/{id:guid}/reports", async (Guid id, ReportRequest body, ClaimsPrincipal user, CommunityService service, CancellationToken ct) =>
        {
            try { return Results.Accepted(value: new { id = await service.ReportCommentAsync(id, user, body.Reason, body.Details, ct), status = "Pending" }); }
            catch (ArgumentException ex) { return Results.BadRequest(new { title = ex.Message }); }
            catch (KeyNotFoundException) { return Results.NotFound(); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { title = ex.Message }); }
            catch (Microsoft.EntityFrameworkCore.DbUpdateException) { return Results.Conflict(new { title = "Você já denunciou este conteúdo; a equipe irá analisá-lo." }); }
        }).RequireAuthorization().RequireRateLimiting("community-report");

        var moderation = community.MapGroup("/moderation").RequireAuthorization("CommunityModerators");
        community.MapPut("/moderators", async (ModeratorAssignmentRequest body, CommunityService service, CancellationToken ct) =>
        {
            try
            {
                var assignment = await service.SetModeratorAsync(body.Email, body.Enabled);
                return assignment is null ? Results.NotFound() : Results.Ok(assignment);
            }
            catch (ArgumentException ex) { return Results.BadRequest(new { title = ex.Message }); }
            catch (InvalidOperationException ex) { return Results.Conflict(new { title = ex.Message }); }
        }).RequireAuthorization("AdminOnly").RequireRateLimiting("community-write");
        moderation.MapGet("/reports", async (CommunityService service, CancellationToken ct) => Results.Ok(await service.PendingReportsAsync(ct)))
            .RequireRateLimiting("public");
        moderation.MapGet("/actions", async (CommunityService service, CancellationToken ct) => Results.Ok(await service.RecentModerationActionsAsync(ct)))
            .RequireRateLimiting("public");
        moderation.MapPost("/reports/{id:guid}/review", async (Guid id, CommunityReviewRequest body, ClaimsPrincipal user, CommunityService service, CancellationToken ct) =>
        {
            try { return await service.ReviewReportAsync(id, user, body, ct) ? Results.NoContent() : Results.NotFound(); }
            catch (ArgumentException ex) { return Results.BadRequest(new { title = ex.Message }); }
        }).RequireRateLimiting("community-write");
        moderation.MapPost("/{targetType}/{id:guid}/{action}", async (string targetType, Guid id, string action, ModerationRequest body, ClaimsPrincipal user, CommunityService service, CancellationToken ct) =>
        {
            try { return await service.ModerateAsync(id, targetType, action, user, body.Reason, ct) ? Results.NoContent() : Results.NotFound(); }
            catch (ArgumentException ex) { return Results.BadRequest(new { title = ex.Message }); }
        }).RequireRateLimiting("community-write");
        return app;
    }

    private static Guid? ViewerId(ClaimsPrincipal user) => Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;
}

public sealed record CreateDiscussionRequest(string? Title, string? Content, string? Category);
public sealed record CommentRequest(string? Content);
public sealed record ReportRequest(string? Reason, string? Details);
public sealed record ModerationRequest(string? Reason);
public sealed record ModeratorAssignmentRequest(string? Email, bool Enabled);
