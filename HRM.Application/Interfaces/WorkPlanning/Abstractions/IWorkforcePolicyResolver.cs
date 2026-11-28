namespace HRM.WorkPlanning.Abstractions;

public sealed record WorkforcePolicyResolutionContext(int OrganisationId, DateOnly EffectiveDate, int? JobId = null, int? IndividualId = null, int? DutyTypeId = null);
public sealed record ResolvedWorkforcePolicy(int PolicyId, string Code, string RulesJson, int Version, int AssignmentId);

public interface IWorkforcePolicyResolver
{
    Task<IReadOnlyList<ResolvedWorkforcePolicy>> ResolveAsync(WorkforcePolicyResolutionContext context, CancellationToken cancellationToken = default);
}
