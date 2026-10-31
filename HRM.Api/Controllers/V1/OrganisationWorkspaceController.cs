// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/organisation-workspace")]
public sealed class OrganisationWorkspaceController(global::HRM.Services.Contracts.OrganisationWorkspaceServiceContracts.IOperations service) : HrmApiController
{
    [HttpPost("leave-overview")]
    public async Task<ActionResult<HRM.DTOs.OrganisationLeaveOverviewData>> LeaveOverview([FromBody]LeaveOverviewRequest request)=>Ok(await service.LeaveOverviewAsync(request.filter,HttpContext.RequestAborted));
    public sealed class LeaveOverviewRequest{public HRM.DTOs.OrganisationLeaveOverviewFilter filter{get;set;}=new();}
    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.DTOs.OrganisationWorkspaceData>> Load([FromQuery] global::System.Int32 organisationId)
    {
        return Ok(await service.LoadAsync(organisationId));
    }

    [HttpPost("staff")]
    public async Task<ActionResult<global::System.ValueTuple<global::System.Collections.Generic.List<global::HRM.DTOs.OrganisationWorkspaceStaff>, global::System.Int32>>> Staff([FromBody] OrganisationWorkspaceStaffRequest request)
    {
        return Ok(await service.StaffAsync(request.organisationId, request.search, request.departmentId, request.page));
    }

    public sealed class OrganisationWorkspaceStaffRequest
    {
        public global::System.Int32 organisationId { get; set; }
        public global::System.String search { get; set; } = default!;
        public global::System.Int32 departmentId { get; set; }
        public global::System.Int32 page { get; set; }
    }

    [HttpPost("documents")]
    public async Task<ActionResult<global::System.ValueTuple<global::System.Collections.Generic.List<global::HRM.DTOs.OrganisationWorkspaceDocument>, global::System.Int32>>> Documents([FromBody] OrganisationWorkspaceDocumentsRequest request)
    {
        return Ok(await service.DocumentsAsync(request.organisationId, request.page));
    }

    public sealed class OrganisationWorkspaceDocumentsRequest
    {
        public global::System.Int32 organisationId { get; set; }
        public global::System.Int32 page { get; set; }
    }

    [HttpPost("resources")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.OrganisationWorkspaceResource>>> Resources([FromBody] OrganisationWorkspaceResourcesRequest request)
    {
        return Ok(await service.ResourcesAsync(request.organisationId, request.tab));
    }

    public sealed class OrganisationWorkspaceResourcesRequest
    {
        public global::System.Int32 organisationId { get; set; }
        public global::HRM.Enum.WorkspaceTab tab { get; set; }
    }

}
