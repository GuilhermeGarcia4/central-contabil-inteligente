namespace CentralContabil.Api.Modules.Community.Domain;

public sealed class CommunityDiscussion
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AuthorId { get; set; }
    public required string AuthorDisplayName { get; set; }
    public required string AuthorPublicRole { get; set; }
    public required string Title { get; set; }
    public required string Content { get; set; }
    public required string Category { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EditedAt { get; set; }
    public bool IsRemoved { get; set; }
    public DateTimeOffset? RemovedAt { get; set; }
    public Guid? RemovedById { get; set; }
}

public sealed class CommunityComment
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DiscussionId { get; set; }
    public Guid AuthorId { get; set; }
    public required string AuthorDisplayName { get; set; }
    public required string AuthorPublicRole { get; set; }
    public required string Content { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? EditedAt { get; set; }
    public bool IsRemoved { get; set; }
    public DateTimeOffset? RemovedAt { get; set; }
    public Guid? RemovedById { get; set; }
}

public sealed class CommunityReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ReporterId { get; set; }
    public Guid? DiscussionId { get; set; }
    public Guid? CommentId { get; set; }
    public required string Reason { get; set; }
    public string Details { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? ReviewedAt { get; set; }
    public Guid? ReviewedById { get; set; }
}

public sealed class CommunityUsefulReaction
{
    public Guid DiscussionId { get; set; }
    public Guid UserId { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CommunityModerationAction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ActorId { get; set; }
    public required string ActorDisplayName { get; set; }
    public required string TargetType { get; set; }
    public Guid TargetId { get; set; }
    public required string Action { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public static class CommunityCategories
{
    public static readonly IReadOnlyList<string> All =
    ["Geral", "Contabilidade", "Tributos", "MEI", "Empreendedorismo", "Dúvidas"];
}
