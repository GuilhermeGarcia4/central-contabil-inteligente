using CentralContabil.Api.Modules.Integrations.Application;
using CentralContabil.Api.Modules.Integrations.Domain;
using System.Collections.Concurrent;

namespace CentralContabil.Api.Modules.Integrations.Infrastructure;

public sealed class IntegrationHealthStore : IIntegrationHealthStore
{
    private sealed class ProviderState
    {
        public readonly object Sync = new();
        public int ConsecutiveFailures;
        public DateTimeOffset? LastSuccessAt;
        public DateTimeOffset? LastFailureAt;
        public long? LastDurationMilliseconds;
        public string? LastError;
        public DateTimeOffset? CircuitOpenUntil;
    }

    private readonly ConcurrentDictionary<string, ProviderState> _states = new(StringComparer.OrdinalIgnoreCase);

    public bool CanExecute(string provider)
    {
        var state = _states.GetOrAdd(provider, _ => new ProviderState());
        lock (state.Sync) return state.CircuitOpenUntil is null || state.CircuitOpenUntil <= DateTimeOffset.UtcNow;
    }

    public void RecordSuccess(string provider, TimeSpan duration)
    {
        var state = _states.GetOrAdd(provider, _ => new ProviderState());
        lock (state.Sync)
        {
            state.ConsecutiveFailures = 0;
            state.LastSuccessAt = DateTimeOffset.UtcNow;
            state.LastDurationMilliseconds = (long)duration.TotalMilliseconds;
            state.LastError = null;
            state.CircuitOpenUntil = null;
        }
    }

    public void RecordFailure(string provider, TimeSpan duration, Exception exception)
    {
        var state = _states.GetOrAdd(provider, _ => new ProviderState());
        lock (state.Sync)
        {
            state.ConsecutiveFailures++;
            state.LastFailureAt = DateTimeOffset.UtcNow;
            state.LastDurationMilliseconds = (long)duration.TotalMilliseconds;
            state.LastError = exception.Message.Length > 300 ? exception.Message[..300] : exception.Message;
            if (state.ConsecutiveFailures >= 3) state.CircuitOpenUntil = DateTimeOffset.UtcNow.AddSeconds(30);
        }
    }

    public IReadOnlyList<IntegrationHealth> Snapshot(IEnumerable<(string Name, bool Enabled)> providers) => providers
        .DistinctBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
        .Select(provider =>
        {
            var state = _states.GetOrAdd(provider.Name, _ => new ProviderState());
            lock (state.Sync)
            {
                var circuitOpen = state.CircuitOpenUntil > DateTimeOffset.UtcNow;
                var status = !provider.Enabled ? "disabled" : circuitOpen ? "circuit-open" : state.LastSuccessAt is null ? "unknown" : "available";
                return new IntegrationHealth(provider.Name, provider.Enabled, status, state.ConsecutiveFailures,
                    state.LastSuccessAt, state.LastFailureAt, state.LastDurationMilliseconds, state.LastError);
            }
        }).ToArray();
}
