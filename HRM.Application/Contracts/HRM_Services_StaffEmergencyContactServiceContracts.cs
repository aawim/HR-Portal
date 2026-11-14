using System.Collections.Generic;
using System.Threading.Tasks;
using HRM.DTOs;
using HRM.DTOs.Staff;

namespace HRM.Services.Contracts;

public abstract class StaffEmergencyContactServiceContracts
{
    public interface IOperations
    {
        Task<List<StaffEmergencyContactDto>> GetContactsAsync(int individualId);
        Task<ServiceResult> SaveContactAsync(StaffEmergencyContactInputDto input);
        Task<ServiceResult> DeleteContactAsync(int id);
        Task<ServiceResult> SetPrimaryAsync(int id, int individualId);
    }
}
