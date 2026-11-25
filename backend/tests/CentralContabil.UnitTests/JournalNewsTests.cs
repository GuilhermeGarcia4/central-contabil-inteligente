using CentralContabil.Api.Modules.Journal.Application;

namespace CentralContabil.UnitTests;

public sealed class JournalNewsTests
{
    [Fact]
    public void ParsesOfficialAtomRdfFeedAndPreservesOriginalDateAndLink()
    {
        const string xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns="http://purl.org/rss/1.0/">
              <item rdf:about="https://www.gov.br/receitafederal/noticia">
                <title>Receita Federal divulga orientações sobre MEI</title>
                <link>https://www.gov.br/receitafederal/noticia</link>
                <description><![CDATA[<p>Veja as <b>orientações</b> oficiais para o microempreendedor individual.</p>]]></description>
                <dc:date>2026-11-25T13:00:00Z</dc:date>
              </item>
            </rdf:RDF>
            """;

        var result = JournalFeedParser.Parse(xml, "Receita Federal", new Uri("https://www.gov.br/receitafederal/RSS"), DateTimeOffset.Parse("2026-11-25T14:00:00Z"));

        var item = Assert.Single(result);
        Assert.Equal("MEI", item.Category);
        Assert.Equal("https://www.gov.br/receitafederal/noticia", item.SourceUrl);
        Assert.Equal(DateTimeOffset.Parse("2026-11-25T13:00:00Z"), item.PublishedAt);
        Assert.Contains("orientações oficiais", item.Summary);
        Assert.DoesNotContain("<p>", item.Summary);
    }

    [Fact]
    public void ParsesRss20AndSkipsEntriesWithoutAnOriginalPublicationDate()
    {
        const string xml = """
            <rss version="2.0"><channel>
              <item><title>Atualização contábil</title><link>https://cfc.org.br/noticia/1</link><description>Publicação do Conselho Federal de Contabilidade.</description><pubDate>Wed, 25 Nov 2026 10:00:00 +0000</pubDate></item>
              <item><title>Sem data</title><link>https://cfc.org.br/noticia/2</link><description>Item sem data de publicação original.</description></item>
            </channel></rss>
            """;

        var result = JournalFeedParser.Parse(xml, "CFC", new Uri("https://cfc.org.br/feed/"), DateTimeOffset.UtcNow);

        var item = Assert.Single(result);
        Assert.Equal("Contabilidade", item.Category);
        Assert.Equal("https://cfc.org.br/noticia/1", item.SourceUrl);
        Assert.DoesNotContain("Sem data", item.Title);
    }

    [Fact]
    public void RejectsNonHttpPublicationLinks()
    {
        const string xml = """
            <rss version="2.0"><channel><item><title>Notícia</title><link>javascript:alert(1)</link><description>Texto</description><pubDate>Wed, 25 Nov 2026 10:00:00 +0000</pubDate></item></channel></rss>
            """;

        var result = JournalFeedParser.Parse(xml, "CFC", new Uri("https://cfc.org.br/feed/"), DateTimeOffset.UtcNow);
        Assert.Empty(result);
    }
}
