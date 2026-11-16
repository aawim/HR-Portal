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
        HttpRequestMessage CreateRequest()
        {
            var req = new HttpRequestMessage(new HttpMethod(context.Request.Method),
                new Uri(origins.Api, context.Request.Path + context.Request.QueryString));
            if (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding"))
            {
                req.Content = new StreamContent(context.Request.Body);
                if (context.Request.ContentType is { } contentType) req.Content.Headers.TryAddWithoutValidation("Content-Type", contentType);
            }
            foreach (var name in new[] { "Cookie", "Accept", "Origin", "User-Agent" })
                if (context.Request.Headers.TryGetValue(name, out var value)) req.Headers.TryAddWithoutValidation(name, value.ToArray());
            // Never forward client-supplied forwarding headers or Host.
            req.Headers.Add("X-Forwarded-Host", origins.Public.Authority);
            req.Headers.Add("X-Forwarded-Proto", origins.Public.Scheme);
            return req;
        }

        HttpResponseMessage? response = null;
        var maxAttempts = HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method) ? 12 : 1;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            using var request = CreateRequest();
            try
            {
                response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
                break;
            }
            catch (HttpRequestException ex) when (attempt < maxAttempts &&
                ex.InnerException is System.Net.Sockets.SocketException { SocketErrorCode: System.Net.Sockets.SocketError.ConnectionRefused })
            {
                await Task.Delay(500, context.RequestAborted);
            }
        }
        using var finalResponse = response!;
        context.Response.StatusCode = (int)finalResponse.StatusCode;
        context.Response.Headers.CacheControl = "no-store";
        foreach (var name in new[] { "Location", "Set-Cookie", "WWW-Authenticate" })
            if (finalResponse.Headers.TryGetValues(name, out var values)) context.Response.Headers[name] = values.ToArray();
        if (finalResponse.Content.Headers.ContentType is { } type) context.Response.ContentType = type.ToString();
        if (context.Request.Method == "POST" && context.Request.Path.Equals("/Online/Auth/Logout", StringComparison.OrdinalIgnoreCase) &&
            (int)finalResponse.StatusCode is >= 200 and < 400) sessions.RemoveBrowserSessions(context.Request.Headers.Cookie.ToString());
        await finalResponse.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
    }
}
