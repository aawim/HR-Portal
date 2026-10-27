// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/job-type-configuration")]
public sealed class JobTypeConfigurationController(global::HRM.Services.Contracts.JobTypeConfigurationServiceContracts.IOperations service) : HrmApiController
{
    [ApiPermission("Configuration.View")]
    [HttpGet("load")]
    public async Task<ActionResult<global::HRM.DTOs.ApprovalFlow.JobTypeConfigurationData>> Load([FromQuery] global::System.Boolean requestTypes = false)
    {
        return Ok(await service.LoadAsync(requestTypes));
    }

    [ApiPermission("Admin")]
    [HttpPost("save-job-type")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveJobType([FromBody] JobTypeConfigurationSaveJobTypeRequest request)
    {
        return Ok(await service.SaveJobTypeAsync(request.model));
    }

    public sealed class JobTypeConfigurationSaveJobTypeRequest
    {
        public global::HRM.DTOs.ApprovalFlow.JobTypeConfigurationDto model { get; set; } = default!;
    }

    [ApiPermission("Admin")]
    [HttpPost("save-request-type")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveRequestType([FromBody] JobTypeConfigurationSaveRequestTypeRequest request)
    {
        return Ok(await service.SaveRequestTypeAsync(request.model));
    }

    public sealed class JobTypeConfigurationSaveRequestTypeRequest
    {
        public global::HRM.DTOs.ApprovalFlow.JobTypeConfigurationDto model { get; set; } = default!;
    }

    [ApiPermission("Admin")]
    [HttpPost("delete-request-type")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> DeleteRequestType([FromBody] JobTypeConfigurationDeleteRequestTypeRequest request)
    {
        return Ok(await service.DeleteRequestTypeAsync(request.id));
    }

    public sealed class JobTypeConfigurationDeleteRequestTypeRequest
    {
        public global::System.Int32 id { get; set; }
    }

    [ApiPermission("Configuration.View")]
    [HttpGet("get-workflow")]
    public async Task<ActionResult<global::HRM.DTOs.ApprovalFlow.JobTypeWorkflowDto>> GetWorkflow([FromQuery] global::System.Int32 jobTypeId, [FromQuery] global::System.Int32 requestTypeId, [FromQuery] global::System.Boolean requestTypeDefault = false)
    {
        return Ok(await service.GetWorkflowAsync(jobTypeId, requestTypeId, requestTypeDefault));
    }

    [ApiPermission("Admin")]
    [HttpPost("save-workflow")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SaveWorkflow([FromBody] JobTypeConfigurationSaveWorkflowRequest request)
    {
        return Ok(await service.SaveWorkflowAsync(request.model, request.requestTypeDefault));
    }

    public sealed class JobTypeConfigurationSaveWorkflowRequest
    {
        public global::HRM.DTOs.ApprovalFlow.JobTypeWorkflowDto model { get; set; } = default!;
        public global::System.Boolean requestTypeDefault { get; set; } = false;
    }

}
