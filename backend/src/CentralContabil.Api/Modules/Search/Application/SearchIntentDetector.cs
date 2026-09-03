using CentralContabil.Api.Modules.Integrations.Domain;
using System.Text.RegularExpressions;

namespace CentralContabil.Api.Modules.Search.Application;

public static partial class SearchIntentDetector
{
    public static SearchIntent Detect(string query)
    {
        var trimmed = query.Trim();
        if (CnpjIdentifier.TryParse(trimmed, out _, out _)) return SearchIntent.Cnpj;
        var digits = new string(trimmed.Where(char.IsAsciiDigit).ToArray());
        if ((digits.Length == 8 && trimmed.All(x => char.IsAsciiDigit(x) || x is '.' or ' ')) || NcmPrefix().IsMatch(trimmed)) return SearchIntent.Ncm;
        if (LegalTerms().IsMatch(trimmed)) return SearchIntent.LegalReference;
        return SearchIntent.General;
    }

    [GeneratedRegex(@"^\s*ncm\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NcmPrefix();
    [GeneratedRegex(@"\b(lei|decreto|portaria|instru[cç][aã]o\s+normativa|norma|projeto\s+de\s+lei|pl\s*\d|pec\s*\d)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex LegalTerms();
}
