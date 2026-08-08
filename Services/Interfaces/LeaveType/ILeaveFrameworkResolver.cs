using HRM.Enum;

namespace HRM.Services.Interfaces.LeaveType
{
    public interface ILeaveFrameworkResolver
    {
        Task<LeaveFrameworkType> GetOrganisationFrameworkAsync(
        int organisationId,
        CancellationToken cancellationToken = default);

        Task<LeaveFrameworkType> ResolveAsync(
            int organisationId,
            int? legacyLeaveTypeId,
            int? leaveDefinitionId,
            CancellationToken cancellationToken = default);
    }
}
