namespace Intranet.Contracts.Events;
public record EmployeeJoinedIntegrationEvent(
    Guid EventId,
    long OrganisationBusinessEntityID,
    long IndividualID,
    long? JobID,
    DateTime JoiningDate,
    DateTime EventOccurredAt,
    string CorrelationId,
    int EventVersion);