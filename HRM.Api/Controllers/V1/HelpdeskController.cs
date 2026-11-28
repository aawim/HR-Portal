using HRM.Api.Infrastructure;
using HRM.DTOs;
using HRM.DTOs.Helpdesk;
using HRM.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Api.Controllers.V1;

[Route("api/v1/helpdesk")]
public sealed class HelpdeskController(HelpdeskServiceContracts.IOperations service) : HrmApiController
{
    [HttpGet("my-tickets")]
    public async Task<ActionResult<List<HelpdeskTicketSummaryDto>>> GetMyTickets()
        => Ok(await service.GetMyTicketsAsync());

    [HttpGet("ticket")]
    public async Task<ActionResult<HelpdeskTicketDetailDto>> GetTicket([FromQuery] long ticketId)
        => Ok(await service.GetTicketAsync(ticketId));

    [HttpPost("create-ticket")]
    public async Task<ActionResult<HelpdeskTicketDetailDto>> CreateTicket([FromBody] CreateTicketRequest request)
        => Ok(await service.CreateTicketAsync(request.category ?? "", request.subject ?? "", request.initialMessage ?? ""));

    public sealed class CreateTicketRequest
    {
        public string? category { get; set; }
        public string? subject { get; set; }
        public string? initialMessage { get; set; }
    }

    [HttpPost("send-user-message")]
    public async Task<ActionResult<HelpdeskTicketDetailDto>> SendUserMessage([FromBody] SendUserMessageRequest request)
        => Ok(await service.SendUserMessageAsync(request.ticketId, request.body ?? ""));

    public sealed class SendUserMessageRequest
    {
        public long ticketId { get; set; }
        public string? body { get; set; }
    }

    [HttpPost("escalate")]
    public async Task<ActionResult<HelpdeskTicketDetailDto>> EscalateToAgent([FromBody] EscalateRequest request)
        => Ok(await service.EscalateToAgentAsync(request.ticketId, request.reason ?? ""));

    public sealed class EscalateRequest
    {
        public long ticketId { get; set; }
        public string? reason { get; set; }
    }

    [HttpPost("resolve-by-user")]
    public async Task<ActionResult<HelpdeskTicketDetailDto>> MarkResolvedByUser([FromBody] ResolveByUserRequest request)
        => Ok(await service.MarkResolvedByUserAsync(request.ticketId, request.rating));

    public sealed class ResolveByUserRequest
    {
        public long ticketId { get; set; }
        public int rating { get; set; }
    }

    [HttpGet("agent-inbox")]
    public async Task<ActionResult<HelpdeskAgentInboxDto>> GetAgentInbox(
        [FromQuery] string? statusFilter,
        [FromQuery] int organisationId,
        [FromQuery] string? search)
        => Ok(await service.GetAgentInboxAsync(statusFilter ?? "queue", organisationId, search ?? ""));

    [HttpPost("take-over")]
    public async Task<ActionResult<HelpdeskTicketDetailDto>> TakeOverTicket([FromBody] TicketIdRequest request)
        => Ok(await service.TakeOverTicketAsync(request.ticketId));

    [HttpPost("send-agent-message")]
    public async Task<ActionResult<HelpdeskTicketDetailDto>> SendAgentMessage([FromBody] SendAgentMessageRequest request)
        => Ok(await service.SendAgentMessageAsync(request.ticketId, request.body ?? "", request.isInternalNote));

    public sealed class SendAgentMessageRequest
    {
        public long ticketId { get; set; }
        public string? body { get; set; }
        public bool isInternalNote { get; set; }
    }

    [HttpPost("resolve-by-agent")]
    public async Task<ActionResult<HelpdeskTicketDetailDto>> ResolveTicketByAgent([FromBody] ResolveByAgentRequest request)
        => Ok(await service.ResolveTicketByAgentAsync(request.ticketId, request.resolutionSummary ?? ""));

    public sealed class ResolveByAgentRequest
    {
        public long ticketId { get; set; }
        public string? resolutionSummary { get; set; }
    }

    [HttpPost("reopen")]
    public async Task<ActionResult<HelpdeskTicketDetailDto>> ReopenTicket([FromBody] TicketIdRequest request)
        => Ok(await service.ReopenTicketAsync(request.ticketId));

    public sealed class TicketIdRequest
    {
        public long ticketId { get; set; }
    }

    [HttpGet("articles")]
    public async Task<ActionResult<List<HelpdeskArticleDto>>> GetArticles([FromQuery] string? search)
        => Ok(await service.GetArticlesAsync(search ?? ""));

    [HttpPost("save-article")]
    public async Task<ActionResult<ServiceResult>> SaveArticle([FromBody] HelpdeskArticleDto model)
        => Ok(await service.SaveArticleAsync(model));

    [HttpPost("delete-article")]
    public async Task<ActionResult<ServiceResult>> DeleteArticle([FromBody] DeleteArticleRequest request)
        => Ok(await service.DeleteArticleAsync(request.articleId));

    public sealed class DeleteArticleRequest
    {
        public int articleId { get; set; }
    }
}
