using HRM.DTOs.Leave;
namespace HRM.Services.Interfaces;
public interface IUserAppDataService
{
    Task<List<JobLeaveTypeDto>> GetJobLeaveTypesAsync();
}
