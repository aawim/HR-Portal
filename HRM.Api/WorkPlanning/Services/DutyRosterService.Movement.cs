using HRM.Enum;
using HRM.Models;
using HRM.Models.WorkPlanning;
using Microsoft.EntityFrameworkCore;

namespace HRM.WorkPlanning.Services;

public sealed partial class DutyRosterService
{
    // Dated exceptions survive regeneration; templates and recurring attachments remain reusable.
    

    private async Task ApplyMovementAsync(HrmTeContext db, DutyChangeRequest c, int actor)
    {
        if (!c.SourceAcceptedAtUtc.HasValue || !c.AcceptedAtUtc.HasValue || c.Acceptance != "Accepted")
            throw new InvalidOperationException("Both staff members must accept before the supervisor can approve the roster change.");
        var locked = new Dictionary<long, WorkAssignment>();
        foreach (var id in new[] { c.SourceAssignmentId, c.SwapAssignmentId ?? c.SourceAssignmentId }.Distinct().Order())
            locked[id] = await LockAssignmentAsync(db, id);
        var source = locked[c.SourceAssignmentId]; var swap = c.SwapAssignmentId.HasValue ? locked[c.SwapAssignmentId.Value] : null;
        await ValidatePairAsync(db, c, source, swap);
        var swapDate = swap?.WorkPlan.WorkDate;
        if (await db.WorkPlans.AnyAsync(x => x.IsValid && x.IsFinalized && ((x.JobId == c.TargetJobId && x.WorkDate == source.WorkPlan.WorkDate) ||
            (swapDate.HasValue && x.JobId == c.SourceJobId && x.WorkDate == swapDate))))
            throw new InvalidOperationException("A receiving work plan has been finalized. This duty change cannot proceed.");
        var assignmentIds = locked.Keys.ToArray();
        if (await db.AttendanceLogResolutions.AnyAsync(x => x.WorkAssignmentId.HasValue && assignmentIds.Contains(x.WorkAssignmentId.Value)))
            throw new InvalidOperationException("A duty already has clock records. This duty change cannot proceed.");
        var audit = await logs.CreateAsync(db, HRM.Components.Shared.SharedConfig.OperationLogActionTypes.WORK_ASSIGNMENT_CREATE,
            $"Approved duty {c.Kind}: {c.Request.ReferenceNumber}; {c.SourceIndividualId} → {c.TargetIndividualId}");
        var incoming = await MoveAsync(db, source, c.TargetJobId, c.TargetIndividualId, c, actor, audit.OperationLogId);
        WorkAssignment? returned = null;
        if (swap != null) returned = await MoveAsync(db, swap, c.SourceJobId, c.SourceIndividualId, c, actor, audit.OperationLogId);
        await db.SaveChangesAsync();
        c.TargetNewAssignmentId = incoming.WorkAssignmentId; c.SourceNewAssignmentId = returned?.WorkAssignmentId;
    }

    private static async Task<WorkAssignment> MoveAsync(HrmTeContext db, WorkAssignment source, int job, int person, DutyChangeRequest c, int actor, int audit)
    {
        var date = source.WorkPlan.WorkDate;
        var targetPlan = await db.WorkPlans.Include(x => x.WorkPlanSegments).Include(x => x.WorkAssignments)
            .SingleOrDefaultAsync(x => x.IsValid && x.JobId == job && x.WorkDate == date);
        if (targetPlan?.IsFinalized == true) throw new InvalidOperationException("The receiving work plan has been finalized.");
        if (targetPlan == null)
        {
            targetPlan = new WorkPlan(); db.Entry(targetPlan).CurrentValues.SetValues(source.WorkPlan);
            targetPlan.WorkPlanId = 0; targetPlan.JobId = job; targetPlan.IndividualId = person; targetPlan.PlanGuid = Guid.NewGuid();
            targetPlan.GeneratedByUserId = actor; targetPlan.GeneratedDate = targetPlan.CreatedDate = DateTime.UtcNow;
            targetPlan.WorkTemplateId = null; targetPlan.OperationLogId = audit; targetPlan.IsManual = true;
            targetPlan.Version = (await db.WorkPlans.Where(x => x.JobId == job && x.WorkDate == date).MaxAsync(x => (int?)x.Version) ?? 0) + 1;
            db.WorkPlans.Add(targetPlan);
        }
        if (targetPlan.OrganisationBusinessEntityId != c.OrganisationId || targetPlan.IndividualId != person)
            throw new UnauthorizedAccessException("The receiving work plan belongs to another context.");
        var copy = new WorkAssignment(); db.Entry(copy).CurrentValues.SetValues(source);
        copy.WorkAssignmentId = 0; copy.WorkPlanId = 0; copy.WorkPlan = targetPlan; copy.CreatedByUserId = actor;
        copy.CreatedDate = DateTime.UtcNow; copy.OperationLogId = audit; copy.AssignmentSource = WorkAssignmentSource.Manual;
        copy.Description = $"{MovementMarker}{c.Request.ReferenceNumber}";
        db.WorkAssignments.Add(copy);
        foreach (var segment in source.WorkAssignmentSegments.Where(x => x.IsValid))
        {
            var next = new WorkAssignmentSegment(); db.Entry(next).CurrentValues.SetValues(segment);
            next.WorkAssignmentSegmentId = 0; next.WorkAssignmentId = 0; next.WorkAssignment = copy; next.OperationLogId = audit;
            copy.WorkAssignmentSegments.Add(next);
            targetPlan.WorkPlanSegments.Add(new WorkPlanSegment { SchedulePurpose = "Duty", WorkTemplateSegmentId = next.WorkTemplateSegmentId,
                WorkSegmentTypeId = next.WorkSegmentTypeId, Name = next.Name, Description = next.Description,
                SequenceNumber = targetPlan.WorkPlanSegments.Count + 1, StartDateTime = next.StartDateTime,
                EndDateTime = next.EndDateTime ?? next.StartDateTime, GraceBeforeMinutes = next.GraceBeforeMinutes, GraceAfterMinutes = next.GraceAfterMinutes,
                IsMandatory = next.IsMandatory, RequiresAttendance = next.RequiresAttendance, RequiresLocationValidation = next.RequiresLocationValidation,
                RequiresDeviceValidation = next.RequiresDeviceValidation, IsPaid = next.WorkTemplateSegment?.IsPaid ?? false,
                IsValid = true, CreatedDate = DateTime.UtcNow, OperationLogId = audit });
        }
        copy.WorkAssignmentOwners.Add(new WorkAssignmentOwner { IndividualId = person, JobId = job, OwnershipType = c.Kind == "Swap" ? WorkOwnershipType.Swap : WorkOwnershipType.Transfer,
            AssignedDate = DateTime.UtcNow, AssignedByUserId = actor, EffectiveFrom = source.StartDateTime, EffectiveTo = source.EndDateTime,
            IsCurrentOwner = true, IsValid = true, OperationLogId = audit });
        var originalSegments = await db.WorkPlanSegments.Where(x => x.WorkPlanId == source.WorkPlanId && x.IsValid && x.SchedulePurpose == "Duty").ToListAsync();
        foreach (var segment in originalSegments.Where(x => source.WorkAssignmentSegments.Any(s => s.IsValid && s.WorkTemplateSegmentId == x.WorkTemplateSegmentId && s.StartDateTime == x.StartDateTime)))
            segment.IsValid = false;
        source.IsValid = false; source.CancelledDate = DateTime.UtcNow; source.CancelledByUserId = actor;
        source.CancellationReason = $"{MovementMarker}{c.Request.ReferenceNumber}";
        source.WorkPlan.IsManual = true;
        foreach (var segment in source.WorkAssignmentSegments) segment.IsValid = false;
        foreach (var owner in source.WorkAssignmentOwners.Where(x => x.IsCurrentOwner))
        { owner.IsCurrentOwner = false; owner.RelievedDate = DateTime.UtcNow; owner.RelievedByUserId = actor; owner.ReliefReason = source.CancellationReason; }
        targetPlan.IsManual = true; targetPlan.WorkTemplateId = null;
        return copy;
    }
}
