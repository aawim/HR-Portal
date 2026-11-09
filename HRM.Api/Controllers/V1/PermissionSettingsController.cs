// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/permission-settings")]
public sealed class PermissionSettingsController(global::HRM.Services.Contracts.PermissionSettingsServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.DTOs.Roles.PermissionSettingsData>> Load([FromQuery] global::System.Boolean groups)
    {
        return Ok(await service.LoadAsync(groups));
    }

    [HttpPost("save-permission")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SavePermission([FromBody] PermissionSettingsSavePermissionRequest request)
    {
        return Ok(await service.SavePermissionAsync(request.model));
    }

    public sealed class PermissionSettingsSavePermissionRequest
    {
        public global::HRM.DTOs.Roles.PermissionDefinitionDto model { get; set; } = default!;
    }

    [HttpPost("save-role")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveRole([FromBody] PermissionSettingsSaveRoleRequest request)
    {
        return Ok(await service.SaveRoleAsync(request.model));
    }

    public sealed class PermissionSettingsSaveRoleRequest
    {
        public global::HRM.DTOs.Roles.AccessRoleDto model { get; set; } = default!;
    }

    [HttpGet("load-staff-access")]
    public async Task<ActionResult<global::HRM.DTOs.Roles.StaffAccessSettingsData>> LoadStaffAccess()
    {
        return Ok(await service.LoadStaffAccessAsync());
    }

    [HttpPost("save-staff-access-policy")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveStaffAccessPolicy([FromBody] PermissionSettingsSaveStaffAccessPolicyRequest request)
    {
        return Ok(await service.SaveStaffAccessPolicyAsync(request.model));
    }

    public sealed class PermissionSettingsSaveStaffAccessPolicyRequest
    {
        public global::HRM.DTOs.Roles.StaffAccessPolicyDto model { get; set; } = default!;
    }

    [HttpPost("save-staff-access-scope")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveStaffAccessScope([FromBody] PermissionSettingsSaveStaffAccessScopeRequest request)
    {
        return Ok(await service.SaveStaffAccessScopeAsync(request.model));
    }

    public sealed class PermissionSettingsSaveStaffAccessScopeRequest
    {
        public global::HRM.DTOs.Roles.StaffAccessScopeDto model { get; set; } = default!;
    }

    [HttpPost("save-staff-access-grant")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveStaffAccessGrant([FromBody] PermissionSettingsSaveStaffAccessGrantRequest request)
    {
        return Ok(await service.SaveStaffAccessGrantAsync(request.model));
    }

    public sealed class PermissionSettingsSaveStaffAccessGrantRequest
    {
        public global::HRM.DTOs.Roles.StaffAccessGrantDto model { get; set; } = default!;
    }

}
