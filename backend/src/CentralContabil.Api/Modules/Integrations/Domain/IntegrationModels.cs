using System.Text.Json.Serialization;

namespace CentralContabil.Api.Modules.Integrations.Domain;

public sealed record AddressData(string Street, string Number, string Complement, string District, string City, string State, string PostalCode);
public sealed record EconomicActivity(string Code, string Description, bool IsPrimary);
public sealed record CompanyData(
    string Cnpj, string FormattedCnpj, string LegalName, string TradeName, string RegistrationStatus,
    DateOnly? OpenedOn, string CompanySize, string LegalNature, AddressData Address,
    EconomicActivity? PrimaryActivity, IReadOnlyList<EconomicActivity> SecondaryActivities,
    bool? IsSimpleNational, bool? IsMei, string Provider, DateTimeOffset RetrievedAt, bool FromCache);

public sealed record NcmData(string Code, string Description, DateOnly? EffectiveFrom, DateOnly? EffectiveUntil,
    string ActType, string ActNumber, string ActYear, string Provider, DateTimeOffset RetrievedAt, bool FromCache);

[JsonConverter(typeof(JsonStringEnumConverter<LegalDocumentKind>))]
public enum LegalDocumentKind { LegalNorm, LegislativeProposal }
public sealed record LegalSearchItem(string Id, string Title, string Summary, LegalDocumentKind Kind, string LegislativeHouse,
    string Status, DateOnly? PresentedOn, string OfficialUrl, string Provider);

[JsonConverter(typeof(JsonStringEnumConverter<SearchIntent>))]
public enum SearchIntent { General, Cnpj, Ncm, LegalReference }
public sealed record UnifiedSearchResponse(string Query, SearchIntent Intent, object Internal, IReadOnlyList<CompanyData> Companies,
    IReadOnlyList<NcmData> Ncms, IReadOnlyList<LegalSearchItem> Legislation, IReadOnlyList<string> Warnings);

public sealed record IntegrationHealth(string Provider, bool Enabled, string State, int ConsecutiveFailures,
    DateTimeOffset? LastSuccessAt, DateTimeOffset? LastFailureAt, long? LastDurationMilliseconds, string? LastError);
