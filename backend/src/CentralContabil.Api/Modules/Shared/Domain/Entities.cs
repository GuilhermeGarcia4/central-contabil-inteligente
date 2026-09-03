using Microsoft.AspNetCore.Identity;

namespace CentralContabil.Api.Modules.Shared.Domain;

public abstract class Entity { public Guid Id { get; set; } = Guid.NewGuid(); }
public abstract class AuditableEntity : Entity
{
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public enum ArticleStatus { Draft, Review, Published, NeedsReview, Archived }
public enum ReviewStatus { Pending, Reviewed, RequiresChanges }
public enum SourceType { Law, Decree, OfficialPortal, NormativeInstruction, GovernmentPublication, Other }
public enum ParameterValueType { Decimal, Integer, Boolean, Text, Json }

public sealed class Category : AuditableEntity
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<Article> Articles { get; set; } = [];
}

public sealed class Article : AuditableEntity
{
    public required string Title { get; set; }
    public required string Slug { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string SimpleContent { get; set; } = string.Empty;
    public string TechnicalContent { get; set; } = string.Empty;
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public ArticleStatus Status { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public DateTimeOffset? LastReviewedAt { get; set; }
    public DateTimeOffset? NeedsReviewAt { get; set; }
    public ReviewStatus ReviewStatus { get; set; }
    public string Jurisdiction { get; set; } = "BR";
    public int ContentVersion { get; set; } = 1;
    public ICollection<ArticleVersion> Versions { get; set; } = [];
    public ICollection<ArticleSource> ArticleSources { get; set; } = [];
    public ICollection<ArticleLegalReference> LegalReferences { get; set; } = [];
}

public sealed class ArticleVersion : Entity
{
    public Guid ArticleId { get; set; }
    public Article Article { get; set; } = null!;
    public int Version { get; set; }
    public required string Title { get; set; }
    public string SimpleContent { get; set; } = string.Empty;
    public string TechnicalContent { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public Guid CreatedBy { get; set; }
    public string ChangeDescription { get; set; } = string.Empty;
}

public sealed class Source : Entity
{
    public required string Name { get; set; }
    public required string Url { get; set; }
    public SourceType SourceType { get; set; }
    public string Publisher { get; set; } = string.Empty;
    public DateTimeOffset? PublishedAt { get; set; }
    public DateTimeOffset RetrievedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastVerifiedAt { get; set; }
    public bool IsOfficial { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class ArticleSource
{
    public Guid ArticleId { get; set; }
    public Article Article { get; set; } = null!;
    public Guid SourceId { get; set; }
    public Source Source { get; set; } = null!;
}

public sealed class LegalReference : Entity
{
    public required string Title { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string ArticleNumber { get; set; } = string.Empty;
    public string Jurisdiction { get; set; } = "BR";
    public required string SourceUrl { get; set; }
    public DateOnly? ValidFrom { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public string Description { get; set; } = string.Empty;
}

public sealed class ArticleLegalReference
{
    public Guid ArticleId { get; set; }
    public Article Article { get; set; } = null!;
    public Guid LegalReferenceId { get; set; }
    public LegalReference LegalReference { get; set; } = null!;
}

public sealed class Calculator : AuditableEntity
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<CalculationRuleSet> RuleSets { get; set; } = [];
}

public sealed class CalculationRuleSet : Entity
{
    public Guid CalculatorId { get; set; }
    public Calculator Calculator { get; set; } = null!;
    public required string Name { get; set; }
    public required string Version { get; set; }
    public DateOnly ValidFrom { get; set; }
    public DateOnly? ValidUntil { get; set; }
    public Guid? SourceId { get; set; }
    public Source? Source { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public ICollection<RuleParameter> Parameters { get; set; } = [];
}

public sealed class RuleParameter : Entity
{
    public Guid RuleSetId { get; set; }
    public CalculationRuleSet RuleSet { get; set; } = null!;
    public required string Key { get; set; }
    public required string Value { get; set; }
    public ParameterValueType ValueType { get; set; }
    public string Description { get; set; } = string.Empty;
}

public sealed class CalculationRun : Entity
{
    public Guid CalculatorId { get; set; }
    public Calculator Calculator { get; set; } = null!;
    public Guid? UserId { get; set; }
    public Guid RuleSetId { get; set; }
    public CalculationRuleSet RuleSet { get; set; } = null!;
    public required string InputJson { get; set; }
    public required string ResultJson { get; set; }
    public decimal ResultAmount { get; set; }
    public DateTimeOffset CalculatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class Favorite : Entity
{
    public Guid UserId { get; set; }
    public Guid ArticleId { get; set; }
    public Article Article { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class PrivacyConsent : Entity
{
    public Guid UserId { get; set; }
    public required string ConsentType { get; set; }
    public required string TermsVersion { get; set; }
    public DateTimeOffset AcceptedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? RevokedAt { get; set; }
}

public sealed class AuditLog : Entity
{
    public Guid? UserId { get; set; }
    public required string Action { get; set; }
    public required string EntityType { get; set; }
    public string EntityId { get; set; } = string.Empty;
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class RefreshToken : Entity
{
    public Guid UserId { get; set; }
    public required string TokenHash { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}
