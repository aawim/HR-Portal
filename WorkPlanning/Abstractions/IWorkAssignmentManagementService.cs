using HRM.DTOs.WorkPlanning;

namespace HRM.WorkPlanning.Abstractions
{
    public interface IWorkAssignmentManagementService
    {
        Task<List<WorkAssignmentDepartmentDto>>
         GetDepartmentsAsync(
             CancellationToken cancellationToken = default);

        Task<List<WorkPlanLookupDto>>
            GetWorkPlansAsync(
                CancellationToken cancellationToken = default);

        Task<List<WorkAssignmentGroupDto>>
            GetAssignmentsAsync(
                WorkAssignmentFilterDto filter,
                CancellationToken cancellationToken = default);

        Task<List<EligibleAssignmentStaffDto>>
            GetDepartmentStaffAsync(
                int organisationStructureId,
                CancellationToken cancellationToken = default);

        Task<List<WorkAssignmentEmployeeDto>>
            GetAssignmentEmployeesAsync(
                int organisationStructureId,
                int? workPlanId,
                string assignmentName,
                DateTime scheduledStart,
                DateTime scheduledEnd,
                CancellationToken cancellationToken = default);

        Task<BulkAssignmentResultDto>
            AssignDepartmentAsync(
                BulkDepartmentAssignmentRequest request,
                CancellationToken cancellationToken = default);





    }
}
