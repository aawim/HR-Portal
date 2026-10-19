using System.Data;
using HRM.Components.Shared;
using HRM.DTOs.Leave;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Leave;

public class LeaveCalendarService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access, IOperationLogService logs)
{
    private const string PlanType = "Planned leave";
    private static DateTime Today => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Indian/Maldives").Date;

    public async Task<List<LeaveCalendarEntry>> LoadAsync(DateTime month, bool includeTeam)
    {
        var actor = await access.RequireContextAsync();
        var start = new DateTime(month.Year, month.Month, 1);
        var end = start.AddMonths(1);
        await using var db = await factory.CreateDbContextAsync();
        var job = await db.Jobs.AsNoTracking().SingleOrDefaultAsync(x => x.JobId == (actor.ActiveJob == null ? 0 : actor.ActiveJob.JobId));
        var organisationId = job?.OrganisationID ?? 0;
        var visible = new HashSet<int> { actor.IndividualId };
        if (includeTeam && job != null)
        {
            var today = Today;
            var departments = db.OrganisationStructureSupervisors.Where(x => x.SupervisorStaffId == actor.IndividualId && x.IsActive && x.IsValid &&
                x.OrganisationStructure.OrganisationBusinessEntityID == job.OrganisationID).Select(x => x.OrganisationStructureId);
            var supervisedTeams = db.TeamStaffs.Where(x => x.StaffId == actor.IndividualId && x.IsSuperVisor && x.IsValid && x.StartDate <= today &&
                (x.EndDate == null || x.EndDate >= today) && x.Team.OrganisationId == job.OrganisationID && x.Team.IsValid == true &&
                (x.Team.StartDate == null || x.Team.StartDate <= today) && (x.Team.EndDate == null || x.Team.EndDate >= today)).Select(x => x.TeamId);
            var members = db.TeamStaffs.Where(x => supervisedTeams.Contains(x.TeamId) && x.IsValid && x.StartDate <= today &&
                (x.EndDate == null || x.EndDate >= today)).Select(x => x.StaffId);
            var supervisorActive = await db.Users.AnyAsync(x => x.BusinessEntityID == actor.IndividualId && x.IsActive) &&
                job.JobStateId == SharedConfig.JobStates.APPROVED && job.TerminatedDate == null && job.JoinedDate <= today;
            if (supervisorActive)
            {
                var reports = await db.Jobs.AsNoTracking().Where(x => x.OrganisationID == job.OrganisationID &&
                    x.JobStateId == SharedConfig.JobStates.APPROVED && x.TerminatedDate == null && x.JoinedDate <= today &&
                    (departments.Contains(x.OrganisationStructureId) || members.Contains(x.IndividualID)))
                    .Select(x => x.IndividualID).Distinct().ToListAsync();
                visible.UnionWith(reports);
            }
        }
        var people = visible.ToArray();
        var plans = await db.BusinessEntitySchedules.AsNoTracking().Where(x => people.Contains(x.BusinessEntityId) && x.IsActive && x.IsLinked &&
            x.Schedule.IsValid && x.Schedule.ScheduleType.Name == PlanType && x.Schedule.StartDate < end && x.Schedule.EndDate >= start &&
            x.Schedule.CalendarSchedules.Any(c => c.Calendar.BusinessEntityCalendars.Any(b => b.BusinessEntityId == x.BusinessEntityId && b.IsLinked)))
            .Select(x => new { x.ScheduleId, x.BusinessEntityId, x.Schedule.StartDate, x.Schedule.EndDate, x.Schedule.Name }).ToListAsync();
        var names = await db.Individuals.AsNoTracking().Where(x => people.Contains(x.BusinessEntityId))
            .ToDictionaryAsync(x => x.BusinessEntityId, x => (x.FirstNameEnglish + " " + (x.MiddleNameEnglish ?? "") + " " + (x.LastNameEnglish ?? "")).Trim());
        var entries = plans.Select(x => new LeaveCalendarEntry(x.ScheduleId, x.BusinessEntityId, names.GetValueOrDefault(x.BusinessEntityId, "Employee"),
            x.StartDate, x.EndDate, x.Name, "Planned", true, x.BusinessEntityId == actor.IndividualId)).ToList();
        var leaves = await db.Leaves.AsNoTracking().Where(x => people.Contains(x.Job.IndividualID) && x.FromDate < end && x.ToDate >= start &&
            x.LeaveStateId != SharedConfig.LeaveStates.CANCELLED && x.LeaveStateId != SharedConfig.LeaveStates.REJECTED &&
            (x.Job.IndividualID == actor.IndividualId || x.Job.OrganisationID == organisationId))
            .Select(x => new { x.LeaveId, Person = x.Job.IndividualID, x.FromDate, x.ToDate, Title = x.LeaveType.Name, Status = x.LeaveState.StateName }).ToListAsync();
        entries.AddRange(leaves.Select(x => new LeaveCalendarEntry(x.LeaveId, x.Person, names.GetValueOrDefault(x.Person, "Employee"), x.FromDate, x.ToDate, x.Title, x.Status, false, x.Person == actor.IndividualId)));
        return entries.OrderBy(x => x.From).ThenBy(x => x.Person).ToList();
    }

    public async Task SavePlanAsync(DateTime from, DateTime to, string title)
    {
        var actor = await access.RequireContextAsync();
        from = from.Date; to = to.Date; title = title.Trim();
        if (from < Today || to < from || (to - from).TotalDays > 365 || title.Length is < 1 or > 100)
            throw new InvalidOperationException("Enter a title of up to 100 characters and a future date range of up to 366 days.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await db.Jobs.AnyAsync(x => x.IndividualID == actor.IndividualId && x.JobId == (actor.ActiveJob == null ? 0 : actor.ActiveJob.JobId) &&
            x.JobStateId == SharedConfig.JobStates.APPROVED && x.TerminatedDate == null && x.JoinedDate <= Today))
            throw new InvalidOperationException("An active approved job is required to plan leave.");
        if (await db.BusinessEntitySchedules.AnyAsync(x => x.BusinessEntityId == actor.IndividualId && x.IsLinked && x.IsActive && x.Schedule.IsValid &&
            x.Schedule.ScheduleType.Name == PlanType && x.Schedule.StartDate <= to && x.Schedule.EndDate >= from))
            throw new InvalidOperationException("These dates overlap an existing leave plan. Remove the old plan first to change it.");
        var log = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.SCHEDULE_CREATE, $"Saved private leave plan for staff {actor.IndividualId}: {from:yyyy-MM-dd} to {to:yyyy-MM-dd}.");
        var type = await db.ScheduleTypes.SingleOrDefaultAsync(x => x.Name == PlanType) ?? throw new InvalidOperationException("The planned leave calendar configuration is missing.");
        var link = await db.BusinessEntityCalendars.Include(x => x.Calender).Where(x => x.BusinessEntityId == actor.IndividualId && x.IsLinked && x.Calender.Name == "Leave planning").FirstOrDefaultAsync();
        if (link == null)
        {
            link = new BusinessEntityCalendar { BusinessEntityId = actor.IndividualId, StartDate = Today, IsLinked = true, OperationLogId = log.OperationLogId,
                Calender = new HRM.Models.Archives.Calendar { Name = "Leave planning", Description = "Private employee leave plans shared with current supervisors", IsPublic = false, IsGlobal = false, OperationLogId = log.OperationLogId } };
            db.BusinessEntityCalendars.Add(link);
        }
        var schedule = new Schedule { Name = title, StartDate = from, EndDate = to, IsPublic = false, IsGlobal = false, IsValid = true, ScheduleTypeId = type.ScheduleTypeId, OperationLogId = log.OperationLogId };
        schedule.BusinessEntitySchedules.Add(new() { BusinessEntityId = actor.IndividualId, IsLinked = true, IsActive = true, OperationLogId = log.OperationLogId });
        schedule.CalendarSchedules.Add(new() { Calendar = link.Calender });
        db.Schedules.Add(schedule);
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }

    public async Task RemovePlanAsync(int id)
    {
        var actor = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var schedule = await db.Schedules.SingleOrDefaultAsync(x => x.ScheduleId == id && x.IsValid && x.ScheduleType.Name == PlanType &&
            x.BusinessEntitySchedules.Any(b => b.BusinessEntityId == actor.IndividualId && b.IsLinked && b.IsActive));
        if (schedule == null) throw new InvalidOperationException("Your leave plan is no longer available.");
        var log = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.SCHEDULE_UPDATE, $"Removed leave plan {id} for staff {actor.IndividualId}.");
        schedule.IsValid = false; schedule.OperationLogId = log.OperationLogId;
        await db.SaveChangesAsync(); await tx.CommitAsync();
    }
}
