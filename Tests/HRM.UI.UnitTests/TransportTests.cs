using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using HRM.Application.Contracts;
using HRM.Application.Contracts.Pages;
using HRM.Application.Transport;
using HRM.Services.Contracts;
using HRM.UI.Authentication;
using HRM.UI.Services.Transport;
using Xunit;

namespace HRM.UI.UnitTests;

public sealed class TransportTests
{
    private static readonly Uri Origin = new("https://localhost:62840/");
    private sealed class RecordingHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
    }
    private sealed class SessionHandler(BackendSession session) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => session.SendAsync(request, ct);
    }
    private static HttpResponseMessage Json<T>(T value) => new(HttpStatusCode.OK) { Content = JsonContent.Create(value, options: ApiOperationKeys.Json) };

    [Fact]
    public void Every_supported_contract_operation_has_a_unique_fixed_route()
    {
        var count = 0;
        foreach (var contract in ApplicationContractCatalog.All)
        foreach (var method in contract.GetMethods().Where(m => m.GetCustomAttribute<ObsoleteAttribute>() is null))
        {
            var endpoint = ApiEndpointCatalog.Resolve(contract, method);
            Assert.StartsWith("/api/v1/", endpoint.Path); count++;
        }
        Assert.Equal(ApiEndpointCatalog.All.Count, count);
        Assert.Equal(count, ApiEndpointCatalog.All.Select(e => (e.Verb, e.Path)).Distinct().Count());
    }

    [Fact]
    public async Task Scalar_reads_use_the_typed_route_and_roundtrip_dates_independent_of_culture()
    {
        using var http = new HttpClient(new RecordingHandler(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("/api/v1/late-check-in-request/get-candidates", request.RequestUri!.AbsolutePath);
            var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(request.RequestUri.Query);
            Assert.Equal("2026-10-28T08:30:00.0000000", query["from"].ToString());
            Assert.Equal("a & b", query["kind"].ToString());
            return Task.FromResult(Json(Array.Empty<object>()));
        })) { BaseAddress = Origin };
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var client = ApiContractProxy.Create<LateCheckInRequestServiceContracts.IOperations>(new HttpApiOperationClient(http));
            await client.GetCandidatesAsync(new DateTime(2026, 10, 28, 8, 30, 0), new DateTime(2026, 10, 29), "a & b");
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Fact]
    public async Task Writes_wrap_named_arguments_and_accept_no_content()
    {
        using var http = new HttpClient(new RecordingHandler(async request =>
        {
            Assert.Equal("/api/v1/user-preference/save-font-size", request.RequestUri!.AbsolutePath);
            var body = await request.Content!.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            Assert.Equal(120, body.GetProperty("percent").GetInt32());
            Assert.Equal(12, body.GetProperty("expectedUserId").GetInt32());
            return new(HttpStatusCode.NoContent);
        })) { BaseAddress = Origin };
        await ApiContractProxy.Create<UserPreferenceServiceContracts.IOperations>(new HttpApiOperationClient(http)).SaveFontSizeAsync(120, 12);
    }

    [Fact]
    public async Task Structured_read_uses_post_query_and_preserves_filter()
    {
        using var http = new HttpClient(new RecordingHandler(async request =>
        {
            Assert.Equal(HttpMethod.Post, request.Method);
            Assert.Equal("/api/v1/attendance-request-queries/query/get-mine", request.RequestUri!.AbsolutePath);
            var body = await request.Content!.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            Assert.Equal("2026-10-01", body.GetProperty("filter").GetProperty("from").GetString());
            return Json(new AttendanceRequestPage([], []));
        })) { BaseAddress = Origin };
        var api = ApiContractProxy.Create<IAttendanceRequestQueries>(new HttpApiOperationClient(http));
        Assert.Empty((await api.GetMineAsync(new(new(2026, 10, 1), new(2026, 10, 28)))).Requests);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(26)]
    public async Task Position_save_resolves_registered_route_and_sends_termination_type(int? terminationType)
    {
        var sent=false;
        using var http=new HttpClient(new RecordingHandler(async request=>
        {
            sent=true;Assert.Equal(HttpMethod.Post,request.Method);Assert.Equal("/api/v1/staff-position-management/save",request.RequestUri!.AbsolutePath);
            var body=await request.Content!.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            Assert.Equal(820,body.GetProperty("staffId").GetInt32());Assert.Equal(terminationType.HasValue,body.GetProperty("remove").GetBoolean());
            var type=body.GetProperty("terminationRequestTypeId");
            if(terminationType.HasValue)Assert.Equal(terminationType.Value,type.GetInt32());else Assert.Equal(System.Text.Json.JsonValueKind.Null,type.ValueKind);
            return new(HttpStatusCode.NoContent);
        })){BaseAddress=Origin};
        var api=ApiContractProxy.Create<StaffPositionManagementServiceContracts.IOperations>(new HttpApiOperationClient(http));
        await api.SaveAsync(820,5,1,new(2026,10,1),new(2026,10,30),"Staff request",remove:terminationType.HasValue,terminationRequestTypeId:terminationType);
        Assert.True(sent);
    }

    [Fact]
    public async Task Business_failures_keep_their_typed_result()
    {
        using var http = new HttpClient(new RecordingHandler(_ => Task.FromResult(Json(HRM.DTOs.ServiceResult.Failed("Select an approved request and task."))))) { BaseAddress = Origin };
        var result = await ApiContractProxy.Create<IOutOfOfficeRequests>(new HttpApiOperationClient(http)).SubmitAsync(new());
        Assert.False(result.Success); Assert.Equal("Select an approved request and task.", result.Message);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    public async Task Authentication_errors_are_not_reported_as_success(HttpStatusCode status)
    {
        using var http = new HttpClient(new RecordingHandler(_ => Task.FromResult(new HttpResponseMessage(status)))) { BaseAddress = Origin };
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => ApiContractProxy.Create<UserPreferenceServiceContracts.IOperations>(new HttpApiOperationClient(http)).GetThemeAsync());
    }

    [Fact]
    public async Task Missing_backend_session_requests_one_full_authentication_recovery()
    {
        var recoveries = 0;
        using var http = new HttpClient(new RecordingHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            response.Headers.Add(CircuitBackendHandler.RecoveryHeader, "required");
            return Task.FromResult(response);
        })) { BaseAddress = Origin };
        using var client = new HttpApiOperationClient(http, () => recoveries++);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            ApiContractProxy.Create<UserPreferenceServiceContracts.IOperations>(client).GetThemeAsync());

        Assert.Equal(1, recoveries);
    }

    [Fact]
    public async Task Anonymous_unauthorized_response_does_not_start_efaas_session_recovery()
    {
        var recoveries = 0;
        using var http = new HttpClient(new RecordingHandler(_ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)))) { BaseAddress = Origin };
        using var client = new HttpApiOperationClient(http, () => recoveries++);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            ApiContractProxy.Create<UserPreferenceServiceContracts.IOperations>(client).GetThemeAsync());

        Assert.Equal(0, recoveries);
    }

    [Fact]
    public async Task Unknown_server_errors_do_not_render_upstream_details_or_retry_writes()
    {
        var calls = 0;
        using var http = new HttpClient(new RecordingHandler(_ => { calls++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError) { Content = new StringContent("secret exception <script>alert(1)</script>") }); })) { BaseAddress = Origin };
        var error = await Assert.ThrowsAsync<ApiOperationException>(() => ApiContractProxy.Create<UserPreferenceServiceContracts.IOperations>(new HttpApiOperationClient(http)).SaveFontSizeAsync(100, 1));
        Assert.DoesNotContain("secret", error.Message); Assert.Equal(1, calls);
    }
    [Fact]
    public async Task Busy_responses_have_a_clear_message_and_do_not_replay_mutations()
    {
        var calls=0;using var http=new HttpClient(new RecordingHandler(_=>{calls++;return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));})){BaseAddress=Origin};
        var error=await Assert.ThrowsAsync<ApiOperationException>(()=>ApiContractProxy.Create<UserPreferenceServiceContracts.IOperations>(new HttpApiOperationClient(http)).SaveFontSizeAsync(100,1));
        Assert.Equal("503",error.Code);Assert.Contains("temporarily busy",error.Message);Assert.Equal(1,calls);
    }

    [Fact]
    public async Task Cookie_and_csrf_pairs_are_private_to_each_user_and_writes_are_not_retried()
    {
        var calls = new List<(string Cookie, string? Token, string Path)>();
        using var http = new HttpClient(new RecordingHandler(request =>
        {
            var cookie = request.Headers.GetValues("Cookie").Single();
            var user = cookie.Contains("user-a", StringComparison.Ordinal) ? "a" : "b";
            var token = request.Headers.TryGetValues("X-CSRF-TOKEN", out var tokens) ? tokens.Single() : null;
            calls.Add((cookie, token, request.RequestUri!.AbsolutePath));
            if (request.RequestUri.AbsolutePath.EndsWith("antiforgery", StringComparison.Ordinal))
            {
                var result = Json(new { requestToken = "token-" + user, headerName = "X-CSRF-TOKEN" });
                result.Headers.Add("Set-Cookie", "csrf=" + user + "; path=/; secure; httponly"); return Task.FromResult(result);
            }
            Assert.Equal("token-" + user, token); Assert.Contains("csrf=" + user, cookie);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NoContent));
        })) { BaseAddress = Origin };
        using var a = new BackendSession(http, Origin); using var b = new BackendSession(http, Origin);
        a.ImportCookies(".AspNetCore.Cookies=user-a"); b.ImportCookies(".AspNetCore.Cookies=user-b");
        using var r1 = await a.SendAsync(new(HttpMethod.Post, "api/v1/user-preference/save-theme"), default);
        using var r2 = await b.SendAsync(new(HttpMethod.Post, "api/v1/user-preference/save-theme"), default);
        using var r3 = await a.SendAsync(new(HttpMethod.Post, "api/v1/user-preference/save-theme"), default);
        Assert.Equal(5, calls.Count);
        Assert.All(calls.Where(c => c.Cookie.Contains("user-a", StringComparison.Ordinal)), c => Assert.DoesNotContain("user-b", c.Cookie));
    }

    [Fact]
    public async Task Independent_card_reads_are_not_serialised_behind_other_reads()
    {
        var entered = 0;
        var both = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var http = new HttpClient(new RecordingHandler(async _ =>
        {
            if (Interlocked.Increment(ref entered) == 2) both.SetResult();
            await release.Task;
            return Json(true);
        })) { BaseAddress = Origin };
        using var session = new BackendSession(http, Origin);
        var first = session.SendAsync(new(HttpMethod.Get, "api/v1/user-context/get-user-id"), default);
        var second = session.SendAsync(new(HttpMethod.Get, "api/v1/user-context/get-job-id"), default);
        try { await both.Task.WaitAsync(TimeSpan.FromSeconds(5)); }
        finally { release.TrySetResult(); }
        using var response1 = await first; using var response2 = await second;
        Assert.Equal(2, entered);
    }

    [Fact]
    public async Task Credential_handler_rejects_redirects_to_other_origins_before_sending()
    {
        using var http = new HttpClient(new RecordingHandler(_ => throw new Exception("Must not send"))) { BaseAddress = Origin };
        using var session = new BackendSession(http, Origin); session.ImportCookies(".AspNetCore.Cookies=secret");
        await Assert.ThrowsAsync<InvalidOperationException>(() => session.SendAsync(new(HttpMethod.Get, "https://other.example/api/v1/session/identity"), default));
    }

    [Fact]
    public async Task Circuit_client_forwards_a_fresh_request_message_to_the_backend_client()
    {
        using var upstream = new HttpClient(new RecordingHandler(async request =>
        {
            if (request.RequestUri!.AbsolutePath == "/api/v1/session/antiforgery")
                return Json(new { requestToken = "test-token", headerName = "X-CSRF-TOKEN" });
            Assert.Equal("/api/v1/example", request.RequestUri!.AbsolutePath);
            Assert.Equal("test-token", request.Headers.GetValues("X-CSRF-TOKEN").Single());
            Assert.Equal("application/json; charset=utf-8", request.Content!.Headers.ContentType!.ToString());
            Assert.Equal("{\"value\":42}", await request.Content.ReadAsStringAsync());
            return Json(true);
        })) { BaseAddress = Origin };
        using var session = new BackendSession(upstream, Origin);
        using var circuit = new HttpClient(new SessionHandler(session)) { BaseAddress = Origin };
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/v1/example")
        {
            Content = JsonContent.Create(new { value = 42 })
        };

        using var response = await circuit.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData(".AspNetCore.Cookies")]
    [InlineData(AuthenticationSessionDefaults.CookieName)]
    public async Task Logout_revokes_all_browser_circuits_but_preserves_other_users(string cookieName)
    {
        using var store = new BackendSessionStore();
        BackendSession Session() => new(new HttpClient(new RecordingHandler(_ => Task.FromResult(Json(true)))), Origin);
        ClaimsPrincipal Principal(string handle) => new(new ClaimsIdentity([new Claim(BackendSessionStore.ClaimType, handle)], "Backend"));
        var a = Session(); var a2 = Session(); var b = Session();
        var pa = Principal(store.Add(a, cookieName + "=user-a; csrf=one"));
        var pa2 = Principal(store.Add(a2, "csrf=two; " + cookieName + "=user-a"));
        var pb = Principal(store.Add(b, cookieName + "=user-b"));
        store.RemoveBrowserSessions("csrf=three; " + cookieName + "=user-a");
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => a.SendAsync(new(HttpMethod.Get, "api/v1/session/identity"), default));
        Assert.Throws<UnauthorizedAccessException>(() => store.Require(pa)); Assert.Throws<UnauthorizedAccessException>(() => store.Require(pa2));
        Assert.Same(b, store.Require(pb));
    }

    [Fact]
    public void Protected_session_recovery_restores_a_missing_handle_but_never_a_revoked_handle()
    {
        using var store = new BackendSessionStore();
        ClaimsPrincipal Principal(string handle) => new(new ClaimsIdentity([new Claim(BackendSessionStore.ClaimType, handle)], "Backend"));
        var missingHandle = Convert.ToHexString(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
        var missing = Principal(missingHandle);
        var restored = store.Restore(missing, AuthenticationSessionDefaults.CookieName + "=valid", () =>
            new BackendSession(new HttpClient(new RecordingHandler(_ => Task.FromResult(Json(true)))), Origin));
        Assert.Same(restored, store.Require(missing));

        var revokedSession = new BackendSession(new HttpClient(new RecordingHandler(_ => Task.FromResult(Json(true)))), Origin);
        var revoked = Principal(store.Add(revokedSession, AuthenticationSessionDefaults.CookieName + "=revoked"));
        store.Remove(revoked);
        Assert.Throws<UnauthorizedAccessException>(() => store.Restore(revoked, AuthenticationSessionDefaults.CookieName + "=revoked", () =>
            new BackendSession(new HttpClient(new RecordingHandler(_ => Task.FromResult(Json(true)))), Origin)));
    }

    [Fact]
    public void Ui_assembly_has_no_api_or_ef_dependency()
    {
        Assert.DoesNotContain(typeof(HttpApiOperationClient).Assembly.GetReferencedAssemblies(), a => a.Name == "HRM.Api" || a.Name!.StartsWith("Microsoft.EntityFrameworkCore", StringComparison.Ordinal));
    }
}
