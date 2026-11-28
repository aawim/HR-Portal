using HRM.Helpers;
using HRM.Models;
using HRM.Models.WorkPlanning;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class GroupOverrideAndExpiryTests
{
    [Fact]
    public async Task IndependentGroup_And_SubGroup_OverrideJobTypeOt_AndRevertOnExpiry()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        var job = new Job { JobId = 200, IndividualID = 100, OrganisationID = 10, OrganisationStructureId = 1, JobTypeId = 5, JobStateId = 4, JoinedDate = new DateTime(2026, 1, 1) };

        using (var db = factory.CreateDbContext())
        {
            db.Jobs.Add(job);
            db.JobTypeOvertimeRules.Add(new JobTypeOvertimeRule
            {
                Id = 1,
                OrganisationId = 10,
                JobTypeId = 5,
                IsAllowed = true,
                MaximumWeeklyHours = 10m,
                MaximumDailyHours = 2m,
                Version = 1,
                OperationLogId = 1
            });

            var parentTeam = new Team
            {
                TeamId = 1,
                OrganisationId = 10,
                Name = "Operations",
                IsValid = true
            };
            var subTeam = new Team
            {
                TeamId = 2,
                ParentTeamId = 1,
                OrganisationId = 10,
                Name = "Operations Taskforce",
                IsValid = true,
                StartDate = new DateTime(2026, 11, 1),
                EndDate = new DateTime(2026, 11, 20),
                OvertimeEnabled = true,
                MaximumWeeklyOvertimeHours = 20m,
                MaximumDailyOvertimeHours = 4m
            };
            db.Teams.AddRange(parentTeam, subTeam);
            db.TeamStaffs.Add(new TeamStaff
            {
                TeamId = 2,
                StaffId = 100,
                IsValid = true,
                StartDate = new DateTime(2026, 11, 1),
                EndDate = new DateTime(2026, 11, 20)
            });

            var staffGroup = new StaffGroup
            {
                StaffGroupId = 1,
                OrganisationId = 10,
                Name = "Emergency Response Group",
                EffectiveFrom = new DateTime(2026, 11, 5),
                EffectiveTo = new DateTime(2026, 11, 10),
                IsActive = true,
                IsDissolved = false,
                OvertimeEnabled = true,
                MaximumWeeklyOvertimeHours = 30m,
                MaximumDailyOvertimeHours = 6m,
                CreatedByUserId = 1,
                CreatedAtUtc = DateTime.UtcNow,
                OperationLogId = 1
            };
            db.StaffGroups.Add(staffGroup);
            db.StaffGroupMembers.Add(new StaffGroupMember
            {
                StaffGroupMemberId = 1,
                StaffGroupId = 1,
                OrganisationId = 10,
                IndividualId = 100,
                JobId = 200,
                IsActive = true,
                JoinedAtUtc = DateTime.UtcNow,
                AddedByUserId = 1,
                OperationLogId = 1
            });
            await db.SaveChangesAsync();
        }

        using var verifyDb = factory.CreateDbContext();

        // Nov 3: Sub-team overrides Job Type OT (20h/week, 4h/day)
        var onNov3 = await TeamOvertimePolicy.ResolveAsync(verifyDb, job, new DateTime(2026, 11, 3, 14, 0, 0));
        Assert.NotNull(onNov3);
        Assert.Equal(20m, onNov3!.MaximumWeeklyHours);
        Assert.Equal(4m, onNov3.MaximumDailyHours);

        // Nov 7: Independent Staff Group overrides both Sub-team and Job Type OT (30h/week, 6h/day)
        var onNov7 = await TeamOvertimePolicy.ResolveAsync(verifyDb, job, new DateTime(2026, 11, 7, 10, 0, 0));
        Assert.NotNull(onNov7);
        Assert.Equal(30m, onNov7!.MaximumWeeklyHours);
        Assert.Equal(6m, onNov7.MaximumDailyHours);

        // Nov 10 (last day, with time component): still covered by Independent Staff Group
        var onNov10 = await TeamOvertimePolicy.ResolveAsync(verifyDb, job, new DateTime(2026, 11, 10, 23, 0, 0));
        Assert.NotNull(onNov10);
        Assert.Equal(30m, onNov10!.MaximumWeeklyHours);

        // Nov 12: Independent Staff Group expired -> reverts to Sub-team OT (20h/week, 4h/day)
        var onNov12 = await TeamOvertimePolicy.ResolveAsync(verifyDb, job, new DateTime(2026, 11, 12));
        Assert.NotNull(onNov12);
        Assert.Equal(20m, onNov12!.MaximumWeeklyHours);
        Assert.Equal(4m, onNov12.MaximumDailyHours);

        // Nov 22: Sub-team expired -> reverts to original Job Type OT rule (10h/week, 2h/day)
        var onNov22 = await TeamOvertimePolicy.ResolveAsync(verifyDb, job, new DateTime(2026, 11, 22));
        Assert.NotNull(onNov22);
        Assert.Equal(10m, onNov22!.MaximumWeeklyHours);
        Assert.Equal(2m, onNov22.MaximumDailyHours);
    }

    [Fact]
    public async Task IndependentGroup_OverridesSchedule_AndRevertsToOriginalScheduleOnExpiry()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        var job = new Job { JobId = 200, IndividualID = 100, OrganisationID = 10, OrganisationStructureId = 1, JobTypeId = 5, JobStateId = 4, JoinedDate = new DateTime(2026, 1, 1) };

        using (var db = factory.CreateDbContext())
        {
            db.WorkTemplateTypes.Add(new WorkTemplateType { WorkTemplateTypeId = 1, Code = "SHIFT", Name = "Shift", IsActive = true });
            db.WorkSegmentTypes.Add(new WorkSegmentType { WorkSegmentTypeId = 1, Code = "CHECK_IN", Name = "Check In", IsActive = true });
            db.WorkTemplates.AddRange(
                new WorkTemplate { WorkTemplateId = 1, WorkTemplateTypeId = 1, OrganisationBusinessEntityId = 10, Name = "Original Official Shift", DefaultStartTime = new TimeOnly(8, 0), DefaultEndTime = new TimeOnly(16, 0), IsActive = true },
                new WorkTemplate { WorkTemplateId = 2, WorkTemplateTypeId = 1, OrganisationBusinessEntityId = 10, Name = "Group Special Shift", DefaultStartTime = new TimeOnly(14, 0), DefaultEndTime = new TimeOnly(22, 0), IsActive = true });

            db.Jobs.Add(job);
            db.JobWorkTemplateAssignments.Add(new JobWorkTemplateAssignment
            {
                JobWorkTemplateAssignmentID = 1,
                JobID = 200,
                WorkTemplateID = 1,
                SchedulePurpose = "Official",
                ScheduleMode = "Standard",
                WorkingDaysMask = 127,
                EffectiveFrom = new DateTime(2026, 1, 1),
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            });

            db.StaffGroups.Add(new StaffGroup
            {
                StaffGroupId = 1,
                OrganisationId = 10,
                Name = "Night Ops Group",
                WorkTemplateId = 2,
                ScheduleMode = "Standard",
                WorkingDaysMask = 127,
                EffectiveFrom = new DateTime(2026, 11, 5),
                EffectiveTo = new DateTime(2026, 11, 10),
                IsActive = true,
                IsDissolved = false,
                CreatedByUserId = 1,
                CreatedAtUtc = DateTime.UtcNow,
                OperationLogId = 1
            });
            db.StaffGroupMembers.Add(new StaffGroupMember
            {
                StaffGroupMemberId = 1,
                StaffGroupId = 1,
                OrganisationId = 10,
                IndividualId = 100,
                JobId = 200,
                IsActive = true,
                JoinedAtUtc = DateTime.UtcNow,
                AddedByUserId = 1,
                OperationLogId = 1
            });
            await db.SaveChangesAsync();
        }

        using var verifyDb = factory.CreateDbContext();
        var candidates = await EffectiveStaffSchedules.LoadAsync(verifyDb, job, new DateTime(2026, 11, 1), new DateTime(2026, 11, 15));

        var duringGroup = EffectiveStaffSchedules.On(candidates, new DateTime(2026, 11, 7)).Single();
        Assert.Equal(2, duringGroup.Assignment.WorkTemplateID);
        Assert.StartsWith("Staff Group:", duringGroup.Source);

        var afterExpiry = EffectiveStaffSchedules.On(candidates, new DateTime(2026, 11, 11)).Single();
        Assert.Equal(1, afterExpiry.Assignment.WorkTemplateID);
        Assert.Equal("Staff override", afterExpiry.Source);
    }

    [Fact]
    public async Task SubGroup_ScheduleAdjustment_OverridesOfficialSchedule_AndRevertsOnExpiry()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        using (var db = factory.CreateDbContext())
        {
            db.Teams.AddRange(
                new Team { TeamId = 1, OrganisationId = 10, Name = "Main Team", IsValid = true },
                new Team
                {
                    TeamId = 2,
                    ParentTeamId = 1,
                    OrganisationId = 10,
                    Name = "Sub Team",
                    IsValid = true,
                    StartDate = new DateTime(2026, 11, 1),
                    EndDate = new DateTime(2026, 11, 10)
                });
            db.TeamStaffs.Add(new TeamStaff
            {
                TeamId = 2,
                StaffId = 100,
                IsValid = true,
                StartDate = new DateTime(2026, 11, 1),
                EndDate = new DateTime(2026, 11, 10)
            });
            db.TeamScheduleAdjustments.AddRange(
                new TeamScheduleAdjustment { Id = 1, TeamId = 2, WorkDate = new DateTime(2026, 11, 10), RevisedCheckIn = new TimeSpan(9, 30, 0), RevisedCheckOut = new TimeSpan(17, 30, 0), Reason = "Taskforce shift", IsActive = true, CreatedByUserId = 1, OperationLogId = 1 },
                new TeamScheduleAdjustment { Id = 2, TeamId = 2, WorkDate = new DateTime(2026, 11, 11), RevisedCheckIn = new TimeSpan(9, 30, 0), RevisedCheckOut = new TimeSpan(17, 30, 0), Reason = "Past expiry", IsActive = true, CreatedByUserId = 1, OperationLogId = 1 });
            await db.SaveChangesAsync();
        }

        using var verifyDb = factory.CreateDbContext();
        var activeAdjustment = await TeamScheduleAdjustments.FindAsync(verifyDb, 100, 10, new DateTime(2026, 11, 10));
        Assert.NotNull(activeAdjustment);
        Assert.Equal(new TimeSpan(9, 30, 0), activeAdjustment!.RevisedCheckIn);

        var expiredAdjustment = await TeamScheduleAdjustments.FindAsync(verifyDb, 100, 10, new DateTime(2026, 11, 11));
        Assert.Null(expiredAdjustment);
    }

    [Fact]
    public async Task GroupEnabledOvertime_ReflectsInPayrollSnapshot_WithGroupRateAndRevertsAfterExpiry()
    {
        var factory = new QueryFactory(ignoreTransactions: true);
        using (var db = factory.CreateDbContext())
        {
            db.Individuals.Add(new Individual { BusinessEntityId = 100, FirstNameEnglish = "Aishath", LastNameEnglish = "Nihad" });
            db.Jobs.Add(new Job { JobId = 200, IndividualID = 100, OrganisationID = 10, OrganisationStructureId = 1, JobTypeId = 5, JobStateId = 4, BasicSalary = 18000m, JoinedDate = new DateTime(2026, 1, 1) });
            // Job Type OT is disabled — employee only gets OT via Independent Staff Group
            db.JobTypeOvertimeRules.Add(new JobTypeOvertimeRule { Id = 1, OrganisationId = 10, JobTypeId = 5, IsAllowed = false, MaximumWeeklyHours = 0m, Version = 1, OperationLogId = 1 });

            db.StaffGroups.Add(new StaffGroup
            {
                StaffGroupId = 1,
                OrganisationId = 10,
                Name = "Special Event Taskforce",
                EffectiveFrom = new DateTime(2026, 11, 5),
                EffectiveTo = new DateTime(2026, 11, 10),
                IsActive = true,
                IsDissolved = false,
                OvertimeEnabled = true,
                MaximumWeeklyOvertimeHours = 25m,
                OvertimeRateMultiplier = 2.00m,
                CreatedByUserId = 1,
                CreatedAtUtc = DateTime.UtcNow,
                OperationLogId = 1
            });
            db.StaffGroupMembers.Add(new StaffGroupMember
            {
                StaffGroupMemberId = 1,
                StaffGroupId = 1,
                OrganisationId = 10,
                IndividualId = 100,
                JobId = 200,
                IsActive = true,
                JoinedAtUtc = new DateTime(2026, 11, 1),
                AddedByUserId = 1,
                OperationLogId = 1
            });

            var req1 = new Request { RequestId = 501, RequestStateId = 4, ReferenceNumber = "OT-501" };
            var req2 = new Request { RequestId = 502, RequestStateId = 4, ReferenceNumber = "OT-502" };
            db.Requests.AddRange(req1, req2);
            db.OvertimeRequests.AddRange(
                new OvertimeRequest { RequestId = 501, Request = req1, OrganisationId = 10, IndividualId = 100, JobId = 200, WorkDate = new DateTime(2026, 11, 8), Minutes = 120, Reason = "During group" }, // Sun Nov 8 (normal day)
                new OvertimeRequest { RequestId = 502, Request = req2, OrganisationId = 10, IndividualId = 100, JobId = 200, WorkDate = new DateTime(2026, 11, 12), Minutes = 120, Reason = "After group expiry" } // Thu Nov 12 (normal day)
            );
            await db.SaveChangesAsync();
        }

        var snapshotService = new HRM.Services.Payroll.PayrollSnapshotService(
            factory,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<HRM.Services.Payroll.PayrollSnapshotService>.Instance);
        var items = await snapshotService.CreateOrGetRunSnapshotsAsync(
            organisationId: 10,
            payrollRunId: 1,
            periodStart: new DateOnly(2026, 11, 1),
            periodEnd: new DateOnly(2026, 11, 30));

        var emp = Assert.Single(items);
        Assert.Equal(2, emp.OvertimeClaims.Count);

        // Basic 18,000 / 180 statutory divisor = 100 MVR/hr base
        var duringGroupClaim = emp.OvertimeClaims.Single(c => c.RequestId == 501);
        Assert.Equal(200m, duringGroupClaim.HourlyRate); // 2.00x StaffGroup multiplier
        Assert.Equal(400m, duringGroupClaim.Amount);     // 2 hours * 200

        var afterExpiryClaim = emp.OvertimeClaims.Single(c => c.RequestId == 502);
        Assert.Equal(125m, afterExpiryClaim.HourlyRate); // Reverted to 1.25x statutory normal multiplier
        Assert.Equal(250m, afterExpiryClaim.Amount);     // 2 hours * 125
    }
}
