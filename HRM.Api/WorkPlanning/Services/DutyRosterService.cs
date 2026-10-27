using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.WorkPlanning;
using HRM.Models;
using HRM.Models.WorkPlanning;
using HRM.Services;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using HRM.WorkPlanning.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HRM.WorkPlanning.Services;

public sealed partial class DutyRosterService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access,
    IOperationLogService logs, IWorkPlanGenerator planner, IWorkflowApproverResolver approvers,
    IWorkflowRequestContextResolver contexts)
:global::HRM.WorkPlanning.Services.Contracts.DutyRosterServiceContracts,global::HRM.WorkPlanning.Services.Contracts.DutyRosterServiceContracts.IOperations{
    
    private static DateTime Now => DateTime.UtcNow.AddHours(5);

    public async Task<List<DutyPartner>> GetPartnersAsync()
    {
        var user = await access.RequireContextAsync();
        var org = await WorkPlanningAccess.RequireOrganisationAsync(access);
        var manage = await access.HasPermissionAsync("Schedule.Attach");
        await using var db = await factory.CreateDbContextAsync();
        var department = await DepartmentAsync(db);
        return await db.Jobs.AsNoTracking().Where(x => x.OrganisationID == org && x.JobStateId == SharedConfig.JobStates.APPROVED &&
            x.JoinedDate <= Now.Date && x.TerminatedDate == null && (manage || (department.HasValue && x.OrganisationStructureId == department) || x.IndividualID == user.IndividualId) &&
            db.Users.Any(u => u.BusinessEntityID == x.IndividualID && u.IsActive && u.LocalUserStateId == 1 && u.UserOrganisation!.BusinessEntityID == org))
            .OrderBy(x => x.Individual.FirstNameEnglish).Select(x => new DutyPartner(x.JobId, x.IndividualID,
                (x.Individual.FirstNameEnglish + " " + (x.Individual.MiddleNameEnglish ?? "") + " " + x.Individual.LastNameEnglish).Trim())).ToListAsync();
    }

    private async Task<int?> DepartmentAsync(HrmTeContext db)
    {
        var jobId = await access.GetCurrentJobIdAsync();
        return await db.Jobs.Where(x => x.JobId == jobId).Select(x => (int?)x.OrganisationStructureId).SingleOrDefaultAsync();
    }

    public async Task<List<DutyRosterRow>> GetRosterAsync(DateTime date)
    {
        date = date.Date;
        if (date < Now.Date.AddDays(-365) || date > Now.Date.AddDays(90)) throw new InvalidOperationException("Choose a date within the past year or next 90 days.");
        var user = await access.RequireContextAsync();
        var org = await WorkPlanningAccess.RequireOrganisationAsync(access);
        var manage = await access.HasPermissionAsync("Schedule.Attach");
        await using var db = await factory.CreateDbContextAsync();
        var department = await DepartmentAsync(db);
        var jobs = await db.Jobs.AsNoTracking().Where(x => x.OrganisationID == org && x.JobStateId == SharedConfig.JobStates.APPROVED &&
            x.JoinedDate <= date && (x.TerminatedDate == null || x.TerminatedDate >= date) &&
            (manage || (department.HasValue && x.OrganisationStructureId == department) || x.IndividualID == user.IndividualId))
            .Select(x => new { x.JobId, x.IndividualID }).ToListAsync();
        if (date >= Now.Date)
            foreach (var job in jobs) await planner.GenerateOrGetAsync(job.IndividualID, job.JobId, org, DateOnly.FromDateTime(date));
        var jobIds = jobs.Select(x => x.JobId).ToList();
        var pending = await db.DutyChangeRequests.Where(x => x.OrganisationId == org && x.IsOpen)
            .Select(x => new { x.SourceAssignmentId, x.SwapAssignmentId }).ToListAsync();
        var rows = await db.WorkAssignments.AsNoTracking().Where(x => x.IsValid && x.CancelledDate == null && x.SchedulePurpose == "Duty" &&
            x.WorkPlan.IsValid && x.WorkPlan.OrganisationBusinessEntityId == org &&
            (x.WorkAssignmentOwners.Any(o => o.IsValid && o.IsCurrentOwner && !o.RelievedDate.HasValue && jobIds.Contains(o.JobId)) ||
                (!x.WorkAssignmentOwners.Any(o => o.IsValid && o.IsCurrentOwner && !o.RelievedDate.HasValue) && jobIds.Contains(x.WorkPlan.JobId))) &&
            x.StartDateTime < date.AddDays(1) && x.EndDateTime > date)
            .Include(x => x.WorkPlan).ThenInclude(x => x.Job).ThenInclude(x => x.Individual)
            .Include(x => x.WorkPlan).ThenInclude(x => x.Job).ThenInclude(x => x.OrganisationStructure)
            .Include(x => x.WorkAssignmentOwners).ThenInclude(x => x.Individual)
            .Include(x => x.WorkAssignmentOwners).ThenInclude(x => x.Job).ThenInclude(x => x.OrganisationStructure).ToListAsync();
        return rows.OrderBy(x => x.StartDateTime).ThenBy(x => x.Name).Select(x => {
            var owner = x.WorkAssignmentOwners.Where(o => o.IsValid && o.IsCurrentOwner && !o.RelievedDate.HasValue).OrderByDescending(o => o.AssignedDate).FirstOrDefault();
            var available = x.StartDateTime > Now && !x.WorkPlan.IsFinalized &&
                !pending.Any(p => p.SourceAssignmentId == x.WorkAssignmentId || p.SwapAssignmentId == x.WorkAssignmentId) &&
                x.WorkAssignmentOwners.Any(o => o.IsValid && o.IsCurrentOwner && o.IndividualId == x.WorkPlan.IndividualId && !o.RelievedDate.HasValue);
            return new DutyRosterRow(x.WorkAssignmentId,
            owner?.JobId ?? x.WorkPlan.JobId, owner?.IndividualId ?? x.WorkPlan.IndividualId, Name(owner?.Individual ?? x.WorkPlan.Job.Individual),
            (owner?.Job ?? x.WorkPlan.Job).OrganisationStructure?.Name ?? "",
            x.Name, x.StartDateTime, x.EndDateTime, x.RequiresAttendance, x.RequiresCheckOut,
            available && (manage || x.WorkPlan.IndividualId == user.IndividualId)) { CanExchange = available }; }).ToList();
    }

    private static string Name(Individual person) => string.Join(" ", new[] { person.FirstNameEnglish, person.MiddleNameEnglish, person.LastNameEnglish }.Where(x => !string.IsNullOrWhiteSpace(x)));

    private static async Task<WorkAssignment> LockAssignmentAsync(HrmTeContext db, long id)
    {
        var query = db.Database.IsSqlServer() ? db.WorkAssignments.FromSqlInterpolated($"SELECT * FROM dbo.WorkAssignments WITH (UPDLOCK,HOLDLOCK) WHERE WorkAssignmentId={id}") : db.WorkAssignments.Where(x => x.WorkAssignmentId == id);
        return await query.Include(x => x.WorkPlan).ThenInclude(x => x.Job).Include(x => x.WorkAssignmentOwners)
            .Include(x => x.WorkAssignmentSegments).ThenInclude(x => x.WorkTemplateSegment)
            .SingleOrDefaultAsync() ?? throw new InvalidOperationException("The duty assignment was not found.");
    }

    private static void RequireFutureDuty(WorkAssignment a, int org)
    {
        if (!a.IsValid || a.CancelledDate.HasValue || !a.WorkPlan.IsValid || a.WorkPlan.IsFinalized || a.SchedulePurpose != "Duty" || a.WorkPlan.OrganisationBusinessEntityId != org)
            throw new InvalidOperationException("Select an active, unfinalized duty in the current organisation.");
        if (a.StartDateTime <= Now) throw new InvalidOperationException("Only duties that have not started can be transferred or swapped.");
        if (!a.WorkAssignmentOwners.Any(x => x.IsValid && x.IsCurrentOwner && !x.RelievedDate.HasValue && x.JobId == a.WorkPlan.JobId && x.IndividualId == a.WorkPlan.IndividualId))
            throw new InvalidOperationException("Duty ownership has changed. Refresh the roster.");
        if (!a.WorkAssignmentSegments.Any(x => x.IsValid)) throw new InvalidOperationException("This duty has no dated segments. Ask HR to repair its plan.");
        if (a.WorkPlan.Job.JobStateId != SharedConfig.JobStates.APPROVED || a.WorkPlan.Job.JoinedDate > a.StartDateTime ||
            (a.WorkPlan.Job.TerminatedDate.HasValue && a.WorkPlan.Job.TerminatedDate <= a.EndDateTime))
            throw new InvalidOperationException("The duty owner no longer has an active job covering these hours.");
    }

    private static async Task CheckAvailabilityAsync(HrmTeContext db, Job job, DateTime start, DateTime end, IReadOnlyList<long> excluded)
    {
        if (!await HRM.Helpers.OfficialSchedulePrerequisite.CoversAsync(db, job.JobId, job.OrganisationID, start.Date, start.Date))
            throw new InvalidOperationException(HRM.Helpers.OfficialSchedulePrerequisite.Message);
        if (job.JobStateId != SharedConfig.JobStates.APPROVED || job.JoinedDate > start || (job.TerminatedDate.HasValue && job.TerminatedDate <= end))
            throw new InvalidOperationException("The receiving staff member needs an approved active job for the complete duty period.");
        var lastDate = end.AddTicks(-1).Date;
        var leave = await db.Leaves.Where(x => x.Job.OrganisationID == job.OrganisationID && x.Job.IndividualID == job.IndividualID && x.LeaveStateId == SharedConfig.LeaveStates.APPROVED && x.FromDate.Date <= lastDate && x.ToDate.Date >= start.Date)
            .OrderBy(x => x.FromDate).Select(x => new { x.FromDate, x.ToDate, Type = x.LeaveType.Name }).FirstOrDefaultAsync();
        if(leave != null) throw new InvalidOperationException($"The receiving staff member has approved {leave.Type ?? "leave"} from {leave.FromDate:dd MMM yyyy} to {leave.ToDate:dd MMM yyyy}, overlapping this duty ({start:dd MMM yyyy HH:mm}–{end:dd MMM yyyy HH:mm}).");
        var overlap = await db.WorkAssignments.Where(x => x.IsValid && x.CancelledDate == null && x.SchedulePurpose == "Duty" && x.WorkPlan.IsValid && x.WorkPlan.OrganisationBusinessEntityId == job.OrganisationID &&
            !excluded.Contains(x.WorkAssignmentId) && x.StartDateTime < end && x.EndDateTime > start &&
            (x.WorkPlan.IndividualId == job.IndividualID || x.WorkAssignmentOwners.Any(o => o.IsValid && o.IsCurrentOwner && !o.RelievedDate.HasValue && o.IndividualId == job.IndividualID)))
            .OrderBy(x => x.StartDateTime).Select(x => new { x.Name, x.StartDateTime, x.EndDateTime }).FirstOrDefaultAsync();
        if(overlap != null) throw new InvalidOperationException($"The receiving staff member has overlapping duty hours: {overlap.Name}, {overlap.StartDateTime:dd MMM yyyy HH:mm}–{overlap.EndDateTime:dd MMM yyyy HH:mm}. Requested duty: {start:dd MMM yyyy HH:mm}–{end:dd MMM yyyy HH:mm}.");
    }

    private async Task ValidatePairAsync(HrmTeContext db, DutyChangeRequest change, WorkAssignment source, WorkAssignment? swap)
    {
        RequireFutureDuty(source, change.OrganisationId);
        if (source.StartDateTime != change.SourceStart || source.EndDateTime != change.SourceEnd || source.WorkPlan.JobId != change.SourceJobId || Fingerprint(source) != change.SourceFingerprint)
            throw new InvalidOperationException("The source duty changed after this request was created. Cancel and submit a new request.");
        var target = await db.Jobs.SingleAsync(x => x.JobId == change.TargetJobId && x.OrganisationID == change.OrganisationId);
        if (target.IndividualID != change.TargetIndividualId) throw new InvalidOperationException("The target staff job changed.");
        var excluded = new List<long> { source.WorkAssignmentId };
        if (swap != null)
        {
            RequireFutureDuty(swap, change.OrganisationId);
            if (swap.WorkPlan.JobId != target.JobId || swap.StartDateTime != change.SwapStart || swap.EndDateTime != change.SwapEnd || Fingerprint(swap) != change.SwapFingerprint)
                throw new InvalidOperationException("The swap duty changed. Cancel and submit a new request.");
            excluded.Add(swap.WorkAssignmentId);
            await CheckAvailabilityAsync(db, source.WorkPlan.Job, swap.StartDateTime, swap.EndDateTime, excluded);
        }
        await CheckAvailabilityAsync(db, target, source.StartDateTime, source.EndDateTime, excluded);
    }

    public async Task<ServiceResult> SubmitAsync(DutyChangeApplication model)
    {
        var actor = await access.RequireContextAsync();
        var org = await WorkPlanningAccess.RequireOrganisationAsync(access);
        if (string.IsNullOrWhiteSpace(model.Reason) || model.Reason.Trim().Length > 500) return ServiceResult.Failed("Enter a reason of up to 500 characters.");
        await using var db = await factory.CreateDbContextAsync();
        try
        {
            var preview = await db.WorkAssignments.AsNoTracking().Include(x => x.WorkPlan).ThenInclude(x => x.Job)
                .SingleOrDefaultAsync(x => x.WorkAssignmentId == model.SourceAssignmentId && x.WorkPlan.OrganisationBusinessEntityId == org)
                ?? throw new UnauthorizedAccessException("Select a duty in the current organisation.");
            var receivingJob = await db.Jobs.AsNoTracking().SingleOrDefaultAsync(x => x.JobId == model.TargetJobId && x.OrganisationID == org)
                ?? throw new UnauthorizedAccessException("Select receiving staff in the current organisation.");
            var manage = await access.HasPermissionAsync("Schedule.Attach");
            if (!manage && (preview.WorkPlan.IndividualId != actor.IndividualId || preview.WorkPlan.Job.OrganisationStructureId <= 0 ||
                receivingJob.OrganisationStructureId != preview.WorkPlan.Job.OrganisationStructureId))
                throw new UnauthorizedAccessException("Choose your own duty and receiving staff in your department.");
            await PreparePlansAsync(receivingJob.JobId, receivingJob.IndividualID, org, preview.StartDateTime, preview.EndDateTime);
            if (model.SwapAssignmentId.HasValue)
            {
                var exchange = await db.WorkAssignments.AsNoTracking().SingleOrDefaultAsync(x => x.WorkAssignmentId == model.SwapAssignmentId &&
                    x.WorkPlan.JobId == receivingJob.JobId && x.WorkPlan.OrganisationBusinessEntityId == org)
                    ?? throw new InvalidOperationException("Select an exchange duty belonging to the receiving staff member.");
                await PreparePlansAsync(preview.WorkPlan.JobId, preview.WorkPlan.IndividualId, org, exchange.StartDateTime, exchange.EndDateTime);
            }
        }
        catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException) { return ServiceResult.Failed(ex.Message); }
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var ids = new[] { model.SourceAssignmentId, model.SwapAssignmentId ?? model.SourceAssignmentId }.Distinct().Order().ToList();
            var assignments = new Dictionary<long, WorkAssignment>();
            foreach (var id in ids) assignments[id] = await LockAssignmentAsync(db, id);
            var source = assignments[model.SourceAssignmentId];
            RequireFutureDuty(source, org);
            if (source.WorkPlan.IndividualId != actor.IndividualId && !await access.HasPermissionAsync("Schedule.Attach")) throw new UnauthorizedAccessException("You can only propose changes to your own duties.");
            var target = await db.Jobs.SingleOrDefaultAsync(x => x.JobId == model.TargetJobId && x.OrganisationID == org) ?? throw new InvalidOperationException("Select staff in this organisation.");
            if (target.IndividualID == source.WorkPlan.IndividualId) throw new InvalidOperationException("Select a different staff member.");
            if (!await access.HasPermissionAsync("Schedule.Attach") && target.OrganisationStructureId != source.WorkPlan.Job.OrganisationStructureId)
                throw new UnauthorizedAccessException("Select staff in your department, or ask HR to arrange a cross-department transfer.");
            if (!await db.Users.AnyAsync(x => x.BusinessEntityID == target.IndividualID && x.IsActive && x.LocalUserStateId == 1 && x.UserOrganisation!.BusinessEntityID == org))
                throw new InvalidOperationException("The receiving staff member needs an active account to accept this request.");
            if (!await db.Users.AnyAsync(x => x.BusinessEntityID == source.WorkPlan.IndividualId && x.IsActive && x.LocalUserStateId == 1 && x.UserOrganisation!.BusinessEntityID == org))
                throw new InvalidOperationException("The source staff member needs an active account to accept this request.");
            if (await db.DutyChangeRequests.AnyAsync(x => x.IsOpen && (ids.Contains(x.SourceAssignmentId) || (x.SwapAssignmentId.HasValue && ids.Contains(x.SwapAssignmentId.Value)))))
                throw new InvalidOperationException("One of these duties already has a pending transfer or swap.");
            var swap = model.SwapAssignmentId.HasValue ? assignments[model.SwapAssignmentId.Value] : null;
            if (swap == source) throw new InvalidOperationException("Select a different duty for the swap.");
            var change = new DutyChangeRequest { OrganisationId = org, Kind = swap == null ? "Transfer" : "Swap", SourceAssignmentId = source.WorkAssignmentId,
                SwapAssignmentId = swap?.WorkAssignmentId, SourceJobId = source.WorkPlan.JobId, TargetJobId = target.JobId, SourceIndividualId = source.WorkPlan.IndividualId,
                TargetIndividualId = target.IndividualID, SourceStart = source.StartDateTime, SourceEnd = source.EndDateTime, SwapStart = swap?.StartDateTime, SwapEnd = swap?.EndDateTime,
                Reason = model.Reason.Trim(), SourceFingerprint = Fingerprint(source), SwapFingerprint = swap == null ? null : Fingerprint(swap),
                CreatedByUserId = actor.UserId, CreatedAtUtc = DateTime.UtcNow };
            await ValidatePairAsync(db, change, source, swap);
            var type = await db.RequestTypes.SingleOrDefaultAsync(x => x.TypeName == (swap == null ? TransferType : SwapType)) ?? throw new InvalidOperationException("Duty Request Types are not installed.");
            await RequestTransitionPolicy.RequireAllowedAsync(db, type.RequestTypeId, 1, 10);
            var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.WORK_ASSIGNMENT_CREATE, $"Duty {change.Kind} request: job {change.SourceJobId} to {change.TargetJobId}; {change.Reason}");
            var request = new Request { RequestTypeId = type.RequestTypeId, RequestStateId = 10, ApplicationDate = DateTime.UtcNow,
                ApplicantBusinessEntityId = change.SourceIndividualId, OrganisationBusinessEntityID = org, RequestEffectiveDate = source.StartDateTime,
                LastStateChangedDate = DateTime.UtcNow, LastStateChangedByUserId = actor.UserId, OperationLogId = audit.OperationLogId };
            request.StateChangeRemarks = change.Reason;
            db.Requests.Add(request); await db.SaveChangesAsync(); request.ReferenceNumber = $"DUTY-{request.RequestId}";
            change.Request = request; db.DutyChangeRequests.Add(change); await db.SaveChangesAsync();
            var definition = await DefinitionAsync(db, change);
            db.WfRequestInstances.Add(new HRM.Models.Workflow.WfRequestInstance { RequestId = request.RequestId,
                WorkflowDefinitionId = definition.WorkflowDefinitionId, WorkflowStatus = "AwaitingAcceptance", StartedAt = DateTime.UtcNow });
            await NotifyAsync(db, change, [change.SourceIndividualId, change.TargetIndividualId], "DutyAcceptance", 10, 10);
            if(actor.IndividualId != change.SourceIndividualId) await NotifyAsync(db, change, [], "DutySubmitted", 10, 10);
            await db.SaveChangesAsync(); await tx.CommitAsync();
            return ServiceResult.Ok($"{request.ReferenceNumber} needs acceptance from both staff members before supervisor approval.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException or DbUpdateException)
        { await tx.RollbackAsync(); return ServiceResult.Failed(ex is DbUpdateException ? "The roster changed while saving. Refresh and try again." : ex.Message); }
    }

    private async Task PreparePlansAsync(int job, int person, int org, DateTime start, DateTime end)
    {
        // Materialize receiving recurring duties as well as manual assignments before checking conflicts.
        for (var day = start.Date.AddDays(-1); day <= end.AddTicks(-1).Date; day = day.AddDays(1))
            if (day >= Now.Date) await planner.GenerateOrGetAsync(person, job, org, DateOnly.FromDateTime(day));
    }

    private static string Fingerprint(WorkAssignment a) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(
        System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new { a.WorkTemplateId, a.Name, a.StartDateTime, a.EndDateTime, a.RequiresAttendance,
            a.RequiresCheckOut, a.GraceMinutes, a.LocationName, a.Latitude, a.Longitude, a.AllowedRadiusMeters,
            Segments = a.WorkAssignmentSegments.Where(x => x.IsValid).OrderBy(x => x.WorkAssignmentSegmentId).Select(x => new {
                x.WorkTemplateSegmentId, x.WorkSegmentTypeId, x.ExpectedInOutModeId, x.StartDateTime, x.EndDateTime, x.SequenceNumber,
                x.IsMandatory, x.RequiresAttendance, x.GraceBeforeMinutes, x.GraceAfterMinutes, x.RequiresLocationValidation, x.RequiresDeviceValidation,
                x.LocationName, x.Latitude, x.Longitude, x.AllowedRadiusMeters }).ToArray() })));
}
