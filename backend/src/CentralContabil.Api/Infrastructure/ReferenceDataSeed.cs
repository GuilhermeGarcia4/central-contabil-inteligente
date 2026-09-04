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

    public async Task SeedAsync(CancellationToken cancellationToken = default)
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

        if (provisionedCategories.Count == 0 && provisionedCalculators.Count == 0) return;

        await db.SaveChangesAsync(cancellationToken);

        foreach (var slug in provisionedCategories)
            logger.LogInformation("Reference category provisioned: {Slug}", slug);
        foreach (var slug in provisionedCalculators)
            logger.LogInformation("Reference calculator provisioned: {Slug}", slug);
    }

    private sealed record CategoryDefinition(string Name, string Slug, string Description);
    private sealed record CalculatorDefinition(string Name, string Slug, string Description, string Category);
}
