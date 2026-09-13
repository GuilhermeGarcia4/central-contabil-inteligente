using CentralContabil.Api.Endpoints;
using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Calculators.Application;
using CentralContabil.Api.Modules.Identity.Application;
using CentralContabil.Api.Modules.Rules.Application;
using CentralContabil.Api.Modules.Shared.Domain;
using CentralContabil.Api.Modules.Integrations.Application;
using CentralContabil.Api.Modules.Integrations.Infrastructure;
using CentralContabil.Api.Modules.Search.Application;
using CentralContabil.Api.Modules.Knowledge.Application;
using CentralContabil.Api.Modules.Knowledge.Infrastructure;
using CentralContabil.Api.Modules.AI.Application;
using CentralContabil.Api.Modules.AI.Infrastructure;
using CentralContabil.Api.Modules.Finance.Application;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Text;
using System.Net.Http.Headers;
using System.Threading.RateLimiting;
using System.Security.Claims;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddDevelopmentEnvironmentFile(builder.Environment);
if (builder.Environment.IsDevelopment())
{
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole();
    builder.Logging.AddDebug();
}
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 1_048_576);
var connection = builder.Configuration.GetRequiredPostgreSqlConnection();
var jwtKey = builder.Configuration.GetRequiredJwtKey();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.ForwardLimit = 1;
    if (!builder.Environment.IsDevelopment())
    {
        // No Render, a conexao direta chega sempre pelo load balancer da plataforma.
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    }
});
builder.Services.ConfigureHttpJsonOptions(options => options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddDbContext<AppDbContext>(o => o.UseNpgsql(connection, npgsql =>
    npgsql.EnableRetryOnFailure(
        maxRetryCount: 8,
        maxRetryDelay: TimeSpan.FromSeconds(5),
        errorCodesToAdd: null)));
builder.Services.AddIdentityCore<ApplicationUser>(o => { o.Password.RequiredLength = 10; o.Password.RequireNonAlphanumeric = true; o.Password.RequireUppercase = true; o.User.RequireUniqueEmail = true; o.Lockout.MaxFailedAccessAttempts = 5; })
    .AddRoles<IdentityRole<Guid>>().AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();
builder.Services.AddScoped<IdentityStructuralSeed>();
builder.Services.AddScoped<ReferenceDataSeed>();
var authentication = builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(o => {
    o.TokenValidationParameters = new TokenValidationParameters { ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true, ValidIssuer = builder.Configuration["Jwt:Issuer"], ValidAudience = builder.Configuration["Jwt:Audience"], IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)), ClockSkew = TimeSpan.FromSeconds(30) };
}).AddCookie("External", options => {
    options.Cookie.Name = "central_contabil_external";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
});
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authentication.AddGoogle("Google", options =>
    {
        options.SignInScheme = "External";
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = GoogleOAuthConfiguration.CallbackPath;
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        if (builder.Environment.IsDevelopment() &&
            Uri.TryCreate(Environment.GetEnvironmentVariable("HTTPS_PROXY"), UriKind.Absolute, out var proxy) &&
            proxy.IsLoopback && proxy.Port == 9)
            options.BackchannelHttpHandler = new HttpClientHandler { UseProxy = false };
        options.Events.OnRemoteFailure = context =>
        {
            var logger = context.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("GoogleAuthentication");
            logger.LogWarning(context.Failure, "Falha no retorno do login Google");
            context.HandleResponse();
            var origin = (builder.Configuration["FRONTEND_ORIGIN"] ?? "http://localhost:4200").TrimEnd('/');
            context.Response.Redirect($"{origin}/entrar?googleError=remote_failure");
            return Task.CompletedTask;
        };
    });
}
builder.Services.AddAuthorization(o => o.AddPolicy("AdminOnly", p => p.RequireRole(ApplicationRoles.Admin)));
builder.Services.AddCors(o => o.AddPolicy("web", p => p.WithOrigins(builder.Configuration["FRONTEND_ORIGIN"] ?? builder.Configuration["FrontendOrigin"] ?? "http://localhost:4200").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("public", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1) }));
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown", _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
    options.AddPolicy("ai", context =>
    {
        var identity = context.User.Identity?.IsAuthenticated == true ? context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "authenticated" : context.Connection.RemoteIpAddress?.ToString() ?? "visitor";
        var limit = context.User.IsInRole(ApplicationRoles.Admin) ? 30 : context.User.Identity?.IsAuthenticated == true ? 15 : 5;
        return RateLimitPartition.GetFixedWindowLimiter(identity, _ => new FixedWindowRateLimiterOptions { PermitLimit = limit, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 });
    });
});
builder.Services.AddScoped<TokenService>(); builder.Services.AddScoped<IRuleSetSelector, RuleSetSelector>();
builder.Services.AddScoped<IFinancialCalculator, CompoundInterestCalculator>(); builder.Services.AddScoped<IFinancialCalculator, VacationCalculator>(); builder.Services.AddScoped<IFinancialCalculator, ThirteenthSalaryCalculator>();
builder.Services.AddSingleton<IIntegrationHealthStore, IntegrationHealthStore>();
builder.Services.AddSingleton<ResilientHttpExecutor>();
builder.Services.AddScoped<IExternalSnapshotCache, ExternalSnapshotCache>();
builder.Services.AddScoped<CompanyLookupService>();
builder.Services.AddScoped<NcmLookupService>();
builder.Services.AddScoped<UnifiedSearchService>();
builder.Services.AddSingleton<KnowledgeChunker>();
builder.Services.AddSingleton<LocalEmbeddingService>();
builder.Services.AddSingleton<OpenAiEmbeddingProvider>();
builder.Services.AddSingleton<GeminiEmbeddingProvider>();
builder.Services.AddSingleton<IEmbeddingService, ConfigurableEmbeddingService>();
builder.Services.AddScoped<KnowledgeIndexingService>();
builder.Services.AddScoped<HybridKnowledgeSearch>();
builder.Services.AddScoped<AssistantToolRouter>();
builder.Services.AddSingleton<GroundedTemplateChatCompletionService>();
builder.Services.AddSingleton<OpenAiAssistantProvider>();
builder.Services.AddSingleton<GeminiAssistantProvider>();
builder.Services.AddSingleton<IChatCompletionService, ConfigurableChatCompletionService>();
builder.Services.AddScoped<AssistantService>();
builder.Services.AddScoped<FinanceSummaryService>();
builder.Services.AddScoped<FinancialRecurrenceService>();
builder.Services.AddScoped<ICategorySuggestionService, RuleBasedCategorySuggestionService>();
builder.Services.AddScoped<FinancialInstallmentService>();
builder.Services.AddScoped<FinanceForecastService>();
builder.Services.AddScoped<FinancePlanningService>();
builder.Services.AddScoped<IFinancialExportService, ExcelFinancialExportService>();
builder.Services.AddScoped<CreditCardService>();
builder.Services.AddScoped<FinancialAccountService>();
builder.Services.AddScoped<ScheduledTransactionService>();
builder.Services.AddScoped<DebtService>();
builder.Services.AddScoped<EmergencyReserveService>();
builder.Services.AddScoped<NetWorthService>();
builder.Services.AddScoped<FinanceCalendarService>();
builder.Services.AddScoped<AnnualReportService>();
builder.Services.AddScoped<FinanceInsightService>();
builder.Services.AddScoped<FinanceOverviewService>();
builder.Services.AddScoped<FinancialAlertService>();
builder.Services.AddScoped<FinancialDuplicateDetectionService>();
builder.Services.AddScoped<FinancialImportService>();
builder.Services.AddScoped<UserPreferenceService>();
builder.Services.AddScoped<AssistantConversationService>();
builder.Services.AddScoped<FinancialAssistantTools>();
builder.Services.AddSingleton<KnowledgeIndexQueue>();
builder.Services.AddSingleton<IKnowledgeIndexQueue>(sp => sp.GetRequiredService<KnowledgeIndexQueue>());
builder.Services.AddHostedService<KnowledgeIndexingWorker>();
builder.Services.AddHttpClient("openai-provider", client => client.Timeout = TimeSpan.FromSeconds(25))
    .ConfigurePrimaryHttpMessageHandler(() => CreateAiHandler(builder.Environment));
builder.Services.AddHttpClient("gemini-provider", client => client.Timeout = TimeSpan.FromSeconds(25))
    .ConfigurePrimaryHttpMessageHandler(() => CreateAiHandler(builder.Environment));
builder.Services.AddHttpClient<BrasilApiCompanyProvider>((services, client) => ConfigureExternalClient(client,
    services.GetRequiredService<IConfiguration>()["Integrations:BrasilApi:BaseUrl"] ?? "https://brasilapi.com.br/api/"));
builder.Services.AddHttpClient<BrasilApiNcmProvider>((services, client) => ConfigureExternalClient(client,
    services.GetRequiredService<IConfiguration>()["Integrations:BrasilApi:BaseUrl"] ?? "https://brasilapi.com.br/api/"));
builder.Services.AddHttpClient<CnpjWsCompanyProvider>((services, client) => ConfigureExternalClient(client,
    services.GetRequiredService<IConfiguration>()["Integrations:CnpjWs:BaseUrl"] ?? "https://publica.cnpj.ws/"));
builder.Services.AddHttpClient<ChamberLegislationProvider>((services, client) => ConfigureExternalClient(client,
    services.GetRequiredService<IConfiguration>()["Integrations:Chamber:BaseUrl"] ?? "https://dadosabertos.camara.leg.br/api/v2/"));
builder.Services.AddHttpClient<SenateLegislationProvider>((services, client) => ConfigureExternalClient(client,
    services.GetRequiredService<IConfiguration>()["Integrations:Senate:BaseUrl"] ?? "https://legis.senado.leg.br/"));
builder.Services.AddScoped<ICompanyDataProvider>(sp => sp.GetRequiredService<BrasilApiCompanyProvider>());
builder.Services.AddScoped<ICompanyDataProvider>(sp => sp.GetRequiredService<CnpjWsCompanyProvider>());
builder.Services.AddScoped<INcmDataProvider>(sp => sp.GetRequiredService<BrasilApiNcmProvider>());
builder.Services.AddScoped<ILegislationProvider>(sp => sp.GetRequiredService<ChamberLegislationProvider>());
builder.Services.AddScoped<ILegislationProvider>(sp => sp.GetRequiredService<SenateLegislationProvider>());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o => { o.SwaggerDoc("v1", new OpenApiInfo { Title = "Central Contábil Inteligente API", Version = "v1" }); o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Name = "Authorization", In = ParameterLocation.Header, Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT" }); });

var app = builder.Build();
app.UseForwardedHeaders();
app.Use(async (context, next) =>
{
    GoogleOAuthConfiguration.ApplyRenderOrigin(context.Request, builder.Configuration);
    await next();
});
app.UseExceptionHandler();
app.Use(async (context, next) => { context.Response.Headers.XContentTypeOptions = "nosniff"; context.Response.Headers.XFrameOptions = "DENY"; context.Response.Headers.ContentSecurityPolicy = context.Request.Path.StartsWithSegments("/swagger") ? "default-src 'self'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; frame-ancestors 'none'" : "default-src 'none'; frame-ancestors 'none'"; await next(); });
if (app.Environment.IsDevelopment()) { app.UseSwagger(); app.UseSwaggerUI(); }
else app.UseHsts();
if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseCors("web"); app.UseRateLimiter(); app.UseAuthentication(); app.UseAuthorization();
app.MapGet("/health/live", () => Results.Ok(new { status = "healthy", utc = DateTimeOffset.UtcNow })).AllowAnonymous();
app.MapGet("/health", async (AppDbContext db, CancellationToken ct) =>
{
    var connected = await db.Database.CanConnectAsync(ct);
    return connected
        ? Results.Ok(new { status = "healthy", database = "connected", utc = DateTimeOffset.UtcNow })
        : Results.Json(new { status = "unhealthy", database = "disconnected", utc = DateTimeOffset.UtcNow }, statusCode: StatusCodes.Status503ServiceUnavailable);
}).AllowAnonymous();
app.MapApi(); app.MapV2Api(); app.MapV3Api(); app.MapFinanceApi(); app.MapFinanceV5Api(); app.MapFinanceV6Api(); app.MapFinanceV7Api(); app.MapFinanceV8Api();
await DatabaseMigration.ApplyAsync(app.Services);
using (var scope = app.Services.CreateScope())
{
    var identitySeed = scope.ServiceProvider.GetRequiredService<IdentityStructuralSeed>();
    await identitySeed.SeedAsync();

    var referenceDataSeed = scope.ServiceProvider.GetRequiredService<ReferenceDataSeed>();
    await referenceDataSeed.SeedAsync();
}
if (app.Environment.IsDevelopment())
    await DevelopmentSeed.ApplyAsync(app.Services, app.Configuration);
app.Run();

static void ConfigureExternalClient(HttpClient client, string baseUrl)
{
    client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    client.DefaultRequestHeaders.UserAgent.ParseAdd("CentralContabilInteligente/2.0");
}

static HttpMessageHandler CreateAiHandler(IHostEnvironment environment)
{
    var handler = new HttpClientHandler();
    if (environment.IsDevelopment() &&
        Uri.TryCreate(Environment.GetEnvironmentVariable("HTTPS_PROXY"), UriKind.Absolute, out var proxy) &&
        proxy.IsLoopback && proxy.Port == 9)
        handler.UseProxy = false;
    return handler;
}

public partial class Program;
