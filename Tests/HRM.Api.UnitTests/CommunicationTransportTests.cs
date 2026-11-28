using System.Text.Json;
using HRM.Api.Controllers.V1;
using HRM.Application.Transport;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class CommunicationTransportTests
{
    [Fact]
    public void ConnectionEnvelopeRetainsEditedIdentityAndSettings()
    {
        var request = JsonSerializer.Deserialize<CommunicationHubController.SaveConnectionRequest>(
            """{"model":{"id":12,"name":"Announcements","credentialReference":"telegram-primary","isEnabled":false}}""", ApiOperationKeys.Json)!;
        Assert.Equal(12, request.model.Id);
        Assert.Equal("Announcements", request.model.Name);
        Assert.Equal("telegram-primary", request.model.CredentialReference);
        Assert.False(request.model.IsEnabled);
    }

    [Fact]
    public void DestinationEnvelopeRetainsConnectionAndChat()
    {
        var request = JsonSerializer.Deserialize<CommunicationHubController.SaveDestinationRequest>(
            """{"model":{"id":4,"connectionId":12,"displayName":"Office","chatId":"-100123","isActive":true}}""", ApiOperationKeys.Json)!;
        Assert.Equal(12, request.model.ConnectionId);
        Assert.Equal("-100123", request.model.ChatId);
        Assert.True(request.model.IsActive);
    }

    [Fact]
    public void RuleEnvelopeRetainsApprovalRequirement()
    {
        var request = JsonSerializer.Deserialize<CommunicationHubController.SaveRuleRequest>(
            """{"model":{"id":8,"name":"Holiday","templateBody":"{{Title}}","requiresApproval":true,"isEnabled":false}}""", ApiOperationKeys.Json)!;
        Assert.Equal(8, request.model.Id);
        Assert.Equal("{{Title}}", request.model.TemplateBody);
        Assert.True(request.model.RequiresApproval);
    }
}
