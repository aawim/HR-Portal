using HRM.Api.Infrastructure;
using HRM.DTOs.Calendar;
using Microsoft.AspNetCore.Mvc;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/organisation-events")]
public sealed class OrganisationEventsController(global::HRM.Services.Calendar.Contracts.OrganisationEventServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("list")]
    public async Task<ActionResult<List<OrganisationEventDto>>> List([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] string? category, [FromQuery] string? status, [FromQuery] string? impact, [FromQuery] int? structureId, [FromQuery] bool? upcomingOnly)
        => Ok(await service.ListAsync(from, to, category, status, impact, structureId, upcomingOnly));

    [HttpGet("get")]
    public async Task<ActionResult<OrganisationEventDto>> Get([FromQuery] int id) => Ok(await service.GetAsync(id));

    [HttpPost("save")]
    public async Task<ActionResult<int>> Save([FromBody] SaveRequest request) => Ok(await service.SaveAsync(request.dto));

    [HttpGet("preview-impact")]
    public async Task<ActionResult<EventImpactPreviewDto>> PreviewImpact([FromQuery] int id) => Ok(await service.PreviewImpactAsync(id));

    [HttpPost("publish")]
    public async Task<IActionResult> Publish([FromBody] IdRequest request) { await service.PublishAsync(request.id); return NoContent(); }

    [HttpPost("cancel")]
    public async Task<IActionResult> Cancel([FromBody] CancelRequest request) { await service.CancelAsync(request.id, request.reason); return NoContent(); }

    [HttpPost("duplicate")]
    public async Task<ActionResult<int>> Duplicate([FromBody] IdRequest request) => Ok(await service.DuplicateAsync(request.id));

    [HttpGet("audit")]
    public async Task<ActionResult<List<OrganisationEventAuditDto>>> Audit([FromQuery] int id) => Ok(await service.AuditAsync(id));

    public sealed class SaveRequest { public OrganisationEventDto dto { get; set; } = new(); }
    public sealed class IdRequest { public int id { get; set; } }
    public sealed class CancelRequest { public int id { get; set; } public string reason { get; set; } = ""; }
}
