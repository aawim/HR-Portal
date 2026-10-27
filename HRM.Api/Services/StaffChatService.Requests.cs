using HRM.Models;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;




public sealed partial class StaffChatService
{
    // This policy is independent of conversation-manager and HR/admin permissions.
    private static async Task<bool> CanOpenRequestAsync(HrmTeContext db, Request request, int userId, int org)
    {
        if (request.OrganisationBusinessEntityID != org) return false;
        var user = await Eligible(db, org).AsNoTracking().SingleOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return false;
        if (request.ApplicantBusinessEntityId == user.BusinessEntityID) return true;
        var job = await db.Jobs.AsNoTracking().Where(j => j.OrganisationID == org && j.IndividualID == request.ApplicantBusinessEntityId &&
            j.JobStateId == 4 && j.JoinedDate <= Today && (!j.TerminatedDate.HasValue || j.TerminatedDate > Today))
            .OrderByDescending(j => j.JoinedDate).ThenByDescending(j => j.JobId).Select(j => (int?)j.JobId).FirstOrDefaultAsync();
        return job.HasValue && (await Leave.LeaveHandoverService.SupervisorsAsync(db, job.Value)).Contains(user.BusinessEntityID);
    }
    public async Task<ChatRequestDetails> GetRequestDetailsAsync(long conversationId, int requestId)
    {
        await using var db = await factory.CreateDbContextAsync(); var actor = await ScopeAsync(db);
        if (!await Owned(db, actor.User, actor.Org).AnyAsync(c => c.Id == conversationId) ||
            !await VisibleMessages(db, conversationId, actor.User).AnyAsync(m => m.RequestId == requestId))
            throw new UnauthorizedAccessException("This request is not available from your conversation.");
        var request = await db.Requests.AsNoTracking().Include(r => r.RequestType).Include(r => r.RequestState)
            .SingleOrDefaultAsync(r => r.RequestId == requestId && r.OrganisationBusinessEntityID == actor.Org);
        if (request == null || !await CanOpenRequestAsync(db, request, actor.User, actor.Org))
            throw new UnauthorizedAccessException("Only the requesting staff member and their current supervisor can open request details from chat.");
        var applicant = await db.Individuals.AsNoTracking().Where(i => i.BusinessEntityId == request.ApplicantBusinessEntityId)
            .Select(i => (i.FirstNameEnglish ?? "") + " " + (i.LastNameEnglish ?? "")).SingleOrDefaultAsync() ?? "Staff member";
        var details = new List<ChatRequestDetail>();
        var leave = await db.LeaveRequests.AsNoTracking().Where(l => l.RequestId == requestId && l.Leave.Job.OrganisationID == actor.Org)
            .Select(l => new { l.Leave.FromDate, l.Leave.ToDate, l.Leave.Duration, l.Leave.LeaveType.Name }).SingleOrDefaultAsync();
        if (leave != null)
        {
            details.Add(new("Leave type", leave.Name));
            details.Add(new("From", leave.FromDate.ToString("dd MMM yyyy")));
            details.Add(new("To", leave.ToDate.ToString("dd MMM yyyy")));
            details.Add(new("Days", leave.Duration.ToString()));
        }
        var ot = await db.OvertimeRequests.AsNoTracking().SingleOrDefaultAsync(r => r.RequestId == requestId && r.OrganisationId == actor.Org);
        if (ot != null)
        {
            details.Add(new("Work date", ot.WorkDate.ToString("dd MMM yyyy")));
            details.Add(new("Hours", (ot.Minutes / 60m).ToString("0.##")));
            details.Add(new("Work performed", ot.Reason));
        }
        var planned = await db.OvertimeWorkRequests.AsNoTracking().Include(r => r.Tasks).SingleOrDefaultAsync(r => r.RequestId == requestId && r.OrganisationId == actor.Org);
        if (planned != null)
        {
            details.Add(new("From", planned.FromDate.ToString("dd MMM yyyy")));
            details.Add(new("To", planned.ToDate.ToString("dd MMM yyyy")));
            details.Add(new("Purpose", planned.Reason));
            foreach (var task in planned.Tasks.OrderBy(t => t.Id)) details.Add(new("Task", task.Description));
        }
        return new(request.ReferenceNumber ?? $"#{requestId}", request.RequestType.TypeName,
            (request.RequestState.IsProcessingState && !request.RequestState.IsFinalState ? "Processing · " : "") + request.RequestState.StateName.Trim(),
            applicant.Trim(), request.ApplicationDate, request.RequestEffectiveDate, details);
    }
}
