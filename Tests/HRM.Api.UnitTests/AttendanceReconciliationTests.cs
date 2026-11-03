using HRM.Components.Shared;
using HRM.Constants;
using HRM.DTOs.Attendance;
using HRM.Enum;
using HRM.Models;
using HRM.Models.WorkPlanning;
using HRM.Services;
using HRM.Services.Attendance.Evaluation;
using HRM.Services.Attendance.Reconciliation;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class AttendanceReconciliationTests
{
    private static readonly DateOnly DefaultWorkDate = new(2026, 10, 20);
    private const int OrgId = 10;
    private const int IndividualId = 500;
    private const int JobId = 50;

    private static DateTime At(DateOnly date, int hour, int minute, int second = 0) =>
        new(date.Year, date.Month, date.Day, hour, minute, second);

    private static async Task<WorkPlan> SeedPlanAsync(
        HrmTeContext db,
        DateOnly workDate,
        (TimeOnly Start, TimeOnly End, int GraceMinutes, bool IncludeLunch)? official = null,
        (TimeOnly LunchStart, TimeOnly LunchEnd)? lunchWindow = null,
        params (long AssignmentId, string Name, DateTime Start, DateTime End, int GraceMinutes)[] duties)
    {
        if (!await db.Jobs.AnyAsync(j => j.JobId == JobId))
        {
            db.Individuals.Add(new Individual
            {
                BusinessEntityId = IndividualId,
                FirstNameEnglish = "Test",
                LastNameEnglish = "Employee"
            });
            db.Jobs.Add(new Job
            {
                JobId = JobId,
                IndividualID = IndividualId,
                OrganisationID = OrgId,
                JobStateId = 4,
                JoinedDate = new DateTime(2025, 1, 1)
            });
        }

        if (!await db.WorkSegmentTypes.AnyAsync())
        {
            db.WorkSegmentTypes.AddRange(
                new WorkSegmentType { WorkSegmentTypeId = 1, Code = "WORK", Name = "Work", IsActive = true },
                new WorkSegmentType { WorkSegmentTypeId = 2, Code = "CHECK_IN", Name = "Check in", IsActive = true },
                new WorkSegmentType { WorkSegmentTypeId = 3, Code = "CHECK_OUT", Name = "Check out", IsActive = true },
                new WorkSegmentType { WorkSegmentTypeId = 4, Code = "LUNCH", Name = "Lunch break", IsActive = true },
                new WorkSegmentType { WorkSegmentTypeId = 5, Code = "DUTY_CHECK_IN", Name = "Duty check in", IsActive = true },
                new WorkSegmentType { WorkSegmentTypeId = 6, Code = "DUTY_CHECK_OUT", Name = "Duty check out", IsActive = true });
        }

        var dt = workDate.ToDateTime(TimeOnly.MinValue);
        var plan = new WorkPlan
        {
            WorkPlanId = workDate.DayNumber,
            OrganisationBusinessEntityId = OrgId,
            IndividualId = IndividualId,
            JobId = JobId,
            WorkDate = dt,
            IsValid = true,
            CreatedDate = DateTime.UtcNow
        };
        db.WorkPlans.Add(plan);

        int nextSegId = workDate.DayNumber * 100;
        int seq = 1;

        if (official.HasValue)
        {
            var offStart = workDate.ToDateTime(official.Value.Start);
            var offEnd = workDate.ToDateTime(official.Value.End);
            if (offEnd <= offStart)
                offEnd = offEnd.AddDays(1);

            var offAssignment = new WorkAssignment
            {
                WorkAssignmentId = workDate.DayNumber * 10 + 1,
                WorkPlanId = plan.WorkPlanId,
                WorkTemplateId = 1,
                Name = "Official Working Schedule",
                SchedulePurpose = "Official",
                StartDateTime = offStart,
                EndDateTime = offEnd,
                GraceMinutes = official.Value.GraceMinutes,
                RequiresAttendance = true,
                IsValid = true,
                WorkAssignmentStateId = 1,
                CreatedDate = DateTime.UtcNow
            };
            db.WorkAssignments.Add(offAssignment);

            db.WorkPlanSegments.Add(new WorkPlanSegment
            {
                WorkPlanSegmentId = ++nextSegId,
                WorkPlanId = plan.WorkPlanId,
                WorkSegmentTypeId = 2,
                Name = "Official · Check in",
                SchedulePurpose = "Official",
                SequenceNumber = seq++,
                StartDateTime = offStart,
                EndDateTime = offStart,
                GraceBeforeMinutes = 180,
                GraceAfterMinutes = Math.Max(official.Value.GraceMinutes, 120),
                RequiresAttendance = true,
                IsMandatory = true,
                CreatedDate = DateTime.UtcNow
            });

            if (official.Value.IncludeLunch)
            {
                var lStart = lunchWindow.HasValue
                    ? workDate.ToDateTime(lunchWindow.Value.LunchStart)
                    : offStart.Date.AddHours(12);
                var lEnd = lunchWindow.HasValue
                    ? workDate.ToDateTime(lunchWindow.Value.LunchEnd)
                    : offStart.Date.AddHours(13);

                db.WorkPlanSegments.Add(new WorkPlanSegment
                {
                    WorkPlanSegmentId = ++nextSegId,
                    WorkPlanId = plan.WorkPlanId,
                    WorkSegmentTypeId = 4,
                    Name = "Official · Lunch Break",
                    SchedulePurpose = "Official",
                    SequenceNumber = seq++,
                    StartDateTime = lStart,
                    EndDateTime = lEnd,
                    GraceBeforeMinutes = 30,
                    GraceAfterMinutes = 45,
                    RequiresAttendance = false,
                    IsMandatory = false,
                    CreatedDate = DateTime.UtcNow
                });
            }

            db.WorkPlanSegments.Add(new WorkPlanSegment
            {
                WorkPlanSegmentId = ++nextSegId,
                WorkPlanId = plan.WorkPlanId,
                WorkSegmentTypeId = 3,
                Name = "Official · Check out",
                SchedulePurpose = "Official",
                SequenceNumber = seq++,
                StartDateTime = offEnd,
                EndDateTime = offEnd,
                GraceBeforeMinutes = 30,
                GraceAfterMinutes = 180,
                RequiresAttendance = true,
                IsMandatory = true,
                CreatedDate = DateTime.UtcNow
            });
        }

        foreach (var d in duties)
        {
            var dutyTemplateId = (int)(d.AssignmentId % 1000) + 10;
            var dutyAssignment = new WorkAssignment
            {
                WorkAssignmentId = d.AssignmentId,
                WorkPlanId = plan.WorkPlanId,
                WorkTemplateId = dutyTemplateId,
                Name = d.Name,
                SchedulePurpose = "Duty",
                StartDateTime = d.Start,
                EndDateTime = d.End,
                GraceMinutes = d.GraceMinutes,
                RequiresAttendance = true,
                IsValid = true,
                WorkAssignmentStateId = 1,
                CreatedDate = DateTime.UtcNow
            };
            db.WorkAssignments.Add(dutyAssignment);

            db.WorkPlanSegments.Add(new WorkPlanSegment
            {
                WorkPlanSegmentId = ++nextSegId,
                WorkPlanId = plan.WorkPlanId,
                WorkSegmentTypeId = 5,
                Name = $"{d.Name} · Check in",
                SchedulePurpose = "Duty",
                SequenceNumber = seq++,
                StartDateTime = d.Start,
                EndDateTime = d.Start,
                GraceBeforeMinutes = 60,
                GraceAfterMinutes = Math.Max(d.GraceMinutes, 60),
                RequiresAttendance = true,
                IsMandatory = true,
                CreatedDate = DateTime.UtcNow
            });

            db.WorkPlanSegments.Add(new WorkPlanSegment
            {
                WorkPlanSegmentId = ++nextSegId,
                WorkPlanId = plan.WorkPlanId,
                WorkSegmentTypeId = 6,
                Name = $"{d.Name} · Check out",
                SchedulePurpose = "Duty",
                SequenceNumber = seq++,
                StartDateTime = d.End,
                EndDateTime = d.End,
                GraceBeforeMinutes = 30,
                GraceAfterMinutes = 180,
                RequiresAttendance = true,
                IsMandatory = true,
                CreatedDate = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        return plan;
    }

    private static async Task<List<AttendanceLog>> AddPunchesAsync(
        HrmTeContext db,
        params (int LogId, DateTime Timestamp, int InOutModeId, int LogModeId)[] punches)
    {
        var list = new List<AttendanceLog>();
        foreach (var p in punches)
        {
            var log = new AttendanceLog
            {
                AttendanceLogId = p.LogId,
                IndividualId = IndividualId,
                OrganisationId = OrgId,
                Date = p.Timestamp,
                InOutModeId = p.InOutModeId,
                AttendanceLogModeId = p.LogModeId,
                AttendanceLogStateId = SharedConfig.AttendanceLogStates.Valid
            };
            db.AttendanceLogs.Add(log);
            list.Add(log);
        }
        await db.SaveChangesAsync();
        return list;
    }

    // -------------------------------------------------------------------------
    // Case 1: Official 08:00-14:00 only, normal check-in/out -> 6h official
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Case01_OfficialOnly_NormalCheckInAndOut_Completes6Hours()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        await using (var db = factory.CreateDbContext())
        {
            await SeedPlanAsync(db, DefaultWorkDate, (new TimeOnly(8, 0), new TimeOnly(14, 0), 0, false));
            await AddPunchesAsync(db,
                (1, At(DefaultWorkDate, 8, 0), 3, 2),
                (2, At(DefaultWorkDate, 14, 0), 3, 2));
        }

        var svc = new AttendanceReconciliationService(factory);
        var res = await svc.ReconcileAsync(IndividualId, JobId, OrgId, DefaultWorkDate);

        Assert.Equal(AttendanceObligationState.Completed, res.OfficialState);
        Assert.Equal(360, res.PlannedOfficialMinutes);
        Assert.Equal(360, res.CreditedOfficialMinutes);
        Assert.Equal(0, res.OfficialLateMinutes);
        Assert.Equal(0, res.ConflictCount);
        Assert.True(res.IsComplete);
    }

    // -------------------------------------------------------------------------
    // Case 2: Official 08:00-14:00 with lunch out 12:30 and lunch in 13:00
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Case02_OfficialWithLunchOutAndIn_PreservesOfficialContinuity()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        await using (var db = factory.CreateDbContext())
        {
            await SeedPlanAsync(db, DefaultWorkDate, (new TimeOnly(8, 0), new TimeOnly(14, 0), 0, true));
            await AddPunchesAsync(db,
                (1, At(DefaultWorkDate, 8, 0), 3, 2),
                (2, At(DefaultWorkDate, 12, 30), 3, 2),
                (3, At(DefaultWorkDate, 13, 0), 3, 2),
                (4, At(DefaultWorkDate, 14, 0), 3, 2));
        }

        var svc = new AttendanceReconciliationService(factory);
        var res = await svc.ReconcileAsync(IndividualId, JobId, OrgId, DefaultWorkDate);

        Assert.Equal(AttendanceObligationState.Completed, res.OfficialState);
        Assert.Equal(At(DefaultWorkDate, 8, 0), res.OfficialCheckIn);
        Assert.Equal(At(DefaultWorkDate, 12, 30), res.OfficialBreakOut);
        Assert.Equal(At(DefaultWorkDate, 13, 0), res.OfficialBreakIn);
        Assert.Equal(At(DefaultWorkDate, 14, 0), res.OfficialCheckOut);
        Assert.Equal(30, res.OfficialBreakMinutes);
        Assert.Equal(0, res.ExcessBreakMinutes);
        Assert.Equal(360, res.CreditedOfficialMinutes);
    }

    // -------------------------------------------------------------------------
    // Case 3 & Regression Test: Official 08:00-14:00, Duty 10:00-12:00,
    // Events 08:00, 10:00, 12:30 -> Official check-in 08:00, Duty 10:00-12:30
    // (credited 120m, NOT 150m), 12:30 shared with OfficialBreakOut, Official
    // check-out remains missing (not falsely completed at 12:30).
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Case03_Regression_Official08To14_Duty10To12_Events0800_1000_1230_ResolvesSharedEventAndPreservesOfficial()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        await using (var db = factory.CreateDbContext())
        {
            await SeedPlanAsync(
                db,
                DefaultWorkDate,
                (new TimeOnly(8, 0), new TimeOnly(14, 0), 0, true),
                (new TimeOnly(12, 0), new TimeOnly(13, 0)),
                (901, "Morning Duty", At(DefaultWorkDate, 10, 0), At(DefaultWorkDate, 12, 0), 15));

            await AddPunchesAsync(db,
                (1001, At(DefaultWorkDate, 8, 0), 3, 2),
                (1002, At(DefaultWorkDate, 10, 0), 3, 2),
                (1003, At(DefaultWorkDate, 12, 30), 3, 2));
        }

        var svc = new AttendanceReconciliationService(factory);
        var res = await svc.ReconcileAsync(IndividualId, JobId, OrgId, DefaultWorkDate);

        // 1. Official check-in is 08:00, Official check-out is NULL (NOT 12:30!)
        Assert.Equal(At(DefaultWorkDate, 8, 0), res.OfficialCheckIn);
        Assert.Null(res.OfficialCheckOut);
        Assert.Equal(At(DefaultWorkDate, 12, 30), res.OfficialBreakOut);
        Assert.NotEqual(AttendanceObligationState.Completed, res.OfficialState);

        // 2. Duty obligation is completed, actual presence = 150m (10:00-12:30), credited = 120m (clamped to 10:00-12:00)
        var dutyOb = Assert.Single(res.Obligations, o => o.ObligationType == "Duty");
        Assert.Equal(AttendanceObligationState.Completed, dutyOb.State);
        Assert.Equal(At(DefaultWorkDate, 10, 0), dutyOb.ActualStartDateTime);
        Assert.Equal(At(DefaultWorkDate, 12, 30), dutyOb.ActualEndDateTime);
        Assert.Equal(120, dutyOb.PlannedMinutes);
        Assert.Equal(150, dutyOb.ActualPresenceMinutes);
        Assert.Equal(120, dutyOb.CreditedWorkedMinutes);
        Assert.True(dutyOb.SharesEndEvent);

        // 3. Physical event 1003 (12:30) has TWO active persisted resolutions:
        //    Primary = OfficialBreak (OfficialBreakOut)
        //    Secondary = Duty (DutyCheckOut)
        var ev1230 = Assert.Single(res.Events, e => e.AttendanceLogId == 1003);
        Assert.True(ev1230.IsSharedAcrossObligations);
        Assert.Equal(2, ev1230.Associations.Count);
        Assert.Contains(ev1230.Associations, a => a.IsPrimaryClassification && a.LogicalEventType == "OfficialBreakOut");
        Assert.Contains(ev1230.Associations, a => !a.IsPrimaryClassification && a.LogicalEventType == "DutyCheckOut");

        await using (var db = factory.CreateDbContext())
        {
            var persistedFor1230 = await db.AttendanceLogResolutions
                .Where(r => r.AttendanceLogId == 1003 && r.IsValid)
                .ToListAsync();
            Assert.Equal(2, persistedFor1230.Count);
            Assert.Single(persistedFor1230, r => r.IsPrimaryClassification);
        }
    }

    // -------------------------------------------------------------------------
    // Case 4: Official 08:00-14:00, Duty 10:00-12:00, Events 08:00, 10:00, 12:00, 14:00
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Case04_Official08To14_Duty10To12_CompleteEvents_CompletesBothWithoutDoubleCounting()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        await using (var db = factory.CreateDbContext())
        {
            await SeedPlanAsync(
                db,
                DefaultWorkDate,
                (new TimeOnly(8, 0), new TimeOnly(14, 0), 0, false),
                null,
                (901, "Inside Duty", At(DefaultWorkDate, 10, 0), At(DefaultWorkDate, 12, 0), 10));

            await AddPunchesAsync(db,
                (1, At(DefaultWorkDate, 8, 0), 3, 2),
                (2, At(DefaultWorkDate, 10, 0), 3, 2),
                (3, At(DefaultWorkDate, 12, 0), 3, 2),
                (4, At(DefaultWorkDate, 14, 0), 3, 2));
        }

        var svc = new AttendanceReconciliationService(factory);
        var res = await svc.ReconcileAsync(IndividualId, JobId, OrgId, DefaultWorkDate);

        Assert.Equal(AttendanceObligationState.Completed, res.OfficialState);
        Assert.Equal(360, res.CreditedOfficialMinutes);
        Assert.Equal(120, res.CreditedDutyMinutes);
        // Duty is 100% inside Official hours -> TotalCreditedPayableMinutes is 360 (not 480!)
        Assert.Equal(360, res.TotalCreditedPayableMinutes);
        Assert.True(res.IsComplete);
    }

    // -------------------------------------------------------------------------
    // Case 5: Official 08:00-14:00, Duty 10:00-12:00, Events 08:00, 10:00, 12:30, 13:00, 14:00
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Case05_Official08To14_Duty10To12_WithDelayedDutyOutLunchAndOfficialOut_CompletesAll()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        await using (var db = factory.CreateDbContext())
        {
            await SeedPlanAsync(
                db,
                DefaultWorkDate,
                (new TimeOnly(8, 0), new TimeOnly(14, 0), 0, true),
                (new TimeOnly(12, 0), new TimeOnly(13, 0)),
                (901, "Inside Duty", At(DefaultWorkDate, 10, 0), At(DefaultWorkDate, 12, 0), 10));

            await AddPunchesAsync(db,
                (1, At(DefaultWorkDate, 8, 0), 3, 2),
                (2, At(DefaultWorkDate, 10, 0), 3, 2),
                (3, At(DefaultWorkDate, 12, 30), 3, 2),
                (4, At(DefaultWorkDate, 13, 0), 3, 2),
                (5, At(DefaultWorkDate, 14, 0), 3, 2));
        }

        var svc = new AttendanceReconciliationService(factory);
        var res = await svc.ReconcileAsync(IndividualId, JobId, OrgId, DefaultWorkDate);

        Assert.Equal(AttendanceObligationState.Completed, res.OfficialState);
        Assert.Equal(At(DefaultWorkDate, 8, 0), res.OfficialCheckIn);
        Assert.Equal(At(DefaultWorkDate, 12, 30), res.OfficialBreakOut);
        Assert.Equal(At(DefaultWorkDate, 13, 0), res.OfficialBreakIn);
        Assert.Equal(At(DefaultWorkDate, 14, 0), res.OfficialCheckOut);
        Assert.Equal(120, res.CreditedDutyMinutes);
        Assert.Equal(360, res.CreditedOfficialMinutes);
        Assert.Equal(1, res.SharedEventCount);
    }

    // -------------------------------------------------------------------------
    // Case 6: Official 08:00-14:00, Duty 08:00-10:00, Events 08:00, 10:00, 14:00
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Case06_CoincidentStart0800_SharesStartBetweenOfficialAndDuty()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        await using (var db = factory.CreateDbContext())
        {
            await SeedPlanAsync(
                db,
                DefaultWorkDate,
                (new TimeOnly(8, 0), new TimeOnly(14, 0), 0, false),
                null,
                (901, "Early Duty", At(DefaultWorkDate, 8, 0), At(DefaultWorkDate, 10, 0), 10));

            await AddPunchesAsync(db,
                (1, At(DefaultWorkDate, 8, 0), 3, 2),
                (2, At(DefaultWorkDate, 10, 0), 3, 2),
                (3, At(DefaultWorkDate, 14, 0), 3, 2));
        }

        var svc = new AttendanceReconciliationService(factory);
        var res = await svc.ReconcileAsync(IndividualId, JobId, OrgId, DefaultWorkDate);

        Assert.Equal(AttendanceObligationState.Completed, res.OfficialState);
        Assert.Equal(360, res.CreditedOfficialMinutes);
        Assert.Equal(120, res.CreditedDutyMinutes);
        Assert.Equal(1, res.SharedEventCount);

        var ev0800 = Assert.Single(res.Events, e => e.AttendanceLogId == 1);
        Assert.True(ev0800.IsSharedAcrossObligations);
        Assert.Contains(ev0800.Associations, a => a.IsPrimaryClassification && a.LogicalEventType == "OfficialCheckIn");
        Assert.Contains(ev0800.Associations, a => !a.IsPrimaryClassification && a.LogicalEventType == "DutyCheckIn");
    }

    // -------------------------------------------------------------------------
    // Case 7: Official 08:00-14:00, Duty 12:00-14:00, Events 08:00, 12:00, 14:00
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Case07_CoincidentEnd1400_SharesEndBetweenOfficialAndDuty()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        await using (var db = factory.CreateDbContext())
        {
            await SeedPlanAsync(
                db,
                DefaultWorkDate,
                (new TimeOnly(8, 0), new TimeOnly(14, 0), 0, false),
                null,
                (901, "Noon Duty", At(DefaultWorkDate, 12, 0), At(DefaultWorkDate, 14, 0), 10));

            await AddPunchesAsync(db,
                (1, At(DefaultWorkDate, 8, 0), 3, 2),
                (2, At(DefaultWorkDate, 12, 0), 3, 2),
                (3, At(DefaultWorkDate, 14, 0), 3, 2));
        }

        var svc = new AttendanceReconciliationService(factory);
        var res = await svc.ReconcileAsync(IndividualId, JobId, OrgId, DefaultWorkDate);

        Assert.Equal(AttendanceObligationState.Completed, res.OfficialState);
        Assert.Equal(360, res.CreditedOfficialMinutes);
        Assert.Equal(120, res.CreditedDutyMinutes);
        Assert.Equal(1, res.SharedEventCount);

        var ev1400 = Assert.Single(res.Events, e => e.AttendanceLogId == 3);
        Assert.True(ev1400.IsSharedAcrossObligations);
        Assert.Contains(ev1400.Associations, a => a.IsPrimaryClassification && a.LogicalEventType == "OfficialCheckOut");
        Assert.Contains(ev1400.Associations, a => !a.IsPrimaryClassification && a.LogicalEventType == "DutyCheckOut");
    }

    // -------------------------------------------------------------------------
    // Case 8: Official 08:00-14:00, Duty 13:00-16:00, Events 08:00, 13:00, 14:00, 16:00
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Case08_DutyOverlappingOfficialEnd_ResolvesBothAndComputesNonDoubleCountedUnion()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        await using (var db = factory.CreateDbContext())
        {
            await SeedPlanAsync(
                db,
                DefaultWorkDate,
                (new TimeOnly(8, 0), new TimeOnly(14, 0), 0, false),
                null,
                (901, "Overlap End Duty", At(DefaultWorkDate, 13, 0), At(DefaultWorkDate, 16, 0), 10));

            await AddPunchesAsync(db,
                (1, At(DefaultWorkDate, 8, 0), 3, 2),
                (2, At(DefaultWorkDate, 13, 0), 3, 2),
                (3, At(DefaultWorkDate, 14, 0), 3, 2),
                (4, At(DefaultWorkDate, 16, 0), 3, 2));
        }

        var svc = new AttendanceReconciliationService(factory);
        var res = await svc.ReconcileAsync(IndividualId, JobId, OrgId, DefaultWorkDate);

        Assert.Equal(AttendanceObligationState.Completed, res.OfficialState);
        Assert.Equal(360, res.CreditedOfficialMinutes);
        Assert.Equal(180, res.CreditedDutyMinutes);
        // 08:00-14:00 (360m) union 13:00-16:00 (180m, 60m overlap, 120m outside) = 480m total payable
        Assert.Equal(60, res.DutyWithinOfficialMinutes);
        Assert.Equal(120, res.DutyOutsideOfficialMinutes);
        Assert.Equal(480, res.TotalCreditedPayableMinutes);
    }

    // -------------------------------------------------------------------------
    // Case 9: Official 08:00-14:00, Duty 16:00-18:00, Events 08:00, 14:00, 16:00, 18:00
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Case09_DutyOutsideOfficialHours_AddsSeparatePayableDuration()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        await using (var db = factory.CreateDbContext())
        {
            await SeedPlanAsync(
                db,
                DefaultWorkDate,
                (new TimeOnly(8, 0), new TimeOnly(14, 0), 0, false),
                null,
                (901, "Evening Duty", At(DefaultWorkDate, 16, 0), At(DefaultWorkDate, 18, 0), 10));

            await AddPunchesAsync(db,
                (1, At(DefaultWorkDate, 8, 0), 3, 2),
                (2, At(DefaultWorkDate, 14, 0), 3, 2),
                (3, At(DefaultWorkDate, 16, 0), 3, 2),
                (4, At(DefaultWorkDate, 18, 0), 3, 2));
        }

        var svc = new AttendanceReconciliationService(factory);
        var res = await svc.ReconcileAsync(IndividualId, JobId, OrgId, DefaultWorkDate);

        Assert.Equal(360, res.CreditedOfficialMinutes);
        Assert.Equal(120, res.CreditedDutyMinutes);
        Assert.Equal(480, res.TotalCreditedPayableMinutes);
    }

    // -------------------------------------------------------------------------
    // Case 10: Official 08:00-14:00, Duty 16:00-18:00, Events 08:00, 16:00, 18:00
    // Missing official clock-out must NOT consume 18:00 duty exit!
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Case10_MissingOfficialClockOut_DoesNotSteal1800DutyExit()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        await using (var db = factory.CreateDbContext())
        {
            await SeedPlanAsync(
                db,
                DefaultWorkDate,
                (new TimeOnly(8, 0), new TimeOnly(14, 0), 0, false),
                null,
                (901, "Evening Duty", At(DefaultWorkDate, 16, 0), At(DefaultWorkDate, 18, 0), 10));

            await AddPunchesAsync(db,
                (1, At(DefaultWorkDate, 8, 0), 3, 2),
                (2, At(DefaultWorkDate, 16, 0), 3, 2),
                (3, At(DefaultWorkDate, 18, 0), 3, 2));
        }

        var svc = new AttendanceReconciliationService(factory);
        var res = await svc.ReconcileAsync(IndividualId, JobId, OrgId, DefaultWorkDate);

        Assert.Equal(AttendanceObligationState.MissingClockOut, res.OfficialState);
        Assert.Null(res.OfficialCheckOut);

        var dutyOb = Assert.Single(res.Obligations, o => o.ObligationType == "Duty");
        Assert.Equal(AttendanceObligationState.Completed, dutyOb.State);
        Assert.Equal(120, dutyOb.CreditedWorkedMinutes);
    }

    // -------------------------------------------------------------------------
    // Case 11: Official 08:00-14:00, Duty A 10:00-12:00, Duty B 12:00-14:00,
    // Events 08:00, 10:00, 12:00, 14:00 -> 12:00 shared between Duty A out & Duty B in
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Case11_BackToBackDuties_Shares1200BoundaryAnd1400OfficialEnd()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        await using (var db = factory.CreateDbContext())
        {
            await SeedPlanAsync(
                db,
                DefaultWorkDate,
                (new TimeOnly(8, 0), new TimeOnly(14, 0), 0, false),
                null,
                (901, "Duty A", At(DefaultWorkDate, 10, 0), At(DefaultWorkDate, 12, 0), 10),
                (902, "Duty B", At(DefaultWorkDate, 12, 0), At(DefaultWorkDate, 14, 0), 10));

            await AddPunchesAsync(db,
                (1, At(DefaultWorkDate, 8, 0), 3, 2),
                (2, At(DefaultWorkDate, 10, 0), 3, 2),
                (3, At(DefaultWorkDate, 12, 0), 3, 2),
                (4, At(DefaultWorkDate, 14, 0), 3, 2));
        }

        var svc = new AttendanceReconciliationService(factory);
        var res = await svc.ReconcileAsync(IndividualId, JobId, OrgId, DefaultWorkDate);

        Assert.Equal(AttendanceObligationState.Completed, res.OfficialState);
        var duties = res.Obligations.Where(o => o.ObligationType == "Duty").OrderBy(o => o.ScheduledStart).ToList();
        Assert.Equal(2, duties.Count);
        Assert.All(duties, d => Assert.Equal(AttendanceObligationState.Completed, d.State));
        Assert.Equal(240, res.CreditedDutyMinutes);
        Assert.Equal(2, res.SharedEventCount); // 12:00 (Duty A Out + Duty B In) and 14:00 (Official Out + Duty B Out)
    }

    // -------------------------------------------------------------------------
    // Case 12: Duplicate punches at 08:00:05 and 08:00:40 -> second is DuplicateIgnored
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Case12_DuplicatePunchesWithinSuppressionThreshold_IgnoresSecondPunch()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        await using (var db = factory.CreateDbContext())
        {
            await SeedPlanAsync(db, DefaultWorkDate, (new TimeOnly(8, 0), new TimeOnly(14, 0), 0, false));
            await AddPunchesAsync(db,
                (1, At(DefaultWorkDate, 8, 0, 5), 3, 2),
                (2, At(DefaultWorkDate, 8, 0, 40), 3, 2),
                (3, At(DefaultWorkDate, 14, 0, 0), 3, 2));
        }

        var svc = new AttendanceReconciliationService(factory);
        var res = await svc.ReconcileAsync(IndividualId, JobId, OrgId, DefaultWorkDate);

        Assert.Equal(1, res.DuplicateSuppressedCount);
        Assert.Equal(AttendanceObligationState.Completed, res.OfficialState);
        var dupEvent = Assert.Single(res.Events, e => e.AttendanceLogId == 2);
        Assert.True(dupEvent.IsDuplicateSuppressed);
        Assert.Equal("DuplicateIgnored", dupEvent.Associations[0].LogicalEventType);
    }

    // -------------------------------------------------------------------------
    // Case 13: Overnight Official 20:00-02:00 with Duty 23:00-01:00
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Case13_OvernightOfficial20To02_WithDuty23To01_ResolvesCrossMidnightEvents()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        var nextDay = DefaultWorkDate.AddDays(1);
        await using (var db = factory.CreateDbContext())
        {
            await SeedPlanAsync(
                db,
                DefaultWorkDate,
                (new TimeOnly(20, 0), new TimeOnly(2, 0), 15, false),
                null,
                (901, "Night Duty", At(DefaultWorkDate, 23, 0), At(nextDay, 1, 0), 15));

            await AddPunchesAsync(db,
                (1, At(DefaultWorkDate, 20, 0), 3, 2),
                (2, At(DefaultWorkDate, 23, 0), 3, 2),
                (3, At(nextDay, 1, 0), 3, 2),
                (4, At(nextDay, 2, 0), 3, 2));
        }

        var svc = new AttendanceReconciliationService(factory);
        var res = await svc.ReconcileAsync(IndividualId, JobId, OrgId, DefaultWorkDate);

        Assert.Equal(AttendanceObligationState.Completed, res.OfficialState);
        Assert.Equal(360, res.CreditedOfficialMinutes);
        Assert.Equal(120, res.CreditedDutyMinutes);
        Assert.Equal(360, res.TotalCreditedPayableMinutes);
    }

    // -------------------------------------------------------------------------
    // Case 14: Late official arrival at 08:25, Duty at 10:00-12:00
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Case14_LateOfficialArrival0825_Records25LateMinutesAndCompletesDuty()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        await using (var db = factory.CreateDbContext())
        {
            await SeedPlanAsync(
                db,
                DefaultWorkDate,
                (new TimeOnly(8, 0), new TimeOnly(14, 0), 0, false),
                null,
                (901, "Midday Duty", At(DefaultWorkDate, 10, 0), At(DefaultWorkDate, 12, 0), 10));

            await AddPunchesAsync(db,
                (1, At(DefaultWorkDate, 8, 25), 3, 2),
                (2, At(DefaultWorkDate, 10, 0), 3, 2),
                (3, At(DefaultWorkDate, 12, 0), 3, 2),
                (4, At(DefaultWorkDate, 14, 0), 3, 2));
        }

        var svc = new AttendanceReconciliationService(factory);
        var res = await svc.ReconcileAsync(IndividualId, JobId, OrgId, DefaultWorkDate);

        Assert.Equal(25, res.OfficialLateMinutes);
        Assert.Equal(335, res.CreditedOfficialMinutes);
        Assert.Equal(120, res.CreditedDutyMinutes);
    }

    // -------------------------------------------------------------------------
    // Case 15: Early official departure at 13:20 after completing Duty 10:00-12:00
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Case15_EarlyOfficialDeparture1320_Records40EarlyMinutes()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        await using (var db = factory.CreateDbContext())
        {
            await SeedPlanAsync(
                db,
                DefaultWorkDate,
                (new TimeOnly(8, 0), new TimeOnly(14, 0), 0, false),
                null,
                (901, "Midday Duty", At(DefaultWorkDate, 10, 0), At(DefaultWorkDate, 12, 0), 10));

            await AddPunchesAsync(db,
                (1, At(DefaultWorkDate, 8, 0), 3, 2),
                (2, At(DefaultWorkDate, 10, 0), 3, 2),
                (3, At(DefaultWorkDate, 12, 0), 3, 2),
                (4, At(DefaultWorkDate, 13, 20), 2, 1)); // Explicit CheckOut at 13:20
        }

        var svc = new AttendanceReconciliationService(factory);
        var res = await svc.ReconcileAsync(IndividualId, JobId, OrgId, DefaultWorkDate);

        Assert.Equal(40, res.OfficialEarlyDepartureMinutes);
        Assert.Equal(320, res.CreditedOfficialMinutes);
        Assert.Equal(120, res.CreditedDutyMinutes);
    }

    // -------------------------------------------------------------------------
    // Case 16: Duty without any duty clock events while official attendance is complete
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Case16_DutyWithoutClockEvents_WhileOfficialComplete_MarksDutyMissingClockIn()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        await using (var db = factory.CreateDbContext())
        {
            await SeedPlanAsync(
                db,
                DefaultWorkDate,
                (new TimeOnly(8, 0), new TimeOnly(14, 0), 0, false),
                null,
                (901, "Missed Evening Duty", At(DefaultWorkDate, 16, 0), At(DefaultWorkDate, 18, 0), 10));

            await AddPunchesAsync(db,
                (1, At(DefaultWorkDate, 8, 0), 3, 2),
                (2, At(DefaultWorkDate, 14, 0), 3, 2));
        }

        var svc = new AttendanceReconciliationService(factory);
        var res = await svc.ReconcileAsync(IndividualId, JobId, OrgId, DefaultWorkDate);

        Assert.Equal(AttendanceObligationState.Completed, res.OfficialState);
        var dutyOb = Assert.Single(res.Obligations, o => o.ObligationType == "Duty");
        Assert.Equal(AttendanceObligationState.MissingClockIn, dutyOb.State);
        Assert.Equal(0, dutyOb.CreditedWorkedMinutes);
        Assert.False(res.IsComplete);
    }

    // -------------------------------------------------------------------------
    // Case 17: Official absent while outside Duty 16:00-18:00 is completed
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Case17_OfficialAbsent_WhileOutsideDutyCompleted_MarksOfficialMissingAndDutyCompleted()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        await using (var db = factory.CreateDbContext())
        {
            await SeedPlanAsync(
                db,
                DefaultWorkDate,
                (new TimeOnly(8, 0), new TimeOnly(14, 0), 0, false),
                null,
                (901, "Evening Duty", At(DefaultWorkDate, 16, 0), At(DefaultWorkDate, 18, 0), 10));

            await AddPunchesAsync(db,
                (1, At(DefaultWorkDate, 16, 0), 3, 2),
                (2, At(DefaultWorkDate, 18, 0), 3, 2));
        }

        var svc = new AttendanceReconciliationService(factory);
        var res = await svc.ReconcileAsync(IndividualId, JobId, OrgId, DefaultWorkDate);

        Assert.Equal(AttendanceObligationState.MissingClockIn, res.OfficialState);
        Assert.Equal(0, res.CreditedOfficialMinutes);
        var dutyOb = Assert.Single(res.Obligations, o => o.ObligationType == "Duty");
        Assert.Equal(AttendanceObligationState.Completed, dutyOb.State);
        Assert.Equal(120, dutyOb.CreditedWorkedMinutes);
    }

    // -------------------------------------------------------------------------
    // Case 18: Two overlapping duties with ambiguous single event -> Conflict flagged
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Case18_TwoOverlappingDutiesSameStart_SinglePunch_FlagsAmbiguousConflict()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        await using (var db = factory.CreateDbContext())
        {
            await SeedPlanAsync(
                db,
                DefaultWorkDate,
                (new TimeOnly(8, 0), new TimeOnly(14, 0), 0, false),
                null,
                (901, "Duty Alpha", At(DefaultWorkDate, 10, 0), At(DefaultWorkDate, 12, 0), 10),
                (902, "Duty Beta", At(DefaultWorkDate, 10, 0), At(DefaultWorkDate, 12, 0), 10));

            await AddPunchesAsync(db,
                (1, At(DefaultWorkDate, 8, 0), 3, 2),
                (2, At(DefaultWorkDate, 10, 0), 3, 2),
                (3, At(DefaultWorkDate, 14, 0), 3, 2));
        }

        var svc = new AttendanceReconciliationService(factory);
        var res = await svc.ReconcileAsync(IndividualId, JobId, OrgId, DefaultWorkDate);

        Assert.True(res.ConflictCount > 0);
        Assert.Contains(res.Events, e => e.ConfidenceState == "Conflict");
    }

    // -------------------------------------------------------------------------
    // Case 19: Explicit device direction (CheckIn/CheckOut/BreakOut/BreakIn)
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Case19_ExplicitDeviceDirection_UsesDeviceDirectionMethod()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        await using (var db = factory.CreateDbContext())
        {
            await SeedPlanAsync(db, DefaultWorkDate, (new TimeOnly(8, 0), new TimeOnly(14, 0), 0, true));
            await AddPunchesAsync(db,
                (1, At(DefaultWorkDate, 8, 0), 1, 1),   // Physical CheckIn
                (2, At(DefaultWorkDate, 12, 10), 3, 1), // Physical BreakOut
                (3, At(DefaultWorkDate, 12, 50), 4, 1), // Physical BreakIn
                (4, At(DefaultWorkDate, 14, 0), 2, 1)); // Physical CheckOut
        }

        var svc = new AttendanceReconciliationService(factory);
        var res = await svc.ReconcileAsync(IndividualId, JobId, OrgId, DefaultWorkDate);

        Assert.Equal(AttendanceObligationState.Completed, res.OfficialState);
        Assert.All(res.Events, e => Assert.True(e.IsDeviceDirectionExplicit));
        Assert.Equal(40, res.OfficialBreakMinutes);
    }

    // -------------------------------------------------------------------------
    // Case 20: Raw event without device direction (online automatic mode)
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Case20_OnlineAutoDirection_InfersFromScheduleBoundaries()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        await using (var db = factory.CreateDbContext())
        {
            await SeedPlanAsync(db, DefaultWorkDate, (new TimeOnly(8, 0), new TimeOnly(14, 0), 0, false));
            await AddPunchesAsync(db,
                (1, At(DefaultWorkDate, 7, 55), 3, 2),
                (2, At(DefaultWorkDate, 14, 5), 3, 2));
        }

        var svc = new AttendanceReconciliationService(factory);
        var res = await svc.ReconcileAsync(IndividualId, JobId, OrgId, DefaultWorkDate);

        Assert.Equal(AttendanceObligationState.Completed, res.OfficialState);
        Assert.All(res.Events, e => Assert.False(e.IsDeviceDirectionExplicit));
        Assert.Equal(360, res.CreditedOfficialMinutes);
        Assert.Equal(5, res.OvertimeCandidateMinutes);
    }

    // -------------------------------------------------------------------------
    // Case 21: Manual correction / override with audit trail
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Case21_ManualOverride_CreatesAuditRecordAndOverridesClassification()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        await using (var db = factory.CreateDbContext())
        {
            await SeedPlanAsync(
                db,
                DefaultWorkDate,
                (new TimeOnly(8, 0), new TimeOnly(14, 0), 0, false),
                null,
                (901, "Afternoon Duty", At(DefaultWorkDate, 15, 0), At(DefaultWorkDate, 17, 0), 10));

            await AddPunchesAsync(db,
                (1, At(DefaultWorkDate, 8, 0), 3, 2),
                (2, At(DefaultWorkDate, 15, 0), 3, 2),
                (3, At(DefaultWorkDate, 17, 0), 3, 2));
        }

        var svc = new AttendanceReconciliationService(factory);
        var updated = await svc.ApplyManualOverrideAsync(new AttendanceManualOverrideRequest
        {
            OrganisationId = OrgId,
            IndividualId = IndividualId,
            JobId = JobId,
            WorkDate = DefaultWorkDate,
            OverrideAction = "AcknowledgeMissingEvent",
            TargetSchedulePurpose = "Official",
            TargetClockTypeId = (int)AttendanceClockType.CheckOut,
            Reason = "Employee attended external ministry meeting at 14:00; approved by HR."
        });

        Assert.Equal(AttendanceObligationState.PartiallyResolved, updated.OfficialState);
        await using (var db = factory.CreateDbContext())
        {
            var ov = await db.AttendanceManualOverrides.SingleAsync();
            Assert.True(ov.IsActive);
            Assert.Contains("external ministry meeting", ov.Reason);
        }
    }

    // -------------------------------------------------------------------------
    // Case 22: Re-running reconciliation on the same date is idempotent
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Case22_ReRunningReconciliation_IsIdempotentAndDoesNotDuplicateActiveRows()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        await using (var db = factory.CreateDbContext())
        {
            await SeedPlanAsync(
                db,
                DefaultWorkDate,
                (new TimeOnly(8, 0), new TimeOnly(14, 0), 0, true),
                (new TimeOnly(12, 0), new TimeOnly(13, 0)),
                (901, "Morning Duty", At(DefaultWorkDate, 10, 0), At(DefaultWorkDate, 12, 0), 15));

            await AddPunchesAsync(db,
                (1001, At(DefaultWorkDate, 8, 0), 3, 2),
                (1002, At(DefaultWorkDate, 10, 0), 3, 2),
                (1003, At(DefaultWorkDate, 12, 30), 3, 2));
        }

        var svc = new AttendanceReconciliationService(factory);
        var run1 = await svc.ReconcileAsync(IndividualId, JobId, OrgId, DefaultWorkDate);
        var run2 = await svc.ReconcileAsync(IndividualId, JobId, OrgId, DefaultWorkDate);

        Assert.Equal(run1.OfficialState, run2.OfficialState);
        Assert.Equal(run1.CreditedOfficialMinutes, run2.CreditedOfficialMinutes);
        Assert.Equal(run1.CreditedDutyMinutes, run2.CreditedDutyMinutes);
        Assert.Equal(run1.SharedEventCount, run2.SharedEventCount);

        await using (var db = factory.CreateDbContext())
        {
            var activeResolutions = await db.AttendanceLogResolutions.Where(r => r.IsValid).ToListAsync();
            var activeObligations = await db.AttendanceObligations.Where(o => o.IsValid).ToListAsync();
            Assert.Equal(4, activeResolutions.Count);
            Assert.Equal(3, activeObligations.Count); // Official, OfficialBreak, Duty
        }
    }

    // -------------------------------------------------------------------------
    // Case 23: Duty starts before official schedule and overlaps official start (07:00-09:00 + 08:00-14:00)
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Case23_DutyBeforeAndOverlappingOfficialStart_ResolvesBothAndAvoidsDoubleCount()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        await using (var db = factory.CreateDbContext())
        {
            await SeedPlanAsync(
                db,
                DefaultWorkDate,
                (new TimeOnly(8, 0), new TimeOnly(14, 0), 0, false),
                null,
                (901, "Early Overlap Duty", At(DefaultWorkDate, 7, 0), At(DefaultWorkDate, 9, 0), 10));

            await AddPunchesAsync(db,
                (1, At(DefaultWorkDate, 7, 0), 3, 2),
                (2, At(DefaultWorkDate, 8, 0), 3, 2),
                (3, At(DefaultWorkDate, 9, 0), 3, 2),
                (4, At(DefaultWorkDate, 14, 0), 3, 2));
        }

        var svc = new AttendanceReconciliationService(factory);
        var res = await svc.ReconcileAsync(IndividualId, JobId, OrgId, DefaultWorkDate);

        Assert.Equal(AttendanceObligationState.Completed, res.OfficialState);
        Assert.Equal(360, res.CreditedOfficialMinutes);
        Assert.Equal(120, res.CreditedDutyMinutes);
        Assert.Equal(60, res.DutyWithinOfficialMinutes);
        Assert.Equal(60, res.DutyOutsideOfficialMinutes);
        Assert.Equal(420, res.TotalCreditedPayableMinutes); // 07:00-14:00 = 7 hours (420m)
    }

    // -------------------------------------------------------------------------
    // Case 24: Duty immediately adjacent to official schedule (06:00-08:00 and 14:00-16:00)
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Case24_AdjacentDutiesBeforeAndAfterOfficial_Shares0800And1400Boundaries()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        await using (var db = factory.CreateDbContext())
        {
            await SeedPlanAsync(
                db,
                DefaultWorkDate,
                (new TimeOnly(8, 0), new TimeOnly(14, 0), 0, false),
                null,
                (901, "Pre-Official Duty", At(DefaultWorkDate, 6, 0), At(DefaultWorkDate, 8, 0), 10),
                (902, "Post-Official Duty", At(DefaultWorkDate, 14, 0), At(DefaultWorkDate, 16, 0), 10));

            await AddPunchesAsync(db,
                (1, At(DefaultWorkDate, 6, 0), 3, 2),
                (2, At(DefaultWorkDate, 8, 0), 3, 2),
                (3, At(DefaultWorkDate, 14, 0), 3, 2),
                (4, At(DefaultWorkDate, 16, 0), 3, 2));
        }

        var svc = new AttendanceReconciliationService(factory);
        var res = await svc.ReconcileAsync(IndividualId, JobId, OrgId, DefaultWorkDate);

        Assert.Equal(AttendanceObligationState.Completed, res.OfficialState);
        Assert.Equal(360, res.CreditedOfficialMinutes);
        Assert.Equal(240, res.CreditedDutyMinutes);
        Assert.Equal(600, res.TotalCreditedPayableMinutes); // 06:00-16:00 = 10h
        Assert.Equal(2, res.SharedEventCount); // 08:00 and 14:00
    }

    // -------------------------------------------------------------------------
    // Case 25: Schedule/template modified after raw clock events were recorded
    // -------------------------------------------------------------------------
    [Fact]
    public async Task Case25_ScheduleModifiedAfterEventsRecorded_ReReconcilesWithUpdatedObligations()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        await using (var db = factory.CreateDbContext())
        {
            await SeedPlanAsync(db, DefaultWorkDate, (new TimeOnly(8, 0), new TimeOnly(14, 0), 0, false));
            await AddPunchesAsync(db,
                (1, At(DefaultWorkDate, 8, 0), 3, 2),
                (2, At(DefaultWorkDate, 10, 0), 3, 2),
                (3, At(DefaultWorkDate, 12, 0), 3, 2),
                (4, At(DefaultWorkDate, 14, 0), 3, 2));
        }

        var svc = new AttendanceReconciliationService(factory);
        var initial = await svc.ReconcileAsync(IndividualId, JobId, OrgId, DefaultWorkDate);
        Assert.Equal(0, initial.PlannedDutyMinutes);

        // Now add a Duty assignment (10:00-12:00) after punches were already recorded and reconciled
        await using (var db = factory.CreateDbContext())
        {
            var plan = await db.WorkPlans.SingleAsync(wp => wp.WorkDate == DefaultWorkDate.ToDateTime(TimeOnly.MinValue));
            db.WorkAssignments.Add(new WorkAssignment
            {
                WorkAssignmentId = 999,
                WorkPlanId = plan.WorkPlanId,
                WorkTemplateId = 99,
                Name = "Retroactive Duty",
                SchedulePurpose = "Duty",
                StartDateTime = At(DefaultWorkDate, 10, 0),
                EndDateTime = At(DefaultWorkDate, 12, 0),
                GraceMinutes = 10,
                RequiresAttendance = true,
                IsValid = true,
                WorkAssignmentStateId = 1,
                CreatedDate = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        var afterUpdate = await svc.ReconcileAsync(IndividualId, JobId, OrgId, DefaultWorkDate);
        Assert.Equal(120, afterUpdate.PlannedDutyMinutes);
        Assert.Equal(120, afterUpdate.CreditedDutyMinutes);
        Assert.Equal(360, afterUpdate.CreditedOfficialMinutes);
    }

    // -------------------------------------------------------------------------
    // Phase 8: Historical Batch Reconciliation (Dry-Run, Commit, Finalized Lock)
    // -------------------------------------------------------------------------
    [Fact]
    public async Task HistoricalBatchReconciliation_DryRunDoesNotMutate_CommitPersistsAndSkipsFinalizedPlans()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        var day1 = DefaultWorkDate;
        var day2 = DefaultWorkDate.AddDays(1);

        await using (var db = factory.CreateDbContext())
        {
            await SeedPlanAsync(
                db,
                day1,
                (new TimeOnly(8, 0), new TimeOnly(14, 0), 0, true),
                (new TimeOnly(12, 0), new TimeOnly(13, 0)),
                (901, "Day1 Duty", At(day1, 10, 0), At(day1, 12, 0), 15));

            await AddPunchesAsync(db,
                (2001, At(day1, 8, 0), 3, 2),
                (2002, At(day1, 10, 0), 3, 2),
                (2003, At(day1, 12, 30), 3, 2));

            var plan2 = await SeedPlanAsync(
                db,
                day2,
                (new TimeOnly(8, 0), new TimeOnly(14, 0), 0, false));
            plan2.IsFinalized = true;
            await db.SaveChangesAsync();
        }

        var svc = new AttendanceReconciliationService(factory);

        // 1. Dry-run preview
        var dryRun = await svc.ReconcileRangeAsync(new AttendanceBatchReconciliationRequest
        {
            OrganisationId = OrgId,
            IndividualId = IndividualId,
            FromDate = day1,
            ToDate = day2,
            DryRunOnly = true,
            AllowFinalizedPlanOverride = false,
            Reason = "Test dry-run"
        });

        Assert.True(dryRun.IsDryRun);
        Assert.Equal(2, dryRun.TotalEmployeeDaysEvaluated);
        Assert.Equal(1, dryRun.TotalSkippedFinalizedOrPayrollLocked);
        Assert.Equal(1, dryRun.TotalDaysChanged);

        await using (var db = factory.CreateDbContext())
        {
            Assert.Empty(await db.AttendanceObligations.ToListAsync());
        }

        // 2. Commit batch reconciliation
        var committed = await svc.ReconcileRangeAsync(new AttendanceBatchReconciliationRequest
        {
            OrganisationId = OrgId,
            IndividualId = IndividualId,
            FromDate = day1,
            ToDate = day2,
            DryRunOnly = false,
            AllowFinalizedPlanOverride = false,
            Reason = "Test commit"
        });

        Assert.False(committed.IsDryRun);
        Assert.NotNull(committed.ReconciliationRunId);
        Assert.Equal(1, committed.TotalDaysChanged);
        Assert.Equal(1, committed.TotalSkippedFinalizedOrPayrollLocked);

        await using (var db = factory.CreateDbContext())
        {
            Assert.NotEmpty(await db.AttendanceObligations.Where(o => o.IsValid).ToListAsync());
            Assert.NotEmpty(await db.AttendanceReconciliationRuns.ToListAsync());
        }
    }
}
