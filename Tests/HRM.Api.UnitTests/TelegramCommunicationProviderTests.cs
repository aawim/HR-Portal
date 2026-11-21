using System.Net;
using HRM.Communications.Contracts;
using HRM.Communications.Infrastructure;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class TelegramCommunicationProviderTests
{
    [Theory]
    [InlineData("administrator", true, true)]
    [InlineData("administrator", false, false)]
    [InlineData("left", false, false)]
    public async Task ChannelVerificationRequiresPostingRights(string status, bool canPost, bool expected)
    {
        var handler = new DestinationHandler(status, canPost);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Communications:Credentials:test"] = "test-token" }).Build();
        var provider = new TelegramCommunicationProvider(new HttpClient(handler) { BaseAddress = new Uri("https://api.telegram.org/") }, configuration);
        var result = await provider.ValidateDestinationAsync("test", "-100123", CancellationToken.None);
        Assert.Equal(expected, result.Succeeded);
        Assert.Equal(3, handler.Calls);
    }

    private sealed class DestinationHandler(string status, bool canPost) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Calls++;
            var result = request.RequestUri!.AbsolutePath.EndsWith("/getMe", StringComparison.Ordinal) ? "{\"id\":123,\"is_bot\":true}" :
                request.RequestUri.AbsolutePath.EndsWith("/getChat", StringComparison.Ordinal) ? "{\"type\":\"channel\"}" :
                System.Text.Json.JsonSerializer.Serialize(new { status, can_post_messages = canPost });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{\"ok\":true,\"result\":" + result + "}") });
        }
    }
    [Theory]
    [InlineData("{\"ok\":true,\"result\":{\"is_bot\":true}}", true)]
    [InlineData("{\"ok\":true,\"result\":{\"is_bot\":false}}", false)]
    [InlineData("not-json", false)]
    public async Task VerificationChecksBotIdentityWithoutSending(string payload, bool expected)
    {
        var handler = new StubHandler(200, payload);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Communications:Credentials:test-reference"] = "test-token" }).Build();
        var provider = new TelegramCommunicationProvider(new HttpClient(handler) { BaseAddress = new Uri("https://api.telegram.org/") }, configuration);
        var result = await provider.ValidateAsync("test-reference", CancellationToken.None);
        Assert.Equal(expected, result.Succeeded);
        Assert.Equal(HttpMethod.Get, handler.Method);
        Assert.EndsWith("/getMe", handler.Path);
    }
    [Theory]
    [InlineData(200, "{\"ok\":true,\"result\":{\"message_id\":42}}", true, false, null)]
    [InlineData(429, "{\"ok\":false,\"parameters\":{\"retry_after\":30}}", false, true, "Telegram_429")]
    [InlineData(401, "{\"ok\":false}", false, false, "Telegram_401")]
    [InlineData(200, "not-json", false, false, "Telegram_IndeterminateResponse")]
    public async Task ResponsesProduceSafeDeliveryOutcomes(int status, string body, bool success, bool transient, string? error)
    {
        var handler = new StubHandler(status, body);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        { ["Communications:Credentials:test-reference"] = "test-token" }).Build();
        var provider = new TelegramCommunicationProvider(new HttpClient(handler) { BaseAddress = new Uri("https://api.telegram.org/") }, configuration);
        var result = await provider.SendAsync(new CommunicationSendRequest("event-1", "test-reference", "-100123", "Title", "Message"), CancellationToken.None);
        Assert.Equal(success, result.Succeeded);
        Assert.Equal(transient, result.IsTransient);
        Assert.Equal(error, result.ErrorCode);
        if (success) Assert.Equal("42", result.ProviderMessageId);
        if (status == 429) Assert.Equal(TimeSpan.FromSeconds(30), result.RetryAfter);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task MissingCredentialNeverCallsProvider()
    {
        var handler = new StubHandler(200, "{}");
        var provider = new TelegramCommunicationProvider(new HttpClient(handler), new ConfigurationBuilder().Build());
        var result = await provider.SendAsync(new CommunicationSendRequest("event-1", "missing", "-100123", "Title", "Message"), CancellationToken.None);
        Assert.Equal("CredentialNotConfigured", result.ErrorCode);
        Assert.Equal(0, handler.Calls);
    }

    private sealed class StubHandler(int status, string body) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        public HttpMethod? Method { get; private set; }
        public string Path { get; private set; } = "";
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            Method = request.Method;
            Path = request.RequestUri!.AbsolutePath;
            return Task.FromResult(new HttpResponseMessage((HttpStatusCode)status) { Content = new StringContent(body) });
        }
    }
}
