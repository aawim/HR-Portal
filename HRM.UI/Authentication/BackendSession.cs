using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using HRM.Application.Transport;
using Microsoft.Extensions.Caching.Memory;

namespace HRM.UI.Authentication;

/// <summary>Credentials stay on the UI server. Each session owns its cookie jar and token pair.</summary>
public sealed class BackendSession(HttpClient upstream, Uri origin, ILogger<BackendSession>? logger = null) : IDisposable
{
    private readonly CookieContainer cookies = new();
    private readonly object cookieLock = new();
    private readonly SemaphoreSlim gate = new(1, 1);
    private string? csrf;
    private volatile bool revoked;

    public void ImportCookies(string header)
    {
        foreach (var pair in header.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var equals = pair.IndexOf('=');
            if (equals <= 0) continue;
            try { cookies.Add(origin, new Cookie(pair[..equals], pair[(equals + 1)..], "/") { Secure = true, HttpOnly = true }); }
            catch (CookieException) { /* Ignore malformed browser cookies; the API authenticates the rest. */ }
        }
    }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        if (revoked) throw new UnauthorizedAccessException("Your session has ended. Please sign in again.");
        // Independent cards can load concurrently. Writes share token acquisition and are never retried.
        if (request.Method == HttpMethod.Get || request.Method == HttpMethod.Head) return await SendCoreAsync(request, ct);
        await gate.WaitAsync(ct);
        try
        {
            if (revoked) throw new UnauthorizedAccessException("Your session has ended. Please sign in again.");
            if (request.Method != HttpMethod.Get && request.Method != HttpMethod.Head)
            {
                if (csrf is null)
                {
                    using var tokenRequest = new HttpRequestMessage(HttpMethod.Get, "api/v1/session/antiforgery");
                    using var tokenResponse = await SendCoreAsync(tokenRequest, ct);
                    if (tokenResponse.StatusCode == HttpStatusCode.Unauthorized) throw new UnauthorizedAccessException("Please sign in again.");
                    tokenResponse.EnsureSuccessStatusCode();
                    var token = await tokenResponse.Content.ReadFromJsonAsync<CsrfToken>(ApiOperationKeys.Json, ct);
                    if (token?.HeaderName != "X-CSRF-TOKEN" || string.IsNullOrWhiteSpace(token.RequestToken))
                        throw new InvalidOperationException("The security token could not be obtained. Please sign in again.");
                    csrf = token.RequestToken;
                }
                request.Headers.Remove("X-CSRF-TOKEN");
                request.Headers.Add("X-CSRF-TOKEN", csrf);
            }
            var response = await SendCoreAsync(request, ct);
            if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized) csrf = null;
            return response;
        }
        finally { gate.Release(); }
    }

    private async Task<HttpResponseMessage> SendCoreAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var uri = request.RequestUri?.IsAbsoluteUri == true ? request.RequestUri : new Uri(origin, request.RequestUri!);
        if (uri.Scheme != origin.Scheme || uri.Authority != origin.Authority || !uri.AbsolutePath.StartsWith("/api/v1/", StringComparison.Ordinal))
            throw new InvalidOperationException("The backend destination is not permitted.");
        using var outbound = await CloneForBackendAsync(request, uri, ct);
        outbound.Headers.Remove("Cookie");
        string header;
        lock (cookieLock) header = cookies.GetCookieHeader(origin);
        if (header.Length > 0) outbound.Headers.TryAddWithoutValidation("Cookie", header);
        var started = System.Diagnostics.Stopwatch.StartNew();
        HttpResponseMessage response;
        try { response = await upstream.SendAsync(outbound, HttpCompletionOption.ResponseHeadersRead, ct); }
        catch (OperationCanceledException) when (revoked)
        {
            throw new UnauthorizedAccessException("Your session has ended. Please sign in again.");
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            logger?.LogWarning("Backend request {Method} {Path} cancelled after {ElapsedMs} ms; configured timeout {TimeoutSeconds} seconds",
                request.Method, uri.AbsolutePath, started.ElapsedMilliseconds, upstream.Timeout.TotalSeconds);
            throw new TimeoutException("The HR service did not respond in time. Please try again.");
        }
        if (response.Headers.TryGetValues("Set-Cookie", out var values))
            lock (cookieLock) foreach (var value in values) cookies.SetCookies(origin, value);
        return response;
    }

    // The incoming message has already been marked as sent by the circuit HttpClient. A message
    // cannot be passed through a second HttpClient, so create a private outbound copy for the API.
    private static async Task<HttpRequestMessage> CloneForBackendAsync(HttpRequestMessage source, Uri uri, CancellationToken ct)
    {
        var clone = new HttpRequestMessage(source.Method, uri)
        {
            Version = source.Version,
            VersionPolicy = source.VersionPolicy
        };
        foreach (var header in source.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        foreach (var option in source.Options)
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);
        if (source.Content is not null)
        {
            clone.Content = new ByteArrayContent(await source.Content.ReadAsByteArrayAsync(ct));
            foreach (var header in source.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }
        return clone;
    }

    public void Dispose() { revoked = true; upstream.Dispose(); }
    private sealed record CsrfToken(string RequestToken, string HeaderName);
}

/// <summary>Opaque handles are only placed in server-side principals, never browser storage or a ticket.</summary>
public sealed class BackendSessionStore : IDisposable
{
    public const string ClaimType = "hrm:backend-session";
    public const string RecoveryClaimType = "hrm:backend-session-recovery";
    private readonly MemoryCache cache = new(new MemoryCacheOptions { SizeLimit = 10000 });
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> credentials = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, byte> revoked = new();
    private readonly object restoreLock = new();
    public string Add(BackendSession session, string cookieHeader)
    {
        var handle = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        credentials[handle] = Fingerprint(cookieHeader);
        cache.Set(handle, session, new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8) }
            .RegisterPostEvictionCallback((key, value, _, _) => { credentials.TryRemove((string)key, out _); ((BackendSession?)value)?.Dispose(); }));
        return handle;
    }
    public BackendSession Require(ClaimsPrincipal user) => user.Identity?.IsAuthenticated == true &&
        user.FindFirst(ClaimType)?.Value is { } handle && cache.TryGetValue<BackendSession>(handle, out var session) && session is not null
            ? session : throw new UnauthorizedAccessException("Your session has ended. Please sign in again.");
    public BackendSession Restore(ClaimsPrincipal user, string cookieHeader, Func<BackendSession> create)
    {
        if (user.Identity?.IsAuthenticated != true || user.FindFirst(ClaimType)?.Value is not { Length: 64 } handle || revoked.ContainsKey(handle))
            throw new UnauthorizedAccessException("Your session has ended. Please sign in again.");
        lock (restoreLock)
        {
            if (revoked.ContainsKey(handle)) throw new UnauthorizedAccessException("Your session has ended. Please sign in again.");
            if (cache.TryGetValue<BackendSession>(handle, out var current) && current is not null) return current;
            var session = create();
            credentials[handle] = Fingerprint(cookieHeader);
            cache.Set(handle, session, new MemoryCacheEntryOptions { Size = 1, AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8) }
                .RegisterPostEvictionCallback((key, value, _, _) => { credentials.TryRemove((string)key, out _); ((BackendSession?)value)?.Dispose(); }));
            return session;
        }
    }
    public void Remove(ClaimsPrincipal user)
    {
        if (user.FindFirst(ClaimType)?.Value is not { } handle) return;
        revoked[handle] = 0;
        cache.Remove(handle);
    }
    public void RemoveBrowserSessions(string cookieHeader)
    {
        var fingerprint = Fingerprint(cookieHeader);
        foreach (var pair in credentials.Where(c => c.Value == fingerprint))
        {
            // Revoke immediately, including already-open circuits; eviction callbacks run asynchronously.
            revoked[pair.Key] = 0;
            if (cache.TryGetValue<BackendSession>(pair.Key, out var session)) session?.Dispose();
            cache.Remove(pair.Key);
        }
    }
    internal static string AuthenticationCookies(string header) => string.Join(";", header.Split(';', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(c => c.StartsWith(AuthenticationSessionDefaults.CookieName + "=", StringComparison.Ordinal) ||
                c.StartsWith(AuthenticationSessionDefaults.CookieName + "C", StringComparison.Ordinal) ||
                c.StartsWith(".AspNetCore.Cookies=", StringComparison.Ordinal) || c.StartsWith(".AspNetCore.CookiesC", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal));
    private static string Fingerprint(string header) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
        System.Text.Encoding.UTF8.GetBytes(AuthenticationCookies(header))));
    public void Dispose()
    {
        foreach (var handle in credentials.Keys) if (cache.TryGetValue<BackendSession>(handle, out var session)) session?.Dispose();
        cache.Dispose();
    }
}
