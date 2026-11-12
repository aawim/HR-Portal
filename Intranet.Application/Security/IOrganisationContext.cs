namespace Intranet.Application.Security;
public interface IOrganisationContext
{
    long OrganisationBusinessEntityID { get; }
    long IndividualID { get; }
    bool IsAuthenticated { get; }
    bool HasPermission(string permission);
}