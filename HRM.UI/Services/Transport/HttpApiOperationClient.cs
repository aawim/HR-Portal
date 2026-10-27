using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HRM.Application.Transport;

namespace HRM.UI.Services.Transport;

/// <summary>The supplied client must use the current server-side staff session's authenticated handler.</summary>
public sealed class HttpApiOperationClient(HttpClient http) : IApiOperationClient
{
    public async Task<T> InvokeAsync<T>(Type contract, string method, IReadOnlyList<ApiArgument> arguments, CancellationToken ct = default)
    {
        var response = await SendAsync(contract, method, arguments, ct);
        if (response.Value is null) return default!;
        return response.Value.Value.Deserialize<T>(ApiOperationKeys.Json)!;
    }

    public async Task InvokeAsync(Type contract, string method, IReadOnlyList<ApiArgument> arguments, CancellationToken ct = default)
        => await SendAsync(contract, method, arguments, ct);

    private async Task<ApiOperationResponse> SendAsync(Type contract, string method, IReadOnlyList<ApiArgument> arguments, CancellationToken ct)
    {
        var descriptor = new ApiOperationDescriptor(contract);
        var operation = descriptor.Resolve(method, arguments);
        var request = new ApiOperationRequest(descriptor.Key, ApiOperationKeys.Method(operation),
            arguments.Select(a => JsonSerializer.SerializeToElement(a.Value, a.DeclaredType, ApiOperationKeys.Json)).ToArray());
        using var response = await http.PostAsJsonAsync("api/operations", request, ApiOperationKeys.Json, ct);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new UnauthorizedAccessException("Your session has ended. Please sign in again.");
        if (response.StatusCode == HttpStatusCode.Forbidden)
            throw new UnauthorizedAccessException("You do not have permission to perform this action.");
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<ApiOperationResponse>(ApiOperationKeys.Json, ct)
            ?? throw new InvalidOperationException("The server returned an empty response.");
        if (payload.Error is not null)
            throw new ApiOperationException(payload.Error.Code, payload.Error.Message);
        // Notification delivery is handled by the UI session adapter after a successful operation.
        return payload;
    }
}

public sealed class ApiOperationException(string code, string message) : InvalidOperationException(message)
{
    public string Code { get; } = code;
}
