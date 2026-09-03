namespace CentralContabil.IntegrationTests;

public sealed class PostgreSqlFactAttribute : FactAttribute
{
    public PostgreSqlFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING")))
            Skip = "Configure TEST_CONNECTION_STRING para executar com PostgreSQL.";
    }
}
