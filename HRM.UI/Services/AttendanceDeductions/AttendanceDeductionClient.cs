using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading;
using System.Threading.Tasks;
using HRM.DTOs;
using HRM.DTOs.AttendanceDeductions;
using HRM.Models.AttendanceDeductions;

namespace HRM.UI.Services.AttendanceDeductions;

public sealed class AttendanceDeductionClient
{
    private readonly HttpClient _http;

    public AttendanceDeductionClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<AttendanceDeductionPolicy?> GetEffectivePolicyAsync(int organisationId, DateOnly? date = null, CancellationToken ct = default)
    {
        var url = $"/api/v1/attendance-deductions/policy?organisationId={organisationId}";
        if (date.HasValue) url += $"&effectiveDate={date.Value:yyyy-MM-dd}";
        return await _http.GetFromJsonAsync<AttendanceDeductionPolicy>(url, ct);
    }

    public async Task<List<AttendanceAdjustmentDto>> GetAdjustmentsAsync(
        int organisationId,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        string? verificationStatus = null,
        string? payrollStatus = null,
        CancellationToken ct = default)
    {
        var url = $"/api/v1/attendance-deductions/adjustments?organisationId={organisationId}";
        if (startDate.HasValue) url += $"&startDate={startDate.Value:yyyy-MM-dd}";
        if (endDate.HasValue) url += $"&endDate={endDate.Value:yyyy-MM-dd}";
        if (!string.IsNullOrWhiteSpace(verificationStatus)) url += $"&verificationStatus={Uri.EscapeDataString(verificationStatus)}";
        if (!string.IsNullOrWhiteSpace(payrollStatus)) url += $"&payrollStatus={Uri.EscapeDataString(payrollStatus)}";

        return await _http.GetFromJsonAsync<List<AttendanceAdjustmentDto>>(url, ct) ?? new();
    }

    public async Task<ServiceResult> VerifyAdjustmentAsync(long adjustmentId, int userId, string note, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("/api/v1/attendance-deductions/verify", new { AdjustmentId = adjustmentId, UserId = userId, Note = note }, ct);
        return await resp.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct) ?? ServiceResult.Fail("Verification request failed.");
    }

    public async Task<ServiceResult> ExcuseAdjustmentAsync(long adjustmentId, int userId, string note, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("/api/v1/attendance-deductions/excuse", new { AdjustmentId = adjustmentId, UserId = userId, Note = note }, ct);
        return await resp.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct) ?? ServiceResult.Fail("Excuse request failed.");
    }

    public async Task<ServiceResult> RejectAdjustmentAsync(long adjustmentId, int userId, string note, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("/api/v1/attendance-deductions/reject", new { AdjustmentId = adjustmentId, UserId = userId, Note = note }, ct);
        return await resp.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct) ?? ServiceResult.Fail("Rejection request failed.");
    }

    public async Task<List<AttendanceAdjustment>> EvaluateRangeAsync(int organisationId, DateOnly startDate, DateOnly endDate, int? jobId = null, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("/api/v1/attendance-deductions/evaluate-range", new
        {
            OrganisationId = organisationId,
            StartDate = startDate,
            EndDate = endDate,
            JobId = jobId
        }, ct);
        return await resp.Content.ReadFromJsonAsync<List<AttendanceAdjustment>>(cancellationToken: ct) ?? new();
    }

    public async Task<AttendanceDeductionCalculationPreviewDto?> PreviewCalculationAsync(int organisationId, int jobId, DateOnly workDate, int lateMinutes, string kind, CancellationToken ct = default)
    {
        var resp = await _http.PostAsJsonAsync("/api/v1/attendance-deductions/preview", new
        {
            OrganisationId = organisationId,
            JobId = jobId,
            WorkDate = workDate,
            LateMinutes = lateMinutes,
            Kind = kind
        }, ct);
        return await resp.Content.ReadFromJsonAsync<AttendanceDeductionCalculationPreviewDto>(cancellationToken: ct);
    }
}
