using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HRM.Application.Transport;
using HRM.UI.Authentication;

namespace HRM.UI.Services.Transport;

/// <summary>The supplied client must use a per-user authenticated handler, including CSRF protection.</summary>
public sealed class HttpApiOperationClient(HttpClient http, Action? sessionEnded = null) : IApiOperationClient, IDisposable
{
    public void Dispose() => http.Dispose();
    public async Task<T> InvokeAsync<T>(Type contract, string method, IReadOnlyList<ApiArgument> arguments, CancellationToken ct = default)
    {
        using var response = await SendAsync(contract, method, arguments, ct);
        if (response.StatusCode == HttpStatusCode.NoContent) return default!;
        return (await response.Content.ReadFromJsonAsync<T>(ApiOperationKeys.Json, ct))!;
    }

    public async Task InvokeAsync(Type contract, string method, IReadOnlyList<ApiArgument> arguments, CancellationToken ct = default)
    {
        using var response = await SendAsync(contract, method, arguments, ct);
    }

    private async Task<HttpResponseMessage> SendAsync(Type contract, string method, IReadOnlyList<ApiArgument> arguments, CancellationToken ct)
    {
        var operation = new ApiOperationDescriptor(contract).Resolve(method, arguments);
        var endpoint = ApiEndpointCatalog.Resolve(contract, operation);
        var parameters = operation.GetParameters().Where(p => p.ParameterType != typeof(CancellationToken)).ToArray();
        var path = endpoint.Path.TrimStart('/');
        using var request = new HttpRequestMessage(new HttpMethod(endpoint.Verb), path);
        if (endpoint.Verb == "GET")
        {
            var query = arguments.Select((a, i) => (Argument: a, Name: parameters[i].Name!))
                .Where(a => a.Argument.Value is not null)
                .Select(a => Uri.EscapeDataString(a.Name) + "=" + Uri.EscapeDataString(QueryValue(a.Argument.Value!)));
            var encoded = string.Join("&", query);
            if (encoded.Length > 0) request.RequestUri = new Uri(path + "?" + encoded, UriKind.Relative);
        }
        else if (parameters.Length > 0)
        {
            var body = arguments.Select((a, i) => new { Name = parameters[i].Name!, Value = JsonSerializer.SerializeToElement(a.Value, a.DeclaredType, ApiOperationKeys.Json) })
                .ToDictionary(a => a.Name, a => a.Value);
            request.Content = JsonContent.Create(body, options: ApiOperationKeys.Json);
        }
        var response = await http.SendAsync(request, ct);
        if (response.IsSuccessStatusCode) return response;
        using (response)
        {
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                if (response.Headers.Contains(CircuitBackendHandler.RecoveryHeader)) sessionEnded?.Invoke();
                throw new UnauthorizedAccessException("Your session has ended. Please sign in again.");
            }
            if (response.StatusCode == HttpStatusCode.Forbidden)
                throw new UnauthorizedAccessException("You do not have permission to perform this action.");
            var code = ((int)response.StatusCode).ToString(CultureInfo.InvariantCulture);
            // Do not render arbitrary upstream HTML, exception details, or model-state values in the UI.
            throw new ApiOperationException(code, response.StatusCode switch
            {
                HttpStatusCode.BadRequest => "Check the request fields and try again.",
                HttpStatusCode.NotFound => "The requested record is unavailable.",
                HttpStatusCode.Conflict => "This action could not be completed. Refresh and try again, or contact your organisation's HR team.",
                _ => "This action could not be completed right now. Please try again."
            });
        }
    }

    private static string QueryValue(object value) => value switch
    {
        DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset date => date.ToString("O", CultureInfo.InvariantCulture),
        DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        TimeOnly time => time.ToString("O", CultureInfo.InvariantCulture),
        bool flag => flag ? "true" : "false",
        IFormattable number => number.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString()!
    };
}

public sealed class ApiOperationException(string code, string message) : InvalidOperationException(message)
{
    public string Code { get; } = code;
}
