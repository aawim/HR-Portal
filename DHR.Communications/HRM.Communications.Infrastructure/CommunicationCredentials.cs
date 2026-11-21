using Microsoft.Extensions.Configuration;
namespace HRM.Communications.Infrastructure;

public static class CommunicationCredentials
{
    public static string? Read(IConfiguration configuration, int organisationId, string reference) => organisationId > 0
        ? configuration[$"Communications:Organisations:{organisationId}:Credentials:{reference}"]
        : configuration[$"Communications:Credentials:{reference}"];
}
