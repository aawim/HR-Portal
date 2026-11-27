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

    [HttpGet("load-for-organisation")]
    public async Task<ActionResult<global::HRM.DTOs.Roles.PermissionSettingsData>> LoadForOrganisation([FromQuery] global::System.Boolean groups, [FromQuery] global::System.Int32? organisationId = null) => Ok(await service.LoadForOrganisationAsync(groups, organisationId));

    [HttpPost("add-staff-to-role")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> AddStaffToRole([FromBody] PermissionSettingsRoleStaffRequest request) => Ok(await service.AddStaffToRoleAsync(request.roleId, request.userId, request.organisationId));

    [HttpPost("remove-staff-from-role")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> RemoveStaffFromRole([FromBody] PermissionSettingsRoleStaffRequest request) => Ok(await service.RemoveStaffFromRoleAsync(request.roleId, request.userId, request.organisationId));

    public sealed class PermissionSettingsRoleStaffRequest
    {
        public global::System.Int32 roleId { get; set; }
        public global::System.Int32 userId { get; set; }
        public global::System.Int32? organisationId { get; set; }
    }

    [HttpGet("load-staff-access-for-organisation")]
    public async Task<ActionResult<global::HRM.DTOs.Roles.StaffAccessSettingsData>> LoadStaffAccessForOrganisation([FromQuery] global::System.Int32? organisationId = null) => Ok(await service.LoadStaffAccessForOrganisationAsync(organisationId));

    [HttpPost("revoke-organisational-access-scope-for-organisation")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> RevokeOrganisationalAccessScopeForOrganisation([FromBody] PermissionSettingsRevokeOrganisationalAccessScopeForOrgRequest request) => Ok(await service.RevokeOrganisationalAccessScopeForOrganisationAsync(request.scopeId, request.reason, request.organisationId));

    public sealed class PermissionSettingsRevokeOrganisationalAccessScopeForOrgRequest
    {
        public global::System.Int32 scopeId { get; set; }
        public global::System.String reason { get; set; } = default!;
        public global::System.Int32? organisationId { get; set; }
    }

    [HttpPost("simulate-effective-access-for-organisation")]
    public async Task<ActionResult<global::HRM.DTOs.Roles.EffectiveAccessSimulationDto>> SimulateEffectiveAccessForOrganisation([FromBody] PermissionSettingsSimulateEffectiveAccessForOrgRequest request) => Ok(await service.SimulateEffectiveAccessForOrganisationAsync(request.userId, request.organisationId));

    public sealed class PermissionSettingsSimulateEffectiveAccessForOrgRequest
    {
        public global::System.Int32 userId { get; set; }
        public global::System.Int32? organisationId { get; set; }
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

    [HttpPost("save-organisational-access-scope")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveOrganisationalAccessScope([FromBody] PermissionSettingsSaveOrganisationalAccessScopeRequest request)
    {
        return Ok(await service.SaveOrganisationalAccessScopeAsync(request.model));
    }

    public sealed class PermissionSettingsSaveOrganisationalAccessScopeRequest
    {
        public global::HRM.DTOs.Roles.OrganisationalAccessScopeDto model { get; set; } = default!;
    }

    [HttpPost("revoke-organisational-access-scope")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> RevokeOrganisationalAccessScope([FromBody] PermissionSettingsRevokeOrganisationalAccessScopeRequest request)
    {
        return Ok(await service.RevokeOrganisationalAccessScopeAsync(request.scopeId, request.reason));
    }

    public sealed class PermissionSettingsRevokeOrganisationalAccessScopeRequest
    {
        public global::System.Int32 scopeId { get; set; }
        public global::System.String reason { get; set; } = default!;
    }

    [HttpPost("simulate-effective-access")]
    public async Task<ActionResult<global::HRM.DTOs.Roles.EffectiveAccessSimulationDto>> SimulateEffectiveAccess([FromBody] PermissionSettingsSimulateEffectiveAccessRequest request)
    {
        return Ok(await service.SimulateEffectiveAccessAsync(request.userId));
    }

    public sealed class PermissionSettingsSimulateEffectiveAccessRequest
    {
        public global::System.Int32 userId { get; set; }
    }

}
