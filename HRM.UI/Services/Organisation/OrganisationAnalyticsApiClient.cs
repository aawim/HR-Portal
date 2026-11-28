using System.Net;
using System.Net.Http.Json;
using HRM.Application.Contracts.Organisation;

namespace HRM.UI.Services.Organisation;

public interface IOrganisationAnalyticsApiClient
{
    Task<OrganisationAnalyticsDto?> GetAnalyticsAsync(int organisationId, int? departmentId = null, int? jobTypeId = null, int months = 6, CancellationToken ct = default);
}

public sealed class OrganisationAnalyticsApiClient(HttpClient http) : IOrganisationAnalyticsApiClient
{
    public async Task<OrganisationAnalyticsDto?> GetAnalyticsAsync(int organisationId, int? departmentId = null, int? jobTypeId = null, int months = 6, CancellationToken ct = default)
    {
        var qs = new List<string> { $"months={months}" };
        if (departmentId is > 0) qs.Add($"departmentId={departmentId.Value}");
        if (jobTypeId is > 0) qs.Add($"jobTypeId={jobTypeId.Value}");
        var url = $"/api/v1/organisation-analytics/{organisationId}?{string.Join("&", qs)}";

        using var r = await http.GetAsync(url, ct);
        if (r.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.Forbidden) return null;
        r.EnsureSuccessStatusCode();
        return await r.Content.ReadFromJsonAsync<OrganisationAnalyticsDto>(cancellationToken: ct);
    }
}
