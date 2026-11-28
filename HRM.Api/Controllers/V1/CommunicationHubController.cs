using HRM.Api.Infrastructure;
using HRM.DTOs.Communications;
using Microsoft.AspNetCore.Mvc;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/communication-hub")]
public sealed class CommunicationHubController(HRM.Services.Contracts.CommunicationHubContracts.IOperations service) : HrmApiController
{
    [HttpGet("load")] public async Task<ActionResult<CommunicationHubConfigurationDto>> Load() => Ok(await service.LoadAsync());
    [HttpPost("telegram-connection")] public async Task<IActionResult> SaveTelegramConnection([FromBody] SaveConnectionRequest request) { await service.SaveTelegramConnectionAsync(request.model); return NoContent(); }
    [HttpPost("telegram-destination")] public async Task<IActionResult> SaveTelegramDestination([FromBody] SaveDestinationRequest request) { await service.SaveTelegramDestinationAsync(request.model); return NoContent(); }
    [HttpPost("rule")] public async Task<IActionResult> SaveRule([FromBody] SaveRuleRequest request) { await service.SaveRuleAsync(request.model); return NoContent(); }

    public sealed class SaveConnectionRequest { public TelegramConnectionDto model { get; set; } = new(); }
    public sealed class SaveDestinationRequest { public TelegramDestinationDto model { get; set; } = new(); }
    public sealed class SaveRuleRequest { public CommunicationRuleDto model { get; set; } = new(); }
    [HttpPost("integration")] public async Task<IActionResult> SaveIntegration([FromBody] IntegrationRequest request) { await service.SaveIntegrationAsync(request.model); return NoContent(); }
    [HttpPost("destination")] public async Task<IActionResult> SaveDestination([FromBody] DestinationRequest request) { await service.SaveDestinationAsync(request.model); return NoContent(); }
    public sealed class IntegrationRequest { public CommunicationIntegrationDto model { get; set; } = new(); }
    public sealed class DestinationRequest { public CommunicationDestinationDto model { get; set; } = new(); }
    [HttpPost("validate-telegram")] public async Task<ActionResult<CommunicationConnectionValidationDto>> ValidateTelegram([FromBody] ValidateRequest request) => Ok(await service.ValidateTelegramConnectionAsync(request.id));
    public sealed class ValidateRequest { public long id { get; set; } }
    [HttpGet("approvals")] public async Task<ActionResult<List<CommunicationApprovalReviewDto>>> Approvals([FromQuery] int requestId = 0) => Ok(await service.LoadApprovalsAsync(requestId));
    [HttpPost("act-on-announcement")] public async Task<IActionResult> ActOnAnnouncement([FromBody] ApprovalActionRequest request) { await service.ActOnAnnouncementAsync(request.model); return NoContent(); }
    public sealed class ApprovalActionRequest { public CommunicationApprovalActionDto model { get; set; } = new(); }
    [HttpPost("submit-announcement")] public async Task<IActionResult> SubmitAnnouncement([FromBody] ValidateRequest request) { await service.SubmitAnnouncementAsync(request.id); return NoContent(); }
    [HttpPost("queue-telegram-test")] public async Task<IActionResult> QueueTelegramTest([FromBody] ValidateRequest request) { await service.QueueTelegramTestAsync(request.id); return NoContent(); }
    [HttpPost("validate-destination")] public async Task<ActionResult<CommunicationConnectionValidationDto>> ValidateDestination([FromBody] ValidateRequest request) => Ok(await service.ValidateTelegramDestinationAsync(request.id));
    [HttpPost("retry-delivery")] public async Task<IActionResult> RetryDelivery([FromBody] ValidateRequest request) { await service.RetryDeliveryAsync(request.id); return NoContent(); }
}
