// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/user-settings")]
public sealed class UserSettingsController(global::HRM.Services.Contracts.UserSettingsServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.DTOs.UserSettingsData>> Load([FromQuery] global::HRM.DTOs.UserSettingsTab tab)
    {
        return Ok(await service.LoadAsync(tab));
    }

    [HttpPost("save-account")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveAccount([FromBody] UserSettingsSaveAccountRequest request)
    {
        return Ok(await service.SaveAccountAsync(request.model));
    }

    public sealed class UserSettingsSaveAccountRequest
    {
        public global::HRM.DTOs.ManagedUserDto model { get; set; } = default!;
    }

    [HttpPost("save-legacy-profile")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveLegacyProfile([FromBody] UserSettingsSaveLegacyProfileRequest request)
    {
        return Ok(await service.SaveLegacyProfileAsync(request.model));
    }

    public sealed class UserSettingsSaveLegacyProfileRequest
    {
        public global::HRM.DTOs.UserContext.UserProfileDto model { get; set; } = default!;
    }

    [HttpPost("save-user-access")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveUserAccess([FromBody] UserSettingsSaveUserAccessRequest request)
    {
        return Ok(await service.SaveUserAccessAsync(request.model));
    }

    public sealed class UserSettingsSaveUserAccessRequest
    {
        public global::HRM.DTOs.ManagedUserDto model { get; set; } = default!;
    }

    [HttpPost("save-staff")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveStaff([FromBody] UserSettingsSaveStaffRequest request)
    {
        return Ok(await service.SaveStaffAsync(request.model));
    }

    public sealed class UserSettingsSaveStaffRequest
    {
        public global::HRM.DTOs.ManagedStaffDto model { get; set; } = default!;
    }

    [HttpPost("save-enrollment")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveEnrollment([FromBody] UserSettingsSaveEnrollmentRequest request)
    {
        return Ok(await service.SaveEnrollmentAsync(request.model));
    }

    public sealed class UserSettingsSaveEnrollmentRequest
    {
        public global::HRM.DTOs.ManagedEnrollmentDto model { get; set; } = default!;
    }

    [HttpPost("save-organisation-access")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveOrganisationAccess([FromBody] UserSettingsSaveOrganisationAccessRequest request)
    {
        return Ok(await service.SaveOrganisationAccessAsync(request.permissionIds));
    }

    public sealed class UserSettingsSaveOrganisationAccessRequest
    {
        public global::System.Collections.Generic.List<global::System.Int32> permissionIds { get; set; } = default!;
    }

}
