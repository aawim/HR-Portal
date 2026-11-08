using HRM.Services.Transport;
namespace HRM.Services;
public partial class AttendanceLogService
{
    async global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::HRM.DTOs.Contracts.AttendanceLogDto>> global::HRM.Services.Contracts.AttendanceLogServiceContracts.IOperations.GetAttendanceLogsAsync(global::System.Int32 userId, global::System.DateTime startDate, global::System.DateTime endDate)
    {
        var result = await GetAttendanceLogsAsync(userId, startDate, endDate);
        return result.Select(item => ContractDtoMappings.ToDto(item)).ToList();
    }

    async global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::HRM.DTOs.Leave.JobLeaveTypeDto>> global::HRM.Services.Contracts.AttendanceLogServiceContracts.IOperations.GetUserLeaveBalancesAsync(global::System.String identifier)
    {
        var result = await GetUserLeaveBalancesAsync(identifier);
        return result.Select(item => ContractDtoMappings.ToDto(item)).ToList();
    }

    async global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::HRM.DTOs.Contracts.AttendanceLogDto>> global::HRM.Services.Contracts.AttendanceLogServiceContracts.IOperations.GetUnevenLogsAsync(global::System.Int32 individualId)
    {
        var result = await GetUnevenLogsAsync(individualId);
        return result.Select(item => ContractDtoMappings.ToDto(item)).ToList();
    }

    async global::System.Threading.Tasks.Task<global::System.Collections.Generic.List<global::HRM.DTOs.Contracts.LeafDto>> global::HRM.Services.Contracts.AttendanceLogServiceContracts.IOperations.GetLeaveRequestsAsync(global::System.Int32 jobId)
    {
        var result = await GetLeaveRequestsAsync(jobId);
        return result.Select(item => ContractDtoMappings.ToDto(item)).ToList();
    }

}
