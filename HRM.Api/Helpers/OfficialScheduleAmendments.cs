using HRM.DTOs.Attendance;
using HRM.Models;
using HRM.Models.WorkPlanning;
using Microsoft.EntityFrameworkCore;

namespace HRM.Helpers;

// Apply a dated overlay, never rewrite a template, physical punch or dated snapshot.
public static class OfficialScheduleAmendments
{
    public static async Task<AttendanceWorkPlanDto> ApplyAsync(HrmTeContext db, AttendanceWorkPlanDto plan, CancellationToken ct = default)
    {
        await TemporaryWorkingHours.ApplyAsync(db, plan, ct);
        var amendments = await LoadAsync(db, plan.OrganisationId, plan.WorkDate, plan.WorkDate, ct);
        if (amendments.Count == 0) return plan;
        var applicable = amendments.Where(a => plan.Assignments.Any(s => s.SchedulePurpose == "Official" && s.StartDateTime.Date == a.WorkDate.Date && s.StartDateTime.TimeOfDay == a.OriginalStart)).ToList();
        plan.ScheduleAmendmentMessage = applicable.Count == 0 ? null : string.Join(" · ", applicable.Select(a =>
            $"Temporary official start {a.RevisedStart:hh\\:mm}; end unchanged. {a.Reason}"));
        var types = await db.WorkSegmentTypes.AsNoTracking().ToDictionaryAsync(x => x.WorkSegmentTypeId, x => x.Code, ct);
        foreach (var s in plan.Segments) s.WorkSegmentTypeCode = types.GetValueOrDefault(s.WorkSegmentTypeId) ?? "";
        Apply(plan, amendments);
        return plan;
    }

    public static async Task<List<OrganisationScheduleAmendment>> LoadAsync(HrmTeContext db, int? org,
        DateTime from, DateTime to, CancellationToken ct = default)
    {
        if (db.Model.FindEntityType(typeof(OrganisationScheduleAmendment)) == null) return [];
        return await db.OrganisationScheduleAmendments.AsNoTracking().Where(x => x.IsActive &&
            (!org.HasValue || x.OrganisationId == org) && x.WorkDate >= from.Date && x.WorkDate <= to.Date).ToListAsync(ct);
    }

    public static OrganisationScheduleAmendment? Match(IEnumerable<OrganisationScheduleAmendment> amendments,
        int org, string purpose, DateTime start) => purpose == "Official"
        ? amendments.SingleOrDefault(a => a.OrganisationId == org && a.WorkDate.Date == start.Date && a.OriginalStart == start.TimeOfDay)
        : null;

    public static DateTime Start(IEnumerable<OrganisationScheduleAmendment> amendments, int org, string purpose, DateTime start)
    {
        var a = Match(amendments, org, purpose, start);
        return a == null ? start : start.Date.Add(a.RevisedStart);
    }

    public static (DateTime Start, DateTime End, int GraceBefore) Segment(IEnumerable<OrganisationScheduleAmendment> amendments,
        int org, string purpose, string code, DateTime start, DateTime end, int graceBefore)
    {
        // Check-out and break events retain their original times.
        if (code is "CHECK_OUT" or "DUTY_CHECK_OUT" || code.Contains("BREAK", StringComparison.OrdinalIgnoreCase)) return (start, end, graceBefore);
        var revised = Start(amendments, org, purpose, start);
        if (revised == start) return (start, end, graceBefore);
        var checkIn = code is "CHECK_IN" or "DUTY_CHECK_IN";
        return (revised, checkIn ? end.Add(revised - start) : end,
            checkIn ? graceBefore + (int)Math.Max(0, (revised - start).TotalMinutes) : graceBefore);
    }

    public static void Apply(WorkPlan plan, List<OrganisationScheduleAmendment> amendments)
    {
        foreach (var s in plan.WorkPlanSegments)
        {
            var t = Segment(amendments, plan.OrganisationBusinessEntityId, s.SchedulePurpose,
                s.WorkSegmentType?.Code ?? "", s.StartDateTime, s.EndDateTime, s.GraceBeforeMinutes);
            s.StartDateTime = t.Start; s.EndDateTime = t.End; s.GraceBeforeMinutes = t.GraceBefore;
        }
        foreach (var a in plan.WorkAssignments)
            a.StartDateTime = Start(amendments, plan.OrganisationBusinessEntityId, a.SchedulePurpose, a.StartDateTime);
    }

    public static void Apply(AttendanceWorkPlanDto plan, List<OrganisationScheduleAmendment> amendments)
    {
        foreach (var s in plan.Segments)
        {
            var t = Segment(amendments, plan.OrganisationId, s.SchedulePurpose, s.WorkSegmentTypeCode,
                s.StartDateTime, s.EndDateTime, s.GraceBeforeMinutes);
            s.StartDateTime = t.Start; s.EndDateTime = t.End; s.GraceBeforeMinutes = t.GraceBefore;
        }
        foreach (var a in plan.Assignments)
            a.StartDateTime = Start(amendments, plan.OrganisationId, a.SchedulePurpose, a.StartDateTime);
    }
}
