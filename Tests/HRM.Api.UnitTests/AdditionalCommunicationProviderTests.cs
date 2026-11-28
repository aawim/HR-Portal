using System.Net;
using HRM.Communications.Contracts;
using HRM.Communications.Infrastructure;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class AdditionalCommunicationProviderTests
{
    private static IConfiguration Configuration() => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Communications:Meta:GraphApiVersion"] = "v23.0",
        ["Communications:Organisations:10:Credentials:page"] = "test-token",
        ["Communications:Credentials:page"] = "must-not-cross-tenants"
    }).Build();

    [Theory]
    [InlineData(200, "{\"id\":\"123_456\"}", true, false)]
    [InlineData(429, "{}", false, true)]
    [InlineData(500, "{}", false, false)]
    [InlineData(200, "[]", false, false)]
    public async Task FacebookReturnsSafeOutcomes(int status, string body, bool succeeded, bool retry)
    {
        var handler = new Handler(status, body);
        var provider = new FacebookPageCommunicationProvider(new HttpClient(handler), Configuration());
        var result = await provider.SendAsync(new("key", "page", "123", "Subject", "Public message", OrganisationId: 10), default);
        Assert.Equal(succeeded, result.Succeeded);
        Assert.Equal(retry, result.IsTransient);
        Assert.Equal("https://graph.facebook.com/v23.0/123/feed", handler.Url);
        Assert.Contains("Public message", handler.Body);
    }

    [Fact]
    public async Task CredentialsCannotFallBackToAnotherOrganisation()
    {
        var handler = new Handler(200, "{\"id\":\"123\"}");
        var provider = new FacebookPageCommunicationProvider(new HttpClient(handler), Configuration());
        var result = await provider.SendAsync(new("key", "page", "123", "Subject", "Body", OrganisationId: 20), default);
        Assert.Equal("CredentialNotConfigured", result.ErrorCode);
        Assert.Null(handler.Url);
    }

    [Theory]
    [InlineData("{\"messages\":[{\"id\":\"wamid.123\"}]}", true)]
    [InlineData("{\"messages\":[1]}", false)]
    public async Task WhatsAppUsesApprovedTemplateAndHandlesMalformedAcknowledgements(string response, bool succeeded)
    {
        var handler = new Handler(200, response);
        var provider = new WhatsAppCommunicationProvider(new HttpClient(handler), Configuration());
        var request = new CommunicationSendRequest("key", "page", "9607777777", "Subject", "Schedule changed",
            "{\"phoneNumberId\":\"123\",\"templateName\":\"schedule_update\",\"language\":\"en_US\"}", 10);
        var result = await provider.SendAsync(request, default);
        Assert.Equal(succeeded, result.Succeeded);
        Assert.EndsWith("/123/messages", handler.Url);
        Assert.Contains("schedule_update", handler.Body);
        Assert.Contains("9607777777", handler.Body);
        Assert.Contains("template", handler.Body);
    }

    [Theory]
    [InlineData("https://tenant.environment.api.powerplatform.com/automations/direct/workflows/123/triggers/manual/paths/invoke", true)]
    [InlineData("https://region.logic.azure.com/workflows/123/triggers/manual/paths/invoke", true)]
    [InlineData("https://tenant.environment.api.powerplatform.com.evil.test/workflows/123", false)]
    [InlineData("http://region.logic.azure.com/workflows/123", false)]
    [InlineData("https://example.test/workflows/123", false)]
    public void TeamsRestrictsWorkflowHosts(string url, bool expected) => Assert.Equal(expected, MicrosoftTeamsCommunicationProvider.IsSupportedWorkflowUrl(url));

    private sealed class Handler(int status, string response) : HttpMessageHandler
    {
        public string? Url { get; private set; }
        public string Body { get; private set; } = "";
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Url = request.RequestUri!.AbsoluteUri;
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent(response) };
        }
    }
}
