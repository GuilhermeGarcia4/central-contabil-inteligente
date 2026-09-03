using System.Text;

namespace CentralContabil.Api.Modules.Integrations.Domain;

/// <summary>Identificador CNPJ textual, compatível com os formatos numérico e alfanumérico.</summary>
public readonly record struct CnpjIdentifier
{
    private static readonly int[] FirstDigitWeights = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
    private static readonly int[] SecondDigitWeights = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];

    public string Value { get; }
    public string Formatted => $"{Value[..2]}.{Value[2..5]}.{Value[5..8]}/{Value[8..12]}-{Value[12..]}";

    private CnpjIdentifier(string value) => Value = value;

    public static bool TryParse(string? input, out CnpjIdentifier identifier, out string? error)
    {
        identifier = default;
        var normalized = Normalize(input);
        if (normalized.Length != 14)
        {
            error = "O CNPJ deve conter 14 caracteres, além dos separadores opcionais.";
            return false;
        }

        if (!HasValidCharacters(normalized))
        {
            error = "O CNPJ deve ter 12 caracteres alfanuméricos e dois dígitos verificadores numéricos.";
            return false;
        }

        if (normalized.Distinct().Count() == 1 || CalculateDigit(normalized.AsSpan(0, 12), FirstDigitWeights) != normalized[12] - '0' ||
            CalculateDigit(normalized.AsSpan(0, 13), SecondDigitWeights) != normalized[13] - '0')
        {
            error = "Os dígitos verificadores do CNPJ são inválidos.";
            return false;
        }

        identifier = new CnpjIdentifier(normalized);
        error = null;
        return true;
    }

    public static CnpjIdentifier Parse(string input) =>
        TryParse(input, out var value, out var error) ? value : throw new FormatException(error);

    public static string Normalize(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var buffer = new StringBuilder(input.Length);
        foreach (var character in input.Trim().ToUpperInvariant())
            if (character is not ('.' or '/' or '-' or ' ' or '\t' or '\r' or '\n')) buffer.Append(character);
        return buffer.ToString();
    }

    private static bool IsUpperAsciiLetterOrDigit(char value) => char.IsAsciiDigit(value) || value is >= 'A' and <= 'Z';

    private static bool HasValidCharacters(string value)
    {
        for (var index = 0; index < 12; index++) if (!IsUpperAsciiLetterOrDigit(value[index])) return false;
        return char.IsAsciiDigit(value[12]) && char.IsAsciiDigit(value[13]);
    }

    private static int CalculateDigit(ReadOnlySpan<char> characters, ReadOnlySpan<int> weights)
    {
        var sum = 0;
        for (var index = 0; index < characters.Length; index++) sum += (characters[index] - 48) * weights[index];
        var remainder = sum % 11;
        return remainder is 0 or 1 ? 0 : 11 - remainder;
    }

    public override string ToString() => Value;
}
