using System.Net;
using System.Net.Http.Json;
using HRM.Application.Contracts.Signatures;
using HRM.DTOs;

namespace HRM.UI.Services.Signatures;

public interface IDocumentSignatureApiClient
{
    Task<UserSignatureDto?> GetMySignatureAsync(CancellationToken ct = default);
    Task<ServiceResult> SaveMySignatureAsync(string signatureDataUrl, string signatureStyle = "Canvas", CancellationToken ct = default);
    Task<ServiceResult> SetSignatureStyleAsync(string signatureStyle, CancellationToken ct = default);
    Task<ServiceResult> SetSignaturePinAsync(string pin, string? currentPin = null, CancellationToken ct = default);
    Task<ServiceResult> VerifySignaturePinAsync(string pin, CancellationToken ct = default);
    Task<ServiceResult> DeleteMySignatureAsync(CancellationToken ct = default);
    Task<IReadOnlyList<DocumentSignatureDto>> GetDocumentSignaturesAsync(string documentType, string documentId, CancellationToken ct = default);
    Task<IReadOnlyList<DocumentSignatureDto>> GetPublicDocumentSignaturesAsync(string documentType, string documentId, CancellationToken ct = default);
    Task<ServiceResult> SignDocumentAsync(SignDocumentInput input, CancellationToken ct = default);
    Task<ServiceResult> RevokeSignatureAsync(int signatureId, string reason, CancellationToken ct = default);
    Task<VerifySignatureDto?> VerifySignatureAsync(string code, CancellationToken ct = default);
}

public sealed class DocumentSignatureApiClient(HttpClient http, HttpClient? publicHttp = null) : IDocumentSignatureApiClient
{
    private HttpClient PublicClient => publicHttp ?? http;

    public async Task<UserSignatureDto?> GetMySignatureAsync(CancellationToken ct = default)
    {
        using var r = await http.GetAsync("/api/v1/signatures/me", ct);
        if (r.StatusCode is HttpStatusCode.NoContent or HttpStatusCode.NotFound) return null;
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<UserSignatureDto>(cancellationToken: ct);
    }

    public async Task<ServiceResult> SaveMySignatureAsync(string signatureDataUrl, string signatureStyle = "Canvas", CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync("/api/v1/signatures/me", new SaveUserSignatureInput(signatureDataUrl, signatureStyle), ct);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct) ?? ServiceResult.Fail("No response returned.");
    }

    public async Task<ServiceResult> SetSignatureStyleAsync(string signatureStyle, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync("/api/v1/signatures/me/style", new SetSignatureStyleInput(signatureStyle), ct);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct) ?? ServiceResult.Fail("No response returned.");
    }

    public async Task<ServiceResult> SetSignaturePinAsync(string pin, string? currentPin = null, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync("/api/v1/signatures/me/pin", new SetSignaturePinInput(pin, currentPin), ct);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct) ?? ServiceResult.Fail("No response returned.");
    }

    public async Task<ServiceResult> VerifySignaturePinAsync(string pin, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync("/api/v1/signatures/me/verify-pin", new VerifySignaturePinInput(pin), ct);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct) ?? ServiceResult.Fail("No response returned.");
    }

    public async Task<ServiceResult> DeleteMySignatureAsync(CancellationToken ct = default)
    {
        using var r = await http.DeleteAsync("/api/v1/signatures/me", ct);
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct) ?? ServiceResult.Fail("No response returned.");
    }

    public async Task<IReadOnlyList<DocumentSignatureDto>> GetDocumentSignaturesAsync(string documentType, string documentId, CancellationToken ct = default)
    {
        var url = $"/api/v1/signatures/document?documentType={Uri.EscapeDataString(documentType)}&documentId={Uri.EscapeDataString(documentId)}";
        return await http.GetFromJsonAsync<List<DocumentSignatureDto>>(url, ct) ?? [];
    }

    public async Task<IReadOnlyList<DocumentSignatureDto>> GetPublicDocumentSignaturesAsync(string documentType, string documentId, CancellationToken ct = default)
    {
        var url = $"/api/v1/signatures/public/document?documentType={Uri.EscapeDataString(documentType)}&documentId={Uri.EscapeDataString(documentId)}";
        return await PublicClient.GetFromJsonAsync<List<DocumentSignatureDto>>(url, ct) ?? [];
    }

    public async Task<ServiceResult> SignDocumentAsync(SignDocumentInput input, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync("/api/v1/signatures/sign", input, ct);
        if (r.StatusCode == HttpStatusCode.Forbidden) return ServiceResult.Fail("You do not have permission to sign this document.");
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct) ?? ServiceResult.Fail("No response returned.");
    }

    public async Task<ServiceResult> RevokeSignatureAsync(int signatureId, string reason, CancellationToken ct = default)
    {
        using var r = await http.PostAsJsonAsync($"/api/v1/signatures/{signatureId}/revoke", new RevokeDocumentSignatureInput(reason), ct);
        if (r.StatusCode == HttpStatusCode.Forbidden) return ServiceResult.Fail("Only the original signer or an Administrator can revoke this signature.");
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct) ?? ServiceResult.Fail("No response returned.");
    }

    public async Task<VerifySignatureDto?> VerifySignatureAsync(string code, CancellationToken ct = default)
    {
        using var r = await PublicClient.GetAsync($"/api/v1/signatures/verify/{Uri.EscapeDataString(code.Trim())}", ct);
        if (r.StatusCode == HttpStatusCode.NotFound) return null;
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<VerifySignatureDto>(cancellationToken: ct);
    }
}
