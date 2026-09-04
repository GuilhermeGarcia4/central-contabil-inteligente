using CentralContabil.Api.Modules.Shared.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CentralContabil.Api.Infrastructure;

public static class DevelopmentSeed
{
    public static async Task ApplyAsync(IServiceProvider services, IConfiguration config)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();

        // Defesa adicional: este seed nunca deve produzir dados fora de Development.
        if (!environment.IsDevelopment()) return;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var calculators = await db.Calculators.ToListAsync();
        foreach (var calculator in calculators) {
            if (await db.CalculationRuleSets.AnyAsync(rule => rule.CalculatorId == calculator.Id && rule.Version == "dev-1")) continue;

            var rs = new CalculationRuleSet { CalculatorId = calculator.Id, Name = $"{calculator.Name} — regra de desenvolvimento", Version = "dev-1", ValidFrom = today.AddYears(-1), IsActive = true };
            if (calculator.Slug == "ferias") rs.Parameters.Add(new RuleParameter { Key = "AdditionalVacationPercentage", Value = "0.333333", ValueType = ParameterValueType.Decimal, Description = "TODO: validar regra e fonte oficial antes da produção" });
            db.CalculationRuleSets.Add(rs);
        }
        if (db.ChangeTracker.HasChanges())
            await db.SaveChangesAsync();
        if (!await db.Articles.AnyAsync()) {
            var category = await db.Categories.FirstAsync(x => x.Slug == "financeiro");
            db.Articles.Add(new Article { Title = "[CONTEÚDO DE DEMONSTRAÇÃO] Entenda os juros compostos", Slug = "entenda-juros-compostos", Summary = "Uma introdução simples para testar a plataforma.", SimpleContent = "Juros compostos fazem o saldo acumulado participar do cálculo dos períodos seguintes.", TechnicalContent = "Modelo discreto: M = C(1+i)^n. Aportes periódicos exigem considerar a convenção de início ou fim do período.", CategoryId = category.Id, Status = ArticleStatus.Published, ReviewStatus = ReviewStatus.Pending, PublishedAt = DateTimeOffset.UtcNow, NeedsReviewAt = DateTimeOffset.UtcNow.AddMonths(3) });
            await db.SaveChangesAsync();
        }
        var email = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? config["ADMIN_EMAIL"];
        var password = Environment.GetEnvironmentVariable("ADMIN_INITIAL_PASSWORD") ?? config["ADMIN_INITIAL_PASSWORD"];
        if (!string.IsNullOrWhiteSpace(email) && !string.IsNullOrWhiteSpace(password)) {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var admin = await users.FindByEmailAsync(email);
            if (admin is null) { admin = new ApplicationUser { UserName = email, Email = email, DisplayName = "Administrador" }; var result = await users.CreateAsync(admin, password); if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description))); }
            else if (config.GetValue<bool>("ADMIN_RESET_PASSWORD_ON_START")) {
                // Ferramenta explícita e exclusiva de Development para recuperar o admin local,
                // inclusive quando uma senha legada não atende mais à política atual.
                admin.PasswordHash = users.PasswordHasher.HashPassword(admin, password);
                admin.SecurityStamp = Guid.NewGuid().ToString();
                var result = await users.UpdateAsync(admin);
                if (!result.Succeeded) throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
                var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DevelopmentSeed));
                logger.LogWarning("A senha do administrador de desenvolvimento foi sincronizada com ADMIN_INITIAL_PASSWORD. Use uma senha forte e desative ADMIN_RESET_PASSWORD_ON_START após a validação.");
            }
            await UserRoleProvisioning.EnsureRoleAsync(users, admin, ApplicationRoles.Admin);
        }
    }

}
