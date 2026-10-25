using HRM.Models;
using HRM.Models.WorkPlanning;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HRM.Helpers;

public static class StaffScheduleCalendar
{
    // Bit 0 = Sunday, through bit 6 = Saturday. Normal week = Sunday–Thursday.
    public static bool IsWeekend(DateTime date) => date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday;
    public static int TemplateOn(JobWorkTemplateAssignment assignment, DateTime date)
    {
        if (date.Date < assignment.EffectiveFrom.Date || (assignment.EffectiveTo.HasValue && date.Date > assignment.EffectiveTo.Value.Date)) return 0;
        if (assignment.ScheduleMode == "Rotation")
        {
            var pattern = Parse(assignment.RotationTemplateIds);
            if (pattern.Count == 0 || !assignment.RotationAnchorDate.HasValue) return 0;
            var offset = (date.Date - assignment.RotationAnchorDate.Value.Date).Days;
            return pattern[((offset % pattern.Count) + pattern.Count) % pattern.Count];
        }
        if (assignment.SchedulePurpose == "Duty" && assignment.ScheduleMode == "Standard") return assignment.WorkTemplateID;
        return (assignment.WorkingDaysMask & (1 << (int)date.DayOfWeek)) != 0 ? assignment.WorkTemplateID : 0;
    }
    public static List<int> Parse(string? json)
    {
        try { return string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize<List<int>>(json) ?? []; }
        catch (JsonException) { return []; }
    }

    public static async Task<HashSet<DateTime>> RestDaysAsync(HrmTeContext db, int individualId, DateTime from, DateTime to, CancellationToken ct = default, int? organisationId = null)
    {
        var jobs=await db.Jobs.AsNoTracking().Where(j=>j.IndividualID==individualId && (!organisationId.HasValue || j.OrganisationID==organisationId)).ToListAsync(ct);
        var candidates=new List<StaffScheduleCandidate>();
        foreach(var job in jobs) candidates.AddRange(await EffectiveStaffSchedules.LoadAsync(db,job,from,to,ct));
        var plans = await db.WorkPlans.AsNoTracking().Where(x => x.IsValid && x.IndividualId == individualId && (!organisationId.HasValue || x.OrganisationBusinessEntityId == organisationId) && x.WorkDate >= from && x.WorkDate <= to &&
            x.WorkAssignments.Any(a => a.IsValid && a.RequiresAttendance && (a.SchedulePurpose == "Duty" || x.IsManual)))
            .Select(x => x.WorkDate).ToListAsync(ct);
        var legacy = await db.JobWorkTemplates.AsNoTracking().Where(x => x.IsActive && x.Job.IndividualID == individualId && (!organisationId.HasValue || x.Job.OrganisationID == organisationId) &&
            x.EffectiveFrom <= to && (x.EffectiveTo == null || x.EffectiveTo >= from)).ToListAsync(ct);
        var rest = new HashSet<DateTime>();
        for (var day = from.Date; day <= to.Date; day = day.AddDays(1))
        {
            var active = candidates.GroupBy(c=>c.Assignment.JobID).SelectMany(g=>EffectiveStaffSchedules.On(g,day)).Select(c=>c.Assignment).ToList();
            var old = legacy.Where(x => x.EffectiveFrom.Date <= day && (x.EffectiveTo == null || x.EffectiveTo.Value.Date >= day)).ToList();
            var working = active.Count > 0 ? active.Any(x => TemplateOn(x, day) > 0) : old.Count > 0 ? old.Any(x => day.DayOfWeek switch
            { DayOfWeek.Sunday => x.Sunday, DayOfWeek.Monday => x.Monday, DayOfWeek.Tuesday => x.Tuesday, DayOfWeek.Wednesday => x.Wednesday,
              DayOfWeek.Thursday => x.Thursday, DayOfWeek.Friday => x.Friday, _ => x.Saturday }) : !IsWeekend(day);
            if (!working && !plans.Any(x => x.Date == day)) rest.Add(day);
        }
        return rest;
    }
}
