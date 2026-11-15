using HRM.DTOs;
using HRM.DTOs.Leadership;
using Microsoft.AspNetCore.Mvc;
namespace HRM.Api.Controllers.V1;
public sealed partial class LeadershipAuthorityController
{
 [HttpGet("configuration")] public async Task<ActionResult<LeadershipConfigurationDto>> Configuration(CancellationToken ct)=>Ok(await administration.GetConfigurationAsync(ct));
 [HttpGet("has-authority")] public async Task<ActionResult<bool>> HasAuthority(int organisationId,string permissionCode,DateOnly? onDate,CancellationToken ct)=>Ok(await service.HasAuthorityAsync(organisationId,permissionCode,onDate,ct));
 [HttpGet("require-authority")] public async Task<IActionResult> RequireAuthority(int organisationId,string permissionCode,DateOnly? onDate,CancellationToken ct){await service.RequireAuthorityAsync(organisationId,permissionCode,onDate,ct);return NoContent();}
 [HttpGet("management/records")] public async Task<ActionResult<IReadOnlyList<LeadershipManagementRecord>>> Records(CancellationToken ct)=>Ok(await administration.ListRecordsAsync(ct));
 [HttpGet("management/audit")] public async Task<ActionResult<IReadOnlyList<LeadershipAuditDto>>> Audit(CancellationToken ct)=>Ok(await administration.ListAuditAsync(ct));
 [HttpPost("management/revoke")] public async Task<ActionResult<ServiceResult>> Revoke(LeadershipRevokeRequest request,CancellationToken ct)=>Ok(await administration.RevokeRecordAsync(request.Kind,request.Id,ct));
 public sealed class LeadershipRevokeRequest{public string Kind{get;set;}="";public int Id{get;set;}}
}
