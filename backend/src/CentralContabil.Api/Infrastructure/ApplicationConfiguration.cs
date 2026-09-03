using Microsoft.Extensions.Configuration;
using Npgsql;

namespace CentralContabil.Api.Infrastructure;

public static class ApplicationConfiguration
{
    public static void AddDevelopmentEnvironmentFile(
        this ConfigurationManager configuration,
        IHostEnvironment environment)
    {
        if (!environment.IsDevelopment()) return;

        var envFile = FindFileUpwards(".env", environment.ContentRootPath)
            ?? FindFileUpwards(".env", Directory.GetCurrentDirectory());

        if (envFile is not null) AddEnvironmentFile(configuration, envFile);

        // User Secrets e variáveis reais do processo sempre vencem o arquivo local.
        configuration.AddUserSecrets<global::Program>(optional: true);
        configuration.AddEnvironmentVariables();
    }

    public static IConfigurationRoot BuildDesignTimeConfiguration()
    {
        var projectDirectory = FindDirectoryContaining(
                "appsettings.json",
                Directory.GetCurrentDirectory())
            ?? FindDirectoryContaining("appsettings.json", AppContext.BaseDirectory)
            ?? throw new InvalidOperationException("Could not locate the API appsettings.json file.");

        var environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? Environments.Development;

        var builder = new ConfigurationBuilder()
            .SetBasePath(projectDirectory)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile($"appsettings.{environmentName}.json", optional: true);

        if (string.Equals(environmentName, Environments.Development, StringComparison.OrdinalIgnoreCase))
        {
            var envFile = FindFileUpwards(".env", projectDirectory)
                ?? FindFileUpwards(".env", Directory.GetCurrentDirectory());
            if (envFile is not null) AddEnvironmentFile(builder, envFile);
        }

        return builder.AddEnvironmentVariables().Build();
    }

    public static string GetRequiredPostgreSqlConnection(this IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connection))
            connection = configuration["CONNECTION_STRING"];

        if (string.IsNullOrWhiteSpace(connection))
        {
            var database = configuration["POSTGRES_DB"];
            var username = configuration["POSTGRES_USER"];
            var password = configuration["POSTGRES_PASSWORD"];
            if (!string.IsNullOrWhiteSpace(database) &&
                !string.IsNullOrWhiteSpace(username) &&
                !string.IsNullOrWhiteSpace(password))
            {
                connection = new NpgsqlConnectionStringBuilder
                {
                    Host = "localhost",
                    Port = 5432,
                    Database = database,
                    Username = username,
                    Password = password
                }.ConnectionString;
            }
        }

        return !string.IsNullOrWhiteSpace(connection)
            ? connection
            : throw new InvalidOperationException(
                "Configure PostgreSQL using ConnectionStrings:Default " +
                "(ConnectionStrings__Default in environment variables) or CONNECTION_STRING.");
    }

    public static string GetRequiredJwtKey(this IConfiguration configuration)
    {
        var key = configuration["JWT_KEY"];
        if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
            throw new InvalidOperationException("JWT_KEY must contain at least 32 non-empty characters.");
        return key;
    }

    private static string? FindFileUpwards(string fileName, string startPath)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(startPath));
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, fileName);
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        return null;
    }

    private static string? FindDirectoryContaining(string fileName, string startPath)
    {
        var file = FindFileUpwards(fileName, startPath);
        return file is null ? null : Path.GetDirectoryName(file);
    }

    private static void AddEnvironmentFile(IConfigurationBuilder builder, string envFile)
    {
        var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var rawLine in File.ReadLines(envFile))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith('#')) continue;

            var separator = line.IndexOf('=');
            if (separator <= 0) continue;

            // Mantém a mesma convenção do provedor de variáveis de ambiente do .NET:
            // Authentication__Google__ClientId -> Authentication:Google:ClientId.
            var key = line[..separator].Trim().Replace("__", ":", StringComparison.Ordinal);
            var value = line[(separator + 1)..].Trim();
            if (value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') ||
                 (value[0] == '\'' && value[^1] == '\'')))
                value = value[1..^1];

            values[key] = value;
        }
        builder.AddInMemoryCollection(values);
    }
}
