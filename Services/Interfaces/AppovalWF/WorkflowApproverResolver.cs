using HRM.DTOs.ApprovalFlow;
using HRM.Models;
using HRM.Models.Workflow;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Interfaces.AppovalWF
{
    public class WorkflowApproverResolver : IWorkflowApproverResolver
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;

        public WorkflowApproverResolver(
            IDbContextFactory<HrmTeContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }


        public async Task<IReadOnlyList<WorkflowApproverCandidate>>
       ResolveAsync(
           int workflowStageId,
           WorkflowRequestContextDto context,
           CancellationToken cancellationToken = default)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync(cancellationToken);

            var rules = await db.Set<WfStageApprover>()
                .AsNoTracking()
                .Where(x => x.WorkflowStageId == workflowStageId)
                .OrderBy(x => x.SequenceNumber)
                .ToListAsync(cancellationToken);

            if (rules.Count == 0)
                throw new InvalidOperationException(
                    "No approvers are configured for this stage.");

            var candidates = new List<WorkflowApproverCandidate>();
            var subjectPerson = context.SubjectJobId.HasValue
                ? await db.Jobs.Where(x => x.JobId == context.SubjectJobId.Value).Select(x => x.IndividualID).SingleAsync(cancellationToken)
                : context.ApplicantBusinessEntityId;

            foreach (var rule in rules)
            {
                if (!rule.ApproverReferenceId.HasValue)
                {
                    throw new InvalidOperationException(
                        $"Approver rule {rule.StageApproverId} " +
                        "does not have a reference ID.");
                }

                var referenceId = rule.ApproverReferenceId.Value;

                List<int> userIds;

                switch (rule.ApproverType.ToUpperInvariant())
                {
                    case "USERGROUP":
                    case "SUPERVISORGROUP":
                        var groupPeople = new List<int>();
                        if (rule.ApproverType.Equals("SupervisorGroup", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!context.SubjectJobId.HasValue) throw new InvalidOperationException("A subject job is required to resolve the supervisor.");
                            groupPeople = await HRM.Services.Leave.LeaveHandoverService.SupervisorsAsync(db, context.SubjectJobId.Value);
                        }
                        var supervisorGroup = rule.ApproverType.Equals("SupervisorGroup", StringComparison.OrdinalIgnoreCase);
                        userIds = await db.Users.Where(u => u.IsActive && u.UserOrganisation != null &&
                            u.UserOrganisation.BusinessEntityID == context.OrganisationBusinessEntityId &&
                            (!supervisorGroup || groupPeople.Contains(u.BusinessEntityID)) &&
                            u.UserAssignedUserGroups.Any(g => g.UserGroupId == referenceId &&
                                (g.UserGroup.IsGlobal || g.UserGroup.UserOrganisation.BusinessEntityID == context.OrganisationBusinessEntityId)))
                            .Select(u => u.UserId).Distinct().ToListAsync(cancellationToken);
                        break;
                    case "SUPERVISOR":
                        if (!context.SubjectJobId.HasValue) throw new InvalidOperationException("A subject job is required to resolve the supervisor.");
                        var people = await HRM.Services.Leave.LeaveHandoverService.SupervisorsAsync(db, context.SubjectJobId.Value);
                        userIds = await db.Users.Where(u => u.IsActive && people.Contains(u.BusinessEntityID) &&
                            u.UserOrganisation != null && u.UserOrganisation.BusinessEntityID == context.OrganisationBusinessEntityId &&
                            (referenceId == 0 || u.UserRoles.Any(r => r.IsActive && r.RoleId == referenceId &&
                                r.UserOrganisation.BusinessEntityID == context.OrganisationBusinessEntityId) ||
                             u.UserAssignedUserGroups.Any(g => g.UserGroup.UserOrganisation.BusinessEntityID == context.OrganisationBusinessEntityId &&
                                g.UserGroup.UserGroupRoles.Any(r => r.IsActive && r.RoleId == referenceId))))
                            .Select(u => u.UserId).Distinct().ToListAsync(cancellationToken);
                        break;
                    case "USER":
                        userIds = await db.Set<User>()
                            .Where(u =>
                                u.UserId == referenceId &&
                                u.IsActive &&
                                u.UserOrganisation != null &&
                                u.UserOrganisation.BusinessEntityID ==
                                    context.OrganisationBusinessEntityId)
                            .Select(u => u.UserId)
                            .ToListAsync(cancellationToken);
                        break;

                    case "ROLE":
                        // Direct permissions and group-derived permissions.
                        userIds = await db.Set<User>()
                            .Where(u =>
                                u.IsActive &&
                                u.UserOrganisation != null &&
                                u.UserOrganisation.BusinessEntityID ==
                                    context.OrganisationBusinessEntityId &&
                                (
                                    u.UserRoles.Any(ur =>
                                        ur.RoleId == referenceId &&
                                        ur.IsActive &&
                                        ur.UserOrganisation.BusinessEntityID ==
                                            context.OrganisationBusinessEntityId)
                                    ||
                                    u.UserAssignedUserGroups.Any(assignment =>
                                        assignment.UserGroup.UserOrganisation.BusinessEntityID ==
                                            context.OrganisationBusinessEntityId &&
                                        assignment.UserGroup.UserGroupRoles.Any(gr =>
                                            gr.RoleId == referenceId &&
                                            gr.IsActive))
                                ))
                            .Select(u => u.UserId)
                            .Distinct()
                            .ToListAsync(cancellationToken);
                        break;

                    default:
                        throw new NotSupportedException(
                            $"Approver type '{rule.ApproverType}' " +
                            "is not yet supported.");
                }

                var eligibleIds = await db.Users.Where(u => userIds.Contains(u.UserId) && u.BusinessEntityID != subjectPerson)
                    .Select(u => u.UserId).ToListAsync(cancellationToken);
                userIds = eligibleIds;
                if (rule.IsRequired && userIds.Count == 0)
                {
                    throw new InvalidOperationException(
                        $"Required approver rule {rule.StageApproverId} " +
                        "resolved no eligible users.");
                }

                foreach (var userId in userIds)
                {
                    candidates.Add(new WorkflowApproverCandidate(
                        userId,
                        rule.StageApproverId,
                        rule.SequenceNumber,
                        rule.IsRequired));
                }
            }

            if (candidates.Count == 0)
            {
                throw new InvalidOperationException(
                    "The workflow stage has no eligible approvers.");
            }

            return candidates;
        }
    }
}
