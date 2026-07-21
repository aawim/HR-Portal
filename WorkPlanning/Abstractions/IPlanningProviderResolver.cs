using HRM.WorkPlanning.Results;
namespace HRM.WorkPlanning.Abstractions
{
    public interface IPlanningProviderResolver
    {
        Task<PlanningProviderResolution> ResolveAsync(
      int organisationBusinessEntityId,
      DateOnly workDate,
      CancellationToken cancellationToken = default);
    }
}
