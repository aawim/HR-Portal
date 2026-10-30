using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Leave;
using HRM.DTOs.UserContext;
using HRM.Helpers;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Leave;

public partial class LeaveChangeService
{
    public async Task<List<MyLeaveApplicationDto>> GetMyApplicationsAsync()
    {
        var user = await access.RequireContextAsync();
        var person = user.IndividualId > 0 ? user.IndividualId : user.BusinessEntityId;
        await using var db = await factory.CreateDbContextAsync();
        return await MyApplicationsQuery(db, person).OrderByDescending(x => x.RequestId).ToListAsync();
    }

    public async Task<MyLeaveApplicationDto> GetMyApplicationDetailsAsync(int requestId)
    {
        var user = await access.RequireContextAsync();
        var person = user.IndividualId > 0 ? user.IndividualId : user.BusinessEntityId;
        await using var db = await factory.CreateDbContextAsync();
        return await MyApplicationsQuery(db, person).SingleOrDefaultAsync(x => x.RequestId == requestId)
            ?? throw new UnauthorizedAccessException("This leave application is not available to you.");
    }

    private static IQueryable<MyLeaveApplicationDto> MyApplicationsQuery(HrmTeContext db, int person)
        => db.LeaveRequests.AsNoTracking().Where(x => person > 0 && x.Leave.Job.IndividualID == person)
            .Select(x => new MyLeaveApplicationDto
            {
                RequestId = x.RequestId, LeaveId = x.LeaveId, HasHandover = db.LeaveHandoverReviews.Any(r => r.LeaveId == x.LeaveId), Reference = x.Request.ReferenceNumber ?? "#" + x.RequestId,
                LeaveType = x.Leave.LeaveType.Name, FromDate = x.Leave.FromDate, ToDate = x.Leave.ToDate,
                Duration = x.Leave.Duration, Status = x.Request.RequestState.StateName,
                Reason = x.Leave.LeaveReason != null ? x.Leave.LeaveReason.LeaveTypeReasonType.ReasonTypeName : "",
                Explanation = x.Leave.LeaveReason != null ? x.Leave.LeaveReason.Reason : "",
                Address = x.Leave.LeaveSpendingLocation != null ? x.Leave.LeaveSpendingLocation.Address : "",
                ContactNumber = x.Leave.LeaveSpendingLocation != null ? x.Leave.LeaveSpendingLocation.ContactNumber : "",
                ReturnReason = x.Request.RequestStateId == SharedConfig.RequestStates.INCOMPLETE ? x.Request.StateChangeRemarks : null,
                CanEdit = x.Request.RequestStateId == SharedConfig.RequestStates.INCOMPLETE &&
                    x.Leave.LeaveStateId == SharedConfig.LeaveStates.INCOMPLETE &&
                    db.WfRequestInstances.Any(w => w.RequestId == x.RequestId && w.WorkflowStatus == "Returned" && w.CompletedAt == null)
            });


    public async Task<ReturnedLeaveApplicationDto> GetReturnedApplicationAsync(int requestId)
    {
        var user = await access.RequireContextAsync();
        var person = user.IndividualId > 0 ? user.IndividualId : user.BusinessEntityId;
        await using var db = await factory.CreateDbContextAsync();
        var link = await db.LeaveRequests.AsNoTracking()
            .Include(x => x.Request).Include(x => x.Leave).ThenInclude(x => x.Job)
            .Include(x => x.Leave).ThenInclude(x => x.LeaveType)
            .Include(x => x.Leave).ThenInclude(x => x.LeaveReason).ThenInclude(x => x!.LeaveTypeReasonType)
            .Include(x => x.Leave).ThenInclude(x => x.LeaveSpendingLocation)
            .SingleOrDefaultAsync(x => x.RequestId == requestId && person > 0 && x.Leave.Job.IndividualID == person)
            ?? throw new UnauthorizedAccessException("This leave application is not available to you.");
        var leave = link.Leave;
        if (link.Request.RequestStateId != SharedConfig.RequestStates.INCOMPLETE ||
            leave.LeaveStateId != SharedConfig.LeaveStates.INCOMPLETE ||
            !await db.WfRequestInstances.AnyAsync(x => x.RequestId == requestId && x.WorkflowStatus == "Returned" && x.CompletedAt == null))
            throw new InvalidOperationException("Only an application returned as Incomplete can be edited.");
        var source = await LeaveService.ResolveApplicationTypeAsync(db,
            new JobLeaveType { LeaveTypeId = leave.LeaveTypeId }, leave.Job.OrganisationID, leave.FromDate)
            ?? throw new InvalidOperationException("The leave type could not be resolved.");
        var reasons = await db.LeaveTypeReasonTypes.AsNoTracking().Where(x => x.LeaveTypeId == source.LeaveTypeId)
            .OrderBy(x => x.ReasonTypeName).Select(x => new LeaveReasonDto { Id = x.LeaveTypeReasonTypeId, Name = x.ReasonTypeName }).ToListAsync();
        var reasonId = leave.LeaveReason?.LeaveTypeReasonTypeId ?? 0;
        if (!reasons.Any(x => x.Id == reasonId))
        {
            // Old applications can reference a global type's reason. Preselect its unique
            // canonical equivalent for editing without rewriting the historical record.
            var equivalent = reasons.Where(x => string.Equals(x.Name.Trim(), leave.LeaveReason?.LeaveTypeReasonType?.ReasonTypeName?.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
            reasonId = equivalent.Count == 1 ? equivalent[0].Id : 0;
        }
        return new()
        {
            RequestId = requestId, ReturnedAt = link.Request.LastStateChangedDate,
            Reference = link.Request.ReferenceNumber ?? "#" + requestId, LeaveType = leave.LeaveType.Name,
            ReturnReason = link.Request.StateChangeRemarks ?? "No return reason was recorded. Contact the reviewer for details.",
            FromDate = leave.FromDate, ToDate = leave.ToDate, IncludeHolidays = source.IncludeHolidays,
            ReasonId = reasonId, Explanation = leave.LeaveReason?.Reason ?? "",
            CountryId = leave.LeaveSpendingLocation?.CountryId, IslandId = leave.LeaveSpendingLocation?.IslandId,
            Address = leave.LeaveSpendingLocation?.Address ?? "", ContactNumber = leave.LeaveSpendingLocation?.ContactNumber ?? "",
            Reasons = reasons,
            HandoverTasks = await db.LeaveWorkHandOvers.AsNoTracking().Where(x => x.LeaveId == leave.LeaveId && x.IsValid)
                .OrderBy(x => x.LeaveWorkHandOverId).Select(x => new LeaveHandoverTaskInput { JobId = x.JobId, Tasks = x.TaskDetails ?? "" }).ToListAsync(),
            Recipients = await db.Jobs.AsNoTracking().Where(x => x.OrganisationID == leave.Job.OrganisationID && x.IndividualID != person &&
                x.TerminatedDate == null && x.JobStateId == SharedConfig.JobStates.APPROVED && db.Users.Any(u => u.BusinessEntityID == x.IndividualID))
                .OrderBy(x => x.Individual.FirstNameEnglish).Select(x => new LeaveHandoverRecipient(x.JobId,
                    x.Individual.FirstNameEnglish + " " + (x.Individual.LastNameEnglish ?? ""))).ToListAsync()
        };
    }

    public Task<ServiceResult> ResubmitApplicationAsync(ReturnedLeaveApplicationDto model)
        => ExecuteRequestActionAsync(model.RequestId, new RequestActionSubmissionDto
        {
            FromStateId = SharedConfig.RequestStates.INCOMPLETE,
            ToStateId = SharedConfig.RequestStates.PENDING_VERIFICATION,
            Comments = "Applicant corrected and resubmitted the leave application."
        }, true, model);

    private async Task ApplyReturnedApplicationEditsAsync(HrmTeContext db, Request request, Leaf leave,
        ReturnedLeaveApplicationDto model, UserContextDto user)
    {
        var person = user.IndividualId > 0 ? user.IndividualId : user.BusinessEntityId;
        if (person <= 0 || leave.Job.IndividualID != person)
            throw new UnauthorizedAccessException("Only the applicant may edit and resubmit this leave.");
        if (request.RequestId != model.RequestId || request.RequestStateId != SharedConfig.RequestStates.INCOMPLETE ||
            leave.LeaveStateId != SharedConfig.LeaveStates.INCOMPLETE)
            throw new InvalidOperationException("This application is no longer Incomplete. Refresh before editing.");
        if (!await db.WfRequestInstances.AnyAsync(x => x.RequestId == request.RequestId && x.WorkflowStatus == "Returned" && x.CompletedAt == null))
            throw new InvalidOperationException("This application is no longer awaiting correction.");
        if (request.LastStateChangedDate != model.ReturnedAt)
            throw new InvalidOperationException("The application has changed since you opened it. Reload the correction form.");
        if (model.FromDate == default || model.ToDate == default || model.ToDate.Date < model.FromDate.Date ||
            model.ToDate.Date == DateTime.MaxValue.Date)
            throw new InvalidOperationException("Select a valid start and end date.");
        if ((model.Explanation?.Length ?? 0) > 250 || (model.Address?.Length ?? 0) > 250 || (model.ContactNumber?.Length ?? 0) > 250)
            throw new InvalidOperationException("Explanation, address and contact number must each be 250 characters or fewer.");
        var source = await LeaveService.ResolveApplicationTypeAsync(db,
            new JobLeaveType { LeaveTypeId = leave.LeaveTypeId }, leave.Job.OrganisationID, leave.FromDate)
            ?? throw new InvalidOperationException("The leave type could not be resolved.");
        if (!await db.LeaveTypeReasonTypes.AnyAsync(x => x.LeaveTypeReasonTypeId == model.ReasonId && x.LeaveTypeId == source.LeaveTypeId))
            throw new InvalidOperationException("Select a valid reason for this leave type.");
        if (await db.Leaves.AnyAsync(x => x.JobId == leave.JobId && x.LeaveId != leave.LeaveId &&
            x.FromDate <= model.ToDate.Date && x.ToDate >= model.FromDate.Date &&
            x.LeaveStateId != SharedConfig.LeaveStates.REJECTED && x.LeaveStateId != SharedConfig.LeaveStates.CANCELLED))
            throw new InvalidOperationException("The revised dates overlap another leave application.");

        var duration = LeaveApplicationDates.Count(model.FromDate, model.ToDate, source.IncludeHolidays);
        if (duration <= 0) throw new InvalidOperationException("The revised dates must contain at least one leave day.");
        var adjustment = leave.Duration - duration;
        var balance = await ValidateBalanceAndOverlapAsync(db, leave, model.ToDate.Date, adjustment);
        await LeaveSubtypeRules.RequireChangeAsync(db,leave,model.FromDate.Date,model.ToDate.Date,duration);
        var handoverRule = await LeaveHandoverRuleResolver.ResolveAsync(db,leave.JobId,source.LeaveTypeId,model.FromDate);
        var needsHandover = handoverRule.Requires(duration);
        var supervisors = new List<int>();
        if (needsHandover)
        {
            var error = await LeaveHandoverService.ValidateTasksAsync(db, model.HandoverTasks, leave.Job.OrganisationID, person, model.FromDate);
            if (error != null) throw new InvalidOperationException(error);
            supervisors = await LeaveHandoverService.SupervisorsAsync(db, leave.JobId);
            if (supervisors.Count == 0) throw new InvalidOperationException("No eligible team or department supervisor is configured for the work handover.");
        }
        if (model.CountryId.HasValue && !await db.Countries.AnyAsync(x => x.CountryId == model.CountryId))
            throw new InvalidOperationException("Select a valid country.");
        if (!model.CountryId.HasValue && (!string.IsNullOrWhiteSpace(model.Address) || !string.IsNullOrWhiteSpace(model.ContactNumber)))
            throw new InvalidOperationException("Select a country for the leave address and contact details.");
        var location = await db.LeaveSpendingLocations.SingleOrDefaultAsync(x => x.LeaveId == leave.LeaveId);
        // An existing island is retained only while its country remains unchanged.
        var islandId = location != null && location.CountryId == model.CountryId ? location.IslandId : null;
        var reason = await db.LeaveReasons.SingleOrDefaultAsync(x => x.LeaveId == leave.LeaveId);
        var review = await db.LeaveHandoverReviews.SingleOrDefaultAsync(x => x.LeaveId == leave.LeaveId);
        var log = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE,
            $"Applicant corrected leave {leave.LeaveId}: {leave.FromDate:yyyy-MM-dd}–{leave.ToDate:yyyy-MM-dd} ({leave.Duration} days) to " +
            $"{model.FromDate:yyyy-MM-dd}–{model.ToDate:yyyy-MM-dd} ({duration} days). Reserved balance adjustment {adjustment}. " +
            $"Return reason: {request.StateChangeRemarks}. Previous handover review: {System.Text.Json.JsonSerializer.Serialize(review)}");
        balance.RemainingDays = checked(balance.RemainingDays!.Value + adjustment);
        balance.OperationLogId = log.OperationLogId;
        leave.HandoverRuleSnapshot = handoverRule.Snapshot(duration);
        leave.FromDate = model.FromDate.Date; leave.ToDate = model.ToDate.Date; leave.Duration = duration;
        leave.OperationLogId = log.OperationLogId;
        var otherLast = await db.Leaves.AsNoTracking().Where(x => x.JobId == leave.JobId && x.LeaveTypeId == leave.LeaveTypeId &&
            x.LeaveId != leave.LeaveId && x.LeaveStateId != SharedConfig.LeaveStates.REJECTED && x.LeaveStateId != SharedConfig.LeaveStates.CANCELLED)
            .Select(x => (DateTime?)x.ToDate).MaxAsync();
        balance.LastLeaveTakenDate = otherLast.HasValue && otherLast > leave.ToDate ? otherLast : leave.ToDate;
        request.RequestEffectiveDate = leave.FromDate; request.OperationLogId = log.OperationLogId;
        if (reason == null) { reason = new LeaveReason { LeaveId = leave.LeaveId }; db.LeaveReasons.Add(reason); }
        reason.LeaveTypeReasonTypeId = model.ReasonId; reason.Reason = model.Explanation?.Trim() ?? "";
        if (model.CountryId.HasValue)
        {
            if (location == null) { location = new LeaveSpendingLocation { LeaveId = leave.LeaveId }; db.LeaveSpendingLocations.Add(location); }
            location.CountryId = model.CountryId.Value; location.IslandId = islandId;
            location.Address = model.Address?.Trim() ?? ""; location.ContactNumber = model.ContactNumber?.Trim();
            location.OperationLogId = log.OperationLogId;
        }
        else if (location != null) db.LeaveSpendingLocations.Remove(location);

        foreach (var task in await db.LeaveWorkHandOvers.Where(x => x.LeaveId == leave.LeaveId && x.IsValid).ToListAsync())
            task.IsValid = false;
        db.LeaveHandoverSupervisors.RemoveRange(await db.LeaveHandoverSupervisors.Where(x => x.LeaveId == leave.LeaveId).ToListAsync());
        if (needsHandover)
        {
            if (review == null) { review = new() { LeaveId = leave.LeaveId }; db.LeaveHandoverReviews.Add(review); }
            else review.Revision++;
            review.SupervisorDecision = "Pending"; review.SupervisorApprovedByIndividualId = null;
            review.Comments = null; review.DecisionAt = null; review.OperationLogId = log.OperationLogId;
        }
        else if (review != null) db.LeaveHandoverReviews.Remove(review);
        await db.SaveChangesAsync();
        if (needsHandover)
        {
            foreach (var supervisor in supervisors) db.LeaveHandoverSupervisors.Add(new() { LeaveId = leave.LeaveId, IndividualId = supervisor });
            foreach (var task in model.HandoverTasks) db.LeaveWorkHandOvers.Add(new()
                { LeaveId = leave.LeaveId, JobId = task.JobId!.Value, TaskDetails = task.Tasks.Trim(), IsValid = true });
        }
    }
}
