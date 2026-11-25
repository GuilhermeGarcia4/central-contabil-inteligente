using System.Security.Claims;
using System.Text.RegularExpressions;
using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Community.Domain;
using CentralContabil.Api.Modules.Shared.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CentralContabil.Api.Modules.Community.Application;

public sealed record CommunityPage(IReadOnlyList<CommunityDiscussionView> Items, int Total, int Page, int PageSize);
public sealed record CommunityDiscussionView(Guid Id, string Title, string Content, string Category, string AuthorDisplayName, string AuthorRole, DateTimeOffset CreatedAt, DateTimeOffset? EditedAt, int CommentCount, int UsefulCount, bool IsUseful, bool IsMine, bool IsRemoved);
public sealed record CommunityCommentView(Guid Id, string Content, string AuthorDisplayName, string AuthorRole, DateTimeOffset CreatedAt, DateTimeOffset? EditedAt, bool IsMine, bool IsRemoved);
public sealed record CommunityDetail(CommunityDiscussionView Discussion, IReadOnlyList<CommunityCommentView> Comments);
public sealed record CommunityReportView(Guid Id, Guid? DiscussionId, Guid? CommentId, string Reason, string Details, string Status, DateTimeOffset CreatedAt, string TargetType, string TargetTitle, string TargetContent, string TargetAuthor);
public sealed record CommunityModerationActionView(Guid Id, string ActorDisplayName, string TargetType, Guid TargetId, string Action, string Reason, DateTimeOffset CreatedAt);
public sealed record CommunityReviewRequest(string Decision, string? Reason);
public sealed record ModeratorAssignment(string DisplayName, bool IsModerator);

public sealed class CommunityService(AppDbContext db, UserManager<ApplicationUser> users)
{
    private static readonly Regex HtmlTag = new("<\\s*/?\\s*[a-zA-Z][^>]*>", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex Cpf = new("(?<!\\d)\\d{3}\\.?\\d{3}\\.?\\d{3}-?\\d{2}(?!\\d)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex LongNumber = new("(?<!\\d)(?:\\d[ -]?){13,19}(?!\\d)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex EmailAddress = new("\\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\\.[A-Z]{2,}\\b", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex PhoneNumber = new("(?<!\\d)(?:\\+?55\\s*)?\\(?\\d{2}\\)?[\\s.-]*9?\\d{4}[\\s.-]*\\d{4}(?!\\d)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly string[] ReportReasons = ["Spam", "Assédio", "Dados pessoais", "Conteúdo ilegal", "Desinformação", "Outro"];
    private const int MaxPageSize = 30;

    public async Task<CommunityPage> ListAsync(string? query, string? category, int page, int pageSize, Guid? viewerId, CancellationToken ct)
    {
        page = Math.Clamp(page, 1, 100_000);
        pageSize = Math.Clamp(pageSize, 1, MaxPageSize);
        query = string.IsNullOrWhiteSpace(query) ? null : query.Trim()[..Math.Min(query.Trim().Length, 100)];
        var source = db.CommunityDiscussions.AsNoTracking().Where(x => !x.IsRemoved);
        if (!string.IsNullOrWhiteSpace(category))
        {
            if (!CommunityCategories.All.Contains(category, StringComparer.OrdinalIgnoreCase)) throw new ArgumentException("Tema inválido.");
            var canonical = CommunityCategories.All.First(x => string.Equals(x, category, StringComparison.OrdinalIgnoreCase));
            source = source.Where(x => x.Category == canonical);
        }
        if (query is not null) source = source.Where(x => x.Title.Contains(query) || x.Content.Contains(query));
        var total = await source.CountAsync(ct);
        var rows = await source.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        var ids = rows.Select(x => x.Id).ToArray();
        var commentCounts = await db.CommunityComments.AsNoTracking().Where(x => ids.Contains(x.DiscussionId) && !x.IsRemoved)
            .GroupBy(x => x.DiscussionId).Select(group => new { Id = group.Key, Count = group.Count() }).ToDictionaryAsync(x => x.Id, x => x.Count, ct);
        var usefulCounts = await db.CommunityUsefulReactions.AsNoTracking().Where(x => ids.Contains(x.DiscussionId))
            .GroupBy(x => x.DiscussionId).Select(group => new { Id = group.Key, Count = group.Count() }).ToDictionaryAsync(x => x.Id, x => x.Count, ct);
        var reactedIds = viewerId.HasValue
            ? await db.CommunityUsefulReactions.AsNoTracking().Where(x => ids.Contains(x.DiscussionId) && x.UserId == viewerId).Select(x => x.DiscussionId).ToHashSetAsync(ct)
            : [];
        return new(rows.Select(x => ToView(x, viewerId, commentCounts.GetValueOrDefault(x.Id), reactedIds.Contains(x.Id), usefulCounts.GetValueOrDefault(x.Id))).ToList(), total, page, pageSize);
    }

    public async Task<CommunityDetail?> GetAsync(Guid id, Guid? viewerId, CancellationToken ct)
    {
        var discussion = await db.CommunityDiscussions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && !x.IsRemoved, ct);
        if (discussion is null) return null;
        var comments = await db.CommunityComments.AsNoTracking().Where(x => x.DiscussionId == id).OrderBy(x => x.CreatedAt).ToListAsync(ct);
        var usefulCount = await db.CommunityUsefulReactions.CountAsync(x => x.DiscussionId == id, ct);
        var isUseful = viewerId.HasValue && await db.CommunityUsefulReactions.AnyAsync(x => x.DiscussionId == id && x.UserId == viewerId, ct);
        var view = ToView(discussion, viewerId, comments.Count(x => !x.IsRemoved), isUseful, usefulCount);
        return new(view, comments.Select(x => new CommunityCommentView(x.Id, x.IsRemoved ? "Conteúdo removido." : x.Content, x.AuthorDisplayName, x.AuthorPublicRole, x.CreatedAt, x.EditedAt, viewerId == x.AuthorId, x.IsRemoved)).ToList());
    }

    public async Task<CommunityDiscussionView> CreateAsync(ClaimsPrincipal principal, string? title, string? content, string? category, CancellationToken ct)
    {
        var user = await RequireUserAsync(principal);
        ValidateText(title, 140, "Informe um título de até 140 caracteres.");
        ValidateText(content, 5000, "Informe um texto de até 5.000 caracteres.");
        ValidateCategory(category);
        var (name, role) = await PublicProfileAsync(user);
        var discussion = new CommunityDiscussion { AuthorId = user.Id, AuthorDisplayName = name, AuthorPublicRole = role, Title = title!.Trim(), Content = content!.Trim(), Category = category!.Trim() };
        db.CommunityDiscussions.Add(discussion);
        await db.SaveChangesAsync(ct);
        return ToView(discussion, user.Id, 0, false);
    }

    public async Task<CommunityDiscussionView?> UpdateAsync(Guid id, ClaimsPrincipal principal, string? title, string? content, string? category, CancellationToken ct)
    {
        var user = await RequireUserAsync(principal);
        ValidateText(title, 140, "Informe um título de até 140 caracteres.");
        ValidateText(content, 5000, "Informe um texto de até 5.000 caracteres.");
        ValidateCategory(category);
        var item = await db.CommunityDiscussions.SingleOrDefaultAsync(x => x.Id == id && x.AuthorId == user.Id && !x.IsRemoved, ct);
        if (item is null) return null;
        item.Title = title!.Trim(); item.Content = content!.Trim(); item.Category = category!.Trim(); item.EditedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToView(item, user.Id, 0, false);
    }

    public async Task<bool> RemoveOwnDiscussionAsync(Guid id, ClaimsPrincipal principal, CancellationToken ct)
    {
        var user = await RequireUserAsync(principal);
        var item = await db.CommunityDiscussions.SingleOrDefaultAsync(x => x.Id == id && x.AuthorId == user.Id && !x.IsRemoved, ct);
        if (item is null) return false;
        item.IsRemoved = true; item.RemovedAt = DateTimeOffset.UtcNow; item.RemovedById = user.Id;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<CommunityCommentView> AddCommentAsync(Guid id, ClaimsPrincipal principal, string? content, CancellationToken ct)
    {
        var user = await RequireUserAsync(principal);
        ValidateText(content, 3000, "Informe uma resposta de até 3.000 caracteres.");
        if (!await db.CommunityDiscussions.AnyAsync(x => x.Id == id && !x.IsRemoved, ct)) throw new KeyNotFoundException("Discussão não encontrada.");
        var (name, role) = await PublicProfileAsync(user);
        var comment = new CommunityComment { DiscussionId = id, AuthorId = user.Id, AuthorDisplayName = name, AuthorPublicRole = role, Content = content!.Trim() };
        db.CommunityComments.Add(comment);
        await db.SaveChangesAsync(ct);
        return ToView(comment, user.Id);
    }

    public async Task<CommunityCommentView?> UpdateCommentAsync(Guid id, ClaimsPrincipal principal, string? content, CancellationToken ct)
    {
        var user = await RequireUserAsync(principal);
        ValidateText(content, 3000, "Informe uma resposta de até 3.000 caracteres.");
        var item = await db.CommunityComments.SingleOrDefaultAsync(x => x.Id == id && x.AuthorId == user.Id && !x.IsRemoved, ct);
        if (item is null || !await db.CommunityDiscussions.AnyAsync(x => x.Id == item.DiscussionId && !x.IsRemoved, ct)) return null;
        item.Content = content!.Trim(); item.EditedAt = DateTimeOffset.UtcNow;
        await db.SaveChangesAsync(ct);
        return ToView(item, user.Id);
    }

    public async Task<bool> RemoveOwnCommentAsync(Guid id, ClaimsPrincipal principal, CancellationToken ct)
    {
        var user = await RequireUserAsync(principal);
        var item = await db.CommunityComments.SingleOrDefaultAsync(x => x.Id == id && x.AuthorId == user.Id && !x.IsRemoved, ct);
        if (item is null) return false;
        item.IsRemoved = true; item.RemovedAt = DateTimeOffset.UtcNow; item.RemovedById = user.Id;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool?> ToggleUsefulAsync(Guid id, ClaimsPrincipal principal, CancellationToken ct)
    {
        var user = await RequireUserAsync(principal);
        if (!await db.CommunityDiscussions.AnyAsync(x => x.Id == id && !x.IsRemoved, ct)) return null;
        var reaction = await db.CommunityUsefulReactions.FindAsync([id, user.Id], ct);
        if (reaction is null) db.CommunityUsefulReactions.Add(new CommunityUsefulReaction { DiscussionId = id, UserId = user.Id });
        else db.CommunityUsefulReactions.Remove(reaction);
        await db.SaveChangesAsync(ct);
        return reaction is null;
    }

    public async Task<Guid> ReportDiscussionAsync(Guid id, ClaimsPrincipal principal, string? reason, string? details, CancellationToken ct)
    {
        var user = await RequireUserAsync(principal);
        if (!await db.CommunityDiscussions.AnyAsync(x => x.Id == id && !x.IsRemoved, ct)) throw new KeyNotFoundException("Publicação não encontrada.");
        return await AddReportAsync(user.Id, id, null, reason, details, ct);
    }

    public async Task<Guid> ReportCommentAsync(Guid id, ClaimsPrincipal principal, string? reason, string? details, CancellationToken ct)
    {
        var user = await RequireUserAsync(principal);
        if (!await db.CommunityComments.AnyAsync(x => x.Id == id && !x.IsRemoved, ct)) throw new KeyNotFoundException("Comentário não encontrado.");
        return await AddReportAsync(user.Id, null, id, reason, details, ct);
    }

    public async Task<IReadOnlyList<CommunityReportView>> PendingReportsAsync(CancellationToken ct)
    {
        var reports = await db.CommunityReports.AsNoTracking().Where(x => x.Status == "Pending").OrderBy(x => x.CreatedAt).Take(100).ToListAsync(ct);
        var result = new List<CommunityReportView>(reports.Count);
        foreach (var report in reports)
        {
            if (report.DiscussionId is Guid discussionId)
            {
                var target = await db.CommunityDiscussions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == discussionId, ct);
                result.Add(new(report.Id, discussionId, null, report.Reason, report.Details, report.Status, report.CreatedAt, "Discussão", target?.Title ?? "Discussão indisponível", target?.IsRemoved == true ? "Conteúdo removido." : target?.Content ?? "Conteúdo indisponível", target?.AuthorDisplayName ?? "Conta indisponível"));
            }
            else if (report.CommentId is Guid commentId)
            {
                var target = await db.CommunityComments.AsNoTracking().SingleOrDefaultAsync(x => x.Id == commentId, ct);
                result.Add(new(report.Id, null, commentId, report.Reason, report.Details, report.Status, report.CreatedAt, "Resposta", "Resposta", target?.IsRemoved == true ? "Conteúdo removido." : target?.Content ?? "Conteúdo indisponível", target?.AuthorDisplayName ?? "Conta indisponível"));
            }
        }
        return result;
    }

    public async Task<IReadOnlyList<CommunityModerationActionView>> RecentModerationActionsAsync(CancellationToken ct) =>
        await db.CommunityModerationActions.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(200)
            .Select(x => new CommunityModerationActionView(x.Id, x.ActorDisplayName, x.TargetType, x.TargetId, x.Action, x.Reason, x.CreatedAt)).ToListAsync(ct);

    public async Task<ModeratorAssignment?> SetModeratorAsync(string? email, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(email) || email.Length > 254) throw new ArgumentException("Informe o e-mail da conta existente.");
        var user = await users.FindByEmailAsync(email.Trim());
        if (user is null) return null;
        if (await users.IsInRoleAsync(user, ApplicationRoles.Admin)) throw new InvalidOperationException("Administradores já possuem acesso de moderação.");
        var hasRole = await users.IsInRoleAsync(user, ApplicationRoles.Moderator);
        if (enabled == hasRole) return new(user.DisplayName, hasRole);
        var result = enabled ? await users.AddToRoleAsync(user, ApplicationRoles.Moderator) : await users.RemoveFromRoleAsync(user, ApplicationRoles.Moderator);
        if (!result.Succeeded) throw new InvalidOperationException("Não foi possível atualizar o papel de moderação.");
        return new(user.DisplayName, enabled);
    }

    public async Task<bool> ReviewReportAsync(Guid id, ClaimsPrincipal principal, CommunityReviewRequest request, CancellationToken ct)
    {
        var actor = await RequireUserAsync(principal);
        if (request.Decision is not ("Dismiss" or "Hide")) throw new ArgumentException("A decisão deve ser Dismiss ou Hide.");
        if (request.Reason?.Length > 500) throw new ArgumentException("A justificativa deve ter até 500 caracteres.");
        if (ContainsSensitiveInfo(request.Reason)) throw new ArgumentException("Não inclua dados pessoais na justificativa.");
        var report = await db.CommunityReports.SingleOrDefaultAsync(x => x.Id == id && x.Status == "Pending", ct);
        if (report is null) return false;
        report.Status = request.Decision == "Hide" ? "Actioned" : "Dismissed";
        report.ReviewedAt = DateTimeOffset.UtcNow; report.ReviewedById = actor.Id;
        var reason = request.Reason?.Trim() ?? string.Empty;
        if (request.Decision == "Hide") await HideTargetAsync(report.DiscussionId, report.CommentId, actor.Id, reason, ct);
        db.CommunityModerationActions.Add(new CommunityModerationAction { ActorId = actor.Id, ActorDisplayName = PublicDisplayName(actor.DisplayName), TargetType = "Report", TargetId = id, Action = report.Status, Reason = reason });
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> ModerateAsync(Guid id, string targetType, string action, ClaimsPrincipal principal, string? reason, CancellationToken ct)
    {
        var actor = await RequireUserAsync(principal);
        if (targetType is not ("discussion" or "comment") || action is not ("hide" or "restore")) throw new ArgumentException("Ação de moderação inválida.");
        if (reason?.Length > 500) throw new ArgumentException("A justificativa deve ter até 500 caracteres.");
        if (ContainsSensitiveInfo(reason)) throw new ArgumentException("Não inclua dados pessoais na justificativa.");
        var removed = action == "hide";
        if (targetType == "discussion")
        {
            var item = await db.CommunityDiscussions.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (item is null) return false;
            item.IsRemoved = removed; item.RemovedAt = removed ? DateTimeOffset.UtcNow : null; item.RemovedById = removed ? actor.Id : null;
        }
        else if (targetType == "comment")
        {
            var item = await db.CommunityComments.SingleOrDefaultAsync(x => x.Id == id, ct);
            if (item is null) return false;
            item.IsRemoved = removed; item.RemovedAt = removed ? DateTimeOffset.UtcNow : null; item.RemovedById = removed ? actor.Id : null;
        }
        else return false;
        db.CommunityModerationActions.Add(new CommunityModerationAction { ActorId = actor.Id, ActorDisplayName = PublicDisplayName(actor.DisplayName), TargetType = targetType, TargetId = id, Action = removed ? "Hide" : "Restore", Reason = reason?.Trim() ?? string.Empty });
        await db.SaveChangesAsync(ct);
        return true;
    }

    public static bool IsValidReason(string? reason) => !string.IsNullOrWhiteSpace(reason) && ReportReasons.Contains(reason.Trim(), StringComparer.OrdinalIgnoreCase);

    private async Task<Guid> AddReportAsync(Guid reporterId, Guid? discussionId, Guid? commentId, string? reason, string? details, CancellationToken ct)
    {
        if (!IsValidReason(reason)) throw new ArgumentException("Selecione um motivo válido para a denúncia.");
        if (details?.Length > 500) throw new ArgumentException("Os detalhes devem ter até 500 caracteres.");
        if (ContainsSensitiveInfo(details))
            throw new ArgumentException("Não inclua dados pessoais nos detalhes da denúncia.");
        var duplicate = await db.CommunityReports.AnyAsync(x => x.ReporterId == reporterId && x.Status == "Pending" && x.DiscussionId == discussionId && x.CommentId == commentId, ct);
        if (duplicate) throw new InvalidOperationException("Você já denunciou este conteúdo; a equipe irá analisá-lo.");
        var report = new CommunityReport { ReporterId = reporterId, DiscussionId = discussionId, CommentId = commentId, Reason = reason!.Trim(), Details = details?.Trim() ?? string.Empty };
        db.CommunityReports.Add(report);
        await db.SaveChangesAsync(ct);
        return report.Id;
    }

    private async Task HideTargetAsync(Guid? discussionId, Guid? commentId, Guid actorId, string reason, CancellationToken ct)
    {
        if (discussionId is Guid discussionKey)
        {
            var item = await db.CommunityDiscussions.SingleOrDefaultAsync(x => x.Id == discussionKey, ct);
            if (item is not null) { item.IsRemoved = true; item.RemovedAt = DateTimeOffset.UtcNow; item.RemovedById = actorId; }
            var actor = await users.FindByIdAsync(actorId.ToString());
            db.CommunityModerationActions.Add(new CommunityModerationAction { ActorId = actorId, ActorDisplayName = PublicDisplayName(actor?.DisplayName), TargetType = "Discussion", TargetId = discussionKey, Action = "Hide", Reason = reason });
        }
        else if (commentId is Guid commentKey)
        {
            var item = await db.CommunityComments.SingleOrDefaultAsync(x => x.Id == commentKey, ct);
            if (item is not null) { item.IsRemoved = true; item.RemovedAt = DateTimeOffset.UtcNow; item.RemovedById = actorId; }
            var actor = await users.FindByIdAsync(actorId.ToString());
            db.CommunityModerationActions.Add(new CommunityModerationAction { ActorId = actorId, ActorDisplayName = PublicDisplayName(actor?.DisplayName), TargetType = "Comment", TargetId = commentKey, Action = "Hide", Reason = reason });
        }
    }

    private async Task<ApplicationUser> RequireUserAsync(ClaimsPrincipal principal) =>
        await users.GetUserAsync(principal) ?? throw new InvalidOperationException("Faça login para continuar.");

    private async Task<(string Name, string Role)> PublicProfileAsync(ApplicationUser user)
    {
        // Name and staff role are the only profile details persisted for public display.
        var roles = await users.GetRolesAsync(user);
        var publicRole = roles.Contains(ApplicationRoles.Admin, StringComparer.OrdinalIgnoreCase) ? "Admin"
            : roles.Contains(ApplicationRoles.Moderator, StringComparer.OrdinalIgnoreCase) ? "Moderador" : "Membro";
        return (PublicDisplayName(user.DisplayName), publicRole);
    }

    private static string PublicDisplayName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || ContainsSensitiveInfo(value)) return "Membro";
        var displayName = value.Trim();
        return displayName[..Math.Min(displayName.Length, 80)];
    }

    private static bool ContainsSensitiveInfo(string? value) =>
        !string.IsNullOrWhiteSpace(value) && (Cpf.IsMatch(value) || LongNumber.IsMatch(value) || EmailAddress.IsMatch(value) || PhoneNumber.IsMatch(value));

    private static void ValidateCategory(string? category)
    {
        if (string.IsNullOrWhiteSpace(category) || !CommunityCategories.All.Contains(category.Trim(), StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Selecione um tema válido.");
    }

    private static void ValidateText(string? value, int maxLength, string message)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength || value.Any(c => char.IsControl(c) && c is not ('\n' or '\r' or '\t')) || HtmlTag.IsMatch(value)) throw new ArgumentException(message);
        if (ContainsSensitiveInfo(value)) throw new ArgumentException("Não publique CPF, e-mail, telefone, senhas ou dados bancários. Remova dados pessoais antes de enviar.");
    }

    private static CommunityDiscussionView ToView(CommunityDiscussion item, Guid? viewerId, int commentCount, bool isUseful, int usefulCount = 0) =>
        new(item.Id, item.Title, item.IsRemoved ? "Conteúdo removido." : item.Content, item.Category, item.AuthorDisplayName, item.AuthorPublicRole, item.CreatedAt, item.EditedAt, commentCount, usefulCount, isUseful, viewerId == item.AuthorId, item.IsRemoved);

    private static CommunityCommentView ToView(CommunityComment item, Guid? viewerId) =>
        new(item.Id, item.IsRemoved ? "Conteúdo removido." : item.Content, item.AuthorDisplayName, item.AuthorPublicRole, item.CreatedAt, item.EditedAt, viewerId == item.AuthorId, item.IsRemoved);
}
