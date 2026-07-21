using HRM.Models;

namespace HRM.Services.Interfaces
{
    public interface IOperationLogService
    {
        /// <summary>
        /// Creates a new operation log and returns the generated OperationLogID.
        /// </summary>
        Task<int> CreateLogAsync(int actionTypeId, string? remarks = null);

        /// <summary>
        /// Updates an existing operation log with modification details.
        /// </summary>
        Task UpdateLogAsync(int operationLogId, string? remarks = null);

        Task<OperationLog> CreateAsync(HrmTeContext context,int actionId,string remarks);
    }
}
