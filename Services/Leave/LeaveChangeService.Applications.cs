using HRM.Components.Shared;
using HRM.DTOs.Leave;
using HRM.Helpers;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Leave;

public partial class LeaveChangeService
{
    public async Task<List<PendingLeaveRequestDto>> GetPendingLeaveRequestsAsync()
    {
        var user = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync();
        var pending = db.WfRequestApprovals.AsNoTracking()
            .Where(x => x.ApproverUserId == user.UserId && x.Decision == "Pending" &&
                x.RequestStageInstance.StageStatus == "Pending" &&
                x.RequestStageInstance.WorkflowRequestInstance.WorkflowStatus == "InProgress" &&
                x.RequestStageInstance.WorkflowRequestInstance.CurrentRequestStageInstanceId == x.RequestStageInstanceId)
            .Select(x => x.RequestStageInstance.WorkflowRequestInstance.RequestId);
        var links = await db.LeaveRequests.AsNoTracking()
            .Include(x => x.Request).ThenInclude(x => x.RequestState)
            .Include(x => x.Leave).ThenInclude(x => x.Job).ThenInclude(x => x.Individual)
            .Include(x => x.Leave).ThenInclude(x => x.LeaveType)
            .Include(x => x.Leave).ThenInclude(x => x.LeaveReason).ThenInclude(x => x!.LeaveTypeReasonType)
            .Include(x => x.Leave).ThenInclude(x => x.LeaveSpendingLocation)
            .Where(x => pending.Contains(x.RequestId) &&
                x.Leave.LeaveStateId != SharedConfig.LeaveStates.APPROVED &&
                x.Leave.LeaveStateId != SharedConfig.LeaveStates.REJECTED &&
                x.Leave.LeaveStateId != SharedConfig.LeaveStates.CANCELLED &&
                x.Request.RequestStateId != SharedConfig.RequestStates.APPROVED &&
                x.Request.RequestStateId != SharedConfig.RequestStates.REJECTED &&
                x.Request.RequestStateId != SharedConfig.RequestStates.CANCELLED).OrderByDescending(x => x.RequestId).ToListAsync();
        var result = new List<PendingLeaveRequestDto>();
        foreach (var link in links)
        {
            if (!await access.HasOrganisationAccessAsync(link.Request.OrganisationBusinessEntityID ?? 0)) continue;
            var instance = await db.WfRequestInstances.AsNoTracking().SingleAsync(x => x.RequestId == link.RequestId);
            var leave = link.Leave;
            var availability = await GetActionAvailabilityAsync(db, link.Request, leave, instance, user);
            var person = leave.Job.Individual;
            result.Add(new()
            {
                RequestId = link.RequestId, LeaveId = leave.LeaveId, HasHandover = await db.LeaveHandoverReviews.AnyAsync(r => r.LeaveId == leave.LeaveId), Reference = link.Request.ReferenceNumber ?? $"#{link.RequestId}",
                Applicant = string.Join(" ", new[] { person.FirstNameEnglish, person.MiddleNameEnglish, person.LastNameEnglish }.Where(x => !string.IsNullOrWhiteSpace(x))),
                LeaveType = leave.LeaveType.Name, FromDate = leave.FromDate, ToDate = leave.ToDate,
                Duration = leave.Duration, Status = link.Request.RequestState.StateName,
                Reason = leave.LeaveReason?.LeaveTypeReasonType?.ReasonTypeName ?? "",
                Explanation = leave.LeaveReason?.Reason ?? "",
                Address = leave.LeaveSpendingLocation?.Address ?? "",
                HandoverStatus = await LeaveHandoverService.StatusAsync(db, leave.LeaveId),
                Contact = leave.LeaveSpendingLocation?.ContactNumber ?? "", Actions = availability.Actions, ActionsMessage = availability.Message
            });
        }
        return result;
    }

    private async Task ApplyApplicationStateAsync(HrmTeContext db, Leaf leave, int target)
    {
        if (leave.LeaveStateId is SharedConfig.LeaveStates.REJECTED or SharedConfig.LeaveStates.CANCELLED)
            throw new InvalidOperationException("This leave application has already closed.");
        var next = LeaveRequestStateMapper.ToLeaveState(target);
        // Applications reserve balance on submission; refund only on unsuccessful closure.
        if (target is SharedConfig.RequestStates.REJECTED or SharedConfig.RequestStates.CANCELLED)
        {
            var balance = await ValidateBalanceAndOverlapAsync(db, leave, leave.ToDate, leave.Duration);
            var log = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE,
                $"Leave {leave.LeaveId}: request state changed to {target}; balance refunded.");
            balance.RemainingDays = checked(balance.RemainingDays!.Value + leave.Duration);
            balance.LastLeaveTakenDate = await db.Leaves.AsNoTracking()
                .Where(x => x.JobId == leave.JobId && x.LeaveTypeId == leave.LeaveTypeId && x.LeaveId != leave.LeaveId &&
                    x.LeaveStateId != SharedConfig.LeaveStates.REJECTED && x.LeaveStateId != SharedConfig.LeaveStates.CANCELLED)
                .Select(x => (DateTime?)x.ToDate).MaxAsync();
            balance.OperationLogId = log.OperationLogId;
            leave.OperationLogId = log.OperationLogId;
        }
        leave.LeaveStateId = next;
    }
}
