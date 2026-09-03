using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.AI.Domain;
using CentralContabil.Api.Modules.AI.Infrastructure;
using CentralContabil.Api.Modules.Calculators.Application;
using CentralContabil.Api.Modules.Finance.Application;
using CentralContabil.Api.Modules.Knowledge.Application;
using CentralContabil.Api.Modules.Knowledge.Domain;
using CentralContabil.Api.Modules.Rules.Application;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Globalization;
using System.Security.Claims;
using System.Text.RegularExpressions;

namespace CentralContabil.Api.Modules.AI.Application;

public sealed record GroundedContext(string Question, AssistantIntent Intent, IReadOnlyList<KnowledgeSearchResult> Documents, object? ToolResult);
public interface IChatCompletionService { Task<string> CompleteAsync(GroundedContext context, CancellationToken ct); }

public sealed class GroundedTemplateChatCompletionService : IChatCompletionService
{
    public Task<string> CompleteAsync(GroundedContext context, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        if (context.ToolResult is CalculationResponse calculation)
            return Task.FromResult($"O resultado bruto estimado é {calculation.Result:C}. O cálculo foi executado pela calculadora determinística usando a regra {calculation.Rule.Name} ({calculation.Rule.Version}), com a fórmula: {calculation.Formula}.");
        if (context.ToolResult is FinancialToolResult financial)
            return Task.FromResult(financial.Summary);
        if (context.Documents.Count == 0) return Task.FromResult("Não encontrei informação suficiente nas fontes verificadas da plataforma para responder com segurança.");
        var statements = context.Documents.Take(3).Select(x => x.Context.Trim())
            .Where(x => x.Length > 0 && !Regex.IsMatch(TextNormalizer.Normalize(x), @"\b(ignore|revele|reveal|system prompt|instrucoes anteriores|segredos do sistema)\b"));
        var grounded = string.Join("\n\n", statements);
        return Task.FromResult(grounded.Length == 0 ? "Não encontrei informação suficiente nas fontes verificadas da plataforma para responder com segurança." : grounded);
    }
}

public static partial class AssistantIntentDetector
{
    public static AssistantIntent Detect(string question)
    {
        var q = TextNormalizer.Normalize(question);
        if (FinancialTerms().IsMatch(q)) return AssistantIntent.FinancialQuestion;
        if (CalculationTerms().IsMatch(q)) return AssistantIntent.Calculation;
        if (MeiTerms().IsMatch(q)) return AssistantIntent.MeiQuestion;
        if (TaxTerms().IsMatch(q)) return AssistantIntent.TaxQuestion;
        if (LaborTerms().IsMatch(q)) return AssistantIntent.LaborQuestion;
        if (LegalTerms().IsMatch(q)) return AssistantIntent.LegalQuestion;
        return q.Length < 3 ? AssistantIntent.OutOfScope : AssistantIntent.Information;
    }
    public static bool IsCritical(AssistantIntent intent) => intent is AssistantIntent.LegalQuestion or AssistantIntent.TaxQuestion or AssistantIntent.LaborQuestion or AssistantIntent.MeiQuestion;
    [GeneratedRegex(@"\b(quanto|calcular|calculo|salario|juros|ferias|13|decimo terceiro)\b")] private static partial Regex CalculationTerms();
    [GeneratedRegex(@"\b(mei|microempreendedor|desenquadramento)\b")] private static partial Regex MeiTerms();
    [GeneratedRegex(@"\b(imposto|tributo|fiscal|receita federal|aliquota)\b")] private static partial Regex TaxTerms();
    [GeneratedRegex(@"\b(trabalho|trabalhista|ferias|salario|rescisao|fgts)\b")] private static partial Regex LaborTerms();
    [GeneratedRegex(@"\b(lei|decreto|juridic|legislacao|norma)\b")] private static partial Regex LegalTerms();
    [GeneratedRegex(@"\b(entrou|entrada|gastei|saida|saída|despesa|parcela|meta|fatura|cartao|cartão|planejamento|orcamento|orçamento|conta a pagar|meu mes|meu mês|minhas financas|minhas finanças|quanto entrou|quanto gastei|investimento|juros|financeir|rendimento)\b")] private static partial Regex FinancialTerms();
}

public sealed class AssistantToolRouter(IRuleSetSelector rules, IEnumerable<IFinancialCalculator> calculators)
{
    public async Task<(object? Result, RelatedItem? Calculator)> TryExecuteAsync(string question, CancellationToken ct)
    {
        var normalized = TextNormalizer.Normalize(question);
        if ((normalized.Contains("13") || normalized.Contains("decimo terceiro")) && TryMoney(question, out var salary) && TryMonths(question, out var months))
            return await Calculate("decimo-terceiro", new ThirteenthSalaryInput(salary, months), "13º salário", ct);
        if (normalized.Contains("ferias") && TryMoney(question, out var grossSalary))
            return await Calculate("ferias", new VacationInput(grossSalary, 30), "Férias", ct);
        return (null, null);
    }

    private async Task<(object?, RelatedItem?)> Calculate(string slug, object input, string title, CancellationToken ct)
    {
        var rule = await rules.GetActiveAsync(slug, DateOnly.FromDateTime(DateTime.UtcNow), ct); if (rule is null) return (null, new RelatedItem(title, slug));
        return (calculators.Single(x => x.Slug == slug).Calculate(input, rule), new RelatedItem(title, slug));
    }
    private static bool TryMoney(string value, out decimal amount)
    {
        amount = Regex.Matches(value, @"(?:R\$\s*)?(\d[\d.,]*)")
            .Select(match => match.Groups[1].Value.Replace(".", "").Replace(',', '.'))
            .Select(raw => decimal.TryParse(raw, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed) ? parsed : 0)
            .DefaultIfEmpty().Max();
        return amount > 0;
    }
    private static bool TryMonths(string value, out int months)
    {
        var match = Regex.Match(value, @"(\d{1,2})\s*(?:mes|meses|mês)", RegexOptions.IgnoreCase);
        return int.TryParse(match.Success ? match.Groups[1].Value : "", out months) && months is >= 1 and <= 12;
    }
}

public sealed class AssistantService(AppDbContext db, HybridKnowledgeSearch search, AssistantToolRouter tools, IChatCompletionService chat, IConfiguration configuration, FinancialAssistantTools financialTools, AssistantConversationService conversations)
{
    public async Task<AssistantAnswer> AskAsync(string question, ClaimsPrincipal principal, CancellationToken ct, Guid? conversationId = null)
    {
        question = question.Trim(); if (question.Length is < 3 or > 1000) throw new FormatException("A pergunta deve conter entre 3 e 1000 caracteres.");
        var watch = Stopwatch.StartNew(); var intent = AssistantIntentDetector.Detect(question); var critical = AssistantIntentDetector.IsCritical(intent);
        var userId = Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : (Guid?)null;
        var (toolResult, calculator) = intent == AssistantIntent.Calculation ? await tools.TryExecuteAsync(question, ct) : (null, null);
        object? financialResult = null;
        if (intent == AssistantIntent.FinancialQuestion && userId is not null) financialResult = await financialTools.TryAnswerAsync(userId.Value, question, ct);
        var documents = await search.SearchAsync(question, DateOnly.FromDateTime(DateTime.UtcNow), critical, ct);
        var sufficient = toolResult is not null || financialResult is not null || documents.Any(x => x.Score >= .25 && (!critical || x.IsOfficial));
        if (!sufficient) { documents = []; await RegisterUnansweredAsync(question, ct); }
        var answer = await chat.CompleteAsync(new GroundedContext(question, intent, documents, toolResult ?? financialResult), ct);
        var sourceList = documents.Where(x => x.Url != null).Select(x => new AssistantSource(x.Title, x.Url, x.IsOfficial, x.LastVerifiedAt)).DistinctBy(x => x.Url).ToArray();
        var related = documents.Where(x => x.Slug != null).Select(x => new RelatedItem(x.Title, x.Slug!)).DistinctBy(x => x.Slug).ToArray();
        var confidence = toolResult is not null || financialResult is not null || documents.Any(x => x.IsOfficial && x.Score >= .55) ? "high" : documents.Count >= 2 ? "medium" : "low";
        var aiSettings = AiProviderSettings.From(configuration);
        db.AIRequests.Add(new AIRequest { UserId = userId, DurationMs = watch.ElapsedMilliseconds, Status = sufficient ? AIRequestStatus.Completed : AIRequestStatus.NoSource, Intent = intent.ToString(),
            Provider = aiSettings.ChatConfigured ? aiSettings.Provider.ToString().ToLowerInvariant() : "local", Model = aiSettings.ChatConfigured ? aiSettings.SelectedChatModel : "grounded-template-v1",
            InputTokens = question.Length / 4, OutputTokens = answer.Length / 4 });
        await db.SaveChangesAsync(ct);
        if (userId is not null && conversationId is not null)
        {
            var sourcesJson = sourceList.Length == 0 ? null : System.Text.Json.JsonSerializer.Serialize(sourceList.Select(x => new { x.Title, x.Url }));
            await conversations.AddMessageAsync(conversationId.Value, userId.Value, "user", question, null, ct);
            await conversations.AddMessageAsync(conversationId.Value, userId.Value, "assistant", answer, sourcesJson, ct);
        }
        var disclaimer = critical ? "Conteúdo informativo. Situações específicas podem exigir análise profissional." : null;
        return new AssistantAnswer(answer, sourceList, confidence, critical, related, calculator is null ? [] : [calculator], toolResult ?? financialResult, disclaimer);
    }

    private async Task RegisterUnansweredAsync(string question, CancellationToken ct)
    {
        var normalized = TextNormalizer.Normalize(question); var item = await db.UnansweredQuestions.FirstOrDefaultAsync(x => x.NormalizedQuestion == normalized, ct);
        if (item is null) db.UnansweredQuestions.Add(new UnansweredQuestion { NormalizedQuestion = normalized });
        else { item.Count++; item.LastAskedAt = DateTimeOffset.UtcNow; }
    }
}
