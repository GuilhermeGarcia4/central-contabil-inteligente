using Microsoft.AspNetCore.Http.Extensions;

namespace CentralContabil.Api.Infrastructure;

public static class GoogleOAuthConfiguration
{
    public const string CallbackPath = "/signin-google";

    public static string CallbackUrl(HttpRequest request, IConfiguration configuration)
    {
        var renderOrigin = RenderOrigin(configuration);
        return renderOrigin is not null
            ? new Uri(renderOrigin, CallbackPath).AbsoluteUri
            : UriHelper.BuildAbsolute(request.Scheme, request.Host, request.PathBase, CallbackPath);
    }

    public static void ApplyRenderOrigin(HttpRequest request, IConfiguration configuration)
    {
        if (!IsGoogleAuthenticationPath(request.Path)) return;

        var renderOrigin = RenderOrigin(configuration);
        if (renderOrigin is null) return;

        request.Scheme = renderOrigin.Scheme;
        request.Host = renderOrigin.IsDefaultPort
            ? new HostString(renderOrigin.Host)
            : new HostString(renderOrigin.Host, renderOrigin.Port);
    }

    private static bool IsGoogleAuthenticationPath(PathString path) =>
        path.StartsWithSegments("/api/v1/auth/google") || path.StartsWithSegments(CallbackPath);

    private static Uri? RenderOrigin(IConfiguration configuration)
    {
        var value = configuration["BACKEND_PUBLIC_ORIGIN"]
            ?? configuration["RENDER_EXTERNAL_URL"];
        return Uri.TryCreate(value, UriKind.Absolute, out var origin) &&
               (origin.Scheme == Uri.UriSchemeHttps || origin.Scheme == Uri.UriSchemeHttp)
            ? new Uri(origin.GetLeftPart(UriPartial.Authority))
            : null;
    }
}
