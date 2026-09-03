using CentralContabil.Api.Modules.Integrations.Application;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CentralContabil.Api.Modules.Integrations.Infrastructure;

public sealed class ResilientHttpExecutor(IIntegrationHealthStore health, ILogger<ResilientHttpExecutor> logger)
{
    public async Task<T> GetAsync<T>(string provider, HttpClient client, string path, CancellationToken cancellationToken)
    {
        if (!health.CanExecute(provider)) throw new ExternalTransientException($"O circuito de {provider} está temporariamente aberto.");

        Exception? finalException = null;
        var stopwatch = Stopwatch.StartNew();
        for (var attempt = 1; attempt <= 2; attempt++)
        {
            try
            {
                logger.LogInformation("External request {Provider} GET {Path} attempt {Attempt}", provider, path, attempt);
                using var response = await client.GetAsync(path, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    health.RecordSuccess(provider, stopwatch.Elapsed);
                    throw new ExternalNotFoundException($"Registro não encontrado no provedor {provider}.");
                }

                if (IsTransient(response.StatusCode))
                {
                    finalException = new ExternalTransientException($"{provider} respondeu HTTP {(int)response.StatusCode}.");
                    if (attempt < 2)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(150 * attempt), cancellationToken);
                        continue;
                    }
                    break;
                }

                if (!response.IsSuccessStatusCode)
                    throw new ExternalProviderException($"{provider} rejeitou a consulta com HTTP {(int)response.StatusCode}.");

                var payload = await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
                    ?? throw new ExternalProviderException($"{provider} retornou uma resposta vazia.");
                health.RecordSuccess(provider, stopwatch.Elapsed);
                logger.LogInformation("External request {Provider} completed in {ElapsedMs} ms", provider, stopwatch.ElapsedMilliseconds);
                return payload;
            }
            catch (ExternalNotFoundException) { throw; }
            catch (ExternalProviderException exception)
            {
                health.RecordFailure(provider, stopwatch.Elapsed, exception);
                throw;
            }
            catch (JsonException exception)
            {
                var schemaException = new ExternalProviderException($"{provider} retornou um payload incompatível com o contrato esperado.", exception);
                health.RecordFailure(provider, stopwatch.Elapsed, schemaException);
                throw schemaException;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (HttpRequestException exception)
            {
                finalException = exception;
                if (attempt < 2)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(150 * attempt), cancellationToken);
                    continue;
                }
            }
            catch (TaskCanceledException exception)
            {
                finalException = exception;
                if (attempt < 2) continue;
            }
        }

        var transient = new ExternalTransientException($"{provider} está temporariamente indisponível.", finalException);
        health.RecordFailure(provider, stopwatch.Elapsed, transient);
        logger.LogWarning(finalException, "External request {Provider} failed after retries", provider);
        throw transient;
    }

    private static bool IsTransient(HttpStatusCode status) => status is HttpStatusCode.RequestTimeout or HttpStatusCode.TooManyRequests || (int)status >= 500;
}
