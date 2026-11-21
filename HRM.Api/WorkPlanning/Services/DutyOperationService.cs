using HRM.Models;
using HRM.Models.WorkPlanning;
using HRM.WorkPlanning.Abstractions;
using Microsoft.EntityFrameworkCore;
namespace HRM.WorkPlanning.Services;
public sealed class DutyOperationService(IDbContextFactory<HrmTeContext> factory, IWorkPlanGenerator planner) : IDutyOperationService
{
    private static readonly HashSet<string> Supported = ["Replacement", "Swap", "Callout", "Extension", "Handover"];
    public async Task<long> RequestAsync(RequestDutyOperation request, CancellationToken cancellationToken = default)
    {
        if (!Supported.Contains(request.OperationType)) throw new InvalidOperationException("Unsupported duty operation.");
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length > 1000) throw new InvalidOperationException("Enter a reason of up to 1,000 characters.");
        if (request.ActualEndAt < request.ActualStartAt) throw new InvalidOperationException("The operation end cannot precede its start.");
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        if (!await db.DutyAssignments.AnyAsync(x => x.Id == request.DutyAssignmentId && x.Status == "Assigned", cancellationToken)) throw new InvalidOperationException("Select an active duty assignment.");
        if (await db.DutyOperations.AnyAsync(x => x.DutyAssignmentId == request.DutyAssignmentId && x.OperationType == request.OperationType && x.Status == "Requested", cancellationToken))
            throw new InvalidOperationException("An equivalent duty operation is already awaiting approval.");
        if (request.ReplacementDutyAssignmentId.HasValue && !await db.DutyAssignments.AnyAsync(x => x.Id == request.ReplacementDutyAssignmentId && x.Status == "Assigned", cancellationToken)) throw new InvalidOperationException("Select an active replacement duty assignment.");
        var assignment = await db.DutyAssignments.Include(x => x.DutyOccurrence).ThenInclude(x => x.WorkforceRosterVersion).ThenInclude(x => x.WorkforceRoster).SingleAsync(x => x.Id == request.DutyAssignmentId, cancellationToken);
        var requestType = await db.RequestTypes.SingleOrDefaultAsync(x => x.TypeName == "Duty Operation Request", cancellationToken) ?? throw new InvalidOperationException("Configure the Duty Operation Request workflow before submitting an operation.");
        var workflowRequest = new Request { RequestTypeId = requestType.RequestTypeId, RequestStateId = 10, ApplicantBusinessEntityId = assignment.IndividualId, OrganisationBusinessEntityID = assignment.DutyOccurrence.WorkforceRosterVersion.WorkforceRoster.OrganisationId, ApplicationDate = DateTime.UtcNow, RequestEffectiveDate = assignment.DutyOccurrence.StartAt, LastStateChangedDate = DateTime.UtcNow, LastStateChangedByUserId = request.RequestedByUserId, StateChangeRemarks = request.Reason.Trim() };
        db.Requests.Add(workflowRequest); await db.SaveChangesAsync(cancellationToken);
        workflowRequest.ReferenceNumber = $"DUTY-OP-{workflowRequest.RequestId}";
        var operation = new DutyOperation { DutyAssignmentId=request.DutyAssignmentId, OperationType=request.OperationType, Reason=request.Reason.Trim(), RequestedByUserId=request.RequestedByUserId, RequestedAtUtc=DateTime.UtcNow, OperationLogId=request.OperationLogId, ReplacementDutyAssignmentId=request.ReplacementDutyAssignmentId, ActualStartAt=request.ActualStartAt, ActualEndAt=request.ActualEndAt, RequestId=workflowRequest.RequestId };
        db.DutyOperations.Add(operation); await db.SaveChangesAsync(cancellationToken); return operation.Id;
    }

    public async Task ApplyApprovedAsync(long operationId, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var operation = await db.DutyOperations.Include(x => x.DutyAssignment).ThenInclude(x => x.DutyOccurrence).ThenInclude(x => x.WorkforceRosterVersion).ThenInclude(x => x.WorkforceRoster).Include(x => x.Request).SingleOrDefaultAsync(x => x.Id == operationId, cancellationToken) ?? throw new InvalidOperationException("Duty operation not found.");
        if (operation.Status != "Requested" || operation.Request?.RequestStateId != 4) throw new InvalidOperationException("Only a final-approved duty operation can be applied.");
        var source = operation.DutyAssignment;
        if (operation.OperationType is "Replacement" or "Swap")
        {
            if (!operation.ReplacementDutyAssignmentId.HasValue) throw new InvalidOperationException("An approved replacement or swap requires a receiving duty assignment.");
            source.Status = "Replaced";
        }
        else if (operation.OperationType == "Callout") source.Status = "CalloutCompleted";
        else if (operation.OperationType == "Extension") source.Status = "Extended";
        else if (operation.OperationType == "Handover") source.Status = "HandedOver";
        operation.Status = "Applied";
        await db.SaveChangesAsync(cancellationToken);
        var occurrence = source.DutyOccurrence;
        if (occurrence.StartAt >= DateTime.UtcNow)
            await planner.GenerateOrGetAsync(source.IndividualId, source.JobId, occurrence.WorkforceRosterVersion.WorkforceRoster.OrganisationId, DateOnly.FromDateTime(occurrence.StartAt), cancellationToken);
    }
}
