using System.Globalization;
using System.Net.Http.Json;
using HRM.WorkPlanning.Abstractions;

namespace HRM.UI.Services.WorkPlanning;

public partial interface IWorkforceScheduleApiClient
{
    Task<WorkforceReviewPage<WorkforceSessionReviewRow>> GetSessionsPageAsync(WorkforceReviewQuery query, CancellationToken ct = default);
    Task<WorkforceReviewPage<WorkforceLedgerReviewRow>> GetLedgerPageAsync(WorkforceReviewQuery query, CancellationToken ct = default);
}

public sealed partial class WorkforceScheduleApiClient
{
    public async Task<WorkforceReviewPage<WorkforceSessionReviewRow>> GetSessionsPageAsync(WorkforceReviewQuery query, CancellationToken ct = default) =>
        await client.GetFromJsonAsync<WorkforceReviewPage<WorkforceSessionReviewRow>>(ReviewUrl("sessions", query), ct) ?? throw new InvalidOperationException("Session review could not be loaded.");
    public async Task<WorkforceReviewPage<WorkforceLedgerReviewRow>> GetLedgerPageAsync(WorkforceReviewQuery query, CancellationToken ct = default) =>
        await client.GetFromJsonAsync<WorkforceReviewPage<WorkforceLedgerReviewRow>>(ReviewUrl("ledger", query), ct) ?? throw new InvalidOperationException("Ledger review could not be loaded.");
    private static string ReviewUrl(string resource, WorkforceReviewQuery query)
    {
        var url = FormattableString.Invariant($"/api/v1/workforce-schedule/{resource}/page?page={query.Page}&pageSize={query.PageSize}");
        if (query.From.HasValue) url += "&from=" + query.From.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        if (query.To.HasValue) url += "&to=" + query.To.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return url;
    }
}
