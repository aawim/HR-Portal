using HRM.Communications.Contracts;
using HRM.DTOs.Communications;
using HRM.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HRM.Services.Communications;

public sealed partial class CommunicationHubService
{
    private WorkflowRequestProcessing Processing => new(approvers, workflowContexts);

    public async Task<List<CommunicationApprovalReviewDto>> LoadApprovalsAsync(int requestId = 0)
    {
        var actor = await access.RequireContextAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org)) throw new UnauthorizedAccessException("An authorised organisation context is required.");
        await using var db = await factory.CreateDbContextAsync();
        await CommunicationSchemaBootstrapper.EnsureSchemaAsync(db);
        var rows = await (from m in db.CommunicationMessages.AsNoTracking()
            join r in db.Requests.Include(x => x.RequestState) on m.ApprovalRequestId equals r.RequestId
            where (requestId <= 0 || r.RequestId == requestId) && m.OrganisationBusinessEntityId == org && r.OrganisationBusinessEntityID == org && r.RequestType.TypeName == "Communication Announcement" &&
                (r.ApplicantBusinessEntityId == actor.IndividualId || db.WfRequestApprovals.Any(a => a.ApproverUserId == actor.UserId && a.RequestStageInstance.WorkflowRequestInstance.RequestId == r.RequestId))
            orderby m.CreatedAtUtc descending
            select new { Message = m, Request = r }).Take(50).ToListAsync();
        var result = new List<CommunicationApprovalReviewDto>();
        foreach (var row in rows)
        {
            if (!row.Message.ApprovalJobId.HasValue || !row.Request.ApplicantBusinessEntityId.HasValue) continue;
            var subject = new WorkflowRequestSubject(row.Request, row.Message.ApprovalJobId.Value, row.Request.ApplicantBusinessEntityId.Value, org);
            var instance = await WorkflowRequestProcessing.Instance(db, row.Request.RequestId);
            var open = row.Message.ApprovalStatus == "Pending" && instance?.WorkflowStatus == "InProgress" && instance.CompletedAt == null && !row.Request.RequestState.IsFinalState;
            result.Add(new(row.Message.CommunicationMessageId, row.Request.RequestId, row.Request.RequestStateId, row.Message.Subject, row.Message.RenderedBody,
                row.Message.DestinationDisplayName ?? "Destination snapshot unavailable", row.Message.ApprovalStatus, instance?.CurrentStage?.WorkflowStage.StageName ?? "",
                open && await Processing.CanApprove(db, subject, instance, actor.UserId, actor.IndividualId), open && row.Request.ApplicantBusinessEntityId == actor.IndividualId));
        }
        return result;
    }

    public async Task ActOnAnnouncementAsync(CommunicationApprovalActionDto model)
    {
        var actor = await access.RequireContextAsync();
        var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org)) throw new UnauthorizedAccessException("An authorised organisation context is required.");
        await using var db = await factory.CreateDbContextAsync();
        await CommunicationSchemaBootstrapper.EnsureSchemaAsync(db);
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var message = await db.CommunicationMessages.SingleOrDefaultAsync(x => x.CommunicationMessageId == model.Id && x.OrganisationBusinessEntityId == org)
            ?? throw new InvalidOperationException("Announcement is not available.");
        if (message.ApprovalStatus != "Pending" || !message.ApprovalRequestId.HasValue || !message.ApprovalJobId.HasValue)
            throw new InvalidOperationException("Announcement is no longer awaiting a workflow decision.");
        var request = await db.Requests.Include(x => x.RequestState).SingleOrDefaultAsync(x => x.RequestId == message.ApprovalRequestId && x.OrganisationBusinessEntityID == org && x.RequestType.TypeName == "Communication Announcement")
            ?? throw new InvalidOperationException("Approval request is unavailable.");
        if (!request.ApplicantBusinessEntityId.HasValue) throw new InvalidOperationException("The request has no applicant.");
        var subject = new WorkflowRequestSubject(request, message.ApprovalJobId.Value, request.ApplicantBusinessEntityId.Value, org);
        await Processing.ExecuteAsync(db, subject, model.ExpectedState, model.Action, model.Comment, actor.UserId, actor.IndividualId,
            () => ValidateAnnouncementReleaseAsync(db, message), async () =>
            {
                await ValidateAnnouncementReleaseAsync(db, message);
                if (await db.CommunicationDeliveries.AnyAsync(x => x.CommunicationMessageId == message.CommunicationMessageId))
                    throw new InvalidOperationException("A delivery already exists for this announcement.");
                var source = await db.CommunicationOutbox.SingleAsync(x => x.CommunicationOutboxId == message.CommunicationOutboxId && x.OrganisationBusinessEntityId == org);
                message.ApprovalStatus = "Approved";
                db.CommunicationDeliveries.Add(new CommunicationDelivery
                {
                    CommunicationMessageId = message.CommunicationMessageId, OrganisationBusinessEntityId = org,
                    CommunicationProviderConnectionId = message.DestinationConnectionId!.Value, CommunicationDestinationId = message.CommunicationDestinationId!.Value,
                    IdempotencyKey = $"{source.EventId:N}:{message.CommunicationAutomationRuleId}:{message.CommunicationDestinationId}", CreatedAtUtc = DateTime.UtcNow
                });
            });
        if (model.Action == "Reject") message.ApprovalStatus = "Rejected";
        if (model.Action == "Cancel") message.ApprovalStatus = "Cancelled";
        await logs.CreateAsync(db, HRM.Components.Shared.SharedConfig.OperationLogActionTypes.REQUEST_STATE_CHANGE,
            $"Communication announcement {message.CommunicationMessageId}: {model.Action} by user {actor.UserId}.");
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    private async Task ValidateAnnouncementReleaseAsync(HrmTeContext db, CommunicationMessage message)
    {
        var org = message.OrganisationBusinessEntityId;
        var maxAge = Math.Clamp(configuration.GetValue<int?>("Communications:DeliveryMaxAgeHours") ?? 24, 1, 168);
        if (message.CreatedAtUtc < DateTime.UtcNow.AddHours(-maxAge)) throw new InvalidOperationException("This announcement has expired. Create a new announcement.");
        if (!message.CommunicationDestinationId.HasValue || !message.DestinationConnectionId.HasValue || string.IsNullOrWhiteSpace(message.DestinationExternalId))
            throw new InvalidOperationException("A complete destination snapshot is required. Create a new announcement.");
        var target = await (from d in db.CommunicationDestinations
            join c in db.CommunicationProviderConnections on d.CommunicationProviderConnectionId equals c.CommunicationProviderConnectionId
            where d.CommunicationDestinationId == message.CommunicationDestinationId && d.CommunicationProviderConnectionId == message.DestinationConnectionId &&
                d.ExternalDestinationId == message.DestinationExternalId && d.OrganisationBusinessEntityId == org && c.OrganisationBusinessEntityId == org &&
                d.IsActive && c.IsEnabled
            select new { Destination = d, c.ProviderCode }).SingleOrDefaultAsync();
        if (target is null) throw new InvalidOperationException("The saved destination has changed or is disabled. Restore it or create a new announcement.");
        var source = await db.CommunicationOutbox.AsNoTracking().SingleOrDefaultAsync(x => x.CommunicationOutboxId == message.CommunicationOutboxId && x.OrganisationBusinessEntityId == org);
        if (source is null || !await CommunicationEligibility.CanDeliverToAsync(db, source, target.Destination, target.ProviderCode))
            throw new InvalidOperationException("The source, recipient consent or audience is no longer eligible for delivery.");
    }
}
