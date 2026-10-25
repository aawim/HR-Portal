using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Helpers;

public sealed record WeeklyAttendanceMakeupResult(decimal ShortfallHours, decimal CompletedHours, decimal RemainingHours);

public static class WeeklyAttendanceMakeup
{
    public static async Task<WeeklyAttendanceMakeupResult> LoadAsync(HrmTeContext db, int person, int org,
        DateTime weekStart, DateTime weekEndExclusive, CancellationToken ct = default, bool includeDuty = true)
    {
        var changes = await db.LateCheckInRequests.AsNoTracking().Where(r => r.IndividualId == person && r.OrganisationId == org &&
            r.Kind == "EarlyCheckOut" && r.Request.RequestStateId == 4 && r.AppliedAtUtc != null &&
            r.ScheduledStart >= weekStart && r.ScheduledStart < weekEndExclusive && r.ScheduledEnd != null && r.RequestedEnd != null)
            .ToListAsync(ct);
        if (changes.Count == 0) return new(0, 0, 0);
        var ids = changes.Select(r => r.WorkAssignmentId).ToList();
        var assignments = await db.WorkAssignments.AsNoTracking().Include(a => a.WorkPlan).Where(a => ids.Contains(a.WorkAssignmentId)).ToListAsync(ct);
        var adjustments = await TemporaryWorkingHours.AdjustmentsAsync(db, assignments, ct);
        var owed = WorkHourCoverage.Minutes(changes.Select(r => Clip(r.RequestedEnd!.Value,
            r.ScheduledEnd!.Value.AddMinutes(adjustments.GetValueOrDefault(r.WorkAssignmentId)?.DeltaMinutes ?? 0), weekStart, weekEndExclusive)));
        var facts = await AttendanceScheduleFacts.LoadAsync(db, person, weekStart.AddDays(-1), weekEndExclusive.AddDays(-1), org, ct);
        var planned = facts.Where(f => f.RequiresAttendance && f.Purpose == "Official").Select(f => Clip(f.ScheduledStart, f.ScheduledEnd, weekStart, weekEndExclusive)).ToList();
        var now = DateTime.UtcNow.AddHours(5);
        var worked = facts.Where(f => f.RequiresAttendance && (f.Purpose == "Official" || includeDuty) && f.CheckIn.HasValue && f.CheckOut.HasValue && f.CheckOut <= now && f.CheckOut > f.CheckIn)
            .Select(f => Clip(f.CheckIn!.Value, f.CheckOut!.Value, weekStart, weekEndExclusive)).ToList();
        // Overlapping official/duty attendance counts once as time worked. Extra time
        // can settle this week's approved shortfall; it does not grant overtime pay.
        var extra = WorkHourCoverage.Calculate(planned, worked).PotentialOvertimeMinutes;
        var completed = Math.Min(owed, extra);
        return new(owed / 60m, completed / 60m, Math.Max(0, owed - completed) / 60m);
    }
    private static (DateTime Start, DateTime End) Clip(DateTime start, DateTime end, DateTime from, DateTime to) =>
        (start < from ? from : start, end > to ? to : end);
}
