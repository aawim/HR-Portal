using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Leave;
using HRM.DTOs.UserContext;
using HRM.Helpers;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Models.Workflow;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using HRM.Services.Interfaces.Leave;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Leave;

public partial class LeaveChangeService(
    IDbContextFactory<HrmTeContext> factory,
    IUserAccessService access,
    IRequestService requests,
    IOperationLogService logs,
    IWorkflowEngine workflow,
    IWorkflowApproverResolver approvers,
    IWorkflowRequestContextResolver workflowContext) : ILeaveChangeService
{
    public async Task<List<LeaveChangeLeaveDto>> GetMyLeavesAsync()
    {
        var user = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync();
        var leaves = await db.Leaves.AsNoTracking().Include(x => x.Job).Include(x => x.LeaveType)
            .Where(x => x.Job.IndividualID == user.IndividualId && x.LeaveStateId == SharedConfig.LeaveStates.APPROVED)
            .OrderByDescending(x => x.FromDate).ToListAsync();
        var ids = leaves.Select(x => x.LeaveId).ToArray();
        var pendingIds = await db.LeaveChangeRequests.AsNoTracking()
            .Where(x => ids.Contains(x.LeaveId) &&
                (x.Request.RequestState.IsProcessingState || x.Request.RequestStateId == SharedConfig.RequestStates.INCOMPLETE))
            .Select(x => x.LeaveId).Distinct().ToListAsync();
        var result = new List<LeaveChangeLeaveDto>();
        var policies = new Dictionary<(int? TypeId, int OrganisationId), bool>();
        foreach (var leave in leaves)
        {
            var key = (leave.LeaveTypeId, leave.Job.OrganisationID);
            if (!policies.TryGetValue(key, out var includeHolidays))
            {
                var source = await LeaveService.ResolveApplicationLegacyTypeAsync(
                    db, new JobLeaveType { LeaveTypeId = leave.LeaveTypeId }, leave.Job.OrganisationID);
                includeHolidays = source?.IncludeHolidays ?? leave.LeaveType.IncludeHolidays;
                policies[key] = includeHolidays;
            }
            result.Add(new()
            {
                LeaveId = leave.LeaveId, LeaveType = leave.LeaveType.Name,
                FromDate = leave.FromDate, ToDate = leave.ToDate, Duration = leave.Duration,
                IncludeHolidays = includeHolidays,
                HasPendingChange = pendingIds.Contains(leave.LeaveId)
            });
        }
        return result;
    }

    public async Task<List<LeaveChangeRequestDto>> GetMyRequestsAsync()
    {
        var user = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync();
        var result = await Project(db.LeaveChangeRequests.AsNoTracking()
            .Where(x => x.Leave.Job.IndividualID == user.IndividualId)).ToListAsync();
        await PopulateActionsAsync(db, result, user);
        return result;
    }

    public async Task<List<LeaveChangeRequestDto>> GetAssignedRequestsAsync()
    {
        var user = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync();
        var pending = db.WfRequestApprovals.AsNoTracking()
            .Where(x => x.ApproverUserId == user.UserId && x.Decision == "Pending" &&
                x.RequestStageInstance.StageStatus == "Pending" &&
                x.RequestStageInstance.WorkflowRequestInstance.WorkflowStatus == "InProgress" &&
                x.RequestStageInstance.WorkflowRequestInstance.CompletedAt == null &&
                x.RequestStageInstance.WorkflowRequestInstance.CurrentRequestStageInstanceId == x.RequestStageInstanceId)
            .Select(x => x.RequestStageInstance.WorkflowRequestInstance.RequestId);
        var result = await Project(db.LeaveChangeRequests.AsNoTracking()
            .Where(x => pending.Contains(x.RequestId) && x.Request.RequestState.IsProcessingState && !x.Request.RequestState.IsFinalState)).ToListAsync();
        await PopulateActionsAsync(db, result, user);
        return result;
    }

    private static IQueryable<LeaveChangeRequestDto> Project(IQueryable<LeaveChangeRequest> query) =>
        query.OrderByDescending(x => x.LeaveChangeRequestId).Select(x => new LeaveChangeRequestDto
        {
            Applicant = (x.Leave.Job.Individual.FirstNameEnglish + " " + (x.Leave.Job.Individual.MiddleNameEnglish ?? "") + " " + (x.Leave.Job.Individual.LastNameEnglish ?? "")).Trim(),
            ChangeKind = x.LeaveChangeRequestTypeId,
            IsProcessing = x.Request.RequestState.IsProcessingState && !x.Request.RequestState.IsFinalState,
            RequestId = x.RequestId, Reference = x.Request.ReferenceNumber ?? "",
            LeaveType = x.Leave.LeaveType.Name, ChangeType = x.LeaveChangeRequestType.TypeName,
            FromDate = x.FromDate, CurrentEndDate = x.Leave.ToDate, RequestedEndDate = x.ToDate,
            Reason = x.Reason, Status = x.Request.RequestState.StateName
        });

    public async Task<ServiceResult> SubmitAsync(LeaveChangeApplicationDto model)
    {
        var user = await access.RequireContextAsync();
        if (string.IsNullOrWhiteSpace(model.Reason) || model.Reason.Trim().Length > 250)
            return ServiceResult.Fail("Enter a reason of 250 characters or fewer.");
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var leave = await LockLeaveAsync(db, model.LeaveId);
            if (leave == null) throw new InvalidOperationException("The leave was not found.");
            await db.Entry(leave).Reference(x => x.Job).LoadAsync();
            if (leave.Job.IndividualID != user.IndividualId)
                throw new UnauthorizedAccessException("You can only change your own leave.");
            RequireApproved(leave);
            if (await db.LeaveChangeRequests.AnyAsync(x => x.LeaveId == leave.LeaveId &&
                (x.Request.RequestState.IsProcessingState || x.Request.RequestStateId == SharedConfig.RequestStates.INCOMPLETE)))
                throw new InvalidOperationException("This leave already has a pending change request.");

            var source = await LeaveService.ResolveApplicationLegacyTypeAsync(db,
                new JobLeaveType { LeaveTypeId = leave.LeaveTypeId }, leave.Job.OrganisationID)
                ?? throw new InvalidOperationException("The leave type could not be resolved.");
            var change = LeaveChangeRules.Calculate(leave.FromDate, leave.ToDate, leave.Duration,
                model.Kind, model.Date, source.IncludeHolidays);
            await ValidateBalanceAndOverlapAsync(db, leave, change.EndDate, change.BalanceAdjustment);

            var log = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.LEAVE_CHANGE_REQUEST_CREATE,
                $"{model.Kind} requested for leave {leave.LeaveId}");
            var request = await requests.CreateAsync(db, new CreateRequestDto
            {
                RequestTypeId = LeaveChangeRequestTypes.ForKind(model.Kind),
                ApplicantBusinessEntityId = user.BusinessEntityId,
                OrganisationBusinessEntityId = leave.Job.OrganisationID,
                OperationLogId = log.OperationLogId, EffectiveDate = model.Date.Date
            });
            db.LeaveChangeRequests.Add(new()
            {
                LeaveId = leave.LeaveId, RequestId = request.RequestId,
                LeaveChangeRequestTypeId = (int)model.Kind, FromDate = leave.FromDate,
                ToDate = change.EndDate, Reason = model.Reason.Trim(), OperationLogId = log.OperationLogId
            });
            await db.SaveChangesAsync();
            await workflow.StartAsync(db, request.RequestId, leave.JobId);
            await transaction.CommitAsync();
            return ServiceResult.Ok("Change request submitted for approval. Your leave dates and balance have not changed.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException)
        {
            await transaction.RollbackAsync();
            if (ex is WorkflowConfigurationException configuration) await configuration.NotifyAsync(factory);
            return ServiceResult.Fail(ex.Message);
        }
    }


    private async Task ApplyApprovedChangeAsync(HrmTeContext db, Leaf leave, LeaveChangeRequest change)
    {
        RequireApproved(leave);
        if (leave.FromDate.Date != change.FromDate.Date)
            throw new InvalidOperationException("The original leave has changed. Submit a new change request.");
        var source = await LeaveService.ResolveApplicationLegacyTypeAsync(db,
            new JobLeaveType { LeaveTypeId = leave.LeaveTypeId }, leave.Job.OrganisationID)
            ?? throw new InvalidOperationException("The leave type could not be resolved.");
        var kind = (LeaveChangeKind)change.LeaveChangeRequestTypeId;
        var target = kind == LeaveChangeKind.EarlyReturn ? change.ToDate.AddDays(1) : change.ToDate;
        var result = LeaveChangeRules.Calculate(leave.FromDate, leave.ToDate, leave.Duration, kind, target, source.IncludeHolidays);
        var balance = await ValidateBalanceAndOverlapAsync(db, leave, result.EndDate, result.BalanceAdjustment);
        var log = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.LEAVE_CHANGE_REQUEST_UPDATE,
            $"Approved {kind} for leave {leave.LeaveId}; balance adjustment {result.BalanceAdjustment}");
        balance.RemainingDays = checked((balance.RemainingDays ?? 0) + result.BalanceAdjustment);
        var otherLastDate = await db.Leaves.AsNoTracking()
            .Where(x => x.JobId == leave.JobId && x.LeaveTypeId == leave.LeaveTypeId && x.LeaveId != leave.LeaveId &&
                x.LeaveStateId != SharedConfig.LeaveStates.CANCELLED && x.LeaveStateId != SharedConfig.LeaveStates.REJECTED)
            .Select(x => (DateTime?)x.ToDate).MaxAsync();
        balance.LastLeaveTakenDate = result.Duration == 0 ? otherLastDate :
            otherLastDate.HasValue && otherLastDate > result.EndDate ? otherLastDate : result.EndDate;
        balance.OperationLogId = log.OperationLogId;
        if (result.Duration == 0)
            leave.LeaveStateId = SharedConfig.LeaveStates.CANCELLED;
        else
            leave.ToDate = result.EndDate;
        leave.Duration = result.Duration;
        leave.OperationLogId = log.OperationLogId;
        change.OperationLogId = log.OperationLogId;
    }

    private static async Task<JobLeaveType> ValidateBalanceAndOverlapAsync(
        HrmTeContext db, Leaf leave, DateTime targetEnd, int balanceAdjustment)
    {
        if (targetEnd > leave.ToDate && await db.Leaves.AnyAsync(x => x.JobId == leave.JobId && x.LeaveId != leave.LeaveId &&
            x.FromDate <= targetEnd && x.ToDate > leave.ToDate &&
            x.LeaveStateId != SharedConfig.LeaveStates.CANCELLED && x.LeaveStateId != SharedConfig.LeaveStates.REJECTED))
            throw new InvalidOperationException("The extension overlaps another leave application.");
        var balanceQuery = db.Database.IsSqlServer()
            ? db.JobLeaveTypes.FromSqlInterpolated($"SELECT * FROM dbo.JobLeaveTypes WITH (UPDLOCK, HOLDLOCK) WHERE JobID = {leave.JobId} AND IsValid = 1")
            : db.JobLeaveTypes.Where(x => x.JobId == leave.JobId && x.IsValid);
        var balances = await balanceQuery.ToListAsync();
        var source = await LeaveService.ResolveApplicationLegacyTypeAsync(db,
            new JobLeaveType { LeaveTypeId = leave.LeaveTypeId }, leave.Job.OrganisationID);
        var candidates = new List<JobLeaveType>();
        foreach (var balance in balances)
        {
            var type = await LeaveService.ResolveApplicationLegacyTypeAsync(db, balance, leave.Job.OrganisationID);
            if (type != null && type.LeaveTypeId == source?.LeaveTypeId) candidates.Add(balance);
        }
        var original = candidates.Where(x => x.OperationLogId == leave.OperationLogId).ToList();
        var exactCopy = candidates.Where(x => x.LeaveTypeId == leave.LeaveTypeId && leave.LeaveTypeId != source?.LeaveTypeId).ToList();
        var selected = original.Count == 1 ? original[0] : exactCopy.Count == 1 ? exactCopy[0] : candidates.Count == 1 ? candidates[0] : null;
        // Duplicate migrated balances must never receive a guessed deduction/refund.
        if (selected == null) throw new InvalidOperationException("The original leave balance could not be uniquely identified. Contact HR to reconcile the duplicate balances.");
        if (balanceAdjustment < 0)
        {
            var contractYearError = await LeaveContractYearBalance.ValidateSpendAsync(db, selected, targetEnd);
            if (contractYearError != null) throw new InvalidOperationException(contractYearError);
        }
        if (selected.RemainingDays == null || selected.RemainingDays.Value + balanceAdjustment < 0)
            throw new InvalidOperationException("There are not enough remaining leave days for this extension.");
        return selected;
    }

    private static Task<Leaf?> LockLeaveAsync(HrmTeContext db, int leaveId) =>
        db.Database.IsSqlServer()
            ? db.Leaves.FromSqlInterpolated($"SELECT * FROM dbo.Leaves WITH (UPDLOCK, HOLDLOCK) WHERE LeaveID = {leaveId}").SingleOrDefaultAsync()
            : db.Leaves.SingleOrDefaultAsync(x => x.LeaveId == leaveId);
    private static void RequireApproved(Leaf leave)
    {
        if (leave.LeaveStateId != SharedConfig.LeaveStates.APPROVED)
            throw new InvalidOperationException("Only approved leave can be extended or shortened.");
    }
}
