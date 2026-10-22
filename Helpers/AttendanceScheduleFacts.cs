using HRM.DTOs.Attendance;
using HRM.Models;
using HRM.Models.WorkPlanning;
using Microsoft.EntityFrameworkCore;

namespace HRM.Helpers;

public static class AttendanceScheduleFacts
{
    public static async Task<List<StaffScheduleAttendanceDto>> LoadAsync(HrmTeContext db, int person, DateTime from, DateTime to, int? org, CancellationToken ct = default, List<AttendanceLog>? sourceLogs = null)
    {
        if (db.Model.FindEntityType(typeof(WorkAssignment)) == null) return [];
        var end = to.Date.AddDays(1);
        var assignments = await db.WorkAssignments.AsNoTracking().Include(a => a.WorkPlan).Include(a => a.WorkTemplateType)
            .Include(a => a.WorkAssignmentSegments)
            .Where(a => a.IsValid && a.CancelledDate == null && a.WorkPlan.IsValid &&
                (!org.HasValue || a.WorkPlan.OrganisationBusinessEntityId == org) &&
                a.WorkPlan.WorkDate >= from.Date && a.WorkPlan.WorkDate < end &&
                (a.WorkAssignmentOwners.Any(o => o.IndividualId == person && o.IsValid && o.IsCurrentOwner &&
                    o.EffectiveFrom <= a.StartDateTime && (o.EffectiveTo == null || o.EffectiveTo >= a.StartDateTime)) ||
                 a.WorkPlan.IndividualId == person && !a.WorkAssignmentOwners.Any(o => o.IsValid && o.IsCurrentOwner && o.IndividualId != person)))
            .ToListAsync(ct);
        var ids = assignments.Select(a => a.WorkAssignmentId).ToArray();
        var logs = sourceLogs ?? await db.AttendanceLogs.AsNoTracking().Include(l => l.AttendanceLogResolutions)
            .Where(l => l.IndividualId == person && (!org.HasValue || l.OrganisationId == org) &&
                l.AttendanceLogStateId == HRM.Components.Shared.SharedConfig.AttendanceLogStates.Valid && l.Date >= from.Date && l.Date < end.AddDays(1) &&
                l.AttendanceLogResolutions.Any(r => r.IsValid && r.WorkAssignmentId.HasValue && ids.Contains(r.WorkAssignmentId.Value)))
            .ToListAsync(ct);
        return assignments.Select(a =>
        {
            var events = logs.Select(l => new { l.Date, l.AttendanceLogId, Resolution = l.AttendanceLogResolutions.Where(r => r.IsValid)
                .OrderByDescending(r => r.AttendanceLogResolutionId).FirstOrDefault() }).Where(e => e.Resolution?.WorkAssignmentId == a.WorkAssignmentId).ToList();
            var start = a.WorkAssignmentSegments.Where(s => s.IsValid && s.ExpectedInOutModeId == 1)
                .Select(s => s.StartDateTime).DefaultIfEmpty(a.StartDateTime).Min();
            var finish = a.WorkAssignmentSegments.Where(s => s.IsValid && s.ExpectedInOutModeId == 2)
                .Select(s => s.StartDateTime).DefaultIfEmpty(a.EndDateTime).Max();
            var type = (a.WorkTemplateType?.Name ?? "") + " " + (a.WorkTemplateType?.Code ?? "");
            var shift = a.SchedulePurpose.Equals("Duty", StringComparison.OrdinalIgnoreCase) ||
                type.Contains("shift", StringComparison.OrdinalIgnoreCase) || type.Contains("rotat", StringComparison.OrdinalIgnoreCase);
            return new StaffScheduleAttendanceDto(a.WorkPlan.WorkDate.Date, a.Name, a.SchedulePurpose, shift,
                a.RequiresAttendance || a.WorkAssignmentSegments.Any(s => s.IsValid && s.IsMandatory && s.RequiresAttendance),
                start, finish, events.Where(e => e.Resolution!.AttendanceClockTypeId == 1).Select(e => (DateTime?)e.Date).Min(),
                events.Where(e => e.Resolution!.AttendanceClockTypeId == 2).Select(e => (DateTime?)e.Date).Max())
            {
                WorkAssignmentId = a.WorkAssignmentId,
                CheckInLogId = events.Where(e => e.Resolution!.AttendanceClockTypeId == 1).OrderBy(e => e.Date).Select(e => (int?)e.AttendanceLogId).FirstOrDefault()
            };
        }).OrderBy(a => a.Date).ThenBy(a => a.ScheduledStart).ToList();
    }
}
