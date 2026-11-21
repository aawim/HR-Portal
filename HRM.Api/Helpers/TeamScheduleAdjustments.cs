using HRM.DTOs.Attendance;
using HRM.Models;
using HRM.Models.WorkPlanning;
using Microsoft.EntityFrameworkCore;

namespace HRM.Helpers;

public static class TeamScheduleAdjustments
{
    public static async Task<TeamScheduleAdjustment?> FindAsync(HrmTeContext db, int person, int org, DateTime date, CancellationToken ct = default)
    {
        var day = date.Date;
        return await db.TeamScheduleAdjustments.AsNoTracking().Where(a => a.IsActive && a.WorkDate == day &&
            a.Team.OrganisationId == org && a.Team.IsValid == true && (a.Team.StartDate == null || a.Team.StartDate <= day) &&
            (a.Team.EndDate == null || a.Team.EndDate >= day) && a.Team.ParentTeamId.HasValue &&
            a.Team.TeamStaffs.Any(m => m.StaffId == person && m.IsValid && m.StartDate <= day && (!m.EndDate.HasValue || m.EndDate >= day)))
            .OrderByDescending(a => a.Id).FirstOrDefaultAsync(ct);
    }

    public static (DateTime Start, DateTime End, int GraceBefore) Segment(TeamScheduleAdjustment? adjustment,
        string purpose, string code, DateTime start, DateTime end, int graceBefore)
    {
        if (adjustment == null || purpose != "Official") return (start, end, graceBefore);
        var revised = code is "CHECK_IN" or "DUTY_CHECK_IN" ? adjustment.RevisedCheckIn :
            code is "CHECK_OUT" or "DUTY_CHECK_OUT" ? adjustment.RevisedCheckOut : null;
        if (!revised.HasValue) return (start, end, graceBefore);
        var value = start.Date.Add(revised.Value);
        if (code is "CHECK_OUT" or "DUTY_CHECK_OUT" && value < start.Date.AddHours(6)) value = value.AddDays(1);
        return (value, end.Add(value - start), graceBefore);
    }

    public static void Apply(AttendanceWorkPlanDto plan, TeamScheduleAdjustment? adjustment)
    {
        if (adjustment == null) return;
        foreach (var s in plan.Segments)
        {
            var t = Segment(adjustment, s.SchedulePurpose, s.WorkSegmentTypeCode, s.StartDateTime, s.EndDateTime, s.GraceBeforeMinutes);
            s.StartDateTime = t.Start; s.EndDateTime = t.End; s.GraceBeforeMinutes = t.GraceBefore;
        }
        foreach (var a in plan.Assignments.Where(x => x.SchedulePurpose == "Official")) ApplyAssignment(a, adjustment);
        plan.ScheduleAmendmentMessage = $"Sub-team schedule adjustment. {adjustment.Reason}";
    }

    public static void Apply(WorkPlan plan, TeamScheduleAdjustment? adjustment)
    {
        if (adjustment == null) return;
        foreach (var s in plan.WorkPlanSegments)
        {
            var t = Segment(adjustment, s.SchedulePurpose, s.WorkSegmentType?.Code ?? "", s.StartDateTime, s.EndDateTime, s.GraceBeforeMinutes);
            s.StartDateTime = t.Start; s.EndDateTime = t.End; s.GraceBeforeMinutes = t.GraceBefore;
        }
        foreach (var a in plan.WorkAssignments.Where(x => x.SchedulePurpose == "Official"))
        {
            if (adjustment.RevisedCheckIn.HasValue) a.StartDateTime = a.StartDateTime.Date.Add(adjustment.RevisedCheckIn.Value);
            if (adjustment.RevisedCheckOut.HasValue)
            {
                var finish = a.EndDateTime.Date.Add(adjustment.RevisedCheckOut.Value);
                if (finish <= a.StartDateTime) finish = finish.AddDays(1);
                a.EndDateTime = finish;
            }
        }
    }

    private static void ApplyAssignment(AttendanceWorkAssignmentDto a, TeamScheduleAdjustment adjustment)
    {
        if (adjustment.RevisedCheckIn.HasValue) a.StartDateTime = a.StartDateTime.Date.Add(adjustment.RevisedCheckIn.Value);
        if (adjustment.RevisedCheckOut.HasValue)
        {
            var finish = a.EndDateTime.Date.Add(adjustment.RevisedCheckOut.Value);
            if (finish <= a.StartDateTime) finish = finish.AddDays(1);
            a.EndDateTime = finish;
        }
    }
}
