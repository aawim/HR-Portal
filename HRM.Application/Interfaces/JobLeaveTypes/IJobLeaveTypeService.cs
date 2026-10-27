using HRM.DTOs.Leave;
using HRM.DTOs;
using HRM.DTOs.LeaveTypes;
using HRM.Models;

namespace HRM.Services.Interfaces.JobLeaveTypes
{
    public interface IJobLeaveTypeService
    {



        Task<ServiceResult> UpdateAsync(JobLeaveTypeEditDto dto);
        Task<List<JobLeaveTypeDto>> GetJobLeaveType();

        Task<List<JobLeaveTypeDto>> GetJobLeaveType(int JobId);
        Task<List<JobLeaveTypeDto>> GetEntitlementsAsync(int jobId);


        







        Task<ServiceResult> AssignAsync(AssignLeaveTypeCatalogueDto request,
            CancellationToken cancellationToken = default);
    }
}
