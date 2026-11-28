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
        Task<HelpdeskTicketDetailDto> CreateTicketWithAttachmentsAsync(string category, string subject, string initialMessage, List<HelpdeskAttachmentUploadDto> attachments);
        Task<HelpdeskTicketDetailDto> SendUserMessageAsync(long ticketId, string body);
        Task<HelpdeskTicketDetailDto> SendUserMessageWithAttachmentsAsync(long ticketId, string body, List<HelpdeskAttachmentUploadDto> attachments);
        Task<HelpdeskTicketDetailDto> EscalateToAgentAsync(long ticketId, string reason);
        Task<HelpdeskTicketDetailDto> MarkResolvedByUserAsync(long ticketId, int rating);

        Task<HelpdeskAgentInboxDto> GetAgentInboxAsync(string statusFilter, int organisationId, string search);
        Task<HelpdeskTicketDetailDto> TakeOverTicketAsync(long ticketId, string pin);
        Task<HelpdeskTicketDetailDto> SendAgentMessageAsync(long ticketId, string body, bool isInternalNote, string pin);
        Task<HelpdeskTicketDetailDto> SendAgentMessageWithAttachmentsAsync(long ticketId, string body, bool isInternalNote, string pin, List<HelpdeskAttachmentUploadDto> attachments);
        Task<HelpdeskTicketDetailDto> ResolveTicketByAgentAsync(long ticketId, string resolutionSummary);
        Task<HelpdeskTicketDetailDto> ReopenTicketAsync(long ticketId);
        Task<HelpdeskAttachmentContentDto> DownloadAttachmentAsync(long attachmentId);

        Task<List<HelpdeskArticleDto>> GetArticlesAsync(string search);
        Task<ServiceResult> SaveArticleAsync(HelpdeskArticleDto model);
        Task<ServiceResult> DeleteArticleAsync(int articleId);

        Task<HelpdeskAvailabilityDto> GetAvailabilityAsync();
        Task<HelpdeskBotSettingsDto> GetBotSettingsAsync();
        Task<ServiceResult> SaveBotSettingsAsync(SaveHelpdeskBotSettingsDto model);
    }
}
