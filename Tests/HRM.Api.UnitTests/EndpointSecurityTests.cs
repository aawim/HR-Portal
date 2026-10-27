using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
using HRM.Api.Infrastructure;
using HRM.DTOs;
using HRM.Application.Contracts.Pages;
using HRM.DTOs.Attendance;
using HRM.Services.Interfaces;
using HRM.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;
namespace HRM.Api.UnitTests;

public sealed class EndpointSecurityTests
{
    private sealed class Fixture : IDisposable
    {
        public FakeAccess Access { get; } = new();
        public AttendanceStub Attendance { get; } = new();
        public WorkspaceStub Workspace { get; } = new();
        public TestServer Server { get; }
        public HttpClient Client { get; }
        public Fixture(bool realCookies = false, bool rootAuthentication = false)
        {
            Server = new TestServer(new WebHostBuilder().UseEnvironment("Testing").ConfigureServices(services =>
            {
                services.AddLogging(); services.AddDataProtection().UseEphemeralDataProtectionProvider(); services.AddHrmApiEndpoints(); services.AddAuthorization();
                if (realCookies)
                    services.AddAuthentication(options => { options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme; options.DefaultChallengeScheme = "external-identity-provider"; }).AddCookie();
                else
                    services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddScheme<AuthenticationSchemeOptions, TestAuthentication>(CookieAuthenticationDefaults.AuthenticationScheme, _ => { });
                services.AddSingleton<IUserAccessService>(Access);
                services.AddSingleton<IDbContextFactory<HrmTeContext>>(new QueryFactory());
                services.AddSingleton<IAttendanceRequestService>(Attendance);
                if (rootAuthentication)
                {
                    services.AddScoped<AuthenticationStateProvider, Microsoft.AspNetCore.Components.Server.ServerAuthenticationStateProvider>();
                    services.AddScoped<IAttendanceRequestService, StateCheckingAttendance>();
                }
                services.AddSingleton<IWorkspaceDirectoryService>(Workspace);
            }).Configure(app => { app.UseRouting(); app.UseAuthentication(); app.UseAuthorization(); app.UseEndpoints(e => e.MapControllers()); }));
            Client = Server.CreateClient(); Client.BaseAddress = new("https://localhost");
        }
        public void SignIn(string name = "alice") { Client.DefaultRequestHeaders.Remove("X-Test-User"); Client.DefaultRequestHeaders.Add("X-Test-User", name); }
        public async Task<string> TokenAsync()
        {
            using var response = await Client.GetAsync("/api/v1/session/antiforgery");
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var cookie = response.Headers.GetValues("Set-Cookie").Single().Split(';')[0];
            Client.DefaultRequestHeaders.Remove("Cookie"); Client.DefaultRequestHeaders.Add("Cookie", cookie);
            return json.RootElement.GetProperty("requestToken").GetString()!;
        }
        public void Dispose() { Client.Dispose(); Server.Dispose(); }
    }
    [Fact]
    public async Task Anonymous_api_request_returns_401_without_login_redirect()
    {
        using var fixture = new Fixture();
        using var response = await fixture.Client.GetAsync("/api/v1/attendance-request/get-date-info?date=2026-10-27");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
        Assert.Equal(0, fixture.Attendance.Reads);
    }
    [Fact]
    public async Task Actual_cookie_handler_returns_401_instead_of_challenging_the_external_provider()
    {
        using var fixture = new Fixture(realCookies: true);
        using var response = await fixture.Client.GetAsync("/api/v1/attendance-request/get-date-info?date=2026-10-27");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); Assert.Null(response.Headers.Location);
    }
    [Fact]
    public async Task Missing_employee_session_is_rejected_before_invoking_services()
    {
        using var fixture = new Fixture(); fixture.SignIn(); fixture.Access.Actor = null;
        using var response = await fixture.Client.GetAsync("/api/v1/attendance-request/get-date-info?date=2026-10-27");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode); Assert.Equal(0, fixture.Attendance.Reads);
    }
    [Fact]
    public async Task Signed_in_employee_can_read_typed_date_info_without_csrf_token()
    {
        using var fixture = new Fixture(); fixture.SignIn();
        using var response = await fixture.Client.GetAsync("/api/v1/attendance-request/get-date-info?date=2026-10-27");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Equal(1, fixture.Attendance.Reads);
        Assert.True((await response.Content.ReadFromJsonAsync<AttendanceRequestDateInfoDto>())!.CanSubmit);
        Assert.Contains("no-store", response.Headers.CacheControl!.ToString());
    }
    [Fact]
    public async Task Write_without_csrf_token_never_reaches_business_service()
    {
        using var fixture = new Fixture(); fixture.SignIn();
        using var response = await fixture.Client.PostAsJsonAsync("/api/v1/attendance-request/submit", new { model = new { date = "2026-10-27", time = "08:30:00", inOutModeId = 1, comments = "Correction" } });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal(0, fixture.Attendance.Writes);
    }
    [Fact]
    public async Task Write_with_paired_token_and_cookie_invokes_canonical_service_once()
    {
        using var fixture = new Fixture(); fixture.SignIn(); var token = await fixture.TokenAsync();
        fixture.Client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", token);
        using var response = await fixture.Client.PostAsJsonAsync("/api/v1/attendance-request/submit", new { model = new { date = "2026-10-27", time = "08:30:00", inOutModeId = 1, comments = "Correction" } });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); Assert.Equal(1, fixture.Attendance.Writes);
    }
    [Fact]
    public async Task Csrf_token_cannot_be_reused_by_another_identity()
    {
        using var fixture = new Fixture(); fixture.SignIn(); var token = await fixture.TokenAsync(); fixture.SignIn("bob");
        fixture.Client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", token);
        using var response = await fixture.Client.PostAsJsonAsync("/api/v1/attendance-request/submit", new { model = new { date = "2026-10-27", time = "08:30:00" } });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal(0, fixture.Attendance.Writes);
    }
    [Fact]
    public async Task Malformed_model_is_rejected_before_service_execution()
    {
        using var fixture = new Fixture(); fixture.SignIn(); var token = await fixture.TokenAsync(); fixture.Client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", token);
        using var response = await fixture.Client.PostAsJsonAsync("/api/v1/attendance-request/submit", new { model = (object?)null });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode); Assert.Equal(0, fixture.Attendance.Writes);
    }
    [Fact]
    public async Task Permission_failure_returns_safe_403_without_server_details()
    {
        using var fixture = new Fixture(); fixture.SignIn(); fixture.Attendance.Fail = true;
        using var response = await fixture.Client.GetAsync("/api/v1/attendance-request/get-date-info?date=2026-10-27");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.DoesNotContain("sensitive", await response.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task Hr_group_flag_does_not_bypass_effective_resource_permissions()
    {
        using var fixture = new Fixture(); fixture.SignIn(); fixture.Access.Actor!.IsHRStaff = true;
        using var denied = await fixture.Client.GetAsync("/api/v1/workspace-directory/get-staff-identity?staffId=200");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode); Assert.Equal(0, fixture.Workspace.Reads);
        fixture.Access.Permissions.Add("Employee.View");
        using var allowed = await fixture.Client.GetAsync("/api/v1/workspace-directory/get-staff-identity?staffId=200");
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode); Assert.Equal(1, fixture.Workspace.Reads);
    }
    [Fact]
    public async Task Signed_in_identity_remains_available_without_an_hrm_employee_record()
    {
        using var fixture = new Fixture(); fixture.SignIn(); fixture.Access.Actor = null;
        using var response = await fixture.Client.GetAsync("/api/v1/session/identity");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var text = await response.Content.ReadAsStringAsync();
        Assert.Contains("alice", text, StringComparison.Ordinal);
        Assert.DoesNotContain("token", text, StringComparison.OrdinalIgnoreCase);
    }
    [Theory]
    [InlineData(20, null)]
    [InlineData(null, 200)]
    public async Task Http_query_cannot_select_another_tenant_or_impersonate_another_staff_member(int? organisation, int? staff)
    {
        using var fixture = new Fixture(); fixture.SignIn(); var token = await fixture.TokenAsync();
        fixture.Client.DefaultRequestHeaders.Add("X-CSRF-TOKEN", token);
        fixture.Client.DefaultRequestHeaders.Add("X-Organisation-Id", "20");
        using var response = await fixture.Client.PostAsJsonAsync("/api/v1/attendance-request-queries/query/get-mine", new
        {
            filter = new { from = "2026-10-01", to = "2026-10-31", organisationId = organisation, staffId = staff }
        });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
    [Fact]
    public async Task Temporary_blazor_host_seeds_http_authentication_state_without_a_circuit()
    {
        using var fixture = new Fixture(rootAuthentication: true); fixture.SignIn();
        using var response = await fixture.Client.GetAsync("/api/v1/attendance-request/get-date-info?date=2026-10-27");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
    public sealed class StateCheckingAttendance(AuthenticationStateProvider authentication) : IAttendanceRequestService
    {
        public async Task<AttendanceRequestDateInfoDto> GetDateInfoAsync(DateOnly date, int? originalLogId = null)
        {
            Assert.True((await authentication.GetAuthenticationStateAsync()).User.Identity?.IsAuthenticated);
            return new() { CanSubmit = true };
        }
        public Task<ServiceResult> SubmitAsync(AttendanceRequestApplicationDto model) => throw new NotSupportedException();
    }
    public sealed class WorkspaceStub : IWorkspaceDirectoryService
    {
        public int Reads;
        public Task<StaffWorkspaceIdentity?> GetStaffIdentityAsync(int staffId, CancellationToken ct = default)
        { Reads++; return Task.FromResult<StaffWorkspaceIdentity?>(new("Staff", false)); }
        public Task<WorkspaceSearchPage> SearchAsync(WorkspaceSearchFilter filter, CancellationToken ct = default) => Task.FromResult(new WorkspaceSearchPage([], []));
    }
    public sealed class AttendanceStub : IAttendanceRequestService
    {
        public int Reads; public int Writes; public bool Fail;
        public Task<AttendanceRequestDateInfoDto> GetDateInfoAsync(DateOnly date, int? originalLogId = null)
        {
            Reads++; if (Fail) throw new UnauthorizedAccessException("sensitive database identifiers");
            return Task.FromResult(new AttendanceRequestDateInfoDto { CanSubmit = true });
        }
        public Task<ServiceResult> SubmitAsync(AttendanceRequestApplicationDto model) { Writes++; return Task.FromResult(ServiceResult.Ok()); }
    }
    public sealed class TestAuthentication(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var name = Request.Headers["X-Test-User"].ToString();
            return Task.FromResult(string.IsNullOrEmpty(name) ? AuthenticateResult.NoResult() : AuthenticateResult.Success(new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, name), new Claim(ClaimTypes.Name, name)], Scheme.Name)), Scheme.Name)));
        }
    }
}
