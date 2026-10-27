using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Roles;
using HRM.DTOs.UserContext;
using HRM.Services.Interfaces;
namespace HRM.Services.Contracts;
public abstract class PermissionSettingsServiceContracts
{

public interface IOperations
{
Task<PermissionSettingsData> LoadAsync(bool groups)
;
Task<ServiceResult> SavePermissionAsync(PermissionDefinitionDto model)
;
Task<ServiceResult> SaveRoleAsync(AccessRoleDto model)
;
}
}
