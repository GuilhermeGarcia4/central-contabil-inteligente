using CentralContabil.Api.Modules.Integrations.Domain;

namespace CentralContabil.Api.Modules.Integrations.Application;

public interface ICompanyDataProvider
{
    string Name { get; }
    bool Enabled { get; }
    Task<CompanyData> GetByCnpjAsync(CnpjIdentifier cnpj, CancellationToken cancellationToken);
}

public interface INcmDataProvider
{
    string Name { get; }
    bool Enabled { get; }
    Task<IReadOnlyList<NcmData>> SearchAsync(string query, CancellationToken cancellationToken);
    Task<NcmData> GetByCodeAsync(string code, CancellationToken cancellationToken);
}

public interface ILegislationProvider
{
    string Name { get; }
    bool Enabled { get; }
    Task<IReadOnlyList<LegalSearchItem>> SearchAsync(string query, CancellationToken cancellationToken);
}

public interface IExternalSnapshotCache
{
    Task<T?> GetAsync<T>(string provider, string entityType, string externalKey, CancellationToken cancellationToken);
    Task SetAsync<T>(string provider, string entityType, string externalKey, T value, TimeSpan timeToLive, CancellationToken cancellationToken);
}

public interface IIntegrationHealthStore
{
    bool CanExecute(string provider);
    void RecordSuccess(string provider, TimeSpan duration);
    void RecordFailure(string provider, TimeSpan duration, Exception exception);
    IReadOnlyList<IntegrationHealth> Snapshot(IEnumerable<(string Name, bool Enabled)> providers);
}

public sealed class ExternalNotFoundException(string message) : Exception(message);
public sealed class ExternalTransientException(string message, Exception? inner = null) : Exception(message, inner);
public sealed class ExternalProviderException(string message, Exception? inner = null) : Exception(message, inner);
