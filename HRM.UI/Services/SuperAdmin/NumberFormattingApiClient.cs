using System.Net.Http.Json;
using HRM.Application.Contracts.SuperAdmin;
using HRM.DTOs;

namespace HRM.UI.Services.SuperAdmin;

public sealed class NumberFormattingApiClient(HttpClient http) : INumberFormattingApiClient
{
    public async Task<NumberFormattingWorkspaceDto> LoadAsync(int? organisationId = null, CancellationToken ct = default)
    {
        var url = organisationId is > 0
            ? $"api/v1/superadmin/number-formatting/load?organisationId={organisationId.Value}"
            : "api/v1/superadmin/number-formatting/load";
        return await http.GetFromJsonAsync<NumberFormattingWorkspaceDto>(url, ct)
               ?? new NumberFormattingWorkspaceDto();
    }

    public async Task<ServiceResult> SaveOrganisationFormatAsync(OrganisationNumberFormatDto model, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/v1/superadmin/number-formatting/save-organisation-format", model, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct)
               ?? ServiceResult.Fail("Could not save organisation number format.");
    }

    public async Task<ServiceResult> DeleteOrganisationFormatAsync(int organisationId, int numberFormatId, CancellationToken ct = default)
    {
        var response = await http.PostAsync(
            $"api/v1/superadmin/number-formatting/delete-organisation-format?organisationId={organisationId}&numberFormatId={numberFormatId}",
            null, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct)
               ?? ServiceResult.Fail("Could not delete organisation number format.");
    }

    public async Task<ServiceResult> SaveGlobalSequenceAsync(GlobalSequenceNumberDto model, CancellationToken ct = default)
    {
        var response = await http.PostAsJsonAsync("api/v1/superadmin/number-formatting/save-global-sequence", model, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct)
               ?? ServiceResult.Fail("Could not save global sequence.");
    }
}
