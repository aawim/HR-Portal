using System.Data;
using HRM.DTOs.ApprovalFlow;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

// Keep configuration details out of employee responses. Report only after the failed
// submission transaction has rolled back, so the alert survives that rollback.
public sealed class WorkflowConfigurationException(WorkflowRequestContextDto context, string detail)
    : InvalidOperationException(StaffMessage)
{
    public const string StaffMessage = "Your request could not be submitted right now. Please contact your organisation's HR team for assistance. No changes have been made.";
    public WorkflowRequestContextDto Context { get; } = context;
    public string Detail { get; } = detail;

    public async Task NotifyAsync(IDbContextFactory<HrmTeContext> factory)
    {
        try
        {
            await using var db = await factory.CreateDbContextAsync();
            var org = Context.OrganisationBusinessEntityId;
            var organisation = await db.Organisations.Where(x => x.BusinessEntityID == org).Select(x => x.OrganisationName).SingleOrDefaultAsync() ?? $"Organisation {org}";
            var jobType = await db.JobTypes.Where(x => x.JobTypeId == Context.JobTypeId).Select(x => x.TypeName).SingleOrDefaultAsync() ?? $"Job Type {Context.JobTypeId}";
            var requestType = await db.RequestTypes.Where(x => x.RequestTypeId == Context.RequestTypeId).Select(x => x.TypeName).SingleOrDefaultAsync() ?? $"Request Type {Context.RequestTypeId}";
            var users = await db.Users.AsNoTracking().Where(x => x.IsActive && x.LocalUserStateId == 1 && x.UserOrganisationId != null)
                .Where(x => x.UserAssignedUserGroups.Any(g => (g.UserGroup.IsGlobal || g.UserGroup.UserOrganisationId == x.UserOrganisationId) &&
                    (g.UserGroup.GroupName == "Developer" || g.UserGroup.GroupName == "Human Resource: Staff" || g.UserGroup.GroupName == "Human Resource :Head")) ||
                    x.AdminPortalAccessEnabled && x.AdminPortalAccessOrganisationId == x.UserOrganisationId &&
                    db.UserOrganisationRoles.Any(r => r.UserOrganisationID == x.UserOrganisationId && r.IsActive &&
                        (r.Role.RoleKey == "Super.Admin.Manage" || r.Role.RoleKey == "Developer")))
                .Select(x => new { x.UserId, x.BusinessEntityID, Org = x.UserOrganisation.BusinessEntityID }).ToListAsync();
            var groups = await db.UserAssignedUserGroups.AsNoTracking()
                .Where(x => x.UserGroup.IsGlobal || x.UserGroup.UserOrganisationId == x.User.UserOrganisationId)
                .Select(x => new { x.UserId, x.UserGroup.GroupName, x.UserGroup.IsGlobal }).ToListAsync();
            var permissions = new EffectivePermissionService(factory);
            var recipients = new List<(int User, int Person, int Org)>();
            foreach (var user in users)
            {
                var memberships = groups.Where(x => x.UserId == user.UserId).ToList();
                var hr = user.Org == org && memberships.Any(x => x.GroupName is "Human Resource: Staff" or "Human Resource :Head");
                var developer = memberships.Any(x => x.GroupName == "Developer" && (user.Org == org || x.IsGlobal));
                if (hr || developer || await permissions.HasPermissionAsync(user.UserId, "Super.Admin.Manage") ||
                    user.Org == org && await permissions.HasPermissionAsync(user.UserId, "Developer"))
                    recipients.Add((user.UserId, user.BusinessEntityID, user.Org));
            }
            // The source organisation is explicit; this link never switches tenant context.
            var url = $"/admin/settings/leaves/approval-flows?issueOrganisationId={org}&jobTypeId={Context.JobTypeId}&requestTypeId={Context.RequestTypeId}";
            var message = $"A request could not be submitted in {organisation}. Job Type: {jobType}. Request Type: {requestType}. {Detail} Review Request Approval Flows in this organisation.";
            if (message.Length > 500) message = message[..497] + "…";
            await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var cutoff = DateTime.UtcNow.AddHours(-24);
            foreach (var recipient in recipients)
            {
                if (await db.StaffNotifications.AnyAsync(x => x.UserId == recipient.User && x.OrganisationId == recipient.Org &&
                    x.Kind == "Approval flow needs attention" && x.Url == url && x.CreatedAtUtc >= cutoff)) continue;
                db.StaffNotifications.Add(new StaffNotification { UserId = recipient.User, IndividualId = recipient.Person,
                    OrganisationId = recipient.Org, Kind = "Approval flow needs attention", Message = message, Url = url, CreatedAtUtc = DateTime.UtcNow });
            }
            await db.SaveChangesAsync();
            await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            // Failure to deliver must not expose configuration details or replace the
            // original friendly response. Retain diagnostics for operations staff.
            Console.Error.WriteLine($"Approval-flow alert failed for organisation {Context.OrganisationBusinessEntityId}, job type {Context.JobTypeId}, request type {Context.RequestTypeId}: {ex}");
        }
    }
}
