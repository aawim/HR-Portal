using HRM.Models;
using HRM.WorkPlanning.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HRM.WorkPlanning.Services;

/// <summary>Resolves the most-specific active, effective-dated assignment for every policy code.</summary>
public sealed class WorkforcePolicyResolver(IDbContextFactory<HrmTeContext> factory) : IWorkforcePolicyResolver
{
    public async Task<IReadOnlyList<ResolvedWorkforcePolicy>> ResolveAsync(WorkforcePolicyResolutionContext context, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var day = context.EffectiveDate.ToDateTime(TimeOnly.MinValue);
        var candidates = await db.WorkforcePolicyAssignments.AsNoTracking()
            .Where(x => x.OrganisationId == context.OrganisationId && x.IsActive && x.EffectiveFrom <= day &&
                (!x.EffectiveTo.HasValue || x.EffectiveTo >= day) &&
                (!x.JobId.HasValue || x.JobId == context.JobId) &&
                (!x.IndividualId.HasValue || x.IndividualId == context.IndividualId) &&
                (!x.DutyTypeId.HasValue || x.DutyTypeId == context.DutyTypeId) &&
                x.WorkforcePolicy.IsActive && x.WorkforcePolicy.EffectiveFrom <= day &&
                (!x.WorkforcePolicy.EffectiveTo.HasValue || x.WorkforcePolicy.EffectiveTo >= day))
            .Select(x => new { x.Id, x.JobId, x.IndividualId, x.DutyTypeId, x.Priority, Policy = x.WorkforcePolicy })
            .ToListAsync(cancellationToken);

        return candidates.GroupBy(x => x.Policy.Code, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.OrderByDescending(x =>
                    (x.IndividualId == context.IndividualId && context.IndividualId.HasValue ? 8 : 0) +
                    (x.JobId == context.JobId && context.JobId.HasValue ? 4 : 0) +
                    (x.DutyTypeId == context.DutyTypeId && context.DutyTypeId.HasValue ? 2 : 0))
                .ThenByDescending(x => x.Priority)
                .ThenByDescending(x => x.Policy.EffectiveFrom).ThenByDescending(x => x.Policy.Version).First())
            .Select(x => new ResolvedWorkforcePolicy(x.Policy.Id, x.Policy.Code, x.Policy.RulesJson, x.Policy.Version, x.Id))
            .ToList();
    }

}
