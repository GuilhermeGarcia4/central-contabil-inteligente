namespace CentralContabil.Api.Modules.AI.Domain;

using CentralContabil.Api.Modules.Shared.Domain;

public enum AssistantIntent { Information, Calculation, LegalQuestion, TaxQuestion, LaborQuestion, MeiQuestion, FinancialQuestion, Navigation, OutOfScope }
public enum AIRequestStatus { Completed, NoSource, Failed, RateLimited }
public enum UnansweredQuestionStatus { Open, ContentPlanned, Resolved }

public sealed class AIRequest
{
    public Guid Id { get; set; } = Guid.NewGuid(); public Guid? UserId { get; set; }
    public string Provider { get; set; } = "local-grounded"; public string Model { get; set; } = "grounded-template-v1";
    public int? InputTokens { get; set; } public int? OutputTokens { get; set; } public long DurationMs { get; set; }
    public AIRequestStatus Status { get; set; } public string Intent { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class UnansweredQuestion
{
    public Guid Id { get; set; } = Guid.NewGuid(); public required string NormalizedQuestion { get; set; }
    public int Count { get; set; } = 1; public DateTimeOffset FirstAskedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastAskedAt { get; set; } = DateTimeOffset.UtcNow; public UnansweredQuestionStatus Status { get; set; }
}

public sealed record AssistantSource(string Title, string? Url, bool IsOfficial, DateTimeOffset? LastVerifiedAt);
public sealed record RelatedItem(string Title, string Slug);
public sealed record AssistantAnswer(string Answer, IReadOnlyList<AssistantSource> Sources, string Confidence,
    bool RequiresProfessionalReview, IReadOnlyList<RelatedItem> RelatedArticles, IReadOnlyList<RelatedItem> SuggestedCalculators,
    object? Calculation = null, string? Disclaimer = null);

public sealed class AssistantConversation : AuditableEntity
{
    public Guid UserId { get; set; }
    public string Title { get; set; } = "Nova conversa";
    public ICollection<AssistantMessage> Messages { get; set; } = [];
}

public sealed class AssistantMessage : Entity
{
    public Guid ConversationId { get; set; }
    public AssistantConversation Conversation { get; set; } = null!;
    public string Role { get; set; } = "user";
    public string Content { get; set; } = string.Empty;
    public string? SourcesJson { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
