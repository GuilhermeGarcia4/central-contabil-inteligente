using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Integrations.Application;
using CentralContabil.Api.Modules.Integrations.Domain;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace CentralContabil.Api.Modules.Integrations.Infrastructure;

public sealed class ExternalSnapshotCache(AppDbContext db, ILogger<ExternalSnapshotCache> logger) : IExternalSnapshotCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<T?> GetAsync<T>(string provider, string entityType, string externalKey, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var snapshot = await db.ExternalEntitySnapshots.AsNoTracking().SingleOrDefaultAsync(x =>
            x.Provider == provider && x.EntityType == entityType && x.ExternalKey == externalKey && x.ExpiresAt > now, cancellationToken);
        if (snapshot is null) return default;
        try { return JsonSerializer.Deserialize<T>(snapshot.PayloadJson, JsonOptions); }
        catch (JsonException exception)
        {
            logger.LogWarning(exception, "Ignoring invalid cached {EntityType} snapshot from {Provider}", entityType, provider);
            return default;
        }
    }

    public async Task SetAsync<T>(string provider, string entityType, string externalKey, T value, TimeSpan timeToLive, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var snapshot = await db.ExternalEntitySnapshots.SingleOrDefaultAsync(x =>
            x.Provider == provider && x.EntityType == entityType && x.ExternalKey == externalKey, cancellationToken);
        if (snapshot is null)
        {
            snapshot = new ExternalEntitySnapshot { Provider = provider, EntityType = entityType, ExternalKey = externalKey, PayloadJson = string.Empty };
            db.ExternalEntitySnapshots.Add(snapshot);
        }
        snapshot.PayloadJson = JsonSerializer.Serialize(value, JsonOptions);
        snapshot.RetrievedAt = now;
        snapshot.ExpiresAt = now.Add(timeToLive);
        await db.SaveChangesAsync(cancellationToken);
    }
}
