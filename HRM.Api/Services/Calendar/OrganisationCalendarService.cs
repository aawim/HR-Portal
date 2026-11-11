using System.Data;
using HRM.DTOs.Calendar;
using HRM.DTOs.Leave;
using HRM.Models;
using HRM.Services.Calendar.Contracts;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Calendar;

public sealed class OrganisationCalendarService(
    IDbContextFactory<HrmTeContext> factory,
    IUserAccessService access,
    IOrganisationCalendarResolver resolver)
    : global::HRM.Services.Calendar.Contracts.OrganisationCalendarServiceContracts.IOperations
{
    private static string FormatName(Individual? ind, int id)
    {
        if (ind == null) return $"Staff #{id}";
        var name = (ind.FirstNameEnglish + " " + (ind.MiddleNameEnglish ?? "") + " " + (ind.LastNameEnglish ?? "")).Trim();
        return string.IsNullOrWhiteSpace(name) ? $"Staff #{id}" : name;
    }

    public async Task<EffectiveWorkdayResolutionDto> ResolveEffectiveWorkdayAsync(DateTime date, int? individualId = null)
    {
        var org = await access.GetCurrentOrganisationIdAsync();
        return await resolver.ResolveAsync(org, date, individualId);
    }

    public async Task<List<OrganisationCalendarDaySummaryDto>> GetOrganisationCalendarMonthAsync(DateTime month)
    {
        var org = await access.GetCurrentOrganisationIdAsync();
        var start = new DateTime(month.Year, month.Month, 1);
        var end = start.AddMonths(1).AddDays(-1);

        var workdays = await resolver.ResolveRangeAsync(org, start, end);

        await using var db = await factory.CreateDbContextAsync();
        await OrganisationCalendarSchemaBootstrapper.EnsureSchemaAsync(db);

        // Load published events
        var events = await db.OrganisationEvents.AsNoTracking()
            .Include(x => x.Audiences)
            .Where(x => x.OrganisationBusinessEntityId == org && x.IsValid && x.Status == "Published" &&
                        x.StartDateTime <= end.AddDays(1) && x.EndDateTime >= start)
            .ToListAsync();

        // Load approved leaves and leave plans in month
        var leaves = await db.Leaves.AsNoTracking()
            .Include(x => x.Job)
            .Include(x => x.LeaveType)
            .Where(x => x.Job.OrganisationID == org &&
                        (x.LeaveStateId == 3 || x.LeaveStateId == 4) && // Pending or Approved
                        x.FromDate <= end && x.ToDate >= start)
            .ToListAsync();

        // Load Leave Plans
        var planType = "Planned leave";
        var plans = await db.BusinessEntitySchedules.AsNoTracking()
            .Include(x => x.Schedule)
            .Include(x => x.Schedule.ScheduleType)
            .Where(x => x.IsLinked && x.IsActive && x.Schedule.IsValid &&
                        x.Schedule.ScheduleType.Name == planType &&
                        x.Schedule.StartDate <= end && x.Schedule.EndDate >= start)
            .ToListAsync();

        var result = new List<OrganisationCalendarDaySummaryDto>();

        for (var day = start; day <= end; day = day.AddDays(1))
        {
            var curDate = day;
            var summary = new OrganisationCalendarDaySummaryDto
            {
                Date = curDate,
                WorkdayResolution = workdays.TryGetValue(curDate, out var w) ? w : new EffectiveWorkdayResolutionDto { WorkDate = curDate }
            };

            // Events on this day
            summary.Events = events.Where(e => e.StartDateTime.Date <= curDate && e.EndDateTime.Date >= curDate)
                .Select(e => new OrganisationEventDto
                {
                    OrganisationEventId = e.OrganisationEventId,
                    Title = e.Title,
                    Category = e.Category,
                    StartDateTime = e.StartDateTime,
                    EndDateTime = e.EndDateTime,
                    IsAllDay = e.IsAllDay,
                    VenueName = e.VenueName,
                    CalendarImpactType = e.CalendarImpactType,
                    Status = e.Status,
                    AttendanceRequirement = e.AttendanceRequirement
                }).ToList();

            // Leaves on this day
            var dayLeaves = leaves.Where(l => l.FromDate.Date <= curDate && l.ToDate.Date >= curDate).ToList();
            summary.ApprovedLeavesCount = dayLeaves.Count(l => l.LeaveStateId == 4);
            summary.PendingLeavesCount = dayLeaves.Count(l => l.LeaveStateId == 3);

            var dayPlans = plans.Where(p => p.Schedule.StartDate.Date <= curDate && p.Schedule.EndDate.Date >= curDate).ToList();
            summary.LeavePlansCount = dayPlans.Count;

            result.Add(summary);
        }

        return result;
    }

    public async Task<OrganisationCalendarDaySummaryDto> GetCalendarDayDetailAsync(DateTime date)
    {
        var org = await access.GetCurrentOrganisationIdAsync();
        var curDate = date.Date;
        var workday = await resolver.ResolveAsync(org, curDate);

        await using var db = await factory.CreateDbContextAsync();
        await OrganisationCalendarSchemaBootstrapper.EnsureSchemaAsync(db);

        var events = await db.OrganisationEvents.AsNoTracking()
            .Include(x => x.Audiences)
            .Where(x => x.OrganisationBusinessEntityId == org && x.IsValid && x.Status == "Published" &&
                        x.StartDateTime.Date <= curDate && x.EndDateTime.Date >= curDate)
            .ToListAsync();

        var leaves = await db.Leaves.AsNoTracking()
            .Include(x => x.Job)
            .Include(x => x.LeaveType)
            .Where(x => x.Job.OrganisationID == org &&
                        (x.LeaveStateId == 3 || x.LeaveStateId == 4) &&
                        x.FromDate.Date <= curDate && x.ToDate.Date >= curDate)
            .ToListAsync();

        var planType = "Planned leave";
        var plans = await db.BusinessEntitySchedules.AsNoTracking()
            .Include(x => x.Schedule)
            .Where(x => x.IsLinked && x.IsActive && x.Schedule.IsValid &&
                        x.Schedule.ScheduleType.Name == planType &&
                        x.Schedule.StartDate.Date <= curDate && x.Schedule.EndDate.Date >= curDate)
            .ToListAsync();

        // Also fetch names for staff
        var allPersonIds = leaves.Select(l => l.Job.IndividualID).Concat(plans.Select(p => p.BusinessEntityId)).Distinct().ToList();
        var individuals = await db.Individuals.AsNoTracking()
            .Where(i => allPersonIds.Contains(i.BusinessEntityId))
            .ToDictionaryAsync(i => i.BusinessEntityId);

        var summary = new OrganisationCalendarDaySummaryDto
        {
            Date = curDate,
            WorkdayResolution = workday,
            Events = events.Select(e => new OrganisationEventDto
            {
                OrganisationEventId = e.OrganisationEventId,
                Title = e.Title,
                Category = e.Category,
                Description = e.Description,
                StartDateTime = e.StartDateTime,
                EndDateTime = e.EndDateTime,
                IsAllDay = e.IsAllDay,
                VenueName = e.VenueName,
                MeetingUrl = e.MeetingUrl,
                DressCode = e.DressCode,
                AttendanceInstructions = e.AttendanceInstructions,
                CalendarImpactType = e.CalendarImpactType,
                AttendanceRequirement = e.AttendanceRequirement,
                Status = e.Status
            }).ToList(),
            ApprovedLeavesCount = leaves.Count(l => l.LeaveStateId == 4),
            PendingLeavesCount = leaves.Count(l => l.LeaveStateId == 3),
            LeavePlansCount = plans.Count,
            Leaves = leaves.Select(l => new LeaveCalendarEntry(
                l.LeaveId,
                l.Job.IndividualID,
                individuals.TryGetValue(l.Job.IndividualID, out var ind) ? FormatName(ind, l.Job.IndividualID) : $"Staff #{l.Job.IndividualID}",
                l.FromDate,
                l.ToDate,
                l.LeaveType?.Name ?? "Leave",
                l.LeaveStateId == 4 ? "Approved" : "Pending",
                false,
                false
            )).Concat(plans.Select(p => new LeaveCalendarEntry(
                p.BusinessEntityScheduleId,
                p.BusinessEntityId,
                individuals.TryGetValue(p.BusinessEntityId, out var ind) ? FormatName(ind, p.BusinessEntityId) : $"Staff #{p.BusinessEntityId}",
                p.Schedule.StartDate,
                p.Schedule.EndDate,
                p.Schedule.Name ?? "Planned Leave",
                "Plan",
                true,
                false
            ))).ToList()
        };

        return summary;
    }
}
