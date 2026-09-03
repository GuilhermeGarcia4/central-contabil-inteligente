using CentralContabil.Api.Modules.AI.Application;
using CentralContabil.Api.Modules.AI.Infrastructure;
using CentralContabil.Api.Modules.Knowledge.Application;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using System.Net;
using System.Text;
using System.Text.Json;

namespace CentralContabil.UnitTests;

public sealed class AiProviderTests
{
    [Theory]
    [InlineData("gemini", AiProviderKind.Gemini)]
    [InlineData("openai", AiProviderKind.OpenAi)]
    [InlineData("openai-compatible", AiProviderKind.OpenAi)]
    public void Selects_configured_provider(string value, AiProviderKind expected)
    {
        var settings = AiProviderSettings.From(Config(("AI_PROVIDER", value)));
        Assert.Equal(expected, settings.Provider);
    }

    [Fact]
    public void Gemini_without_key_is_not_externally_configured()
    {
        var settings = AiProviderSettings.From(Config(("AI_PROVIDER", "gemini"), ("GEMINI_API_KEY", "")));
        Assert.False(settings.ChatConfigured);
        Assert.False(settings.EmbeddingConfigured);
    }

    [Fact]
    public void Invalid_provider_uses_invalid_state_instead_of_an_external_client() =>
        Assert.Equal(AiProviderKind.Invalid, AiProviderSettings.From(Config(("AI_PROVIDER", "unknown"))).Provider);

    [Fact]
    public async Task Gemini_chat_parses_official_generate_content_response()
    {
        var handler = new FakeHandler(_ => Json(HttpStatusCode.OK, """{"candidates":[{"content":{"parts":[{"text":"Resposta fundamentada"}]}}]}"""));
        var provider = new GeminiAssistantProvider(new FakeClientFactory(handler), GeminiConfig(), Environment());
        var answer = await provider.CompleteAsync(Context(), default);
        Assert.Equal("Resposta fundamentada", answer);
        Assert.Equal("generativelanguage.googleapis.com", handler.LastRequest?.RequestUri?.Host);
        Assert.True(handler.LastRequest?.Headers.Contains("x-goog-api-key"));
    }

    [Fact]
    public async Task Gemini_embedding_keeps_pgvector_dimension_and_rag_task_type()
    {
        var vector = string.Join(',', Enumerable.Repeat("0.125", 384));
        var handler = new FakeHandler(_ => Json(HttpStatusCode.OK, $"{{\"embedding\":{{\"values\":[{vector}]}}}}"));
        var provider = new GeminiEmbeddingProvider(new FakeClientFactory(handler), GeminiConfig());
        var result = await provider.GenerateEmbeddingAsync("consulta", EmbeddingPurpose.Query, default);
        Assert.Equal(384, result.Vector.Length);
        Assert.Equal("gemini", result.Provider);
        Assert.Contains("RETRIEVAL_QUERY", handler.LastBody);
        Assert.Contains("outputDimensionality", handler.LastBody);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task Api_error_falls_back_without_breaking_assistant(HttpStatusCode status)
    {
        var service = ChatRouter(new FakeHandler(_ => Json(status, "{}")));
        var answer = await service.CompleteAsync(Context(), default);
        Assert.Contains("Não encontrei informação suficiente", answer);
    }

    [Fact]
    public async Task Timeout_falls_back_without_breaking_assistant()
    {
        var service = ChatRouter(new FakeHandler(_ => throw new TaskCanceledException("timeout")));
        var answer = await service.CompleteAsync(Context(), default);
        Assert.Contains("Não encontrei informação suficiente", answer);
    }

    [Fact]
    public async Task Invalid_gemini_json_falls_back_without_breaking_assistant()
    {
        var service = ChatRouter(new FakeHandler(_ => Json(HttpStatusCode.OK, "{\"candidates\":[]}")));
        var answer = await service.CompleteAsync(Context(), default);
        Assert.Contains("Não encontrei informação suficiente", answer);
    }

    [Fact]
    public async Task Invalid_structured_output_is_rejected()
    {
        var handler = new FakeHandler(_ => Json(HttpStatusCode.OK, """{"candidates":[{"content":{"parts":[{"text":"not-json"}]}}]}"""));
        var provider = new GeminiAssistantProvider(new FakeClientFactory(handler), GeminiConfig(), Environment());
        using var schema = JsonDocument.Parse("""{"type":"object","properties":{"answer":{"type":"string"}},"required":["answer"]}""");
        await Assert.ThrowsAsync<JsonException>(() => provider.CompleteStructuredAsync<StructuredAnswer>("Responda em JSON.", "teste", schema.RootElement, default));
        Assert.Contains("responseMimeType", handler.LastBody);
    }

    [Fact]
    public async Task Embedding_api_error_falls_back_to_local_for_rag()
    {
        var config = GeminiConfig();
        var factory = new FakeClientFactory(new FakeHandler(_ => Json(HttpStatusCode.ServiceUnavailable, "{}")));
        var service = new ConfigurableEmbeddingService(
            new LocalEmbeddingService(config),
            new OpenAiEmbeddingProvider(factory, config),
            new GeminiEmbeddingProvider(factory, config),
            config,
            NullLogger<ConfigurableEmbeddingService>.Instance);
        var result = await service.GenerateEmbeddingAsync("férias", EmbeddingPurpose.Query, default);
        Assert.Equal("local", result.Provider);
        Assert.Equal(384, result.Vector.Length);
    }

    private static ConfigurableChatCompletionService ChatRouter(FakeHandler handler)
    {
        var config = GeminiConfig();
        var factory = new FakeClientFactory(handler);
        return new ConfigurableChatCompletionService(
            new GroundedTemplateChatCompletionService(),
            new OpenAiAssistantProvider(factory, config, Environment()),
            new GeminiAssistantProvider(factory, config, Environment()),
            config,
            NullLogger<ConfigurableChatCompletionService>.Instance);
    }

    private static GroundedContext Context() => new("Como funcionam as férias?", CentralContabil.Api.Modules.AI.Domain.AssistantIntent.LaborQuestion, [], null);
    private static IConfiguration GeminiConfig() => Config(
        ("AI_PROVIDER", "gemini"),
        ("GEMINI_API_KEY", "test-only-key"),
        ("GEMINI_MODEL", "gemini-3.7-flash"),
        ("GEMINI_EMBEDDING_MODEL", "gemini-embedding-2"),
        ("AI:EmbeddingDimensions", "384"));
    private static IConfiguration Config(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values.ToDictionary(x => x.Key, x => (string?)x.Value)).Build();
    private static IHostEnvironment Environment() => new FakeEnvironment
    {
        ContentRootPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "CentralContabil.Api"))
    };
    private static HttpResponseMessage Json(HttpStatusCode status, string body) => new(status)
    { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private sealed record StructuredAnswer(string Answer);
    private sealed class FakeClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    { public HttpClient CreateClient(string name) => new(handler, disposeHandler: false) { Timeout = TimeSpan.FromMilliseconds(100) }; }
    private sealed class FakeHandler(Func<HttpRequestMessage, HttpResponseMessage> response) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }
        public string LastBody { get; private set; } = "";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastBody = request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken);
            return response(request);
        }
    }
    private sealed class FakeEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
