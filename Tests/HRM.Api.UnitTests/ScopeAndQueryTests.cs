using HRM.Application.Contracts.Pages;
using HRM.Models;
using HRM.Services;
using HRM.Services.Queries;
using Xunit;
namespace HRM.Api.UnitTests;

public sealed class ScopeAndQueryTests
{
    [Fact]
    public async Task Organisation_selection_requires_actual_superadministrator()
    {
        var access = new FakeAccess(); access.Permissions.Add("Super.Admin.Manage");
        var scope = new ApiReadScope(access);
        Assert.Equal(10, await scope.OrganisationAsync());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => scope.OrganisationAsync(20));
        access.Actor!.IsSuperAdministrator = true;
        Assert.Equal(20, await scope.OrganisationAsync(20));
    }
    [Fact]
    public async Task Staff_queries_require_effective_permissions_even_for_hr_group_flags()
    {
        var access = new FakeAccess(); access.Actor!.IsHRStaff = true;
        var scope = new ApiReadScope(access);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => scope.RequireStaffReadAsync("Attendance.View"));
        access.Permissions.Add("Attendance.View");
        Assert.Same(access.Actor, await scope.RequireStaffReadAsync("Attendance.View"));
    }
    [Fact]
    public void Date_ranges_reject_invalid_and_unbounded_periods()
    {
        Assert.Throws<ArgumentException>(() => ApiReadScope.Dates(default, new(2026, 10, 27)));
        Assert.Throws<ArgumentException>(() => ApiReadScope.Dates(new(2026, 10, 27), new(2026, 10, 26)));
        Assert.Throws<ArgumentException>(() => ApiReadScope.Dates(new(2026, 10, 27), DateOnly.MaxValue));
        Assert.Throws<ArgumentException>(() => ApiReadScope.Dates(new(2000, 1, 1), new(2026, 10, 27)));
    }
    [Fact]
    public async Task Organisation_lookup_never_lists_other_tenants_for_regular_staff()
    {
        var factory = new QueryFactory(); var access = new FakeAccess();
        await using (var db = factory.CreateDbContext()) { db.Organisations.AddRange(new() { BusinessEntityID = 10, OrganisationName = "Current" }, new() { BusinessEntityID = 20, OrganisationName = "Other" }); await db.SaveChangesAsync(); }
        var service = new OrganisationService(factory, access);
        Assert.Equal(10, Assert.Single(await service.GetAllOrganisationsAsync()).Id);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetOrganisationAsync(20));
        access.Actor!.IsSuperAdministrator = true;
        Assert.Equal(2, (await service.GetAllOrganisationsAsync()).Count);
    }
    [Fact]
    public async Task Attendance_requests_are_owned_and_scoped_and_received_reads_need_assignment_or_permission()
    {
        var factory = new QueryFactory(); var access = new FakeAccess();
        var date = new DateTime(2026, 10, 27, 8, 30, 0);
        await using (var db = factory.CreateDbContext())
        {
            db.Organisations.AddRange(new() { BusinessEntityID = 10, OrganisationName = "Current" }, new() { BusinessEntityID = 20, OrganisationName = "Other" });
            db.RequestStates.Add(new() { RequestStateId = 7, StateName = "Pending HR Approval", IsProcessingState = true });
            db.InOutModes.Add(new() { InOutModeId = 1, NameLong = "Check in", NameShort = "In" });
            foreach (var (id, person, org) in new[] { (1, 100, 10), (2, 200, 10), (3, 300, 20) })
            {
                db.Individuals.Add(new() { BusinessEntityId = person, FirstNameEnglish = "Staff " + person });
                db.Requests.Add(new() { RequestId = id, RequestStateId = 7, ReferenceNumber = "ATT-" + id, ApplicantBusinessEntityId = person, OrganisationBusinessEntityID = org });
                db.AttendanceLogs.Add(new() { AttendanceLogId = id, IndividualId = person, OrganisationId = org, Date = date, InOutModeId = 1 });
                db.AttendanceLogRequests.Add(new() { AttendanceLogRequestId = id, RequestId = id, AttendanceLogId = id, Comments = "Explanation" });
            }
            await db.SaveChangesAsync();
        }
        var service = new AttendanceRequestQueries(factory, access, new(access));
        var filter = new AttendanceRequestFilter(new(2026, 10, 27), new(2026, 10, 27));
        Assert.Equal("ATT-1", Assert.Single((await service.GetMineAsync(filter)).Requests).Reference);
        Assert.Empty((await service.GetReceivedAsync(filter)).Requests);
        Assert.Empty((await service.GetReceivedAsync(filter with { RequestId = 2 })).Requests);
        await using (var db = factory.CreateDbContext())
        {
            db.WfRequestApprovals.Add(new()
            {
                ApproverUserId = access.Actor!.UserId,
                RequestStageInstance = new()
                {
                    WorkflowRequestInstance = new() { RequestId = 2 }
                }
            });
            await db.SaveChangesAsync();
        }
        Assert.Equal("ATT-2", Assert.Single((await service.GetReceivedAsync(filter with { RequestId = 2 })).Requests).Reference);
        Assert.Empty((await service.GetReceivedAsync(filter with { RequestId = 3 })).Requests);
        access.Permissions.Add("Attendance.View");
        Assert.Equal(2, (await service.GetReceivedAsync(filter)).Requests.Count);
        var notification = filter with { From = new(2026, 10, 29), To = new(2026, 10, 29), RequestId = 2 };
        Assert.Equal("ATT-2", Assert.Single((await service.GetReceivedAsync(notification)).Requests).Reference);
        Assert.Empty((await service.GetReceivedAsync(notification with { RequestId = 3 })).Requests);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetMineAsync(filter with { StaffId = 200 }));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetReceivedAsync(filter with { OrganisationId = 20 }));
        Assert.Empty((await service.GetMineAsync(filter with { StateId = 4 })).Requests);
    }
    [Fact]
    public async Task Current_employee_session_allows_missing_employee_without_impersonation_parameters()
    {
        var access = new FakeAccess { Actor = null };
        Assert.Null(await new CurrentEmployeeSession(access).GetAsync());
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new CurrentEmployeeSession(access).GetAsync(cancellation.Token));
    }
}
