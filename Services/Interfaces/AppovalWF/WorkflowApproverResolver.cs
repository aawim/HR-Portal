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
                    case "USER":
                        userIds = await db.Set<User>()
                            .Where(u =>
                                u.UserId == referenceId &&
                                u.IsActive &&
                                u.UserOrganisation != null )
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
                                    context.ApplicantBusinessEntityId &&
                                (
                                    u.UserRoles.Any(ur =>
                                        ur.RoleId == referenceId &&
                                        ur.IsActive &&
                                        ur.UserOrganisation.BusinessEntityID ==
                                            context.ApplicantBusinessEntityId)
                                    ||
                                    u.UserAssignedUserGroups.Any(assignment =>
                                        assignment.UserGroup.UserOrganisation.BusinessEntityID ==
                                            context.ApplicantBusinessEntityId &&
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
