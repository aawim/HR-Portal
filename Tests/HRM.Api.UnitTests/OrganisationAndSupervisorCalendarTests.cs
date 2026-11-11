using HRM.DTOs.Calendar;
using HRM.Models;
using HRM.Services.Calendar;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class OrganisationAndSupervisorCalendarTests
{
    [Fact]
    public async Task Central_Calendar_Resolver_Identifies_Weekend_As_NonWorking()
    {
        var factory = new QueryFactory();
        var resolver = new OrganisationCalendarResolver(factory);

        // 2026-11-20 is a Friday (standard Maldives weekend/rest day)
        var friday = new DateTime(2026, 11, 20);
        var result = await resolver.ResolveAsync(1, friday);

        Assert.Equal(DayOfWeek.Friday, result.NormalWeekday);
        Assert.False(result.IsWorkingDay);
        Assert.True(result.IsHoliday);
        Assert.False(result.AttendanceRequired);
        Assert.False(result.LeaveChargeable);
    }

    [Fact]
    public async Task Approved_Calendar_Exception_Converts_NonWorking_Friday_To_Special_Working_Day()
    {
        var factory = new QueryFactory();
        var friday = new DateTime(2026, 11, 20);

        await using (var db = factory.CreateDbContext())
        {
            var ex = new OrganisationCalendarException
            {
                OrganisationBusinessEntityId = 1,
                EffectiveDate = friday,
                EffectiveEndDate = friday,
                TargetScope = "Organisation",
                CalendarImpactType = "MarkAsWorkingDay",
                Priority = 10,
                Reason = "Special Working Day",
                IsApproved = true,
                IsActive = true
            };
            ex.Segments.Add(new OrganisationCalendarExceptionSegment
            {
                StartTime = new TimeSpan(9, 0, 0),
                EndTime = new TimeSpan(15, 0, 0)
            });
            db.OrganisationCalendarExceptions.Add(ex);
            await db.SaveChangesAsync();
        }

        var resolver = new OrganisationCalendarResolver(factory);
        var result = await resolver.ResolveAsync(1, friday);

        Assert.True(result.IsWorkingDay);
        Assert.False(result.IsHoliday);
        Assert.True(result.IsSpecialWorkingDay);
        Assert.True(result.AttendanceRequired);
        Assert.True(result.LeaveChargeable);
        Assert.Equal(new TimeSpan(9, 0, 0), result.EffectiveStartTime);
        Assert.Equal(new TimeSpan(15, 0, 0), result.EffectiveEndTime);
    }

    [Fact]
    public async Task Approved_Calendar_Exception_Converts_Working_Monday_To_NonWorking_Day()
    {
        var factory = new QueryFactory();
        var monday = new DateTime(2026, 11, 23); // Monday is normally a working day

        await using (var db = factory.CreateDbContext())
        {
            var ex = new OrganisationCalendarException
            {
                OrganisationBusinessEntityId = 1,
                EffectiveDate = monday,
                EffectiveEndDate = monday,
                TargetScope = "Organisation",
                CalendarImpactType = "MarkAsNonWorkingDay",
                Priority = 10,
                Reason = "National Holiday",
                IsApproved = true,
                IsActive = true
            };
            db.OrganisationCalendarExceptions.Add(ex);
            await db.SaveChangesAsync();
        }

        var resolver = new OrganisationCalendarResolver(factory);
        var result = await resolver.ResolveAsync(1, monday);

        Assert.False(result.IsWorkingDay);
        Assert.True(result.IsHoliday);
        Assert.False(result.AttendanceRequired);
        Assert.False(result.LeaveChargeable);
        Assert.Null(result.EffectiveStartTime);
        Assert.Null(result.EffectiveEndTime);
    }

    [Fact]
    public async Task Supervisor_Can_Only_See_Supervised_Teams_And_Manage_Tasks()
    {
        var factory = new QueryFactory();
        var access = new FakeAccess();
        access.Actor!.IndividualId = 100;
        access.Organisation = 1;

        await using (var db = factory.CreateDbContext())
        {
            var t1 = new Team { TeamId = 10, OrganisationId = 1, Name = "Alpha Team", IsValid = true };
            var t2 = new Team { TeamId = 20, OrganisationId = 1, Name = "Beta Team", IsValid = true };
            db.Teams.AddRange(t1, t2);

            // User 100 supervises Team 10 only
            db.TeamStaffs.Add(new TeamStaff { TeamStaffsId = 1, TeamId = 10, StaffId = 100, IsSuperVisor = true, IsValid = true, StartDate = DateTime.Today.AddDays(-10) });
            await db.SaveChangesAsync();
        }

        var resolver = new OrganisationCalendarResolver(factory);
        var supervisorService = new SupervisorCalendarService(factory, access, resolver);

        var teams = await supervisorService.GetSupervisedTeamsAsync();
        var singleTeam = Assert.Single(teams);
        Assert.Equal(10, singleTeam.TeamId);
        Assert.Equal("Alpha Team", singleTeam.Name);

        // Unauthorized access to Team 20 returns empty
        var schedules = await supervisorService.GetTeamSchedulesAsync(DateTime.Today, DateTime.Today.AddDays(7), 20, null);
        Assert.Empty(schedules);

        // Add task for supervisor
        var taskId = await supervisorService.SaveTaskAsync(new SupervisorCalendarTaskDto
        {
            TeamId = 10,
            Title = "Verify Monthly Overtime",
            TaskCategory = "Payroll",
            DueDateTime = DateTime.Today.AddDays(2),
            Priority = "High"
        });

        Assert.True(taskId > 0);

        var tasks = await supervisorService.GetTasksAsync(DateTime.Today, DateTime.Today.AddDays(7), 10);
        var created = Assert.Single(tasks);
        Assert.Equal("Verify Monthly Overtime", created.Title);
        Assert.Equal("High", created.Priority);
    }
}
