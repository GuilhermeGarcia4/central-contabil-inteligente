using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Calculators.Application;
using CentralContabil.Api.Modules.Identity.Application;
using CentralContabil.Api.Modules.Rules.Application;
using CentralContabil.Api.Modules.Shared.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace CentralContabil.Api.Endpoints;

public static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapApi(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/v1");
        MapAuth(api); MapPublic(api); MapCalculations(api); MapAccount(api); MapAdmin(api);
        return app;
    }

    private static void MapAuth(RouteGroupBuilder api)
    {
        var auth = api.MapGroup("/auth").WithTags("Authentication");
        auth.MapPost("/register", async (RegisterRequest body, UserManager<ApplicationUser> users) => {
            if (string.IsNullOrWhiteSpace(body.DisplayName) || string.IsNullOrWhiteSpace(body.Email) || body.Password.Length < 10)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["registration"] = ["Nome, e-mail e senha com ao menos 10 caracteres são obrigatórios."] });
            var user = new ApplicationUser { UserName = body.Email.Trim(), Email = body.Email.Trim(), DisplayName = body.DisplayName.Trim() };
            var result = await users.CreateAsync(user, body.Password);
            if (!result.Succeeded) return Results.ValidationProblem(result.Errors.GroupBy(x => x.Code).ToDictionary(x => x.Key, x => x.Select(e => e.Description).ToArray()));
            await users.AddToRoleAsync(user, "User");
            return Results.Created("/api/v1/account", new { user.Id, user.Email, user.DisplayName });
        }).AllowAnonymous();
        auth.MapPost("/login", async (LoginRequest body, HttpContext http, UserManager<ApplicationUser> users, AppDbContext db, TokenService tokens) => {
            var user = await users.FindByEmailAsync(body.Email.Trim());
            if (user is null || await users.IsLockedOutAsync(user) || !await users.CheckPasswordAsync(user, body.Password)) {
                if (user is not null && !await users.IsLockedOutAsync(user)) await users.AccessFailedAsync(user);
                return Results.Problem(statusCode: 401, title: "Credenciais inválidas");
            }
            await users.ResetAccessFailedCountAsync(user);
            var issued = tokens.Issue(user, await users.GetRolesAsync(user));
            db.RefreshTokens.Add(new RefreshToken { UserId = user.Id, TokenHash = tokens.Hash(issued.RefreshToken), ExpiresAt = DateTimeOffset.UtcNow.AddDays(7) });
            await db.SaveChangesAsync();
            SetRefreshCookie(http, issued.RefreshToken);
            return Results.Ok(new { issued.AccessToken, issued.ExpiresAt, user = new { user.Id, user.Email, user.DisplayName } });
        }).AllowAnonymous().RequireRateLimiting("auth");
        auth.MapPost("/refresh", async (HttpContext http, AppDbContext db, UserManager<ApplicationUser> users, TokenService tokens) => {
            if (!http.Request.Cookies.TryGetValue("refresh_token", out var raw)) return Results.Unauthorized();
            var stored = await db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokens.Hash(raw) && x.RevokedAt == null && x.ExpiresAt > DateTimeOffset.UtcNow);
            if (stored is null) return Results.Unauthorized();
            stored.RevokedAt = DateTimeOffset.UtcNow;
            var user = await users.FindByIdAsync(stored.UserId.ToString()); if (user is null) return Results.Unauthorized();
            var issued = tokens.Issue(user, await users.GetRolesAsync(user));
            db.RefreshTokens.Add(new RefreshToken { UserId = user.Id, TokenHash = tokens.Hash(issued.RefreshToken), ExpiresAt = DateTimeOffset.UtcNow.AddDays(7) });
            await db.SaveChangesAsync(); SetRefreshCookie(http, issued.RefreshToken);
            return Results.Ok(new { issued.AccessToken, issued.ExpiresAt });
        }).AllowAnonymous().RequireRateLimiting("auth");
        auth.MapPost("/logout", async (HttpContext http, AppDbContext db, TokenService tokens) => {
            if (http.Request.Cookies.TryGetValue("refresh_token", out var raw)) {
                var stored = await db.RefreshTokens.FirstOrDefaultAsync(x => x.TokenHash == tokens.Hash(raw) && x.RevokedAt == null);
                if (stored is not null) { stored.RevokedAt = DateTimeOffset.UtcNow; await db.SaveChangesAsync(); }
            }
            http.Response.Cookies.Delete("refresh_token", RefreshCookieOptions(http)); return Results.NoContent();
        }).AllowAnonymous();
        auth.MapGet("/google/status", (HttpContext http, IConfiguration config) => Results.Ok(new {
            enabled = GoogleIsConfigured(config),
            callbackUrl = GoogleIsConfigured(config) ? GoogleOAuthConfiguration.CallbackUrl(http.Request, config) : null
        })).AllowAnonymous();
        auth.MapGet("/google/start", (string? returnUrl, IConfiguration config) => {
            if (!GoogleIsConfigured(config)) return Results.Problem(statusCode: 503, title: "Login com Google ainda não configurado");
            var safeReturnUrl = SafeReturnUrl(returnUrl);
            var properties = new AuthenticationProperties {
                RedirectUri = $"/api/v1/auth/google/complete?returnUrl={Uri.EscapeDataString(safeReturnUrl)}"
            };
            return Results.Challenge(properties, ["Google"]);
        }).AllowAnonymous().RequireRateLimiting("auth");
        auth.MapGet("/google/complete", async (string? returnUrl, HttpContext http, IConfiguration config, UserManager<ApplicationUser> users, AppDbContext db, TokenService tokens) => {
            var safeReturnUrl = SafeReturnUrl(returnUrl);
            var external = await http.AuthenticateAsync("External");
            if (!external.Succeeded || external.Principal is null)
                return Results.Redirect(FrontendRedirect(config, "/entrar", "googleError=authentication_failed"));

            var email = external.Principal.FindFirstValue(ClaimTypes.Email)?.Trim();
            var providerKey = external.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(providerKey)) {
                await http.SignOutAsync("External");
                return Results.Redirect(FrontendRedirect(config, "/entrar", "googleError=email_unavailable"));
            }

            var user = await users.FindByLoginAsync("Google", providerKey) ?? await users.FindByEmailAsync(email);
            if (user is null) {
                var displayName = external.Principal.FindFirstValue(ClaimTypes.Name) ?? email.Split('@')[0];
                user = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, DisplayName = displayName };
                var created = await users.CreateAsync(user);
                if (!created.Succeeded) {
                    await http.SignOutAsync("External");
                    return Results.Redirect(FrontendRedirect(config, "/entrar", "googleError=account_creation_failed"));
                }
            }

            var linkedUser = await users.FindByLoginAsync("Google", providerKey);
            if (linkedUser is null) {
                var linked = await users.AddLoginAsync(user, new UserLoginInfo("Google", providerKey, "Google"));
                if (!linked.Succeeded) {
                    await http.SignOutAsync("External");
                    return Results.Redirect(FrontendRedirect(config, "/entrar", "googleError=account_link_failed"));
                }
            }
            var roles = await users.GetRolesAsync(user);
            if (roles.Count == 0) {
                await users.AddToRoleAsync(user, "User");
                roles = await users.GetRolesAsync(user);
            }
            var issued = tokens.Issue(user, roles);
            db.RefreshTokens.Add(new RefreshToken { UserId = user.Id, TokenHash = tokens.Hash(issued.RefreshToken), ExpiresAt = DateTimeOffset.UtcNow.AddDays(7) });
            await db.SaveChangesAsync();
            SetRefreshCookie(http, issued.RefreshToken);
            await http.SignOutAsync("External");
            return Results.Redirect(FrontendRedirect(config, safeReturnUrl, "googleLogin=success"));
        }).AllowAnonymous().RequireRateLimiting("auth");
    }

    private static void MapPublic(RouteGroupBuilder api)
    {
        api.MapGet("/categories", async (AppDbContext db, CancellationToken ct) => Results.Ok(await db.Categories.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.Slug, x.Description }).ToListAsync(ct))).WithTags("Content");
        api.MapGet("/articles", async (AppDbContext db, CancellationToken ct) => Results.Ok(await db.Articles.AsNoTracking().Where(x => x.Status == ArticleStatus.Published).OrderByDescending(x => x.PublishedAt).Select(x => new { x.Id, x.Title, x.Slug, x.Summary, category = x.Category.Name, x.PublishedAt, x.NeedsReviewAt }).ToListAsync(ct))).WithTags("Content");
        api.MapGet("/articles/{slug}", async (string slug, AppDbContext db, CancellationToken ct) => {
            var item = await db.Articles.AsNoTracking().Where(x => x.Slug == slug && x.Status == ArticleStatus.Published).Select(x => new { x.Id, x.Title, x.Slug, x.Summary, x.SimpleContent, x.TechnicalContent, category = x.Category.Name, x.PublishedAt, x.ContentVersion, sources = x.ArticleSources.Select(s => new { s.Source.Name, s.Source.Url, s.Source.IsOfficial }) }).FirstOrDefaultAsync(ct);
            return item is null ? Results.NotFound() : Results.Ok(item);
        }).WithTags("Content");
        api.MapGet("/calculators", async (AppDbContext db, CancellationToken ct) => Results.Ok(await db.Calculators.AsNoTracking().Where(x => x.IsActive).Select(x => new { x.Id, x.Name, x.Slug, x.Description, x.Category }).ToListAsync(ct))).WithTags("Calculators");
        api.MapGet("/search", async (string q, AppDbContext db, CancellationToken ct) => {
            q = q.Trim(); if (q.Length < 2) return Results.BadRequest(new { message = "Informe ao menos 2 caracteres." });
            var pattern = $"%{q}%";
            var articles = await db.Articles.AsNoTracking().Where(x => x.Status == ArticleStatus.Published && (EF.Functions.ILike(x.Title, pattern) || EF.Functions.ILike(x.Summary, pattern) || EF.Functions.ILike(x.SimpleContent, pattern) || EF.Functions.ILike(x.TechnicalContent, pattern))).Take(20).Select(x => new { x.Title, x.Slug, x.Summary }).ToListAsync(ct);
            var calculators = await db.Calculators.AsNoTracking().Where(x => x.IsActive && (EF.Functions.ILike(x.Name, pattern) || EF.Functions.ILike(x.Description, pattern))).Take(20).Select(x => new { x.Name, x.Slug, x.Description }).ToListAsync(ct);
            var categories = await db.Categories.AsNoTracking().Where(x => x.IsActive && EF.Functions.ILike(x.Name, pattern)).Take(20).Select(x => new { x.Name, x.Slug, x.Description }).ToListAsync(ct);
            return Results.Ok(new { articles, calculators, categories });
        }).WithTags("Search").RequireRateLimiting("public");
    }

    private static void MapCalculations(RouteGroupBuilder api)
    {
        var group = api.MapGroup("/calculations").WithTags("Calculations");
        group.MapPost("/compound-interest", (CompoundInterestInput body, HttpContext h, IRuleSetSelector selector, IEnumerable<IFinancialCalculator> calculators, AppDbContext db, CancellationToken ct) => Run("juros-compostos", body, h, selector, calculators, db, ct));
        group.MapPost("/vacation", (VacationInput body, HttpContext h, IRuleSetSelector selector, IEnumerable<IFinancialCalculator> calculators, AppDbContext db, CancellationToken ct) => Run("ferias", body, h, selector, calculators, db, ct));
        group.MapPost("/thirteenth-salary", (ThirteenthSalaryInput body, HttpContext h, IRuleSetSelector selector, IEnumerable<IFinancialCalculator> calculators, AppDbContext db, CancellationToken ct) => Run("decimo-terceiro", body, h, selector, calculators, db, ct));
    }

    private static async Task<IResult> Run(string slug, object input, HttpContext http, IRuleSetSelector selector, IEnumerable<IFinancialCalculator> calculators, AppDbContext db, CancellationToken ct)
    {
        var rs = await selector.GetActiveAsync(slug, DateOnly.FromDateTime(DateTime.UtcNow), ct);
        if (rs is null) return Results.Problem(statusCode: 422, title: "Nenhuma regra vigente", detail: "Não há conjunto de regras vigente e validado para a data informada.");
        try {
            var result = calculators.Single(x => x.Slug == slug).Calculate(input, rs);
            var calculatorId = rs.CalculatorId;
            Guid? userId = Guid.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier), out var parsed) ? parsed : null;
            if (userId is not null) { db.CalculationRuns.Add(new CalculationRun { CalculatorId = calculatorId, UserId = userId, RuleSetId = rs.Id, InputJson = JsonSerializer.Serialize(input), ResultJson = JsonSerializer.Serialize(result), ResultAmount = result.Result }); await db.SaveChangesAsync(ct); }
            return Results.Ok(result);
        } catch (ArgumentOutOfRangeException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["input"] = [ex.Message] }); }
    }

    private static void MapAccount(RouteGroupBuilder api)
    {
        var account = api.MapGroup("/account").RequireAuthorization().WithTags("Account");
        account.MapGet("/calculations", async (ClaimsPrincipal user, AppDbContext db, CancellationToken ct) => Results.Ok(await db.CalculationRuns.AsNoTracking().Where(x => x.UserId == UserId(user)).OrderByDescending(x => x.CalculatedAt).Select(x => new { x.Id, calculator = x.Calculator.Name, x.CalculatedAt, x.ResultAmount, ruleVersion = x.RuleSet.Version }).ToListAsync(ct)));
        account.MapGet("/favorites", async (ClaimsPrincipal user, AppDbContext db, CancellationToken ct) => Results.Ok(await db.Favorites.AsNoTracking().Where(x => x.UserId == UserId(user)).OrderByDescending(x => x.CreatedAt).Select(x => new { x.ArticleId, x.Article.Title, x.Article.Slug, x.CreatedAt }).ToListAsync(ct)));
        account.MapPost("/favorites/{articleId:guid}", async (Guid articleId, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) => { if (!await db.Articles.AnyAsync(x => x.Id == articleId && x.Status == ArticleStatus.Published, ct)) return Results.NotFound(); db.Favorites.Add(new Favorite { UserId = UserId(user), ArticleId = articleId }); await db.SaveChangesAsync(ct); return Results.NoContent(); });
        account.MapDelete("/favorites/{articleId:guid}", async (Guid articleId, ClaimsPrincipal user, AppDbContext db, CancellationToken ct) => { var item = await db.Favorites.FirstOrDefaultAsync(x => x.ArticleId == articleId && x.UserId == UserId(user), ct); if (item is null) return Results.NotFound(); db.Favorites.Remove(item); await db.SaveChangesAsync(ct); return Results.NoContent(); });
    }

    private static void MapAdmin(RouteGroupBuilder api)
    {
        var admin = api.MapGroup("/admin").RequireAuthorization("AdminOnly").WithTags("Admin");
        admin.MapGet("/dashboard", async (AppDbContext db, CancellationToken ct) => Results.Ok(new {
            totalArticles = await db.Articles.CountAsync(ct), publishedArticles = await db.Articles.CountAsync(x => x.Status == ArticleStatus.Published, ct), reviewArticles = await db.Articles.CountAsync(x => x.Status == ArticleStatus.Review, ct),
            needsReview = await db.Articles.CountAsync(x => x.NeedsReviewAt <= DateTimeOffset.UtcNow || x.Status == ArticleStatus.NeedsReview, ct), expiringIn30Days = await db.Articles.CountAsync(x => x.ValidUntil != null && x.ValidUntil <= DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)), ct),
            users = await db.Users.CountAsync(ct), calculations = await db.CalculationRuns.CountAsync(ct) }));
        admin.MapGet("/articles", async (AppDbContext db, CancellationToken ct) => Results.Ok(await db.Articles.AsNoTracking().Include(x => x.Category).OrderByDescending(x => x.UpdatedAt).ToListAsync(ct)));
        admin.MapPost("/articles", async (ArticleEditRequest body, ClaimsPrincipal user, HttpContext http, AppDbContext db, CancellationToken ct) => {
            var article = new Article { Title = body.Title, Slug = body.Slug, Summary = body.Summary, SimpleContent = body.SimpleContent, TechnicalContent = body.TechnicalContent, CategoryId = body.CategoryId, Status = body.Status, NeedsReviewAt = body.NeedsReviewAt, PublishedAt = body.Status == ArticleStatus.Published ? DateTimeOffset.UtcNow : null };
            db.Articles.Add(article); db.AuditLogs.Add(Log("Create", article, user, http, null, body)); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/admin/articles/{article.Id}", article);
        });
        admin.MapPut("/articles/{id:guid}", async (Guid id, ArticleEditRequest body, ClaimsPrincipal user, HttpContext http, AppDbContext db, CancellationToken ct) => {
            var article = await db.Articles.Include(x => x.Versions).FirstOrDefaultAsync(x => x.Id == id, ct); if (article is null) return Results.NotFound();
            var old = new { article.Title, article.SimpleContent, article.TechnicalContent, article.Status, article.ContentVersion };
            if (article.Status == ArticleStatus.Published && (article.SimpleContent != body.SimpleContent || article.TechnicalContent != body.TechnicalContent)) { article.Versions.Add(new ArticleVersion { Version = article.ContentVersion, Title = article.Title, SimpleContent = article.SimpleContent, TechnicalContent = article.TechnicalContent, CreatedBy = UserId(user), ChangeDescription = body.ChangeDescription ?? "Atualização de conteúdo" }); article.ContentVersion++; }
            article.Title = body.Title; article.Slug = body.Slug; article.Summary = body.Summary; article.SimpleContent = body.SimpleContent; article.TechnicalContent = body.TechnicalContent; article.CategoryId = body.CategoryId; article.Status = body.Status; article.NeedsReviewAt = body.NeedsReviewAt; article.UpdatedAt = DateTimeOffset.UtcNow;
            if (body.Status == ArticleStatus.Published && article.PublishedAt is null) article.PublishedAt = DateTimeOffset.UtcNow;
            db.AuditLogs.Add(Log("Update", article, user, http, old, body)); await db.SaveChangesAsync(ct); return Results.Ok(article);
        });
        admin.MapGet("/sources", async (AppDbContext db, CancellationToken ct) => Results.Ok(await db.Sources.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct)));
        admin.MapPost("/sources", async (SourceEditRequest body, ClaimsPrincipal user, HttpContext http, AppDbContext db, CancellationToken ct) => { var source = new Source { Name = body.Name, Url = body.Url, SourceType = body.SourceType, Publisher = body.Publisher, IsOfficial = body.IsOfficial, LastVerifiedAt = body.LastVerifiedAt }; db.Sources.Add(source); db.AuditLogs.Add(Audit("Create", source.Id, nameof(Source), user, http, body)); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/admin/sources/{source.Id}", source); });
        admin.MapPut("/sources/{id:guid}", async (Guid id, SourceEditRequest body, ClaimsPrincipal user, HttpContext http, AppDbContext db, CancellationToken ct) => { var source = await db.Sources.FindAsync([id], ct); if (source is null) return Results.NotFound(); source.Name = body.Name; source.Url = body.Url; source.SourceType = body.SourceType; source.Publisher = body.Publisher; source.IsOfficial = body.IsOfficial; source.LastVerifiedAt = body.LastVerifiedAt; db.AuditLogs.Add(Audit("Update", id, nameof(Source), user, http, body)); await db.SaveChangesAsync(ct); return Results.Ok(source); });
        admin.MapGet("/categories", async (AppDbContext db, CancellationToken ct) => Results.Ok(await db.Categories.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct)));
        admin.MapPost("/categories", async (CategoryEditRequest body, ClaimsPrincipal user, HttpContext http, AppDbContext db, CancellationToken ct) => { var category = new Category { Name = body.Name, Slug = body.Slug, Description = body.Description, IsActive = body.IsActive }; db.Categories.Add(category); db.AuditLogs.Add(Audit("Create", category.Id, nameof(Category), user, http, body)); await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/admin/categories/{category.Id}", category); });
        admin.MapPut("/categories/{id:guid}", async (Guid id, CategoryEditRequest body, ClaimsPrincipal user, HttpContext http, AppDbContext db, CancellationToken ct) => { var category = await db.Categories.FindAsync([id], ct); if (category is null) return Results.NotFound(); category.Name = body.Name; category.Slug = body.Slug; category.Description = body.Description; category.IsActive = body.IsActive; category.UpdatedAt = DateTimeOffset.UtcNow; db.AuditLogs.Add(Audit("Update", id, nameof(Category), user, http, body)); await db.SaveChangesAsync(ct); return Results.Ok(category); });
        admin.MapGet("/calculators", async (AppDbContext db, CancellationToken ct) => Results.Ok(await db.Calculators.AsNoTracking().OrderBy(x => x.Name).ToListAsync(ct)));
        admin.MapGet("/rules", async (AppDbContext db, CancellationToken ct) => Results.Ok(await db.CalculationRuleSets.AsNoTracking().Include(x => x.Calculator).Include(x => x.Parameters).Include(x => x.Source).ToListAsync(ct)));
        admin.MapGet("/audit", async (AppDbContext db, CancellationToken ct) => Results.Ok(await db.AuditLogs.AsNoTracking().OrderByDescending(x => x.CreatedAt).Take(200).ToListAsync(ct)));
    }

    private static Guid UserId(ClaimsPrincipal user) => Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static AuditLog Log(string action, Article entity, ClaimsPrincipal user, HttpContext http, object? oldValue, object newValue) => new() { Action = action, EntityType = nameof(Article), EntityId = entity.Id.ToString(), UserId = UserId(user), IpAddress = http.Connection.RemoteIpAddress?.ToString(), OldValues = oldValue is null ? null : JsonSerializer.Serialize(oldValue), NewValues = JsonSerializer.Serialize(newValue) };
    private static AuditLog Audit(string action, Guid id, string type, ClaimsPrincipal user, HttpContext http, object value) => new() { Action = action, EntityType = type, EntityId = id.ToString(), UserId = UserId(user), IpAddress = http.Connection.RemoteIpAddress?.ToString(), NewValues = JsonSerializer.Serialize(value) };
    private static void SetRefreshCookie(HttpContext http, string token)
    {
        var options = RefreshCookieOptions(http);
        options.MaxAge = TimeSpan.FromDays(7);
        http.Response.Cookies.Append("refresh_token", token, options);
    }

    private static bool GoogleIsConfigured(IConfiguration config) =>
        !string.IsNullOrWhiteSpace(config["Authentication:Google:ClientId"]) &&
        !string.IsNullOrWhiteSpace(config["Authentication:Google:ClientSecret"]);

    private static string SafeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl) || !returnUrl.StartsWith('/') || returnUrl.StartsWith("//") || returnUrl.Contains('\\'))
            return "/minha-conta";
        return Uri.TryCreate(returnUrl, UriKind.Relative, out _) ? returnUrl : "/minha-conta";
    }

    private static string FrontendRedirect(IConfiguration config, string path, string query)
    {
        var separator = path.Contains('?') ? '&' : '?';
        return $"{FrontendOrigin(config)}{path}{separator}{query}";
    }

    private static string FrontendOrigin(IConfiguration config) =>
        (config["FRONTEND_ORIGIN"] ?? config["FrontendOrigin"] ?? "http://localhost:4200").TrimEnd('/');

    private static CookieOptions RefreshCookieOptions(HttpContext http)
    {
        var environment = http.RequestServices.GetRequiredService<IHostEnvironment>();
        return new CookieOptions
        {
            HttpOnly = true,
            // Acesso LAN em Development usa HTTP. Production continua exigindo HTTPS.
            Secure = !environment.IsDevelopment(),
            // Netlify e Render sao sites distintos em Production; o refresh precisa
            // acompanhar a requisicao CORS feita pelo Angular depois do redirect final.
            SameSite = environment.IsDevelopment() ? SameSiteMode.Strict : SameSiteMode.None,
            Path = "/api/v1/auth"
        };
    }
}

public sealed record RegisterRequest(string DisplayName, string Email, string Password);
public sealed record LoginRequest(string Email, string Password);
public sealed record ArticleEditRequest(string Title, string Slug, string Summary, string SimpleContent, string TechnicalContent, Guid CategoryId, ArticleStatus Status, DateTimeOffset? NeedsReviewAt, string? ChangeDescription);
public sealed record SourceEditRequest(string Name, string Url, SourceType SourceType, string Publisher, bool IsOfficial, DateTimeOffset? LastVerifiedAt);
public sealed record CategoryEditRequest(string Name, string Slug, string Description, bool IsActive);
