using HRM.Models.WorkPlanning;
namespace HRM.WorkPlanning.Abstractions
{
    public interface IWorkTemplateResolver
    {
        Task<WorkTemplate?> ResolveAsync(int organisationBusinessEntityId,int? workTemplateId,DateOnly workDate,
       CancellationToken cancellationToken = default);
    }
}
