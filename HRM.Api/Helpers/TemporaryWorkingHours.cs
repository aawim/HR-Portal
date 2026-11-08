using HRM.DTOs.Attendance;
using HRM.Models;
using HRM.Models.WorkPlanning;
using Microsoft.EntityFrameworkCore;

namespace HRM.Helpers;

public sealed record ScheduleHourAdjustment(long AssignmentId, int TemplateId, DateTime Start, DateTime End, int DeltaMinutes, bool HasEarlyCheckout);

public static class TemporaryWorkingHours
{
    public static async Task<List<OrganisationWorkingHourRequirement>> LoadAsync(HrmTeContext db, int? org, DateTime from, DateTime to, CancellationToken ct = default)
    {
        if (db.Model.FindEntityType(typeof(OrganisationWorkingHourRequirement)) == null) return [];
        return await db.OrganisationWorkingHourRequirements.AsNoTracking().Where(r => r.IsActive && (!org.HasValue || r.OrganisationId == org) &&
            r.FromDate <= to.Date && r.ToDate >= from.Date).ToListAsync(ct);
    }
    public static OrganisationWorkingHourRequirement? Select(IEnumerable<OrganisationWorkingHourRequirement> rows, int org, int jobType, DateTime day) =>
        rows.Where(r => r.IsActive && r.OrganisationId == org && (!r.JobTypeId.HasValue || r.JobTypeId == jobType) &&
            r.FromDate.Date <= day.Date && r.ToDate.Date >= day.Date).OrderByDescending(r => r.JobTypeId.HasValue).FirstOrDefault();

    public static async Task<Dictionary<long, ScheduleHourAdjustment>> AdjustmentsAsync(HrmTeContext db, IEnumerable<WorkAssignment> assignments, CancellationToken ct = default)
    {
        var items = assignments.Where(a => a.IsValid && a.SchedulePurpose == "Official").ToList();
        if (items.Count == 0) return [];
        var from = items.Min(a => a.WorkPlan.WorkDate); var to = items.Max(a => a.WorkPlan.WorkDate);
        var rows = await LoadAsync(db, null, from, to, ct);
        if (rows.Count == 0) return [];
        var jobs = items.Select(a => a.WorkPlan.JobId).Distinct().ToList();
        var types = await db.Jobs.Where(j => jobs.Contains(j.JobId)).ToDictionaryAsync(j => j.JobId, j => j.JobTypeId, ct);
        var ids = items.Select(a => a.WorkAssignmentId).ToList();
        var changes = await db.LateCheckInRequests.AsNoTracking().Where(r => ids.Contains(r.WorkAssignmentId) && r.AppliedAtUtc != null && r.Request.RequestStateId == 4)
            .OrderBy(r => r.AppliedAtUtc).ToListAsync(ct);
        var amendments = await OfficialScheduleAmendments.LoadAsync(db, null, from, to, ct);
        var result = new Dictionary<long, ScheduleHourAdjustment>();
        foreach (var a in items)
        {
            var org = a.WorkPlan.OrganisationBusinessEntityId;
            var requirement = Select(rows, org, types.GetValueOrDefault(a.WorkPlan.JobId), a.WorkPlan.WorkDate);
            if (requirement == null) continue;
            var personal = changes.FirstOrDefault(r => r.WorkAssignmentId == a.WorkAssignmentId && r.ScheduledEnd.HasValue);
            var baselineStart = personal?.ScheduledStart ?? a.StartDateTime;
            var baselineEnd = personal?.ScheduledEnd ?? a.EndDateTime;
            var waived = personal == null ? 0 : amendments.Where(r => r.OrganisationId == org && r.WorkDate.Date == baselineStart.Date && r.RevisedStart == baselineStart.TimeOfDay)
                .Select(r => (int)(r.RevisedStart - r.OriginalStart).TotalMinutes).DefaultIfEmpty(0).Max();
            var baseMinutes = (int)(baselineEnd - baselineStart).TotalMinutes + waived;
            var delta = Math.Max(0, requirement.RequiredDailyMinutes - baseMinutes);
            result[a.WorkAssignmentId] = new(a.WorkAssignmentId, a.WorkTemplateId, a.StartDateTime, a.EndDateTime, delta,
                changes.Any(r => r.WorkAssignmentId == a.WorkAssignmentId && r.Kind == "EarlyCheckOut"));
        }
        return result;
    }
    public static (DateTime Start, DateTime End) Segment(ScheduleHourAdjustment a, string code, DateTime start, DateTime end)
    {
        if (a.HasEarlyCheckout || a.DeltaMinutes == 0) return (start, end);
        if (code is "CHECK_OUT" or "DUTY_CHECK_OUT") return (start.AddMinutes(a.DeltaMinutes), end.AddMinutes(a.DeltaMinutes));
        return (start, end == a.End && !code.Contains("BREAK", StringComparison.OrdinalIgnoreCase) ? end.AddMinutes(a.DeltaMinutes) : end);
    }
    public static async Task ApplyAsync(HrmTeContext db, AttendanceWorkPlanDto plan, CancellationToken ct = default)
    {
        var assignments = await db.WorkAssignments.AsNoTracking().Include(a => a.WorkPlan)
            .Where(a => a.WorkPlanId == plan.WorkPlanId && a.IsValid && a.SchedulePurpose == "Official").ToListAsync(ct);
        var adjustments = await AdjustmentsAsync(db, assignments, ct);
        if (adjustments.Count == 0) return;
        var typeCodes = await db.WorkSegmentTypes.AsNoTracking().ToDictionaryAsync(t => t.WorkSegmentTypeId, t => t.Code, ct);
        var segmentIds = plan.Segments.Where(s => s.WorkTemplateSegmentId.HasValue).Select(s => s.WorkTemplateSegmentId!.Value).ToList();
        var templateIds = await db.WorkTemplateSegments.Where(s => segmentIds.Contains(s.WorkTemplateSegmentId)).ToDictionaryAsync(s => s.WorkTemplateSegmentId, s => s.WorkTemplateId, ct);
        foreach (var segment in plan.Segments.Where(s => s.SchedulePurpose == "Official"))
        {
            var candidates = adjustments.Values.Where(a => templateIds.GetValueOrDefault(segment.WorkTemplateSegmentId ?? 0) == a.TemplateId ||
                adjustments.Count == 1).ToList();
            if (candidates.Count != 1) continue;
            var timing = Segment(candidates[0], typeCodes.GetValueOrDefault(segment.WorkSegmentTypeId) ?? "", segment.StartDateTime, segment.EndDateTime);
            segment.StartDateTime = timing.Start; segment.EndDateTime = timing.End;
        }
        foreach (var a in plan.Assignments)
            if (adjustments.TryGetValue(a.WorkAssignmentId, out var change) && !change.HasEarlyCheckout) a.EndDateTime = a.EndDateTime.AddMinutes(change.DeltaMinutes);
        plan.TemporaryWorkingHoursMessage = "Temporary daily working hours apply. Your template is unchanged; check the required finish time below.";
    }
    public static async Task ApplyAsync(HrmTeContext db, WorkPlan plan, CancellationToken ct = default)
    {
        var adjustments = await AdjustmentsAsync(db, plan.WorkAssignments, ct);
        foreach (var s in plan.WorkPlanSegments.Where(s => s.SchedulePurpose == "Official"))
        {
            var candidates = adjustments.Values.Where(a => s.WorkTemplateSegment?.WorkTemplateId == a.TemplateId || adjustments.Count == 1).ToList();
            if (candidates.Count != 1) continue;
            var timing = Segment(candidates[0], s.WorkSegmentType?.Code ?? "", s.StartDateTime, s.EndDateTime);
            s.StartDateTime = timing.Start; s.EndDateTime = timing.End;
        }
        foreach (var a in plan.WorkAssignments)
            if (adjustments.TryGetValue(a.WorkAssignmentId, out var change) && !change.HasEarlyCheckout) a.EndDateTime = a.EndDateTime.AddMinutes(change.DeltaMinutes);
    }
}
