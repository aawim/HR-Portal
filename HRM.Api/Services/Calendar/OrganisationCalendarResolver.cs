using System.Data;
using HRM.DTOs.Calendar;
using HRM.Models;
using HRM.Services.Calendar.Contracts;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Calendar;

public sealed class OrganisationCalendarResolver(IDbContextFactory<HrmTeContext> factory) : IOrganisationCalendarResolver
{
    private static readonly TimeSpan DefaultStart = new(8, 0, 0);
    private static readonly TimeSpan DefaultEnd = new(14, 0, 0);

    public async Task<EffectiveWorkdayResolutionDto> ResolveAsync(int organisationId, DateTime date, int? individualId = null, int? jobId = null)
    {
        var range = await ResolveRangeAsync(organisationId, date.Date, date.Date, individualId, jobId);
        return range.TryGetValue(date.Date, out var res) ? res : CreateDefault(date.Date);
    }

    public async Task<Dictionary<DateTime, EffectiveWorkdayResolutionDto>> ResolveRangeAsync(int organisationId, DateTime from, DateTime to, int? individualId = null, int? jobId = null)
    {
        from = from.Date;
        to = to.Date;
        if (to < from) (from, to) = (to, from);

        await using var db = await factory.CreateDbContextAsync();

        // 1. Load active exceptions covering range
        var query = db.OrganisationCalendarExceptions.AsNoTracking()
            .Include(x => x.Segments)
            .Where(x => x.OrganisationBusinessEntityId == organisationId && x.IsActive && x.IsApproved &&
                        x.EffectiveDate <= to && x.EffectiveEndDate >= from);

        // Filter applicable scopes
        int? deptId = null;
        var teamIds = new List<int>();
        if (individualId.HasValue && individualId.Value > 0)
        {
            var job = await db.Jobs.AsNoTracking().FirstOrDefaultAsync(j => j.IndividualID == individualId.Value && j.OrganisationID == organisationId);
            if (job != null) deptId = job.OrganisationStructureId;
            teamIds = await db.TeamStaffs.AsNoTracking()
                .Where(t => t.StaffId == individualId.Value && t.IsValid && t.StartDate <= to && (!t.EndDate.HasValue || t.EndDate >= from))
                .Select(t => t.TeamId).Distinct().ToListAsync();
        }

        var allExceptions = await query.ToListAsync();

        // Also check if any OrganisationEvent is linked to get title
        var eventIds = allExceptions.Where(x => x.OrganisationEventId.HasValue).Select(x => x.OrganisationEventId!.Value).Distinct().ToList();
        var eventTitles = await db.OrganisationEvents.AsNoTracking()
            .Where(x => eventIds.Contains(x.OrganisationEventId))
            .ToDictionaryAsync(x => x.OrganisationEventId, x => x.Title);

        var result = new Dictionary<DateTime, EffectiveWorkdayResolutionDto>();

        for (var cur = from; cur <= to; cur = cur.AddDays(1))
        {
            var curDate = cur;
            var weekday = curDate.DayOfWeek;
            // Standard Maldives work week: Sunday (0) to Thursday (4) are working days, Friday (5) & Saturday (6) are weekend/rest days
            var isNormallyWeekend = weekday is DayOfWeek.Friday or DayOfWeek.Saturday;
            var isNormallyWorking = !isNormallyWeekend;

            var dayDto = new EffectiveWorkdayResolutionDto
            {
                WorkDate = curDate,
                NormalWeekday = weekday,
                IsWorkingDay = isNormallyWorking,
                IsHoliday = isNormallyWeekend,
                IsSpecialWorkingDay = false,
                AttendanceRequired = isNormallyWorking,
                LeaveChargeable = isNormallyWorking,
                NormalStartTime = isNormallyWorking ? DefaultStart : null,
                NormalEndTime = isNormallyWorking ? DefaultEnd : null,
                EffectiveStartTime = isNormallyWorking ? DefaultStart : null,
                EffectiveEndTime = isNormallyWorking ? DefaultEnd : null,
                StatusDescription = isNormallyWorking ? "Normal Working Day" : "Weekend / Rest Day",
                OverrideSource = "OrganisationDefault"
            };

            // Find applicable exceptions for this specific date
            var applicable = allExceptions.Where(x => x.EffectiveDate <= curDate && x.EffectiveEndDate >= curDate).Where(x =>
            {
                if (x.TargetScope == "Organisation") return true;
                if (x.TargetScope == "Individual" && individualId.HasValue && x.TargetEntityId == individualId.Value) return true;
                if (x.TargetScope == "Department" && deptId.HasValue && x.TargetEntityId == deptId.Value) return true;
                if (x.TargetScope == "Team" && x.TargetEntityId.HasValue && teamIds.Contains(x.TargetEntityId.Value)) return true;
                return false;
            }).OrderByDescending(x => x.Priority).ToList();

            if (applicable.Count > 0)
            {
                var winning = applicable.First();
                dayDto.OverrideSource = $"{winning.TargetScope}Exception";
                dayDto.OrganisationEventId = winning.OrganisationEventId;
                if (winning.OrganisationEventId.HasValue && eventTitles.TryGetValue(winning.OrganisationEventId.Value, out var title))
                {
                    dayDto.EventTitle = title;
                }

                switch (winning.CalendarImpactType)
                {
                    case "MarkAsNonWorkingDay":
                        dayDto.IsWorkingDay = false;
                        dayDto.IsHoliday = true;
                        dayDto.AttendanceRequired = false;
                        dayDto.LeaveChargeable = false;
                        dayDto.EffectiveStartTime = null;
                        dayDto.EffectiveEndTime = null;
                        dayDto.StatusDescription = string.IsNullOrWhiteSpace(dayDto.EventTitle)
                            ? $"Official Non-Working Day ({winning.Reason})"
                            : $"Non-Working Day: {dayDto.EventTitle}";
                        break;

                    case "MarkAsWorkingDay":
                        dayDto.IsWorkingDay = true;
                        dayDto.IsHoliday = false;
                        dayDto.IsSpecialWorkingDay = isNormallyWeekend;
                        dayDto.AttendanceRequired = true;
                        dayDto.LeaveChargeable = true;
                        var firstSeg = winning.Segments.FirstOrDefault();
                        dayDto.EffectiveStartTime = firstSeg?.StartTime ?? DefaultStart;
                        dayDto.EffectiveEndTime = firstSeg?.EndTime ?? DefaultEnd;
                        dayDto.StatusDescription = isNormallyWeekend
                            ? $"Special Working Day: {(string.IsNullOrWhiteSpace(dayDto.EventTitle) ? winning.Reason : dayDto.EventTitle)} ({dayDto.EffectiveStartTime:hh\\:mm}–{dayDto.EffectiveEndTime:hh\\:mm})"
                            : $"Working Day Override: {(string.IsNullOrWhiteSpace(dayDto.EventTitle) ? winning.Reason : dayDto.EventTitle)}";
                        break;

                    case "OverrideWorkingHours":
                    case "ShortenedWorkingDay":
                    case "ExtendedWorkingDay":
                        dayDto.IsWorkingDay = true;
                        dayDto.IsHoliday = false;
                        dayDto.AttendanceRequired = true;
                        dayDto.LeaveChargeable = true;
                        var seg = winning.Segments.FirstOrDefault();
                        if (seg != null)
                        {
                            dayDto.EffectiveStartTime = seg.StartTime;
                            dayDto.EffectiveEndTime = seg.EndTime;
                        }
                        dayDto.StatusDescription = $"{winning.CalendarImpactType}: {(string.IsNullOrWhiteSpace(dayDto.EventTitle) ? winning.Reason : dayDto.EventTitle)} ({dayDto.EffectiveStartTime:hh\\:mm}–{dayDto.EffectiveEndTime:hh\\:mm})";
                        break;
                }

                dayDto.Segments = winning.Segments.Select(s => new OrganisationCalendarExceptionSegmentDto
                {
                    StartTime = s.StartTime,
                    EndTime = s.EndTime
                }).ToList();
            }

            result[curDate] = dayDto;
        }

        return result;
    }

    private static EffectiveWorkdayResolutionDto CreateDefault(DateTime date)
    {
        var isWeekend = date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday;
        return new EffectiveWorkdayResolutionDto
        {
            WorkDate = date,
            NormalWeekday = date.DayOfWeek,
            IsWorkingDay = !isWeekend,
            IsHoliday = isWeekend,
            AttendanceRequired = !isWeekend,
            LeaveChargeable = !isWeekend,
            StatusDescription = isWeekend ? "Weekend / Rest Day" : "Normal Working Day"
        };
    }
}
