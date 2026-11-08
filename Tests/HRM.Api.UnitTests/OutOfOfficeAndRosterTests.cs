using HRM.Application.Contracts.Pages;
using HRM.DTOs;
using HRM.DTOs.Attendance;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using HRM.Services.Queries;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace HRM.Api.UnitTests;

public sealed class OutOfOfficeAndRosterTests
{
    [Fact]
    public async Task Completed_out_of_office_requests_cannot_be_opened_for_editing()
    {
        var factory = new QueryFactory(); var access = new FakeAccess();
        await using (var db = factory.CreateDbContext())
        {
            db.Requests.Add(new() { RequestId = 9, RequestStateId = 4 });
            db.OutOfOfficeRequests.Add(new() { RequestId = 9, OrganisationBusinessEntityID = 10, StaffId = 100 });
            await db.SaveChangesAsync();
        }
        var service = Service(factory, access);
        Assert.Null(await service.GetEditableAsync(9));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => service.GetMineAsync(new(new(2026, 10, 27), new(2026, 10, 27), StaffId: 200)));
    }
    [Fact]
    public async Task Out_of_office_submission_uses_central_type_50_and_configured_workflow_not_category_id()
    {
        var factory = new QueryFactory(ignoreTransactions: true); var access = new FakeAccess();
        var today = DateTime.UtcNow.AddHours(5).Date;
        access.Actor!.ActiveJob = new ActiveJobDto { JobId = 1, IndividualId = 100, OrganisationId = 10 };
        await using (var db = factory.CreateDbContext())
        {
            db.Individuals.AddRange(new() { BusinessEntityId = 100 }, new() { BusinessEntityId = 400 });
            db.Staffs.AddRange(new() { IndividualId = 100 }, new() { IndividualId = 400 });
            db.Jobs.Add(new() { JobId = 1, IndividualID = 100, OrganisationID = 10, JobStateId = 4, JoinedDate = today.AddDays(-1) });
            db.Users.Add(new() { UserId = 2, BusinessEntityID = 400 });
            db.RequestTypes.Add(new() { RequestTypeId = 50, TypeName = "Out of Office Request" });
            db.OutOfficeRequestTypes.Add(new() { OutOfOfficeRequestTypeId = 7, TypeName = "Appointment" });
            await db.SaveChangesAsync();
        }
        var starts = 0;
        var requests = StubProxy.For<IRequestService>((name, args) => name == "CreateAsync" ? Create((HrmTeContext)args[0]!, (CreateRequestDto)args[1]!) : throw new Exception(name));
        async Task<Request> Create(HrmTeContext db, CreateRequestDto dto)
        {
            Assert.Equal(50, dto.RequestTypeId); Assert.Equal(100, dto.ApplicantBusinessEntityId); Assert.Equal(10, dto.OrganisationBusinessEntityId);
            var request = new Request { RequestId = 99, RequestTypeId = dto.RequestTypeId, ApplicantBusinessEntityId = dto.ApplicantBusinessEntityId, OrganisationBusinessEntityID = dto.OrganisationBusinessEntityId, RequestStateId = 2, ReferenceNumber = "OOF-99" };
            db.Requests.Add(request); await db.SaveChangesAsync(); return request;
        }
        var logs = StubProxy.For<IOperationLogService>((name, args) => Task.FromResult(new OperationLog { OperationLogId = 1 }));
        var workflow = StubProxy.For<IWorkflowEngine>((name, args) => Start((HrmTeContext)args[0]!, (int)args[1]!, (int)args[2]!));
        async Task<long> Start(HrmTeContext db, int request, int job)
        {
            starts++; Assert.Equal(99, request); Assert.Equal(1, job);
            db.WfRequestStageInstances.Add(new() { RequestStageInstanceId = 1, WorkflowRequestInstanceId = 1 });
            db.WfRequestApprovals.Add(new() { RequestApprovalId = 1, RequestStageInstanceId = 1, ApproverUserId = 2 });
            await db.SaveChangesAsync(); return 1;
        }
        var service = new OutOfOfficeRequests(factory, access, new(access), requests, logs, workflow,
            StubProxy.For<IWorkflowApproverResolver>((name, _) => throw new Exception(name)), StubProxy.For<IWorkflowRequestContextResolver>((name, _) => throw new Exception(name)));
        var result = await service.SubmitAsync(new() { RequestTypeId = 7, Date = DateOnly.FromDateTime(today), ExpectedOut = new(10, 0), ExpectedIn = new(11, 0), Remarks = "Appointment" });
        Assert.True(result.Success); Assert.Equal(1, starts);
        await using var saved = factory.CreateDbContext();
        var row = await saved.OutOfOfficeRequests.SingleAsync();
        Assert.Equal(1, (await saved.RequestJobs.SingleAsync()).JobId); Assert.Equal(7, row.OutOfOfficeRequestTypeId); Assert.Equal(400, row.SupervisorStaffId); Assert.Equal(100, row.StaffId);
        Assert.NotEqual(4, (await saved.Requests.SingleAsync()).RequestStateId);
    }
    [Fact]
    public async Task Out_of_office_requests_reject_invalid_time_ranges_before_writing()
    {
        var factory = new QueryFactory(); var access = new FakeAccess();
        await Assert.ThrowsAsync<ArgumentException>(() => Service(factory, access).SubmitAsync(new() { Date = new(2026, 10, 27), ExpectedOut = new(11, 0), ExpectedIn = new(10, 0), Remarks = "Invalid" }));
    }
    [Fact]
    public async Task Daily_roster_uses_tenant_staff_and_keeps_missing_schedule_ineligible_for_absence()
    {
        var factory = new QueryFactory(); var access = new FakeAccess(); access.Permissions.Add("Attendance.View");
        var today = DateTime.UtcNow.AddHours(5).Date;
        await using (var db = factory.CreateDbContext())
        {
            db.Individuals.AddRange(new() { BusinessEntityId = 100, FirstNameEnglish = "Current" }, new() { BusinessEntityId = 200, FirstNameEnglish = "Foreign" });
            db.Staffs.AddRange(new() { IndividualId = 100 }, new() { IndividualId = 200 });
            db.Jobs.AddRange(new() { JobId = 1, IndividualID = 100, OrganisationID = 10, JobStateId = 4, JoinedDate = today.AddDays(-1) }, new() { JobId = 2, IndividualID = 200, OrganisationID = 20, JobStateId = 4, JoinedDate = today.AddDays(-1) });
            await db.SaveChangesAsync();
        }
        var attendance = StubProxy.For<IAttendanceService>((name, args) => name switch
        {
            "GetStaffClockEventsAsync" => Task.FromResult(new List<StaffAttendanceClockEventDto>()),
            "GetStaffScheduleAttendanceAsync" => Task.FromResult(new List<StaffScheduleAttendanceDto>()),
            "GetStaffAttendanceRangeAsync" => Task.FromResult(new List<YearlyAttendanceDayDto> { new() { Date = today, EligibilityMessage = "No active schedule found" } }),
            _ => throw new Exception(name)
        });
        var result = await new OrganisationAttendanceQueries(factory, attendance, new(access)).GetDailyAsync(new(DateOnly.FromDateTime(today)));
        var staff = Assert.Single(result.Staff); Assert.Equal(100, staff.StaffId); Assert.False(staff.Eligibility.CanBeAbsent);
        Assert.NotNull(staff.Overview); Assert.False(staff.Overview.IsAbsent); Assert.Equal("No active schedule found", staff.Overview.EligibilityMessage);
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Applicants_cannot_approve_their_own_out_of_office_requests_even_as_superadministrator(bool super)
    {
        var factory = new QueryFactory(ignoreTransactions: true); var access = new FakeAccess(); access.Actor!.IsSuperAdministrator = super;
        await using (var db = factory.CreateDbContext())
        {
            db.Requests.Add(new() { RequestId = 9, RequestTypeId = 50, RequestStateId = 2 });
            db.OutOfOfficeRequests.Add(new() { RequestId = 9, OrganisationBusinessEntityID = 10, StaffId = 100 });
            await db.SaveChangesAsync();
        }
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Service(factory, access).ActAsync(9, 2, "Approve", "Self approval"));
        await using var saved = factory.CreateDbContext();
        Assert.Equal(2, (await saved.Requests.SingleAsync()).RequestStateId);
    }
    private static OutOfOfficeRequests Service(QueryFactory factory, FakeAccess access) => new(factory, access, new(access),
        StubProxy.For<IRequestService>((name, _) => throw new Exception("No write expected: " + name)),
        StubProxy.For<IOperationLogService>((name, _) => throw new Exception("No audit expected: " + name)),
        StubProxy.For<IWorkflowEngine>((name, _) => throw new Exception("No workflow expected: " + name)),
        StubProxy.For<IWorkflowApproverResolver>((name, _) => throw new Exception(name)), StubProxy.For<IWorkflowRequestContextResolver>((name, _) => throw new Exception(name)));
}
