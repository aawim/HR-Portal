using System.Text.Json;
using HRM.Application.Transport;
static class ApiOperationContractTests
{
    public interface IOperations
    {
        Task<string> ReadAsync(int id, CancellationToken ct = default);
        Task<string> ReadAsync(string id);
    }
    public interface IInvalid { string Read(); }
    static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    static void Reject(Action action) { try { action(); } catch (ArgumentException) { return; } throw new Exception("Invalid contract was accepted"); }
    public static async Task RunAsync()
    {
        var descriptor = new ApiOperationDescriptor(typeof(IOperations));
        var method = descriptor.Resolve("ReadAsync", [ApiArgument.From(42)]);
        Check(method.GetParameters()[0].ParameterType == typeof(int), "Overload resolution lost declared argument type");
        var text = descriptor.Resolve("ReadAsync", [ApiArgument.From<string?>(null)]);
        Check(text.GetParameters()[0].ParameterType == typeof(string), "Null argument lost its declared type");
        using var cancellation = new CancellationTokenSource();
        var values = descriptor.Deserialize(method, [JsonSerializer.SerializeToElement(42)], cancellation.Token);
        Check((int)values[0]! == 42 && (CancellationToken)values[1]! == cancellation.Token, "Argument or server cancellation binding failed");
        Reject(() => descriptor.Deserialize(method, [], default));
        Reject(() => descriptor.Deserialize(method, [JsonSerializer.SerializeToElement<object?>(null)], default));
        Reject(() => descriptor.Resolve("ReadAsync", [ApiArgument.From(42L)]));
        Reject(() => new ApiOperationDescriptor(typeof(IInvalid)));
        var unknownRejected = false;
        try { descriptor.Resolve("GetType()"); } catch (KeyNotFoundException) { unknownRejected = true; }
        Check(unknownRejected, "Unregistered method was exposed");
        Console.WriteLine("PASS API contracts: explicit interfaces, overloads, null types, argument validation and server cancellation");
        foreach (var contract in Contracts) _ = new ApiOperationDescriptor(contract);
        await CheckHttpAsync();
    }
    static readonly Type[] Contracts =
    [
        typeof(HRM.Services.Contracts.UserPreferenceServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.LateCheckInRequestServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.RequestNotificationServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.OvertimeRequestServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.StaffPositionManagementServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.SupervisorChangeRequestServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.WeeklyHourExceptionServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.WeeklyWorkingHoursServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.AttendanceLogServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.StaffChatServiceContracts.IOperations),
        typeof(HRM.WorkPlanning.Services.Contracts.DutyRosterServiceContracts.IOperations),
        typeof(HRM.Services.Leave.Contracts.LeaveHandoverServiceContracts.IOperations),
        typeof(HRM.Services.Leave.Contracts.LeaveCalendarServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.LeaveConfigurationSettingsServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.LeaveSetConfigurationServiceContracts.IOperations),
        typeof(HRM.Services.Leave.Contracts.LeaveHandoverRuleSettingsServiceContracts.IOperations),
        typeof(HRM.Services.Leave.Contracts.LeaveAccrualSettingsServiceContracts.IOperations),
        typeof(HRM.Services.Leave.Contracts.LeaveCarryForwardServiceContracts.IOperations),
        typeof(HRM.Services.Leave.Contracts.LeaveFlowConfigurationServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.RequestTransitionSettingsServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.WorkingHoursSettingsServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.ScopeScheduleSettingsServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.AdminAccessGrantServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.UserSettingsServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.PermissionSettingsServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.PositionOverviewServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.OrganisationResourceSettingsServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.OrganisationPermissionServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.OrganisationSettingsServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.JobTypeConfigurationServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.JobTypeOvertimeSettingsServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.WorkspaceRecentSearchServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.OrganisationWorkspaceServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.StaffOnboardingServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.FormerStaffServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.GroupsManagementServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.IndividualRegistrationServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.HrRequestInboxServiceContracts.IOperations),
        typeof(HRM.Services.Contracts.UserServiceContracts.IOperations)
    ];

    static async Task CheckHttpAsync()
    {
        var handler = new OperationHandler();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://api.invalid/") };
        var client = new HRM.UI.Services.Transport.HttpApiOperationClient(http);
        var proxy = HRM.UI.Services.Transport.ApiContractProxy.Create<HRM.Services.Interfaces.IUserContext>(client);
        var result = await proxy.GetUserIdAsync();
        Check(result == 42, "Typed HTTP response was not deserialized");
        Check(handler.LastPath == "/api/v1/user-context/get-user-id", "The client did not use the registered typed endpoint");
        handler.Status = System.Net.HttpStatusCode.Forbidden;
        var denied = false;
        try { await proxy.GetUserIdAsync(); } catch (UnauthorizedAccessException) { denied = true; }
        Check(denied, "HTTP permission denial was swallowed");
        handler.Status = System.Net.HttpStatusCode.Conflict;
        var blocked = false;
        try { await proxy.GetUserIdAsync(); }
        catch (HRM.UI.Services.Transport.ApiOperationException ex) { blocked = ex.Code == "409"; }
        Check(blocked, "HTTP business-rule rejection was swallowed");
        var rejected = false;
        try { await client.InvokeAsync<string>(typeof(IOperations), "ReadAsync", [ApiArgument.From(42)]); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected, "An unreviewed contract was sent to the backend");
        Console.WriteLine("PASS API HTTP client: registered typed routes, permission denials and blocked actions; all 39 feature contracts validated");
    }
    sealed class OperationHandler : HttpMessageHandler
    {
        public System.Net.HttpStatusCode Status = System.Net.HttpStatusCode.OK;
        public string? LastPath;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            LastPath = request.RequestUri!.AbsolutePath;
            return Task.FromResult(new HttpResponseMessage(Status) { Content = new StringContent("42", System.Text.Encoding.UTF8, "application/json") });
        }
    }
}
