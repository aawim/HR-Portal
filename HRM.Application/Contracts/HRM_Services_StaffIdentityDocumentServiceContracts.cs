using System.Threading.Tasks;
using HRM.DTOs;
using HRM.DTOs.Staff;

namespace HRM.Services.Contracts;

public abstract class StaffIdentityDocumentServiceContracts
{
    public interface IOperations
    {
        Task<StaffIdentityDocumentDto> GetIdentityDocumentsAsync(int individualId);
        Task<ServiceResult> UpdateIdentityDocumentsAsync(StaffIdentityDocumentUpdateInput input);
    }
}
