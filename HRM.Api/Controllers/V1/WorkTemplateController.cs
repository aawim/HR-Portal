// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/work-template")]
public sealed class WorkTemplateController(global::HRM.Services.Interfaces.IWorkTemplateService service) : HrmApiController
{
    [HttpGet("get-all")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.WorkPlanning.WorkTemplateDto>>> GetAll([FromQuery] global::System.Boolean includeInactive = true)
    {
        return Ok(await service.GetAllAsync(includeInactive));
    }

    [HttpGet("get-by-id")]
    public async Task<ActionResult<global::HRM.DTOs.WorkPlanning.WorkTemplateDto?>> GetById([FromQuery] global::System.Int32 workTemplateId)
    {
        return Ok(await service.GetByIdAsync(workTemplateId));
    }

    [HttpGet("get-all-template-types")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.WorkPlanning.WorkTemplateTypeDto>>> GetAllTemplateTypes()
    {
        return Ok(await service.GetAllTemplateTypesAsync());
    }

    [HttpPost("create")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Create([FromBody] WorkTemplateCreateRequest request)
    {
        return Ok(await service.CreateAsync(request.dto));
    }

    public sealed class WorkTemplateCreateRequest
    {
        public global::HRM.DTOs.WorkPlanning.CreateWorkTemplateDto dto { get; set; } = default!;
    }

    [HttpPost("update")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Update([FromBody] WorkTemplateUpdateRequest request)
    {
        return Ok(await service.UpdateAsync(request.dto));
    }

    public sealed class WorkTemplateUpdateRequest
    {
        public global::HRM.DTOs.WorkPlanning.UpdateWorkTemplateDto dto { get; set; } = default!;
    }

    [HttpPost("archive")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Archive([FromBody] WorkTemplateArchiveRequest request)
    {
        return Ok(await service.ArchiveAsync(request.workTemplateId));
    }

    public sealed class WorkTemplateArchiveRequest
    {
        public global::System.Int32 workTemplateId { get; set; }
    }

    [HttpGet("get-all-segment-types")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.WorkPlanning.WorkSegmentTypeDto>>> GetAllSegmentTypes()
    {
        return Ok(await service.GetAllSegmentTypesAsync());
    }

    [HttpGet("get-segments")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.WorkPlanning.WorkTemplateSegmentDto>>> GetSegments([FromQuery] global::System.Int32 workTemplateId)
    {
        return Ok(await service.GetSegmentsAsync(workTemplateId));
    }

    [HttpPost("create-segment")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> CreateSegment([FromBody] WorkTemplateCreateSegmentRequest request)
    {
        return Ok(await service.CreateSegmentAsync(request.dto));
    }

    public sealed class WorkTemplateCreateSegmentRequest
    {
        public global::HRM.DTOs.WorkPlanning.SaveWorkTemplateSegmentDto dto { get; set; } = default!;
    }

    [HttpPost("update-segment")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> UpdateSegment([FromBody] WorkTemplateUpdateSegmentRequest request)
    {
        return Ok(await service.UpdateSegmentAsync(request.dto));
    }

    public sealed class WorkTemplateUpdateSegmentRequest
    {
        public global::HRM.DTOs.WorkPlanning.SaveWorkTemplateSegmentDto dto { get; set; } = default!;
    }

    [HttpPost("archive-segment")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> ArchiveSegment([FromBody] WorkTemplateArchiveSegmentRequest request)
    {
        return Ok(await service.ArchiveSegmentAsync(request.workTemplateSegmentId));
    }

    public sealed class WorkTemplateArchiveSegmentRequest
    {
        public global::System.Int32 workTemplateSegmentId { get; set; }
    }

}
