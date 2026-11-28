using System.Net.Http.Json;
using HRM.Application.Contracts.Positions;
using HRM.DTOs;

namespace HRM.UI.Services.Positions;

public interface IPositionContractApiClient
{
    Task<List<PositionContractDto>> GetStaffContractsAsync(int staffId, CancellationToken ct = default);
    Task<List<PositionContractDto>> GetMyContractsAsync(CancellationToken ct = default);
    Task<ServiceResult> RegisterUploadedAsync(RegisterPositionContractInput input, CancellationToken ct = default);
    Task<ServiceResult> AcceptAsync(long documentId, CancellationToken ct = default);
    Task<ServiceResult> RequestUnlockAsync(RequestContractUnlockInput input, CancellationToken ct = default);
    Task<ServiceResult> RespondUnlockAsync(RespondContractUnlockInput input, CancellationToken ct = default);
}

public sealed class PositionContractApiClient(HttpClient http) : IPositionContractApiClient
{
    public async Task<List<PositionContractDto>> GetStaffContractsAsync(int staffId, CancellationToken ct = default)
        => await http.GetFromJsonAsync<List<PositionContractDto>>($"/api/v1/position-contracts/staff/{staffId}", ct) ?? [];

    public async Task<List<PositionContractDto>> GetMyContractsAsync(CancellationToken ct = default)
        => await http.GetFromJsonAsync<List<PositionContractDto>>("/api/v1/position-contracts/my", ct) ?? [];

    public Task<ServiceResult> RegisterUploadedAsync(RegisterPositionContractInput input, CancellationToken ct = default)
        => PostAsync("/api/v1/position-contracts/register-uploaded", input, ct);

    public Task<ServiceResult> AcceptAsync(long documentId, CancellationToken ct = default)
        => PostAsync($"/api/v1/position-contracts/{documentId}/accept", new { }, ct);

    public Task<ServiceResult> RequestUnlockAsync(RequestContractUnlockInput input, CancellationToken ct = default)
        => PostAsync("/api/v1/position-contracts/request-unlock", input, ct);

    public Task<ServiceResult> RespondUnlockAsync(RespondContractUnlockInput input, CancellationToken ct = default)
        => PostAsync("/api/v1/position-contracts/respond-unlock", input, ct);

    private async Task<ServiceResult> PostAsync<T>(string path, T payload, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync(path, payload, ct);
        if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            return ServiceResult.Fail("You do not have permission to perform this contract action.");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct)
            ?? ServiceResult.Fail("No response returned from the server.");
    }
}
