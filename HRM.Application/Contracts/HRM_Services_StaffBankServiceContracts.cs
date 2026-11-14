using System.Collections.Generic;
using System.Threading.Tasks;
using HRM.DTOs;
using HRM.DTOs.Staff;

namespace HRM.Services.Contracts;

public abstract class StaffBankServiceContracts
{
    public interface IOperations
    {
        Task<List<StaffBankAccountDto>> GetAccountsAsync(int individualId);
        Task<ServiceResult> SaveAccountAsync(StaffBankAccountInputDto input);
        Task<ServiceResult> DeleteAccountAsync(int id);
        Task<ServiceResult> SetPrimaryAsync(int id, int individualId);
    }
}
