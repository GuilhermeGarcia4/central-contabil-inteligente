using CentralContabil.Api.Modules.Integrations.Application;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace CentralContabil.Api.Modules.Journal.Application;

public sealed record JournalNewsItem(
    string Id, string Title, string Summary, string Category, string Source,
    DateTimeOffset PublishedAt, string SourceUrl, DateTimeOffset RetrievedAt);

public sealed record JournalNewsResponse(
    IReadOnlyList<JournalNewsItem> Items, int Page, int PageSize, int Total,
    DateTimeOffset GeneratedAt, int SourcesAvailable, int SourcesTotal, IReadOnlyList<string> Warnings);

public sealed record JournalFeedSnapshot(DateTimeOffset RetrievedAt, IReadOnlyList<JournalNewsItem> Items);

public sealed class JournalNewsService(
    IHttpClientFactory clients,
    IExternalSnapshotCache cache,
    IIntegrationHealthStore health,
    ILogger<JournalNewsService> logger)
{
    private sealed record Feed(string Name, string Url);
    private sealed record FeedResult(IReadOnlyList<JournalNewsItem> Items, bool Available, string? Warning);

    public static readonly string[] Categories = ["Legislação", "Receita Federal", "Tributos", "eSocial", "MEI", "Contabilidade", "Negócios"];
    private static readonly Feed[] Feeds =
    [
        new("Receita Federal", "https://www.gov.br/receitafederal/pt-br/assuntos/noticias/RSS"),
        new("CFC", "https://cfc.org.br/category/noticias/feed/")
    ];
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);

    public async Task<JournalNewsResponse> SearchAsync(
        string? query, string? category, int page, int pageSize, string? order, CancellationToken cancellationToken)
    {
        var feedResults = new List<FeedResult>(Feeds.Length);
        foreach (var feed in Feeds)
            feedResults.Add(await LoadFeedAsync(feed, cancellationToken));
        var results = feedResults.ToArray();
        var all = results.SelectMany(x => x.Items)
            .GroupBy(x => x.SourceUrl, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.OrderByDescending(x => x.PublishedAt).First());

        var term = (query ?? string.Empty).Trim();
        if (term.Length > 120) term = term[..120];
        if (term.Length > 0)
            all = all.Where(item => Contains(item.Title, term) || Contains(item.Summary, term) || Contains(item.Source, term) || Contains(item.Category, term));
        if (!string.IsNullOrWhiteSpace(category) && Categories.Contains(category, StringComparer.OrdinalIgnoreCase))
            all = all.Where(item => string.Equals(item.Category, category, StringComparison.OrdinalIgnoreCase));

        var ordered = string.Equals(order, "oldest", StringComparison.OrdinalIgnoreCase)
            ? all.OrderBy(x => x.PublishedAt).ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
            : all.OrderByDescending(x => x.PublishedAt).ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase);
        var materialized = ordered.ToArray();
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var items = materialized.Skip((page - 1) * pageSize).Take(pageSize).ToArray();
        return new JournalNewsResponse(items, page, pageSize, materialized.Length, DateTimeOffset.UtcNow,
            results.Count(x => x.Available), Feeds.Length, results.Where(x => x.Warning is not null).Select(x => x.Warning!).ToArray());
    }

    private async Task<FeedResult> LoadFeedAsync(Feed feed, CancellationToken cancellationToken)
    {
        var fresh = await cache.GetAsync<JournalFeedSnapshot>(feed.Name, "journal-feed", "latest", cancellationToken);
        if (fresh is not null) return new FeedResult(fresh.Items, true, null);

        if (!health.CanExecute(feed.Name))
            return await StaleOrUnavailableAsync(feed, cancellationToken);

        var timer = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var client = clients.CreateClient("journal-feed");
            using var response = await client.GetAsync(feed.Url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode) throw new HttpRequestException($"HTTP {(int)response.StatusCode}");
            if (response.Content.Headers.ContentLength > 2_000_000) throw new InvalidDataException("Feed excede o tamanho permitido.");
            var xml = await response.Content.ReadAsStringAsync(cancellationToken);
            var now = DateTimeOffset.UtcNow;
            var parsed = JournalFeedParser.Parse(xml, feed.Name, new Uri(feed.Url), now);
            health.RecordSuccess(feed.Name, timer.Elapsed);
            var snapshot = new JournalFeedSnapshot(now, parsed);
            await cache.SetAsync(feed.Name, "journal-feed", "latest", snapshot, CacheDuration, cancellationToken);
            return new FeedResult(parsed, true, null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidDataException or XmlException)
        {
            health.RecordFailure(feed.Name, timer.Elapsed, exception);
            logger.LogWarning(exception, "Official journal feed {Provider} is unavailable", feed.Name);
            return await StaleOrUnavailableAsync(feed, cancellationToken);
        }
    }

    private async Task<FeedResult> StaleOrUnavailableAsync(Feed feed, CancellationToken cancellationToken)
    {
        var stale = await cache.GetStaleAsync<JournalFeedSnapshot>(feed.Name, "journal-feed", "latest", cancellationToken);
        var state = stale is null ? "sem cópia anterior disponível" : "exibindo a última cópia salva";
        return new FeedResult(stale?.Items ?? [], false, $"Fonte {feed.Name} indisponível; {state}.");
    }

    private static bool Contains(string value, string term) => value.Contains(term, StringComparison.OrdinalIgnoreCase);
}

public static class JournalFeedParser
{
    private static readonly Regex HtmlTags = new("<[^>]*>", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex Whitespace = new("\\s+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IReadOnlyList<JournalNewsItem> Parse(string xml, string source, Uri feedUri, DateTimeOffset retrievedAt)
    {
        using var stringReader = new StringReader(xml);
        using var reader = XmlReader.Create(stringReader, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 2_000_000 });
        var document = XDocument.Load(reader);
        return document.Descendants().Where(x => x.Name.LocalName.Equals("item", StringComparison.OrdinalIgnoreCase))
            .Select(item => ParseItem(item, source, feedUri, retrievedAt))
            .Where(item => item is not null)
            .Select(item => item!)
            .OrderByDescending(item => item.PublishedAt)
            .Take(100)
            .ToArray();
    }

    private static JournalNewsItem? ParseItem(XElement item, string source, Uri feedUri, DateTimeOffset retrievedAt)
    {
        var title = Clean(ChildValue(item, "title"));
        var link = ChildValue(item, "link").Trim();
        var summary = Clean(ChildValue(item, "description", "summary", "encoded", "content"));
        var dateText = ChildValue(item, "pubDate", "published", "date", "issued", "updated");
        if (title.Length == 0 || !DateTimeOffset.TryParse(dateText, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var publishedAt)) return null;
        if (!Uri.TryCreate(feedUri, WebUtility.HtmlDecode(link), out var sourceUri) || sourceUri.Scheme is not ("http" or "https")) return null;
        if (summary.Length == 0) summary = "A fonte não disponibilizou um resumo para esta publicação.";
        if (summary.Length > 360) summary = summary[..357].TrimEnd() + "…";
        var category = Classify(title + " " + summary, source);
        return new JournalNewsItem(sourceUri.AbsoluteUri, title, summary, category, source, publishedAt, sourceUri.AbsoluteUri, retrievedAt);
    }

    private static string ChildValue(XElement item, params string[] names) => item.Elements()
        .FirstOrDefault(element => names.Contains(element.Name.LocalName, StringComparer.OrdinalIgnoreCase))?.Value ?? string.Empty;

    private static string Clean(string value)
    {
        var decoded = WebUtility.HtmlDecode(value);
        decoded = Regex.Replace(decoded, "<script\\b[^>]*>.*?</script>|<style\\b[^>]*>.*?</style>", " ", RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return Whitespace.Replace(HtmlTags.Replace(decoded, " "), " ").Trim();
    }

    private static string Classify(string content, string source)
    {
        var normalized = content.Normalize(NormalizationForm.FormD);
        normalized = new string(normalized.Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark).ToArray()).ToLowerInvariant();
        if (Regex.IsMatch(normalized, @"\besocial\b")) return "eSocial";
        if (Regex.IsMatch(normalized, @"\bmei\b|microempreendedor individual")) return "MEI";
        if (Regex.IsMatch(normalized, @"\b(lei|decreto|portaria|instrucao normativa|resolucao|norma|legislacao)\b")) return "Legislação";
        if (Regex.IsMatch(normalized, @"\b(tribut|imposto|aliquota|icms|iss|ibs|cbs|irpf|irpj|simples nacional|dctf|sp ed)\w*\b")) return "Tributos";
        if (source.Equals("Receita Federal", StringComparison.OrdinalIgnoreCase)) return "Receita Federal";
        if (source.Equals("CFC", StringComparison.OrdinalIgnoreCase)) return "Contabilidade";
        return "Negócios";
    }
}
