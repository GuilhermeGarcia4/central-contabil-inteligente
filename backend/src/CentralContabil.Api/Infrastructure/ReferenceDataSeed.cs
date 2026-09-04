using CentralContabil.Api.Modules.Shared.Domain;
using Microsoft.EntityFrameworkCore;

namespace CentralContabil.Api.Infrastructure;

public sealed class ReferenceDataSeed(
    AppDbContext db,
    ILogger<ReferenceDataSeed> logger)
{
    private static readonly CategoryDefinition[] Categories =
    [
        new("Trabalhista", "trabalhista", "Direitos e relações de trabalho"),
        new("MEI", "mei", "Orientações para microempreendedores"),
        new("Financeiro", "financeiro", "Educação e cálculos financeiros"),
        new("Contabilidade", "contabilidade", "Conceitos contábeis")
    ];

    private static readonly CalculatorDefinition[] Calculators =
    [
        new("Férias", "ferias", "Estimativa bruta e auditável de férias", "Trabalhista"),
        new("13º salário", "decimo-terceiro", "Estimativa proporcional bruta", "Trabalhista"),
        new("Juros compostos", "juros-compostos", "Simule a evolução de um investimento", "Financeiro")
    ];

    // Estes são os únicos conjuntos encontrados no seed original e no banco local.
    // Os valores são preservados literalmente; não há Source versionada no projeto.
    private static readonly RuleSetDefinition[] RuleSets =
    [
        new("ferias", "Férias — regra de desenvolvimento", "dev-1", new DateOnly(2025, 8, 27), null, true,
        [
            new("AdditionalVacationPercentage", "0.333333", ParameterValueType.Decimal,
                "TODO: validar regra e fonte oficial antes da produção")
        ]),
        new("decimo-terceiro", "13º salário — regra de desenvolvimento", "dev-1", new DateOnly(2025, 8, 27), null, true, []),
        new("juros-compostos", "Juros compostos — regra de desenvolvimento", "dev-1", new DateOnly(2025, 8, 27), null, true, [])
    ];

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Reference data provisioning started.");

        try
        {
            var categorySlugs = await db.Categories
                .Select(category => category.Slug)
                .ToHashSetAsync(cancellationToken);
            var calculatorSlugs = await db.Calculators
                .Select(calculator => calculator.Slug)
                .ToHashSetAsync(cancellationToken);

            var provisionedCategories = new List<string>();
            var provisionedCalculators = new List<string>();

            foreach (var definition in Categories.Where(item => !categorySlugs.Contains(item.Slug)))
            {
                db.Categories.Add(new Category
                {
                    Name = definition.Name,
                    Slug = definition.Slug,
                    Description = definition.Description
                });
                provisionedCategories.Add(definition.Slug);
            }

            foreach (var definition in Calculators.Where(item => !calculatorSlugs.Contains(item.Slug)))
            {
                db.Calculators.Add(new Calculator
                {
                    Name = definition.Name,
                    Slug = definition.Slug,
                    Description = definition.Description,
                    Category = definition.Category
                });
                provisionedCalculators.Add(definition.Slug);
            }

            if (db.ChangeTracker.HasChanges())
                await db.SaveChangesAsync(cancellationToken);

            foreach (var slug in provisionedCategories)
                logger.LogInformation("Reference category provisioned: {Slug}", slug);
            foreach (var slug in provisionedCalculators)
                logger.LogInformation("Reference calculator provisioned: {Slug}", slug);

            var calculatorsBySlug = await db.Calculators
                .Where(calculator => RuleSets.Select(rule => rule.CalculatorSlug).Contains(calculator.Slug))
                .ToDictionaryAsync(calculator => calculator.Slug, cancellationToken);
            var missingCalculators = RuleSets
                .Select(rule => rule.CalculatorSlug)
                .Where(slug => !calculatorsBySlug.ContainsKey(slug))
                .ToArray();
            if (missingCalculators.Length > 0)
                throw new InvalidOperationException(
                    $"Failed to provision required reference calculators: {string.Join(", ", missingCalculators)}.");

            foreach (var definition in RuleSets)
            {
                var calculator = calculatorsBySlug[definition.CalculatorSlug];
                var ruleSet = await db.CalculationRuleSets
                    .Include(rule => rule.Parameters)
                    .SingleOrDefaultAsync(
                        rule => rule.CalculatorId == calculator.Id && rule.Version == definition.Version,
                        cancellationToken);

                if (ruleSet is null)
                {
                    ruleSet = new CalculationRuleSet
                    {
                        CalculatorId = calculator.Id,
                        Name = definition.Name,
                        Version = definition.Version,
                        ValidFrom = definition.ValidFrom,
                        ValidUntil = definition.ValidUntil,
                        IsActive = definition.IsActive
                    };
                    db.CalculationRuleSets.Add(ruleSet);
                    logger.LogInformation(
                        "Reference rule set provisioned: {CalculatorSlug} / {Version}",
                        definition.CalculatorSlug,
                        definition.Version);
                }

                var parameterKeys = ruleSet.Parameters.Select(parameter => parameter.Key).ToHashSet();
                foreach (var parameter in definition.Parameters.Where(item => !parameterKeys.Contains(item.Key)))
                {
                    ruleSet.Parameters.Add(new RuleParameter
                    {
                        Key = parameter.Key,
                        Value = parameter.Value,
                        ValueType = parameter.ValueType,
                        Description = parameter.Description
                    });
                    logger.LogInformation(
                        "Reference rule parameter provisioned: {CalculatorSlug} / {Version} / {Key}",
                        definition.CalculatorSlug,
                        definition.Version,
                        parameter.Key);
                }
            }

            if (db.ChangeTracker.HasChanges())
                await db.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Reference data provisioning completed. Sources: {Sources}; Calculators: {Calculators}; RuleSets: {RuleSets}; Parameters: {Parameters}.",
                await db.Sources.CountAsync(cancellationToken),
                await db.Calculators.CountAsync(cancellationToken),
                await db.CalculationRuleSets.CountAsync(cancellationToken),
                await db.RuleParameters.CountAsync(cancellationToken));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Reference data provisioning failed.");
            throw new InvalidOperationException("Failed to provision required reference data.", exception);
        }
    }

    private sealed record CategoryDefinition(string Name, string Slug, string Description);
    private sealed record CalculatorDefinition(string Name, string Slug, string Description, string Category);
    private sealed record RuleSetDefinition(
        string CalculatorSlug,
        string Name,
        string Version,
        DateOnly ValidFrom,
        DateOnly? ValidUntil,
        bool IsActive,
        RuleParameterDefinition[] Parameters);
    private sealed record RuleParameterDefinition(
        string Key,
        string Value,
        ParameterValueType ValueType,
        string Description);
}
