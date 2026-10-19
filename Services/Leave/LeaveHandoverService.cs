using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Leave;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Leave;

public class LeaveHandoverService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access, IOperationLogService logs)
{
    public static async Task<List<int>> SupervisorsAsync(HrmTeContext db, int jobId)
    {
        var job = await db.Jobs.AsNoTracking().SingleAsync(x => x.JobId == jobId);
        return await db.OrganisationStructureSupervisors.AsNoTracking()
            .Where(x => x.OrganisationStructureId == job.OrganisationStructureId && x.IsActive && x.IsValid && x.SupervisorStaffId != job.IndividualID &&
                db.Jobs.Any(j => j.IndividualID == x.SupervisorStaffId && j.OrganisationID == job.OrganisationID && j.JobStateId == SharedConfig.JobStates.APPROVED && j.TerminatedDate == null) &&
                db.Users.Any(u => u.BusinessEntityID == x.SupervisorStaffId))
            .Select(x => x.SupervisorStaffId).Distinct().ToListAsync();
    }
    public static async Task<string?> ValidateTasksAsync(HrmTeContext db, List<LeaveHandoverTaskInput> tasks, int organisationId, int applicant, DateTime from)
    {
        if (tasks.Count is < 1 or > 50) return "Assign between 1 and 50 handover tasks.";
        if (tasks.Any(x => x.JobId is not > 0 || string.IsNullOrWhiteSpace(x.Tasks) || x.Tasks.Trim().Length > 250))
            return "Choose an employee and enter task details of up to 250 characters for each handover task.";
        var jobs = tasks.Select(x => x.JobId!.Value).Distinct().ToList();
        var count = await db.Jobs.CountAsync(x => jobs.Contains(x.JobId) && x.OrganisationID == organisationId && x.IndividualID != applicant &&
            x.TerminatedDate == null && x.JobStateId == SharedConfig.JobStates.APPROVED && x.JoinedDate <= from.Date && db.Users.Any(u => u.BusinessEntityID == x.IndividualID));
        return count == jobs.Count ? null : "Handover employees must have an active job and user account in your organisation, and cannot be yourself.";
    }
    public static async Task<bool> ReadyAsync(HrmTeContext db, int leaveId)
    {
        var review = await db.LeaveHandoverReviews.AsNoTracking().SingleOrDefaultAsync(x => x.LeaveId == leaveId);
        if (review == null) return true;
        return review.SupervisorDecision == "Approved" && await db.LeaveWorkHandOvers.AnyAsync(x => x.LeaveId == leaveId && x.IsValid) &&
            !await db.LeaveWorkHandOvers.AnyAsync(x => x.LeaveId == leaveId && x.IsValid && x.Decision != "Approved");
    }
    public static async Task<string> StatusAsync(HrmTeContext db, int leaveId)
    {
        var review = await db.LeaveHandoverReviews.AsNoTracking().SingleOrDefaultAsync(x => x.LeaveId == leaveId);
        if (review == null) return "Not required";
        if (review.SupervisorDecision == "Rejected" || await db.LeaveWorkHandOvers.AnyAsync(x => x.LeaveId == leaveId && x.IsValid && x.Decision == "Rejected")) return "Handover changes required";
        return await ReadyAsync(db, leaveId) ? "Handover approved — ready for HR" :
            await db.LeaveWorkHandOvers.AnyAsync(x => x.LeaveId == leaveId && x.IsValid && x.Decision != "Approved") ? "Awaiting employee task verification" : "Awaiting supervisor approval";
    }
    private static int Person(UserContextDto user) => user.IndividualId > 0 ? user.IndividualId : user.BusinessEntityId;
    public async Task<List<LeaveHandoverInboxItem>> InboxAsync()
    {
        var user = await access.RequireContextAsync(); var person = Person(user);
        await using var db = await factory.CreateDbContextAsync();
        var links = await db.LeaveRequests.AsNoTracking().Include(x => x.Leave).ThenInclude(x => x.Job).ThenInclude(x => x.Individual)
            .Include(x => x.Request)
            .Where(x => x.Leave.LeaveStateId != SharedConfig.LeaveStates.CANCELLED && x.Leave.LeaveStateId != SharedConfig.LeaveStates.REJECTED &&
                db.LeaveHandoverReviews.Any(r => r.LeaveId == x.LeaveId) && (x.Leave.Job.IndividualID == person ||
                db.LeaveWorkHandOvers.Any(t => t.LeaveId == x.LeaveId && t.IsValid && t.Job.IndividualID == person) ||
                db.LeaveHandoverSupervisors.Any(s => s.LeaveId == x.LeaveId && s.IndividualId == person)))
            .OrderByDescending(x => x.LeaveId).ToListAsync();
        var result = new List<LeaveHandoverInboxItem>();
        foreach (var link in links)
        {
            var review = await db.LeaveHandoverReviews.AsNoTracking().SingleAsync(x => x.LeaveId == link.LeaveId);
            var tasks = await db.LeaveWorkHandOvers.AsNoTracking().Include(x => x.Job).ThenInclude(x => x.Individual)
                .Where(x => x.LeaveId == link.LeaveId && x.IsValid).OrderBy(x => x.LeaveWorkHandOverId).ToListAsync();
            var open = link.Request.RequestStateId is not (SharedConfig.RequestStates.APPROVED or SharedConfig.RequestStates.CANCELLED or SharedConfig.RequestStates.REJECTED);
            var canRevise = open && review.SupervisorDecision != "Approved" && link.Leave.Job.IndividualID == person;
            result.Add(new()
            {
                LeaveId = link.LeaveId, Revision = review.Revision, Reference = link.Request.ReferenceNumber ?? $"#{link.RequestId}",
                Applicant = Name(link.Leave.Job.Individual), Dates = $"{link.Leave.FromDate:dd MMM yyyy} – {link.Leave.ToDate:dd MMM yyyy}",
                Status = await StatusAsync(db, link.LeaveId), SupervisorComments = review.Comments, CanRevise = canRevise,
                CanSupervise = open && review.SupervisorDecision == "Pending" && tasks.Count > 0 && tasks.All(x => x.Decision == "Approved") &&
                    await db.LeaveHandoverSupervisors.AnyAsync(x => x.LeaveId == link.LeaveId && x.IndividualId == person),
                Tasks = tasks.Select(x => new LeaveHandoverTaskView(x.LeaveWorkHandOverId, x.JobId, Name(x.Job.Individual), x.TaskDetails ?? "", x.Decision, x.Comments,
                    open && review.SupervisorDecision == "Pending" && x.Decision == "Pending" && x.Job.IndividualID == person)).ToList(),
                Recipients = canRevise ? await db.Jobs.Where(x => x.OrganisationID == link.Leave.Job.OrganisationID && x.IndividualID != person && x.TerminatedDate == null &&
                    x.JobStateId == SharedConfig.JobStates.APPROVED && x.JoinedDate <= link.Leave.FromDate && db.Users.Any(u => u.BusinessEntityID == x.IndividualID))
                    .OrderBy(x => x.Individual.FirstNameEnglish).Select(x => new LeaveHandoverRecipient(x.JobId, x.Individual.FirstNameEnglish + " " + (x.Individual.LastNameEnglish ?? ""))).ToListAsync() : []
            });
        }
        return result;
    }
    private static string Name(Individual person) => string.Join(" ", new[] { person.FirstNameEnglish, person.MiddleNameEnglish, person.LastNameEnglish }.Where(x => !string.IsNullOrWhiteSpace(x)));
    public async Task<ServiceResult> DecideAsync(int leaveId, int revision, int? taskId, bool approve, string? comments)
        => await MutateAsync(leaveId, revision, async (db, review, leave, user) =>
        {
            if (comments?.Length > 250 || !approve && string.IsNullOrWhiteSpace(comments)) throw new InvalidOperationException("Provide a comment of up to 250 characters when returning a handover.");
            var person = Person(user);
            if (review.SupervisorDecision != "Pending") throw new InvalidOperationException("This handover decision has already been recorded.");
            if (taskId.HasValue)
            {
                var task = await db.LeaveWorkHandOvers.Include(x => x.Job).SingleOrDefaultAsync(x => x.LeaveWorkHandOverId == taskId && x.LeaveId == leaveId && x.IsValid);
                if (task == null || task.Job.IndividualID != person) throw new UnauthorizedAccessException("Only the employee assigned to this task may verify it.");
                if (task.Decision != "Pending") throw new InvalidOperationException("This task has already been reviewed.");
                task.Decision = approve ? "Approved" : "Rejected"; task.Comments = comments?.Trim(); task.DecisionAt = DateTime.UtcNow;
            }
            else
            {
                if (!await db.LeaveHandoverSupervisors.AnyAsync(x => x.LeaveId == leaveId && x.IndividualId == person) ||
                    !await db.OrganisationStructureSupervisors.AnyAsync(x => x.OrganisationStructureId == leave.Job.OrganisationStructureId && x.SupervisorStaffId == person && x.IsActive && x.IsValid))
                    throw new UnauthorizedAccessException("Only a configured department supervisor may approve this handover.");
                if (!await db.LeaveWorkHandOvers.AnyAsync(x => x.LeaveId == leaveId && x.IsValid) ||
                    await db.LeaveWorkHandOvers.AnyAsync(x => x.LeaveId == leaveId && x.IsValid && x.Decision != "Approved"))
                    throw new InvalidOperationException("Every assigned employee must verify and approve their tasks first.");
                review.SupervisorDecision = approve ? "Approved" : "Rejected"; review.SupervisorApprovedByIndividualId = person;
                review.Comments = comments?.Trim(); review.DecisionAt = DateTime.UtcNow;
            }
        }, taskId.HasValue ? $"Task {taskId}: {(approve ? "verified and approved" : "returned")}" : $"Supervisor {(approve ? "approved" : "returned")} handover");
    public async Task<ServiceResult> ReviseAsync(int leaveId, int revision, List<LeaveHandoverTaskInput> tasks)
        => await MutateAsync(leaveId, revision, async (db, review, leave, user) =>
        {
            if (leave.Job.IndividualID != Person(user)) throw new UnauthorizedAccessException("Only the leave applicant may revise this handover.");
            if (review.SupervisorDecision == "Approved") throw new InvalidOperationException("The supervisor has already approved this handover.");
            var error = await ValidateTasksAsync(db, tasks, leave.Job.OrganisationID, leave.Job.IndividualID, leave.FromDate);
            if (error != null) throw new InvalidOperationException(error);
            var supervisors = await SupervisorsAsync(db, leave.JobId);
            if (supervisors.Count == 0) throw new InvalidOperationException("No active department supervisor is configured. Contact HR.");
            foreach (var old in await db.LeaveWorkHandOvers.Where(x => x.LeaveId == leaveId && x.IsValid).ToListAsync()) old.IsValid = false;
            foreach (var task in tasks) db.LeaveWorkHandOvers.Add(new() { LeaveId = leaveId, JobId = task.JobId!.Value, TaskDetails = task.Tasks.Trim(), IsValid = true });
            db.LeaveHandoverSupervisors.RemoveRange(await db.LeaveHandoverSupervisors.Where(x => x.LeaveId == leaveId).ToListAsync());
            await db.SaveChangesAsync();
            foreach (var supervisor in supervisors) db.LeaveHandoverSupervisors.Add(new() { LeaveId = leaveId, IndividualId = supervisor });
            review.Revision++; review.SupervisorDecision = "Pending"; review.SupervisorApprovedByIndividualId = null; review.DecisionAt = null; review.Comments = null;
        }, "Applicant revised handover; all employee and supervisor approvals reset");
    private async Task<ServiceResult> MutateAsync(int leaveId, int revision, Func<HrmTeContext, HRM.Models.LeaveTypes.LeaveHandoverReview, Leaf, UserContextDto, Task> mutation, string description)
    {
        var user = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var leave = await db.Leaves.FromSqlInterpolated($"SELECT * FROM dbo.Leaves WITH (UPDLOCK,HOLDLOCK) WHERE LeaveID={leaveId}")
                .Include(x => x.Job).SingleOrDefaultAsync() ?? throw new InvalidOperationException("Leave was not found.");
            var review = await db.LeaveHandoverReviews.SingleOrDefaultAsync(x => x.LeaveId == leaveId) ?? throw new InvalidOperationException("Handover was not found.");
            if (review.Revision != revision) throw new InvalidOperationException("The handover has changed. Refresh before reviewing it.");
            if (leave.LeaveStateId is SharedConfig.LeaveStates.APPROVED or SharedConfig.LeaveStates.REJECTED or SharedConfig.LeaveStates.CANCELLED)
                throw new InvalidOperationException("This leave request is already closed.");
            await mutation(db, review, leave, user);
            var log = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE, $"Leave {leaveId}, handover revision {review.Revision}: {description}");
            review.OperationLogId = log.OperationLogId;
            await db.SaveChangesAsync(); await transaction.CommitAsync();
            return ServiceResult.Ok("Handover updated successfully.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException)
        { await transaction.RollbackAsync(); return ServiceResult.Fail(ex.Message); }
    }
}
