using HRM.DTOs.Communications;
namespace HRM.Services.Contracts;
public abstract class CommunicationHubContracts
{
    public interface IOperations
    {
        Task<CommunicationHubConfigurationDto> LoadAsync();
        Task SaveTelegramConnectionAsync(TelegramConnectionDto model);
        Task SaveTelegramDestinationAsync(TelegramDestinationDto model);
        Task SaveRuleAsync(CommunicationRuleDto model);
        Task<CommunicationConnectionValidationDto> ValidateTelegramConnectionAsync(long id);
        Task RetryDeliveryAsync(long id);
        Task<CommunicationConnectionValidationDto> ValidateTelegramDestinationAsync(long id);
        Task QueueTelegramTestAsync(long id);
    }
}
