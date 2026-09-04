using CentralContabil.Api.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace CentralContabil.UnitTests;

public sealed class GoogleOAuthConfigurationTests
{
    [Fact]
    public void Callback_uses_Render_backend_in_production()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["RENDER_EXTERNAL_URL"] = "https://central-contabil-api.onrender.com"
        }).Build();
        var context = new DefaultHttpContext();
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("frontend.netlify.app");

        var callback = GoogleOAuthConfiguration.CallbackUrl(context.Request, configuration);

        Assert.Equal("https://central-contabil-api.onrender.com/signin-google", callback);
    }

    [Fact]
    public void Callback_uses_actual_local_backend_in_development()
    {
        var configuration = new ConfigurationBuilder().Build();
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("localhost", 5042);

        var callback = GoogleOAuthConfiguration.CallbackUrl(context.Request, configuration);

        Assert.Equal("http://localhost:5042/signin-google", callback);
    }

    [Fact]
    public void Render_origin_is_applied_to_Google_authentication_paths()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["RENDER_EXTERNAL_URL"] = "https://central-contabil-api.onrender.com"
        }).Build();
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("internal", 10000);
        context.Request.Path = "/api/v1/auth/google/start";

        GoogleOAuthConfiguration.ApplyRenderOrigin(context.Request, configuration);

        Assert.Equal("https", context.Request.Scheme);
        Assert.Equal("central-contabil-api.onrender.com", context.Request.Host.Value);
    }

    [Fact]
    public void Render_origin_does_not_change_regular_api_requests()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["RENDER_EXTERNAL_URL"] = "https://central-contabil-api.onrender.com"
        }).Build();
        var context = new DefaultHttpContext();
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("internal", 10000);
        context.Request.Path = "/api/v1/articles";

        GoogleOAuthConfiguration.ApplyRenderOrigin(context.Request, configuration);

        Assert.Equal("http", context.Request.Scheme);
        Assert.Equal("internal:10000", context.Request.Host.Value);
    }
}
