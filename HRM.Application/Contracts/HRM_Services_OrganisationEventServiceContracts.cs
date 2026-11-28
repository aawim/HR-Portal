using HRM.DTOs.Calendar;
namespace HRM.Services.Calendar.Contracts;

public abstract class OrganisationEventServiceContracts
{
    public interface IOperations
    {
        Task<List<OrganisationEventDto>> ListAsync(DateTime? from, DateTime? to, string? category, string? status, string? impact, int? structureId, bool? upcomingOnly);
        Task<OrganisationEventDto> GetAsync(int id);
        /// <summary>Creates (id 0) or updates a Draft/Published event; returns the event id.</summary>
        Task<int> SaveAsync(OrganisationEventDto dto);
        Task<EventImpactPreviewDto> PreviewImpactAsync(int id);
        Task PublishAsync(int id);
        Task CancelAsync(int id, string reason);
        Task<int> DuplicateAsync(int id);
        Task<List<OrganisationEventAuditDto>> AuditAsync(int id);
    }
}
