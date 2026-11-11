using System.Data;
using HRM.DTOs.Calendar;
using HRM.Models;
using HRM.Services.Calendar.Contracts;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Calendar;

public sealed class SupervisorCalendarService(
    IDbContextFactory<HrmTeContext> factory,
    IUserAccessService access,
    IOrganisationCalendarResolver resolver)
    : global::HRM.Services.Calendar.Contracts.SupervisorCalendarServiceContracts.IOperations
{
    private static DateTime Today => TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Indian/Maldives").Date;

    private static string FormatName(Individual? ind, int id)
    {
        if (ind == null) return $"Staff #{id}";
        var name = (ind.FirstNameEnglish + " " + (ind.MiddleNameEnglish ?? "") + " " + (ind.LastNameEnglish ?? "")).Trim();
        return string.IsNullOrWhiteSpace(name) ? $"Staff #{id}" : name;
    }

    public async Task<List<SupervisorTeamOptionDto>> GetSupervisedTeamsAsync()
    {
        var actor = await access.RequireContextAsync();
        var today = Today;
        await using var db = await factory.CreateDbContextAsync();

        var supervisedTeamIds = await db.TeamStaffs.AsNoTracking()
            .Where(x => x.StaffId == actor.IndividualId && x.IsSuperVisor && x.IsValid &&
                        x.StartDate <= today && (x.EndDate == null || x.EndDate >= today) &&
                        x.Team.IsValid == true)
            .Select(x => x.TeamId)
            .Distinct()
            .ToListAsync();

        var teams = await db.Teams.AsNoTracking()
            .Where(t => supervisedTeamIds.Contains(t.TeamId))
            .Select(t => new SupervisorTeamOptionDto
            {
                TeamId = t.TeamId,
                Name = t.Name,
                MemberCount = t.TeamStaffs.Count(m => m.IsValid && m.StartDate <= today && (m.EndDate == null || m.EndDate >= today))
            })
            .ToListAsync();

        return teams;
    }

    public async Task<List<SupervisorCalendarScheduleItemDto>> GetTeamSchedulesAsync(DateTime from, DateTime to, int? teamId, int? staffId)
    {
        var actor = await access.RequireContextAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        var today = Today;
        from = from.Date;
        to = to.Date;

        await using var db = await factory.CreateDbContextAsync();

        // 1. Resolve supervised team IDs
        var supervisedTeamIds = await db.TeamStaffs.AsNoTracking()
            .Where(x => x.StaffId == actor.IndividualId && x.IsSuperVisor && x.IsValid &&
                        x.StartDate <= today && (x.EndDate == null || x.EndDate >= today) &&
                        x.Team.IsValid == true)
            .Select(x => x.TeamId)
            .Distinct()
            .ToListAsync();

        if (teamId.HasValue && teamId.Value > 0)
        {
            if (!supervisedTeamIds.Contains(teamId.Value))
            {
                // Not authorized to view this team
                return [];
            }
            supervisedTeamIds = [teamId.Value];
        }

        // 2. Members of these teams
        var memberQuery = db.TeamStaffs.AsNoTracking()
            .Where(m => supervisedTeamIds.Contains(m.TeamId) && m.IsValid &&
                        m.StartDate <= to && (m.EndDate == null || m.EndDate >= from));

        if (staffId.HasValue && staffId.Value > 0)
        {
            memberQuery = memberQuery.Where(m => m.StaffId == staffId.Value);
        }

        var memberIds = await memberQuery
            .Select(m => m.StaffId)
            .Distinct()
            .ToListAsync();

        var individuals = await db.Individuals.AsNoTracking()
            .Where(i => memberIds.Contains(i.BusinessEntityId))
            .ToDictionaryAsync(i => i.BusinessEntityId);

        var memberMap = memberIds.ToDictionary(id => id, id => individuals.TryGetValue(id, out var ind) ? FormatName(ind, id) : $"Staff #{id}");

        var result = new List<SupervisorCalendarScheduleItemDto>();

        // 3. Resolve effective workdays for the org/date range
        var workdays = await resolver.ResolveRangeAsync(org, from, to);

        // 4. Approved & Pending Leaves for team members
        var leaves = await db.Leaves.AsNoTracking()
            .Include(l => l.Job)
            .Include(l => l.LeaveType)
            .Where(l => memberIds.Contains(l.Job.IndividualID) &&
                        (l.LeaveStateId == 3 || l.LeaveStateId == 4) &&
                        l.FromDate <= to && l.ToDate >= from)
            .ToListAsync();

        // 5. Leave plans
        var planType = "Planned leave";
        var plans = await db.BusinessEntitySchedules.AsNoTracking()
            .Include(x => x.Schedule)
            .Where(p => memberIds.Contains(p.BusinessEntityId) && p.IsLinked && p.IsActive && p.Schedule.IsValid &&
                        p.Schedule.ScheduleType.Name == planType &&
                        p.Schedule.StartDate <= to && p.Schedule.EndDate >= from)
            .ToListAsync();

        // 6. Tasks
        var tasks = await db.SupervisorCalendarTasks.AsNoTracking()
            .Include(t => t.Assignees)
            .Where(t => t.OrganisationBusinessEntityId == org && !t.IsDeleted &&
                        (supervisedTeamIds.Contains(t.TeamId ?? 0) || t.OwnerIndividualId == actor.IndividualId) &&
                        t.DueDateTime.Date >= from && t.DueDateTime.Date <= to)
            .ToListAsync();

        // Build schedule items day by day
        for (var day = from; day <= to; day = day.AddDays(1))
        {
            var curDate = day;
            var workday = workdays.TryGetValue(curDate, out var wd) ? wd : new EffectiveWorkdayResolutionDto { WorkDate = curDate, IsWorkingDay = true };

            foreach (var staff in memberIds)
            {
                var sName = memberMap.TryGetValue(staff, out var name) ? name : $"Staff #{staff}";

                // Leave?
                var staffLeaves = leaves.Where(l => l.Job.IndividualID == staff && l.FromDate.Date <= curDate && l.ToDate.Date >= curDate).ToList();
                foreach (var l in staffLeaves)
                {
                    result.Add(new SupervisorCalendarScheduleItemDto
                    {
                        IndividualId = staff,
                        StaffName = sName,
                        Date = curDate,
                        ScheduleType = "Leave",
                        Title = l.LeaveType?.Name ?? "Leave",
                        Status = l.LeaveStateId == 4 ? "Approved" : "Pending",
                        Details = $"{l.LeaveType?.Name} ({l.FromDate:dd MMM} - {l.ToDate:dd MMM})"
                    });
                }

                // Leave Plan?
                var staffPlans = plans.Where(p => p.BusinessEntityId == staff && p.Schedule.StartDate.Date <= curDate && p.Schedule.EndDate.Date >= curDate).ToList();
                foreach (var p in staffPlans)
                {
                    result.Add(new SupervisorCalendarScheduleItemDto
                    {
                        IndividualId = staff,
                        StaffName = sName,
                        Date = curDate,
                        ScheduleType = "Plan",
                        Title = p.Schedule.Name ?? "Planned Leave",
                        Status = "Plan",
                        Details = $"Planned Leave ({p.Schedule.StartDate:dd MMM} - {p.Schedule.EndDate:dd MMM})"
                    });
                }

                // Official Work Schedule if working day
                if (workday.IsWorkingDay && staffLeaves.Count == 0)
                {
                    result.Add(new SupervisorCalendarScheduleItemDto
                    {
                        IndividualId = staff,
                        StaffName = sName,
                        Date = curDate,
                        ScheduleType = "Official",
                        StartTime = workday.EffectiveStartTime,
                        EndTime = workday.EffectiveEndTime,
                        Title = workday.IsSpecialWorkingDay ? "Special Working Day" : "Official Schedule",
                        Status = "Scheduled",
                        Details = $"{workday.StatusDescription} ({workday.EffectiveStartTime:hh\\:mm} - {workday.EffectiveEndTime:hh\\:mm})"
                    });
                }
                else if (!workday.IsWorkingDay && staffLeaves.Count == 0 && staffPlans.Count == 0)
                {
                    result.Add(new SupervisorCalendarScheduleItemDto
                    {
                        IndividualId = staff,
                        StaffName = sName,
                        Date = curDate,
                        ScheduleType = "RestDay",
                        Title = workday.StatusDescription,
                        Status = "Off",
                        Details = workday.StatusDescription
                    });
                }
            }

            // Add Tasks on this date
            var dayTasks = tasks.Where(t => t.DueDateTime.Date == curDate).ToList();
            foreach (var t in dayTasks)
            {
                result.Add(new SupervisorCalendarScheduleItemDto
                {
                    IndividualId = t.OwnerIndividualId,
                    StaffName = "Team Task",
                    Date = curDate,
                    ScheduleType = "Task",
                    StartTime = t.StartDateTime.HasValue ? t.StartDateTime.Value.TimeOfDay : null,
                    EndTime = t.DueDateTime.TimeOfDay,
                    Title = t.Title,
                    Status = t.Status,
                    Details = $"{t.TaskCategory} Task [{t.Priority}]: {t.Description}"
                });
            }
        }

        return result;
    }

    public async Task<List<SupervisorCalendarTaskDto>> GetTasksAsync(DateTime from, DateTime to, int? teamId)
    {
        var actor = await access.RequireContextAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        await using var db = await factory.CreateDbContextAsync();

        var query = db.SupervisorCalendarTasks.AsNoTracking()
            .Include(t => t.Assignees)
            .Where(t => t.OrganisationBusinessEntityId == org && !t.IsDeleted &&
                        t.DueDateTime.Date >= from.Date && t.DueDateTime.Date <= to.Date);

        if (teamId.HasValue && teamId.Value > 0)
        {
            query = query.Where(t => t.TeamId == teamId.Value);
        }
        else
        {
            query = query.Where(t => t.OwnerIndividualId == actor.IndividualId ||
                                     db.TeamStaffs.Any(ts => ts.TeamId == t.TeamId && ts.StaffId == actor.IndividualId && ts.IsSuperVisor && ts.IsValid));
        }

        var list = await query.OrderBy(t => t.DueDateTime).ToListAsync();

        return list.Select(t => new SupervisorCalendarTaskDto
        {
            SupervisorCalendarTaskId = t.SupervisorCalendarTaskId,
            TeamId = t.TeamId,
            OwnerIndividualId = t.OwnerIndividualId,
            Title = t.Title,
            Description = t.Description,
            TaskCategory = t.TaskCategory,
            StartDateTime = t.StartDateTime,
            DueDateTime = t.DueDateTime,
            Priority = t.Priority,
            Status = t.Status,
            ReminderMinutes = t.ReminderMinutes,
            SourceModule = t.SourceModule,
            SourceRecordId = t.SourceRecordId,
            AssigneeIndividualIds = t.Assignees.Select(a => a.IndividualId).ToList()
        }).ToList();
    }

    public async Task<int> SaveTaskAsync(SupervisorCalendarTaskDto dto)
    {
        var actor = await access.RequireContextAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        if (string.IsNullOrWhiteSpace(dto.Title)) throw new InvalidOperationException("Task title is required.");

        await using var db = await factory.CreateDbContextAsync();
        SupervisorCalendarTask task;

        if (dto.SupervisorCalendarTaskId == 0)
        {
            task = new SupervisorCalendarTask
            {
                OrganisationBusinessEntityId = org,
                OwnerIndividualId = actor.IndividualId,
                CreatedBy = actor.IndividualId,
                CreatedDate = DateTime.UtcNow,
                UpdatedDate = DateTime.UtcNow,
                Status = dto.Status ?? "NotStarted"
            };
            db.SupervisorCalendarTasks.Add(task);
        }
        else
        {
            task = await db.SupervisorCalendarTasks.Include(t => t.Assignees)
                .SingleOrDefaultAsync(t => t.SupervisorCalendarTaskId == dto.SupervisorCalendarTaskId && t.OrganisationBusinessEntityId == org)
                ?? throw new InvalidOperationException("Task not found.");
            task.UpdatedDate = DateTime.UtcNow;
        }

        task.TeamId = dto.TeamId;
        task.Title = dto.Title.Trim();
        task.Description = dto.Description;
        task.TaskCategory = dto.TaskCategory ?? "General";
        task.StartDateTime = dto.StartDateTime;
        task.DueDateTime = dto.DueDateTime;
        task.Priority = dto.Priority ?? "Normal";
        task.Status = dto.Status ?? "NotStarted";
        task.ReminderMinutes = dto.ReminderMinutes;
        task.SourceModule = dto.SourceModule;
        task.SourceRecordId = dto.SourceRecordId;

        task.Assignees.Clear();
        foreach (var assignee in dto.AssigneeIndividualIds.Distinct())
        {
            task.Assignees.Add(new SupervisorCalendarTaskAssignee
            {
                SupervisorCalendarTaskId = task.SupervisorCalendarTaskId,
                IndividualId = assignee
            });
        }

        await db.SaveChangesAsync();
        return task.SupervisorCalendarTaskId;
    }

    public async Task UpdateTaskStatusAsync(int taskId, string status)
    {
        var org = await access.GetCurrentOrganisationIdAsync();
        await using var db = await factory.CreateDbContextAsync();
        var task = await db.SupervisorCalendarTasks.SingleOrDefaultAsync(t => t.SupervisorCalendarTaskId == taskId && t.OrganisationBusinessEntityId == org)
            ?? throw new InvalidOperationException("Task not found.");

        task.Status = status;
        task.UpdatedDate = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }

    public async Task DeleteTaskAsync(int taskId)
    {
        var org = await access.GetCurrentOrganisationIdAsync();
        await using var db = await factory.CreateDbContextAsync();
        var task = await db.SupervisorCalendarTasks.SingleOrDefaultAsync(t => t.SupervisorCalendarTaskId == taskId && t.OrganisationBusinessEntityId == org)
            ?? throw new InvalidOperationException("Task not found.");

        task.IsDeleted = true;
        task.UpdatedDate = DateTime.UtcNow;
        await db.SaveChangesAsync();
    }
}
