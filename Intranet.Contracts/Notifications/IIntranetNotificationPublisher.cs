namespace Intranet.Contracts.Notifications;
public interface IIntranetNotificationPublisher
{
    Task PublishAsync(IntranetNotificationRequest request, CancellationToken ct = default);
}
public record IntranetNotificationRequest(
    string MessageType,
    long OrganisationBusinessEntityID,
    long? IndividualID,
    string Title,
    string? Body,
    string? CorrelationId);