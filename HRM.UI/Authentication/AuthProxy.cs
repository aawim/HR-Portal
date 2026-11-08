namespace HRM.UI.Authentication;

/// <summary>Only authentication routes are proxied; API operations stay server-to-server.</summary>
public static class AuthProxy
{
    private static readonly string[] Paths = ["/Online/Auth/login", "/Online/Auth/Callback", "/Online/Auth/Logout",
        "/Online/Auth/LogoutToken", "/Online/Auth/NoAccessLogout", "/signout-callback-oidc", "/signout-oidc"];

    public static void MapBackendAuthentication(this Microsoft.AspNetCore.Routing.IEndpointRouteBuilder app)
    {
        foreach (var path in Paths)
            app.MapMethods(path, ["GET", "POST"], ForwardAsync).AllowAnonymous();
    }

    private static async Task ForwardAsync(HttpContext context, IHttpClientFactory clients, BackendOrigins origins, BackendSessionStore sessions)
    {
        using var client = clients.CreateClient("backend");
        using var request = new HttpRequestMessage(new HttpMethod(context.Request.Method),
            new Uri(origins.Api, context.Request.Path + context.Request.QueryString));
        if (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
        {
            request.Content = new StreamContent(context.Request.Body);
            if (context.Request.ContentType is { } contentType) request.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
        }
        foreach (var name in new[] { "Cookie", "Accept", "Origin", "User-Agent" })
            if (context.Request.Headers.TryGetValue(name, out var value)) request.Headers.TryAddWithoutValidation(name, value.ToArray());
        // Never forward client-supplied forwarding headers or Host.
        request.Headers.Add("X-Forwarded-Host", origins.Public.Authority);
        request.Headers.Add("X-Forwarded-Proto", origins.Public.Scheme);
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
        context.Response.StatusCode = (int)response.StatusCode;
        context.Response.Headers.CacheControl = "no-store";
        foreach (var name in new[] { "Location", "Set-Cookie", "WWW-Authenticate" })
            if (response.Headers.TryGetValues(name, out var values)) context.Response.Headers[name] = values.ToArray();
        if (response.Content.Headers.ContentType is { } type) context.Response.ContentType = type.ToString();
        if (context.Request.Method == "POST" && context.Request.Path.Equals("/Online/Auth/Logout", StringComparison.OrdinalIgnoreCase) &&
            (int)response.StatusCode is >= 200 and < 400) sessions.RemoveBrowserSessions(context.Request.Headers.Cookie.ToString());
        await response.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
    }
}
