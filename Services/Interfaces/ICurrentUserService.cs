namespace HRM.Services.Interfaces
{
    public interface ICurrentUserService
    {
        int? UserId { get; }
        int? IndividualId { get; }
        int? OrganisationId { get; }
        string? IpAddress { get; }
        int? ContextId { get; }
    }
}
