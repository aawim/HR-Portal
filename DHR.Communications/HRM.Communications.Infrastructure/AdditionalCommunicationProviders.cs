using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using HRM.Communications.Contracts;
using Microsoft.Extensions.Configuration;

namespace HRM.Communications.Infrastructure;

public sealed class FacebookPageCommunicationProvider(HttpClient client, IConfiguration configuration) : ICommunicationProvider
{
    public string ProviderCode => "FACEBOOK_PAGE";
    public Task<ProviderSendResult> SendAsync(CommunicationSendRequest request, CancellationToken ct) =>
        MetaDelivery.SendAsync(client, configuration, request, "feed", new { message = request.Body }, false, ct);
}

public sealed class WhatsAppCommunicationProvider(HttpClient client, IConfiguration configuration) : ICommunicationProvider
{
    public string ProviderCode => "WHATSAPP";
    public Task<ProviderSendResult> SendAsync(CommunicationSendRequest request, CancellationToken ct)
    {
        WhatsAppTemplateConfiguration? options;
        try { options = JsonSerializer.Deserialize<WhatsAppTemplateConfiguration>(request.ConfigurationJson ?? "{}", new JsonSerializerOptions(JsonSerializerDefaults.Web)); }
        catch (JsonException) { return Task.FromResult(new ProviderSendResult(false, ErrorCode: "InvalidWhatsAppConfiguration")); }
        if (options is null || !MetaDelivery.NumericId(options.PhoneNumberId) || !Regex.IsMatch(options.TemplateName ?? "", "^[a-z0-9_]{1,512}$") || !Regex.IsMatch(options.Language ?? "", "^[a-z]{2,3}(_[A-Z]{2})?$"))
            return Task.FromResult(new ProviderSendResult(false, ErrorCode: "InvalidWhatsAppConfiguration"));
        if (!Regex.IsMatch(request.Destination, "^[1-9][0-9]{6,14}$")) return Task.FromResult(new ProviderSendResult(false, ErrorCode: "InvalidRecipientNumber"));
        if (string.IsNullOrWhiteSpace(request.Body) || request.Body.Length > 1024) return Task.FromResult(new ProviderSendResult(false, ErrorCode: "InvalidWhatsAppBody"));
        // Initial support is an approved template with one positional text body parameter.
        var payload = new { messaging_product = "whatsapp", to = request.Destination, type = "template", template = new {
            name = options.TemplateName, language = new { code = options.Language },
            components = new[] { new { type = "body", parameters = new[] { new { type = "text", text = request.Body } } } } } };
        return MetaDelivery.SendAsync(client, configuration, request with { Destination = options.PhoneNumberId! }, "messages", payload, true, ct);
    }
}

public sealed record WhatsAppTemplateConfiguration(string? PhoneNumberId, string? TemplateName, string? Language);

internal static class MetaDelivery
{
    internal static bool NumericId(string? value) => value is { Length: > 0 and <= 64 } && value.All(char.IsAsciiDigit);
    internal static async Task<ProviderSendResult> SendAsync(HttpClient client, IConfiguration configuration, CommunicationSendRequest request, string endpoint, object payload, bool whatsapp, CancellationToken ct)
    {
        var version = configuration["Communications:Meta:GraphApiVersion"];
        var token = CommunicationCredentials.Read(configuration, request.OrganisationId, request.CredentialReference);
        if (string.IsNullOrWhiteSpace(token)) return new(false, ErrorCode: "CredentialNotConfigured");
        if (!Regex.IsMatch(version ?? "", "^v[0-9]{1,3}\\.[0-9]{1,2}$") || !NumericId(request.Destination)) return new(false, ErrorCode: "InvalidMetaConfiguration");
        try
        {
            using var message = new HttpRequestMessage(HttpMethod.Post, $"https://graph.facebook.com/{version}/{request.Destination}/{endpoint}") { Content = JsonContent.Create(payload) };
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await client.SendAsync(message, ct);
            if (!response.IsSuccessStatusCode)
                return new(false, ErrorCode: "Meta_" + (int)response.StatusCode, IsTransient: response.StatusCode == HttpStatusCode.TooManyRequests,
                    RetryAfter: response.Headers.RetryAfter?.Delta);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            string? id = null;
            if (result.ValueKind == JsonValueKind.Object)
            {
                if (!whatsapp && result.TryGetProperty("id", out var post) && post.ValueKind == JsonValueKind.String) id = post.GetString();
                if (whatsapp && result.TryGetProperty("messages", out var messages) && messages.ValueKind == JsonValueKind.Array && messages.GetArrayLength() > 0 && messages[0].ValueKind == JsonValueKind.Object && messages[0].TryGetProperty("id", out var sent) && sent.ValueKind == JsonValueKind.String) id = sent.GetString();
            }
            return string.IsNullOrWhiteSpace(id) ? new(false, ErrorCode: "Meta_IndeterminateResponse") : new(true, id);
        }
        catch (JsonException) { return new(false, ErrorCode: "Meta_IndeterminateResponse"); }
        catch (HttpRequestException) { return new(false, ErrorCode: "Meta_IndeterminateNetworkFailure"); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return new(false, ErrorCode: "Meta_IndeterminateTimeout"); }
    }
}

public sealed class MicrosoftTeamsCommunicationProvider(HttpClient client, IConfiguration configuration) : ICommunicationProvider
{
    public string ProviderCode => "MSTEAMS";
    public static bool IsSupportedWorkflowUrl(string? value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme == "https" && uri.IsDefaultPort &&
        string.IsNullOrEmpty(uri.UserInfo) && (uri.Host.EndsWith(".environment.api.powerplatform.com", StringComparison.OrdinalIgnoreCase) || uri.Host.EndsWith(".logic.azure.com", StringComparison.OrdinalIgnoreCase)) &&
        (uri.AbsolutePath.Contains("/workflows/", StringComparison.Ordinal) || uri.AbsolutePath.Contains("/automations/", StringComparison.Ordinal));

    public async Task<ProviderSendResult> SendAsync(CommunicationSendRequest request, CancellationToken ct)
    {
        var url = CommunicationCredentials.Read(configuration, request.OrganisationId, request.CredentialReference);
        if (!IsSupportedWorkflowUrl(url)) return new(false, ErrorCode: "InvalidTeamsWorkflowConfiguration");
        try
        {
            var payload = new { type = "message", attachments = new[] { new { contentType = "application/vnd.microsoft.card.adaptive", contentUrl = (string?)null,
                content = new { type = "AdaptiveCard", version = "1.2", body = new[] { new { type = "TextBlock", text = request.Body, wrap = true } } } } } };
            using var response = await client.PostAsJsonAsync(url, payload, ct);
            return response.IsSuccessStatusCode ? new(true) : new(false, ErrorCode: "Teams_" + (int)response.StatusCode,
                IsTransient: response.StatusCode == HttpStatusCode.TooManyRequests, RetryAfter: response.Headers.RetryAfter?.Delta);
        }
        catch (HttpRequestException) { return new(false, ErrorCode: "Teams_IndeterminateNetworkFailure"); }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested) { return new(false, ErrorCode: "Teams_IndeterminateTimeout"); }
    }
}
