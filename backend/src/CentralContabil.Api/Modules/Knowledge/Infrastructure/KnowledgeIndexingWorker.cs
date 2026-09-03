using CentralContabil.Api.Modules.Knowledge.Application;
using System.Threading.Channels;

namespace CentralContabil.Api.Modules.Knowledge.Infrastructure;

public interface IKnowledgeIndexQueue { bool Enqueue(Guid? articleId = null); }
public sealed class KnowledgeIndexQueue : IKnowledgeIndexQueue
{
    internal Channel<Guid?> Channel { get; } = System.Threading.Channels.Channel.CreateBounded<Guid?>(new BoundedChannelOptions(100) { FullMode = BoundedChannelFullMode.DropOldest });
    public bool Enqueue(Guid? articleId = null) => Channel.Writer.TryWrite(articleId);
}

public sealed class KnowledgeIndexingWorker(IServiceScopeFactory scopes, KnowledgeIndexQueue queue, ILogger<KnowledgeIndexingWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        queue.Enqueue();
        await foreach (var articleId in queue.Channel.Reader.ReadAllAsync(stoppingToken))
        {
            try { using var scope = scopes.CreateScope(); var service = scope.ServiceProvider.GetRequiredService<KnowledgeIndexingService>(); if (articleId is Guid id) await service.IndexArticleByIdAsync(id, stoppingToken); else await service.IndexAllEligibleAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex) { logger.LogError(ex, "Falha no processamento da fila de conhecimento"); }
        }
    }
}
