using HRM.Components.Shared;
using HRM.Models;
using HRM.Models.WorkPlanning;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed partial class LateCheckInRequestService
{
    private async Task ApplyScheduleAsync(HrmTeContext db, LateCheckInRequest request)
    {
        if (request.AppliedAtUtc.HasValue || request.RequestedStart == null || request.RequestedEnd == null)
            throw new InvalidOperationException("This request cannot change the schedule. Refresh and try again.");
        var assignment = await db.WorkAssignments.Include(a => a.WorkPlan).ThenInclude(p => p.WorkPlanSegments).ThenInclude(s => s.WorkSegmentType)
            .Include(a => a.WorkAssignmentSegments).ThenInclude(s => s.WorkSegmentType)
            .SingleAsync(a => a.WorkAssignmentId == request.WorkAssignmentId && a.IsValid && a.CancelledDate == null &&
                a.WorkPlan.IsValid && a.WorkPlan.OrganisationBusinessEntityId == request.OrganisationId);
        var checkout = assignment.WorkAssignmentSegments.Where(s => s.IsValid && IsCheckout(s)).ToList();
        if (assignment.WorkPlan.IsFinalized) throw new InvalidOperationException("This work plan is finalized. Contact HR before requesting a schedule change.");
        if (await db.Leaves.AnyAsync(l => l.Job.IndividualID == request.IndividualId && l.Job.OrganisationID == request.OrganisationId && l.LeaveStateId == 4 &&
            l.FromDate < assignment.WorkPlan.WorkDate.Date.AddDays(1) && l.ToDate >= assignment.WorkPlan.WorkDate.Date))
            throw new InvalidOperationException("Approved leave now covers this schedule. Reject or cancel the time-change request and contact HR.");
        if (checkout.Count == 0)
            throw new InvalidOperationException("This schedule has no check-out event. Ask HR to add a check-out event to the dated schedule before approving this request.");
        var start = request.RequestedStart.Value; var finish = request.RequestedEnd.Value;
        var amendments = await HRM.Helpers.OfficialScheduleAmendments.LoadAsync(db, request.OrganisationId,
            assignment.WorkPlan.WorkDate, assignment.WorkPlan.WorkDate);
        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.WORK_ASSIGNMENT_CREATE,
            $"Approved {request.Kind} request {request.RequestId}: assignment {assignment.WorkAssignmentId}, {request.ScheduledStart:O}–{request.ScheduledEnd:O} → {start:O}–{finish:O}; compulsory check-out.");
        var hourAdjustments = await HRM.Helpers.TemporaryWorkingHours.AdjustmentsAsync(db, new[] { assignment });
        var changedBoundaries = new Dictionary<int, WorkAssignmentSegment>();
        foreach (var segment in assignment.WorkAssignmentSegments.Where(s => s.IsValid))
        {
            // Match both dated copies before changing times; never alter another assignment's segments.
            var snapshots = assignment.WorkPlan.WorkPlanSegments.Where(p => p.IsValid && p.SchedulePurpose == assignment.SchedulePurpose &&
                p.WorkTemplateSegmentId == segment.WorkTemplateSegmentId && p.WorkSegmentTypeId == segment.WorkSegmentTypeId && p.StartDateTime == segment.StartDateTime).ToList();
            if (snapshots.Count != 1)
                throw new InvalidOperationException("This schedule's event details need review. Please contact HR before approving this request.");
            var snapshot = snapshots[0];
            // Materialise the same effective schedule used when the staff request
            // was submitted before applying its personal time change.
            var baseTiming = (Start: segment.StartDateTime, End: segment.EndDateTime ?? segment.StartDateTime);
            if (hourAdjustments.TryGetValue(assignment.WorkAssignmentId, out var hourAdjustment))
                baseTiming = HRM.Helpers.TemporaryWorkingHours.Segment(hourAdjustment, segment.WorkSegmentType.Code, baseTiming.Start, baseTiming.End);
            var amended = HRM.Helpers.OfficialScheduleAmendments.Segment(amendments, request.OrganisationId,
                assignment.SchedulePurpose, segment.WorkSegmentType.Code, baseTiming.Start,
                baseTiming.End, segment.GraceBeforeMinutes);
            segment.StartDateTime = amended.Start;
            if (segment.EndDateTime.HasValue) segment.EndDateTime = amended.End;
            segment.GraceBeforeMinutes = amended.GraceBefore;
            var shift = start - request.ScheduledStart;
            var latestWindow = (segment.EndDateTime ?? segment.StartDateTime) - segment.StartDateTime;
            if (request.Kind == "LateCheckIn")
            {
                segment.StartDateTime += shift;
                if (segment.EndDateTime.HasValue) segment.EndDateTime += shift;
            }
            else if (!IsCheckout(segment))
            {
                if (segment.StartDateTime >= finish) { segment.RequiresAttendance = false; segment.IsMandatory = false; }
                if (segment.EndDateTime > finish && segment.StartDateTime < finish) segment.EndDateTime = finish;
            }
            if (IsCheckout(segment))
            {
                segment.StartDateTime = finish;
                segment.EndDateTime = finish.Add(latestWindow < TimeSpan.Zero ? TimeSpan.Zero : latestWindow);
                segment.ExpectedInOutModeId = 2; segment.RequiresAttendance = true; segment.IsMandatory = true;
                segment.GraceBeforeMinutes = 0;
            }
            if (segment.ExpectedInOutModeId == 1 || segment.WorkSegmentType.Code is "CHECK_IN" or "DUTY_CHECK_IN")
            {
                segment.ExpectedInOutModeId = 1;
                segment.GraceAfterMinutes = 0;
                // A later approved start still permits arrival during the original check-in window.
                if (shift > TimeSpan.Zero) segment.GraceBeforeMinutes += (int)Math.Ceiling(shift.TotalMinutes);
            }
            segment.OperationLogId = audit.OperationLogId;
            snapshot.StartDateTime = segment.StartDateTime; snapshot.EndDateTime = segment.EndDateTime ?? segment.StartDateTime;
            snapshot.RequiresAttendance = segment.RequiresAttendance; snapshot.IsMandatory = segment.IsMandatory;
            snapshot.GraceBeforeMinutes = segment.GraceBeforeMinutes; snapshot.GraceAfterMinutes = segment.GraceAfterMinutes;
            snapshot.OperationLogId = audit.OperationLogId; snapshot.UpdatedDate = DateTime.UtcNow;
            if (segment.ExpectedInOutModeId is 1 or 2) changedBoundaries.Add(snapshot.WorkPlanSegmentId, segment);
        }
        assignment.StartDateTime = start; assignment.EndDateTime = finish;
        assignment.RequiresAttendance = true; assignment.RequiresCheckOut = true; assignment.OperationLogId = audit.OperationLogId;
        assignment.WorkPlan.OperationLogId = audit.OperationLogId;
        await ResolveNewlyCoveredEventsAsync(db, request, assignment, changedBoundaries, audit.OperationLogId);
        request.AppliedAtUtc = DateTime.UtcNow;
    }
    private static bool IsCheckout(WorkAssignmentSegment s) => s.ExpectedInOutModeId == 2 || s.WorkSegmentType.Code is "CHECK_OUT" or "DUTY_CHECK_OUT";

    private static async Task ResolveNewlyCoveredEventsAsync(HrmTeContext db, LateCheckInRequest request, WorkAssignment assignment,
        Dictionary<int, WorkAssignmentSegment> changed, int operation)
    {
        // An approval may arrive after the physical punch. Keep its timestamp and append
        // an audited resolution only when the new boundary unambiguously covers it.
        var boundaries = assignment.WorkPlan.WorkPlanSegments.Where(p => p.IsValid && p.RequiresAttendance &&
            p.WorkSegmentType.Code is "CHECK_IN" or "DUTY_CHECK_IN" or "CHECK_OUT" or "DUTY_CHECK_OUT").ToList();
        if (boundaries.Count == 0) return;
        var lower = boundaries.Min(p => p.StartDateTime.AddMinutes(-Math.Max(180, p.GraceBeforeMinutes)));
        var upper = boundaries.Max(p => p.EndDateTime);
        var punches = await db.AttendanceLogs.Include(l => l.AttendanceLogResolutions).Where(l => l.IndividualId == request.IndividualId &&
            l.OrganisationId == request.OrganisationId && l.AttendanceLogStateId == SharedConfig.AttendanceLogStates.Valid && l.Date >= lower && l.Date <= upper)
            .OrderBy(l => l.Date).ToListAsync();
        var occupied = punches.SelectMany(l => l.AttendanceLogResolutions.Where(r => r.IsValid &&
            r.AttendanceResolutionStatusId == HRM.Constants.AttendanceResolutionStatusIds.Resolved && r.WorkPlanSegmentId.HasValue))
            .Select(r => r.WorkPlanSegmentId!.Value).ToHashSet();
        foreach (var punch in punches)
        {
            var old = punch.AttendanceLogResolutions.Where(r => r.IsValid).OrderByDescending(r => r.AttendanceLogResolutionId).FirstOrDefault();
            if (old == null || old.AttendanceClockTypeId != 0 || old.AttendanceResolutionStatusId != HRM.Constants.AttendanceResolutionStatusIds.OutsideResolutionWindow) continue;
            var matches = boundaries.Where(p => punch.Date >= p.StartDateTime.AddMinutes(
                    -(p.WorkSegmentType.Code is "CHECK_IN" or "DUTY_CHECK_IN" ? Math.Max(180, p.GraceBeforeMinutes) : p.GraceBeforeMinutes)) && punch.Date <= p.EndDateTime)
                .OrderBy(p => Math.Abs((punch.Date - p.StartDateTime).TotalMinutes)).ToList();
            var selected = matches.FirstOrDefault();
            if (selected == null || !changed.TryGetValue(selected.WorkPlanSegmentId, out var segment) || occupied.Contains(selected.WorkPlanSegmentId)) continue;
            if (matches.Count > 1 && Math.Abs((punch.Date - matches[0].StartDateTime).TotalMinutes) == Math.Abs((punch.Date - matches[1].StartDateTime).TotalMinutes)) continue;
            old.IsValid = false;
            db.AttendanceLogResolutions.Add(new AttendanceLogResolution { AttendanceLogId = punch.AttendanceLogId,
                WorkPlanId = assignment.WorkPlanId, WorkAssignmentId = assignment.WorkAssignmentId,
                WorkAssignmentSegmentId = segment.WorkAssignmentSegmentId, WorkPlanSegmentId = selected.WorkPlanSegmentId,
                JobId = request.JobId, AttendanceClockTypeId = segment.ExpectedInOutModeId,
                AttendanceResolutionStatusId = HRM.Constants.AttendanceResolutionStatusIds.Resolved,
                ResolutionMessage = $"Resolved after approved {request.Kind} request {request.RequestId}; physical clock time preserved.",
                ResolutionDate = DateTime.UtcNow.AddHours(5), CreatedDate = DateTime.UtcNow.AddHours(5), IsValid = true, OperationLogId = operation });
            occupied.Add(selected.WorkPlanSegmentId);
        }
    }
}
