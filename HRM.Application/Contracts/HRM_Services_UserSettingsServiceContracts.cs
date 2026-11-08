using System.ComponentModel.DataAnnotations;
using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Roles;
using HRM.DTOs.UserContext;
using HRM.Services.Interfaces;
namespace HRM.Services.Contracts;
public abstract class UserSettingsServiceContracts
{

public interface IOperations
{
Task<UserSettingsData> LoadAsync(UserSettingsTab tab)
;
Task<ServiceResult> SaveAccountAsync(ManagedUserDto model)
;

Task<ServiceResult> SaveLegacyProfileAsync(UserProfileDto model)
;
Task<ServiceResult> SaveUserAccessAsync(ManagedUserDto model)
;
Task<ServiceResult> SaveStaffAsync(ManagedStaffDto model)
;
Task<ServiceResult> SaveEnrollmentAsync(ManagedEnrollmentDto model)
;
Task<ServiceResult> SaveOrganisationAccessAsync(List<int> permissionIds)
;
}
}
