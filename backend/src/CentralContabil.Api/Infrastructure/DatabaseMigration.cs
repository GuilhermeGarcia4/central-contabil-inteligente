using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace CentralContabil.Api.Infrastructure;

public static class DatabaseMigration
{
    public static async Task ApplyAsync(IServiceProvider services)
    {
        const int maxAttempts = 12;
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(nameof(DatabaseMigration));

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var scope = services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Database.MigrateAsync();
                if (attempt > 1)
                    logger.LogInformation("Conexao com o PostgreSQL restabelecida na tentativa {Attempt}.", attempt);
                return;
            }
            catch (Exception exception) when (
                attempt < maxAttempts &&
                (exception is DbException || exception is TimeoutException))
            {
                var delay = TimeSpan.FromSeconds(Math.Min(attempt, 5));
                logger.LogWarning(
                    "PostgreSQL indisponivel na inicializacao (tentativa {Attempt}/{MaxAttempts}). Nova tentativa em {DelaySeconds}s.",
                    attempt,
                    maxAttempts,
                    delay.TotalSeconds);
                await Task.Delay(delay);
            }
        }
    }
}
