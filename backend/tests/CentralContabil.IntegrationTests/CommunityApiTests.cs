using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CentralContabil.Api.Infrastructure;
using CentralContabil.Api.Modules.Shared.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CentralContabil.IntegrationTests;

public sealed class CommunityApiTests
{
    [PostgreSqlFact]
    public async Task Visitors_members_and_moderators_follow_community_permissions()
    {
        var connection = Environment.GetEnvironmentVariable("TEST_CONNECTION_STRING")!;
        Environment.SetEnvironmentVariable("CONNECTION_STRING", connection);
        Environment.SetEnvironmentVariable("JWT_KEY", "community-integration-key-long-enough-123456789");
        Environment.SetEnvironmentVariable("ADMIN_RESET_PASSWORD_ON_START", "false");
        await using var factory = new WebApplicationFactory<Program>();
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
        await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
        foreach (var role in ApplicationRoles.Required)
            if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new IdentityRole<Guid>(role));

        var tag = Guid.NewGuid().ToString("N");
        var member = await CreateUser(users, $"community-member-{tag}@example.test", "Membro teste");
        var moderator = await CreateUser(users, $"community-moderator-{tag}@example.test", "Moderação teste");
        var admin = await CreateUser(users, $"community-admin-{tag}@example.test", "Admin teste");
        await users.AddToRoleAsync(admin, ApplicationRoles.Admin);

        using var visitor = factory.CreateClient();
        Assert.Equal(HttpStatusCode.OK, (await visitor.GetAsync("/api/v1/community")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.PostAsJsonAsync("/api/v1/community", new { title = "Dúvida", content = "Texto", category = "Geral" })).StatusCode);

        using var memberClient = factory.CreateClient();
        var discussionId = Guid.Empty;
        try
        {
        await SignIn(memberClient, member.Email!, "Strong!Pass123");
        var created = await memberClient.PostAsJsonAsync("/api/v1/community", new { title = "Como organizar as notas?", content = "Qual rotina ajuda a separar documentos?", category = "Contabilidade" });
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var createdJson = await created.Content.ReadFromJsonAsync<JsonElement>();
        discussionId = createdJson.GetProperty("id").GetGuid();
        Assert.Equal("Membro", createdJson.GetProperty("authorRole").GetString());
        Assert.DoesNotContain(member.Email!, createdJson.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(HttpStatusCode.BadRequest, (await memberClient.PostAsJsonAsync("/api/v1/community", new { title = "Dados", content = "Meu CPF é 529.982.247-25", category = "Geral" })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await memberClient.PostAsJsonAsync("/api/v1/community", new { title = "HTML", content = "<script>alert(1)</script>", category = "Geral" })).StatusCode);

        var outsider = await CreateUser(users, $"community-outsider-{tag}@example.test", "Outro membro");
        using var outsiderClient = factory.CreateClient();
        await SignIn(outsiderClient, outsider.Email!, "Strong!Pass123");
        Assert.Equal(HttpStatusCode.OK, (await outsiderClient.PostAsJsonAsync($"/api/v1/community/{discussionId}/useful", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await outsiderClient.PutAsJsonAsync($"/api/v1/community/{discussionId}", new { title = "Alterado", content = "Tentativa indevida.", category = "Geral" })).StatusCode);

        var ownReply = await memberClient.PostAsJsonAsync($"/api/v1/community/{discussionId}/comments", new { content = "Guarde os comprovantes por mês." });
        Assert.Equal(HttpStatusCode.Created, ownReply.StatusCode);
        var commentId = (await ownReply.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var listing = await visitor.GetFromJsonAsync<JsonElement>("/api/v1/community");
        Assert.Equal(1, listing.GetProperty("items")[0].GetProperty("commentCount").GetInt32());

        using var moderatorClient = factory.CreateClient();
        using var adminClient = factory.CreateClient();
        await SignIn(adminClient, admin.Email!, "Strong!Pass123");
        var assignment = await adminClient.PutAsJsonAsync("/api/v1/community/moderators", new { email = moderator.Email, enabled = true });
        Assert.Equal(HttpStatusCode.OK, assignment.StatusCode);
        await SignIn(moderatorClient, moderator.Email!, "Strong!Pass123");
        var reports = await memberClient.PostAsJsonAsync($"/api/v1/community/comments/{commentId}/reports", new { reason = "Outro", details = "Resposta fora do contexto." });
        Assert.Equal(HttpStatusCode.Accepted, reports.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await moderatorClient.GetAsync("/api/v1/community/moderation/reports")).StatusCode);
        var report = (await reports.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await moderatorClient.PostAsJsonAsync($"/api/v1/community/moderation/reports/{report}/review", new { decision = "Hide", reason = "Conteúdo revisado." })).StatusCode);
        var detail = await visitor.GetFromJsonAsync<JsonElement>($"/api/v1/community/{discussionId}");
        Assert.Equal("Conteúdo removido.", detail.GetProperty("comments")[0].GetProperty("content").GetString());
        Assert.Equal(HttpStatusCode.OK, (await moderatorClient.GetAsync("/api/v1/community/moderation/actions")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await memberClient.GetAsync("/api/v1/community/moderation/reports")).StatusCode);
        }
        finally
        {
            if (discussionId != Guid.Empty)
            {
                var cleanup = await memberClient.DeleteAsync($"/api/v1/community/{discussionId}");
                Assert.True(cleanup.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.NotFound,
                    $"Could not clean up integration discussion {discussionId}: {(int)cleanup.StatusCode}.");
            }
        }
    }

    private static async Task<ApplicationUser> CreateUser(UserManager<ApplicationUser> users, string email, string displayName)
    {
        var user = new ApplicationUser { UserName = email, Email = email, DisplayName = displayName };
        Assert.True((await users.CreateAsync(user, "Strong!Pass123")).Succeeded);
        await UserRoleProvisioning.EnsureRoleAsync(users, user, ApplicationRoles.User);
        return user;
    }

    private static async Task SignIn(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", json.GetProperty("accessToken").GetString());
    }
}
