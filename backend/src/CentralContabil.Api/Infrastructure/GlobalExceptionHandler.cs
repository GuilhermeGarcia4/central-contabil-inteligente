using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using CentralContabil.Api.Modules.Integrations.Application;
using System.Text.Json;

namespace CentralContabil.Api.Infrastructure;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = exception switch
        {
            BadHttpRequestException => (400, "Entrada inválida", environment.IsDevelopment() ? exception.Message : "O corpo da requisição é inválido."),
            FormatException => (400, "Entrada inválida", exception.Message),
            ExternalNotFoundException => (404, "Registro não encontrado", exception.Message),
            ExternalTransientException => (503, "Integração temporariamente indisponível", exception.Message),
            ExternalProviderException => (502, "Falha no provedor externo", exception.Message),
            _ => (500, "Erro interno", environment.IsDevelopment() ? exception.Message : "Ocorreu um erro inesperado.")
        };
        if (status >= 500) logger.LogError(exception, "Request failed with {Status} while processing {Method} {Path}", status, context.Request.Method, context.Request.Path);
        else logger.LogInformation("Request rejected with {Status} while processing {Method} {Path}: {Message}", status, context.Request.Method, context.Request.Path, exception.Message);
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        if (status == 503) context.Response.Headers.RetryAfter = "30";
        await JsonSerializer.SerializeAsync(context.Response.Body, problem, cancellationToken: cancellationToken);
        return true;
    }
}
