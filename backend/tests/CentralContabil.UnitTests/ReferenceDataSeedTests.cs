using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Calculators.Application;
using CentralContabil.Api.Modules.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace CentralContabil.UnitTests;

public sealed class ReferenceDataSeedTests
{
    [Fact]
    public async Task Empty_database_receives_complete_existing_reference_data()
    {
        await using var services = CreateServices();

        await RunSeedAsync(services);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(4, await db.Categories.CountAsync());
        Assert.Equal(3, await db.Calculators.CountAsync());
        Assert.Equal(3, await db.CalculationRuleSets.CountAsync());
        Assert.Single(await db.RuleParameters.ToListAsync());
        Assert.Empty(await db.Sources.ToListAsync());
        Assert.Contains(await db.Calculators.ToListAsync(), item => item.Slug == "ferias");
        Assert.Contains(await db.Calculators.ToListAsync(), item => item.Slug == "decimo-terceiro");
        Assert.Contains(await db.Calculators.ToListAsync(), item => item.Slug == "juros-compostos");
    }

    [Fact]
    public async Task Running_reference_seed_again_does_not_duplicate_data()
    {
        await using var services = CreateServices();

        await RunSeedAsync(services);
        await RunSeedAsync(services);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(4, await db.Categories.CountAsync());
        Assert.Equal(3, await db.Calculators.CountAsync());
        Assert.Equal(3, await db.CalculationRuleSets.CountAsync());
        Assert.Single(await db.RuleParameters.ToListAsync());
    }

    [Fact]
    public async Task Existing_catalog_and_historical_rule_are_not_overwritten()
    {
        await using var services = CreateServices();
        Guid ruleId;
        using (var scope = services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var calculator = new Calculator
            {
                Name = "Nome administrado",
                Slug = "ferias",
                Description = "Descrição administrada",
                Category = "Administrada"
            };
            var rule = new CalculationRuleSet
            {
                Calculator = calculator,
                Name = "Histórica administrada",
                Version = "admin-2025",
                ValidFrom = new DateOnly(2025, 1, 1),
                ValidUntil = new DateOnly(2025, 12, 31),
                IsActive = false
            };
            db.Add(rule);
            await db.SaveChangesAsync();
            ruleId = rule.Id;
        }

        await RunSeedAsync(services);

        using var verificationScope = services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var calculatorAfterSeed = await verificationDb.Calculators.SingleAsync(item => item.Slug == "ferias");
        var ruleAfterSeed = await verificationDb.CalculationRuleSets.SingleAsync(item => item.Id == ruleId);
        Assert.Equal("Nome administrado", calculatorAfterSeed.Name);
        Assert.Equal("Descrição administrada", calculatorAfterSeed.Description);
        Assert.Equal("Histórica administrada", ruleAfterSeed.Name);
        Assert.Equal("admin-2025", ruleAfterSeed.Version);
        Assert.False(ruleAfterSeed.IsActive);
    }

    [Fact]
    public async Task Seeded_rules_are_selected_for_all_existing_calculators()
    {
        await using var services = CreateServices();
        await RunSeedAsync(services);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var selector = new CentralContabil.Api.Modules.Rules.Application.RuleSetSelector(db);
        var supportedDate = new DateOnly(2026, 9, 3);

        var vacationRule = await selector.GetActiveAsync("ferias", supportedDate, default);
        Assert.NotNull(vacationRule);
        Assert.NotNull(await selector.GetActiveAsync("decimo-terceiro", supportedDate, default));
        Assert.NotNull(await selector.GetActiveAsync("juros-compostos", supportedDate, default));

        var result = new VacationCalculator().Calculate(
            new VacationInput(3000m, 30, SellOneThird: true),
            vacationRule);
        Assert.Equal(5333.33m, result.Result);
    }

    private static ServiceProvider CreateServices()
    {
        var collection = new ServiceCollection();
        var databaseRoot = new InMemoryDatabaseRoot();
        var databaseName = Guid.NewGuid().ToString();
        collection.AddLogging();
        collection.AddDbContext<AppDbContext>(options =>
            options.UseInMemoryDatabase(databaseName, databaseRoot));
        collection.AddScoped<ReferenceDataSeed>();
        return collection.BuildServiceProvider();
    }

    private static async Task RunSeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ReferenceDataSeed>().SeedAsync();
    }
}
