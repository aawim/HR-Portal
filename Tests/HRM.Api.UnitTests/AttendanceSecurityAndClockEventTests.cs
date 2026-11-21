using HRM.DTOs.Attendance;
using HRM.DTOs.UserContext;
using HRM.Enum;
using HRM.Models;
using HRM.Models.WorkPlanning;
using HRM.Services;
using HRM.Services.Attendance.Reconciliation;
using HRM.Services.Interfaces;
using HRM.WorkPlanning.Abstractions;
using HRM.WorkPlanning.Results;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class AttendanceSecurityAndClockEventTests
{
    [Fact]
    public async Task AttendanceReconciliationService_BlocksRegularStaffFromManualOverrideAndBatchReconcile()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        var staffAccess = FakeStaffAccess(individualId: 100, userId: 10, orgId: 1);
        var service = new AttendanceReconciliationService(factory, access: staffAccess);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ApplyManualOverrideAsync(new AttendanceManualOverrideRequest
            {
                OrganisationId = 1,
                IndividualId = 200,
                JobId = 50,
                WorkDate = new DateOnly(2026, 11, 20),
                AttendanceLogId = 1,
                TargetSchedulePurpose = "Official",
                OverrideAction = "Reassign",
                Reason = "Unauthorized attempt"
            }));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ReconcileRangeAsync(new AttendanceBatchReconciliationRequest
            {
                OrganisationId = 1,
                FromDate = new DateOnly(2026, 11, 1),
                ToDate = new DateOnly(2026, 11, 10)
            }));

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            service.ReconcileRangeAsync(new AttendanceBatchReconciliationRequest
            {
                OrganisationId = 1,
                FromDate = new DateOnly(2025, 1, 1),
                ToDate = new DateOnly(2026, 11, 10)
            }));
    }

    [Fact]
    public async Task AttendancePlanService_BlocksCrossStaffIdorAccess()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        using (var db = factory.CreateDbContext())
        {
            db.Jobs.Add(new Job { JobId = 50, IndividualID = 200, OrganisationID = 1, OrganisationStructureId = 1, JobStateId = 4, JoinedDate = DateTime.Today.AddMonths(-1) });
            await db.SaveChangesAsync();
        }

        var staffAccess = FakeStaffAccess(individualId: 100, userId: 10, orgId: 1);
        var loader = StubProxy.For<IAttendanceLogDataLoader>((_, _) => throw new NotImplementedException());
        var resolver = StubProxy.For<IPlanningProviderResolver>((_, _) => Task.FromResult(new PlanningProviderResolution { PlanningProviderId = 1, Code = "WORK_PLANNING" }));
        var generator = StubProxy.For<IWorkPlanGenerator>((_, _) => Task.FromResult<AttendanceWorkPlanDto?>(null));

        var service = new AttendancePlanService(factory, loader, staffAccess, resolver, generator);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetPlanAsync(individualId: 200, jobId: 50, workDate: DateTime.Today));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.GetOrGenerateAsync(individualId: 200, jobId: 50, organisationBusinessEntityId: 1, attendanceTime: DateTime.Today));
    }

    [Fact]
    public async Task AttendanceWorkPlanResolver_ResolvesPairedShiftAndPrayerBreakClockEvents()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        var day = new DateTime(2026, 11, 22);
        using (var db = factory.CreateDbContext())
        {
            db.WorkSegmentTypes.AddRange(
                new WorkSegmentType { WorkSegmentTypeId = 1, Code = "SHIFT", Name = "Shift", IsActive = true },
                new WorkSegmentType { WorkSegmentTypeId = 2, Code = "PRAYER_BREAK", Name = "Prayer Break", IsActive = true });

            var plan = new WorkPlan
            {
                WorkPlanId = 1,
                IndividualId = 100,
                JobId = 200,
                OrganisationBusinessEntityId = 10,
                WorkDate = day,
                IsValid = true,
                GeneratedDate = DateTime.UtcNow,
                CreatedDate = DateTime.UtcNow,
                PlanGuid = Guid.NewGuid()
            };
            db.WorkPlans.Add(plan);
            db.WorkPlanSegments.AddRange(
                new WorkPlanSegment
                {
                    WorkPlanSegmentId = 1,
                    WorkPlan = plan,
                    WorkPlanId = 1,
                    WorkSegmentTypeId = 1,
                    SchedulePurpose = "Official",
                    Name = "Morning Shift",
                    SequenceNumber = 1,
                    StartDateTime = day.AddHours(10),
                    EndDateTime = day.AddHours(14),
                    GraceBeforeMinutes = 15,
                    GraceAfterMinutes = 15,
                    IsMandatory = true,
                    RequiresAttendance = true,
                    IsValid = true,
                    OperationLogId = 1,
                    CreatedDate = DateTime.UtcNow
                },
                new WorkPlanSegment
                {
                    WorkPlanSegmentId = 2,
                    WorkPlan = plan,
                    WorkPlanId = 1,
                    WorkSegmentTypeId = 2,
                    SchedulePurpose = "Official",
                    Name = "Dhuhr Prayer",
                    SequenceNumber = 2,
                    StartDateTime = day.AddHours(12),
                    EndDateTime = day.AddHours(12).AddMinutes(30),
                    IsMandatory = false,
                    RequiresAttendance = true,
                    IsValid = true,
                    OperationLogId = 1,
                    CreatedDate = DateTime.UtcNow
                });
            await db.SaveChangesAsync();
        }

        var generator = StubProxy.For<IWorkPlanGenerator>((_, _) =>
            Task.FromResult<AttendanceWorkPlanDto?>(new AttendanceWorkPlanDto { WorkPlanId = 1, IndividualId = 100, JobId = 200, OrganisationId = 10, WorkDate = day }));
        var resolver = new AttendanceWorkPlanResolver(factory, generator);

        // 1. Shift Check-In at 10:00
        var shiftIn = await resolver.ResolveAsync(100, 200, 10, day.AddHours(10));
        Assert.True(shiftIn.IsResolved);
        Assert.Equal(AttendanceClockType.CheckIn, shiftIn.ClockType);
        Assert.Equal(1, shiftIn.WorkPlanSegmentId);

        // Record the shift check-in so open state is tracked
        using (var db = factory.CreateDbContext())
        {
            var log = new AttendanceLog { AttendanceLogId = 1, IndividualId = 100, OrganisationId = 10, Date = day.AddHours(10), AttendanceLogStateId = 1, AttendanceLogModeId = 2, InOutModeId = 3, OperationLogId = 1 };
            db.AttendanceLogs.Add(log);
            db.AttendanceLogResolutions.Add(new AttendanceLogResolution
            {
                AttendanceLogResolutionId = 1,
                AttendanceLogId = 1,
                AttendanceLog = log,
                WorkPlanId = 1,
                WorkPlanSegmentId = 1,
                JobId = 200,
                AttendanceResolutionStatusId = 1,
                AttendanceClockTypeId = (int)AttendanceClockType.CheckIn,
                IsValid = true,
                ResolutionDate = day.AddHours(10),
                CreatedDate = day.AddHours(10)
            });
            await db.SaveChangesAsync();
        }

        // 2. Prayer Break Out at 12:05
        var prayerOut = await resolver.ResolveAsync(100, 200, 10, day.AddHours(12).AddMinutes(5));
        Assert.True(prayerOut.IsResolved);
        Assert.Equal(AttendanceClockType.CheckOut, prayerOut.ClockType);
        Assert.Equal("OfficialPrayerOut", prayerOut.LogicalEventType);

        // 3. Shift Check-Out at 14:00
        var shiftOut = await resolver.ResolveAsync(100, 200, 10, day.AddHours(14));
        Assert.True(shiftOut.IsResolved);
        Assert.Equal(AttendanceClockType.CheckOut, shiftOut.ClockType);
        Assert.Equal(1, shiftOut.WorkPlanSegmentId);
    }

    private static IUserAccessService FakeStaffAccess(int individualId, int userId, int orgId)
    {
        var ctx = new UserContextDto
        {
            UserId = userId,
            IndividualId = individualId,
            IsSuperAdministrator = false,
            IsHRStaff = false,
            IsHRHead = false,
            IsHRManager = false,
            IsOrganisationAdmin = false,
            IsSupervisor = false
        };
        return StubProxy.For<IUserAccessService>((method, _) => method switch
        {
            nameof(IUserAccessService.RequireContextAsync) => Task.FromResult(ctx),
            nameof(IUserAccessService.GetContextAsync) => Task.FromResult<UserContextDto?>(ctx),
            nameof(IUserAccessService.GetCurrentOrganisationIdAsync) => Task.FromResult(orgId),
            nameof(IUserAccessService.HasOrganisationAccessAsync) => Task.FromResult(true),
            nameof(IUserAccessService.HasPermissionAsync) => Task.FromResult(false),
            nameof(IUserAccessService.HasUserGroupAsync) => Task.FromResult(false),
            _ => Task.FromResult(false)
        });
    }
}
