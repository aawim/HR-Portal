using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HRM.Communications.Contracts;
using Microsoft.Extensions.Configuration;

namespace HRM.Communications.Infrastructure;

/// <summary>Official Telegram Bot API adapter. Tokens are resolved at runtime, never stored in SQL or logged.</summary>
public sealed class TelegramCommunicationProvider(HttpClient client, IConfiguration configuration) : ICommunicationProvider, ICommunicationConnectionValidator
{
    public string ProviderCode => "TELEGRAM";

    public async Task<ProviderValidationResult> ValidateDestinationAsync(string credentialReference, string destination, CancellationToken cancellationToken, int organisationId = 0)
    {
        var token = CommunicationCredentials.Read(configuration, organisationId, credentialReference);
        if (string.IsNullOrWhiteSpace(token)) return new(false, "CredentialNotConfigured");
        try
        {
            var prefix = $"bot{Uri.EscapeDataString(token)}/";
            var bot = await ReadResultAsync(prefix + "getMe", cancellationToken);
            if (!bot.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.Number || !id.TryGetInt64(out var botId) || !IsTrue(bot, "is_bot"))
                return new(false, "Telegram_InvalidBotResponse");
            var chat = await ReadResultAsync(prefix + "getChat?chat_id=" + Uri.EscapeDataString(destination), cancellationToken);
            var type = chat.TryGetProperty("type", out var chatType) && chatType.ValueKind == JsonValueKind.String ? chatType.GetString() : null;
            if (type is not ("channel" or "group" or "supergroup")) return new(false, "Telegram_UnsupportedDestinationType");
            var member = await ReadResultAsync(prefix + "getChatMember?chat_id=" + Uri.EscapeDataString(destination) + "&user_id=" + botId, cancellationToken);
            var status = member.TryGetProperty("status", out var state) && state.ValueKind == JsonValueKind.String ? state.GetString() : null;
            var allowed = status == "creator" || (status == "administrator" && (type != "channel" || IsTrue(member, "can_post_messages")));
            if (type != "channel")
            {
                allowed |= status == "restricted" && IsTrue(member, "is_member") && IsTrue(member, "can_send_messages");
                allowed |= status == "member" && chat.TryGetProperty("permissions", out var permissions) && permissions.ValueKind == JsonValueKind.Object && IsTrue(permissions, "can_send_messages");
            }
            return allowed ? new(true) : new(false, "Telegram_PostingPermissionUnavailable");
        }
        catch (JsonException) { return new(false, "Telegram_InvalidDestinationResponse"); }
        catch (HttpRequestException) { return new(false, "Telegram_DestinationUnavailable"); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return new(false, "Telegram_ValidationTimeout"); }
    }

    private static bool IsTrue(JsonElement value, string name) => value.TryGetProperty(name, out var property) && property.ValueKind == JsonValueKind.True;

    private async Task<JsonElement> ReadResultAsync(string path, CancellationToken token)
    {
        using var response = await client.GetAsync(path, token);
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: token);
        if (payload.ValueKind != JsonValueKind.Object || !IsTrue(payload, "ok") || !payload.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Object)
            throw new JsonException("Invalid Telegram response.");
        return result;
    }

    public async Task<ProviderValidationResult> ValidateAsync(string credentialReference, CancellationToken cancellationToken, int organisationId = 0)
    {
        var token = CommunicationCredentials.Read(configuration, organisationId, credentialReference);
        if (string.IsNullOrWhiteSpace(token)) return new(false, "CredentialNotConfigured");
        try
        {
            using var response = await client.GetAsync($"bot{Uri.EscapeDataString(token)}/getMe", cancellationToken);
            if (!response.IsSuccessStatusCode) return new(false, "Telegram_" + (int)response.StatusCode);
            var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            return payload.ValueKind == JsonValueKind.Object && payload.TryGetProperty("ok", out var ok) && ok.ValueKind == JsonValueKind.True &&
                payload.TryGetProperty("result", out var result) && result.ValueKind == JsonValueKind.Object && result.TryGetProperty("is_bot", out var bot) && bot.ValueKind == JsonValueKind.True
                ? new(true) : new(false, "Telegram_InvalidBotResponse");
        }
        catch (JsonException) { return new(false, "Telegram_InvalidBotResponse"); }
        catch (HttpRequestException) { return new(false, "Telegram_ConnectionUnavailable"); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested) { return new(false, "Telegram_ValidationTimeout"); }
    }

    public async Task<ProviderSendResult> SendAsync(CommunicationSendRequest request, CancellationToken cancellationToken)
    {
        var token = CommunicationCredentials.Read(configuration, request.OrganisationId, request.CredentialReference);
        if (string.IsNullOrWhiteSpace(token)) return new(false, ErrorCode: "CredentialNotConfigured");

        try
        {
            using var response = await client.PostAsJsonAsync($"bot{Uri.EscapeDataString(token)}/sendMessage", new { chat_id = request.Destination, text = request.Body }, cancellationToken);
            var payload = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            if (response.IsSuccessStatusCode && payload.TryGetProperty("ok", out var ok) && ok.GetBoolean())
            {
                var messageId = payload.TryGetProperty("result", out var result) && result.TryGetProperty("message_id", out var id) ? id.GetInt64().ToString() : null;
                return new(true, messageId);
            }

            TimeSpan? retryAfter = response.StatusCode == (HttpStatusCode)429 && payload.TryGetProperty("parameters", out var parameters) && parameters.TryGetProperty("retry_after", out var seconds)
                ? TimeSpan.FromSeconds(seconds.GetInt32()) : null;
            return new(false, ErrorCode: "Telegram_" + (int)response.StatusCode, IsTransient: response.StatusCode == (HttpStatusCode)429 || (int)response.StatusCode >= 500, RetryAfter: retryAfter);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            // A timeout can occur after Telegram accepted the message. Do not retry automatically.
            return new(false, ErrorCode: "Telegram_IndeterminateTimeout");
        }
        catch (JsonException)
        {
            return new(false, ErrorCode: "Telegram_IndeterminateResponse");
        }
        catch (HttpRequestException)
        {
            // Network failure is also ambiguous from the provider's perspective.
            return new(false, ErrorCode: "Telegram_IndeterminateNetworkFailure");
        }
    }
}
