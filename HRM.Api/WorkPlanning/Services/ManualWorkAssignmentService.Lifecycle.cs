using HRM.DTOs.WorkPlanning;
using HRM.Enum;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.WorkPlanning.Services;

public partial class ManualWorkAssignmentService
{
    public Task<GeneratedWorkPlanResult> UpdateAsync(long assignmentId, ManualWorkAssignmentRequest request, CancellationToken cancellationToken = default) =>
        ChangeAsync(assignmentId, request, null, cancellationToken);
    public Task<GeneratedWorkPlanResult> CancelAsync(long assignmentId, string reason, CancellationToken cancellationToken = default) =>
        ChangeAsync(assignmentId, null, reason, cancellationToken);

    private async Task<GeneratedWorkPlanResult> ChangeAsync(long assignmentId, ManualWorkAssignmentRequest? request, string? reason, CancellationToken ct)
    {
        try
        {
            var org = await WorkPlanningAccess.RequireOrganisationAsync(_access, true);
            var actor = await _access.RequireContextAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            var assignment = await db.WorkAssignments.Include(x => x.WorkPlan).ThenInclude(x => x.WorkPlanSegments)
                .Include(x => x.WorkAssignmentOwners).Include(x => x.WorkAssignmentSegments).ThenInclude(x => x.WorkTemplateSegment).ThenInclude(x => x.WorkSegmentType)
                .SingleOrDefaultAsync(x => x.WorkAssignmentId == assignmentId && x.IsValid && x.AssignmentSource == WorkAssignmentSource.Manual &&
                    x.WorkPlan.IsValid && x.WorkPlan.OrganisationBusinessEntityId == org, ct);
            if (assignment == null) return Failure("The manual assignment is unavailable in the current organisation.");
            var plan = assignment.WorkPlan;
            if (plan.IsFinalized || await db.AttendanceLogResolutions.AnyAsync(x => x.WorkPlanId == plan.WorkPlanId, ct) ||
                await db.AttendanceLogs.AnyAsync(x => x.IndividualId == plan.IndividualId && x.OrganisationId == org && x.Date.Date == plan.WorkDate.Date, ct))
                return Failure("This plan is finalized or has clock events. Use the attendance correction process to preserve its history.");
            if (request != null && (request.IndividualId != plan.IndividualId || request.JobId != plan.JobId || request.OrganisationBusinessEntityId != org ||
                request.WorkDate.Date != plan.WorkDate.Date || request.WorkTemplateId != assignment.WorkTemplateId))
                return Failure("The staff member, date and template cannot be changed here. Cancel this assignment and add a new one to change its hours or date.");
            if (request != null && (request.Priority < 0 || request.AssignmentTitle?.Length > 150 || request.AssignmentDescription?.Length > 500 || request.Remarks?.Length > 500))
                return Failure("Enter a valid title, description, remarks and priority.");
            if (request == null && (string.IsNullOrWhiteSpace(reason) || reason.Length > 500)) return Failure("Enter a cancellation reason (up to 500 characters).");
            var segments = plan.WorkPlanSegments.Where(p => p.IsValid && p.SchedulePurpose == assignment.SchedulePurpose &&
                assignment.WorkAssignmentSegments.Any(s => s.WorkTemplateSegmentId == p.WorkTemplateSegmentId && s.StartDateTime == p.StartDateTime)).ToList();
            await _operationLogService.CreateAsync(db, HRM.Components.Shared.SharedConfig.OperationLogActionTypes.WORK_ASSIGNMENT_CREATE,
                $"Manual assignment {(request == null ? "cancel" : "update")}: tenant {org}, assignment {assignmentId}, job {plan.JobId}; {reason ?? request?.Remarks}");
            if (request == null)
            {
                assignment.IsValid = false; assignment.CancelledDate = DateTime.UtcNow; assignment.CancelledByUserId = actor.UserId; assignment.CancellationReason = reason!.Trim();
                foreach (var owner in assignment.WorkAssignmentOwners) { owner.IsValid = false; owner.IsCurrentOwner = false; }
                foreach (var segment in assignment.WorkAssignmentSegments) segment.IsValid = false;
                foreach (var segment in segments) segment.IsValid = false;
            }
            else
            {
                assignment.Name = string.IsNullOrWhiteSpace(request.AssignmentTitle) ? assignment.Name : request.AssignmentTitle.Trim();
                assignment.Description = request.AssignmentDescription?.Trim(); plan.Remarks = request.Remarks?.Trim();
                assignment.Priority = request.Priority; assignment.RequiresAttendance = request.RequiresAttendance; assignment.RequiresCheckOut = request.RequiresCheckout;
                foreach (var segment in assignment.WorkAssignmentSegments.Where(x => x.IsValid))
                {
                    var source = segment.WorkTemplateSegment;
                    segment.RequiresAttendance = request.RequiresAttendance && (source?.RequiresAttendance ?? segment.RequiresAttendance) &&
                        (request.RequiresCheckout || source?.WorkSegmentType?.Code is not ("CHECK_OUT" or "DUTY_CHECK_OUT"));
                    segment.IsMandatory = source?.IsMandatory ?? segment.IsMandatory;
                    foreach (var snapshot in segments.Where(x => x.WorkTemplateSegmentId == segment.WorkTemplateSegmentId && x.StartDateTime == segment.StartDateTime))
                    { snapshot.RequiresAttendance = segment.RequiresAttendance; snapshot.IsMandatory = segment.IsMandatory; snapshot.Name = $"{assignment.Name} · {segment.Name}"; }
                }
            }
            await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
            return new GeneratedWorkPlanResult { Success = true, WorkPlanId = plan.WorkPlanId, WorkAssignmentId = assignmentId,
                Version = plan.Version, Message = request == null ? "Manual assignment cancelled." : "Manual assignment updated." };
        }
        catch (UnauthorizedAccessException ex) { return Failure(ex.Message); }
    }
}
