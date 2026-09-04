using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Shared.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;

namespace CentralContabil.UnitTests;

public sealed class ReferenceDataSeedTests
{
    [Fact]
    public async Task Empty_database_receives_trusted_reference_catalog_without_unvalidated_rules()
    {
        await using var services = CreateServices();

        await RunSeedAsync(services);

        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        Assert.Equal(4, await db.Categories.CountAsync());
        Assert.Equal(3, await db.Calculators.CountAsync());
        Assert.Empty(await db.CalculationRuleSets.ToListAsync());
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
