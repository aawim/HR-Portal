using System.Collections.Generic;
using System.Threading.Tasks;
using HRM.DTOs;
using HRM.DTOs.Helpdesk;

namespace HRM.Services.Contracts;

public abstract class HelpdeskServiceContracts
{
    public interface IOperations
    {
        Task<List<HelpdeskTicketSummaryDto>> GetMyTicketsAsync();
        Task<HelpdeskTicketDetailDto> GetTicketAsync(long ticketId);
        Task<HelpdeskTicketDetailDto> CreateTicketAsync(string category, string subject, string initialMessage);
        Task<HelpdeskTicketDetailDto> SendUserMessageAsync(long ticketId, string body);
        Task<HelpdeskTicketDetailDto> EscalateToAgentAsync(long ticketId, string reason);
        Task<HelpdeskTicketDetailDto> MarkResolvedByUserAsync(long ticketId, int rating);

        Task<HelpdeskAgentInboxDto> GetAgentInboxAsync(string statusFilter, int organisationId, string search);
        Task<HelpdeskTicketDetailDto> TakeOverTicketAsync(long ticketId);
        Task<HelpdeskTicketDetailDto> SendAgentMessageAsync(long ticketId, string body, bool isInternalNote);
        Task<HelpdeskTicketDetailDto> ResolveTicketByAgentAsync(long ticketId, string resolutionSummary);
        Task<HelpdeskTicketDetailDto> ReopenTicketAsync(long ticketId);

        Task<List<HelpdeskArticleDto>> GetArticlesAsync(string search);
        Task<ServiceResult> SaveArticleAsync(HelpdeskArticleDto model);
        Task<ServiceResult> DeleteArticleAsync(int articleId);
    }
}
