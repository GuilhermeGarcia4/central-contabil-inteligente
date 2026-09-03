using CentralContabil.Api.Modules.Shared.Domain;

namespace CentralContabil.Api.Modules.Integrations.Domain;

public sealed class ExternalProvider : AuditableEntity
{
    public required string Name { get; set; }
    public string BaseUrl { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public DateTimeOffset? LastSuccessAt { get; set; }
    public DateTimeOffset? LastFailureAt { get; set; }
}

public sealed class ExternalEntitySnapshot : Entity
{
    public required string Provider { get; set; }
    public required string EntityType { get; set; }
    public required string ExternalKey { get; set; }
    public required string PayloadJson { get; set; }
    public DateTimeOffset RetrievedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
