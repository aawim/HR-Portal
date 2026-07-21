using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace HRM.Services.Interfaces
{
    public interface IAuditLogger
    {
        Task CreateLogAsync(
               int dataTypeId,
               int actionTypeId,
               int dataItemId,
               object data
            );

        Task<int> CreateNewLogAsync(
              int dataTypeId,
              int actionTypeId,
              int dataItemId,
              object data
           );

        
    }
}
