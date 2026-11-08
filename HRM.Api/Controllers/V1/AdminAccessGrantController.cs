// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/admin-access-grant")]
public sealed class AdminAccessGrantController(global::HRM.Services.Contracts.AdminAccessGrantServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.DTOs.AdminAccessGrantData>> Load([FromQuery] global::System.Int32? organisationId = null)
    {
        return Ok(await service.LoadAsync(organisationId));
    }

    [HttpPost("submit")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Submit([FromBody] AdminAccessGrantSubmitRequest request)
    {
        return Ok(await service.SubmitAsync(request.organisationId, request.userId, request.groupIds, request.permissionIds, request.reason));
    }

    public sealed class AdminAccessGrantSubmitRequest
    {
        public global::System.Int32 organisationId { get; set; }
        public global::System.Int32 userId { get; set; }
        public global::System.Collections.Generic.IEnumerable<global::System.Int32> groupIds { get; set; } = default!;
        public global::System.Collections.Generic.IEnumerable<global::System.Int32> permissionIds { get; set; } = default!;
        public global::System.String reason { get; set; } = default!;
    }

    [HttpPost("act")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Act([FromBody] AdminAccessGrantActRequest request)
    {
        return Ok(await service.ActAsync(request.organisationId, request.id, request.expectedState, request.action, request.comment));
    }

    public sealed class AdminAccessGrantActRequest
    {
        public global::System.Int32 organisationId { get; set; }
        public global::System.Int32 id { get; set; }
        public global::System.Int32 expectedState { get; set; }
        public global::System.String action { get; set; } = default!;
        public global::System.String? comment { get; set; }
    }

    [HttpPost("revoke")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Revoke([FromBody] AdminAccessGrantRevokeRequest request)
    {
        return Ok(await service.RevokeAsync(request.organisationId, request.userId, request.reason));
    }

    public sealed class AdminAccessGrantRevokeRequest
    {
        public global::System.Int32 organisationId { get; set; }
        public global::System.Int32 userId { get; set; }
        public global::System.String reason { get; set; } = default!;
    }

}
