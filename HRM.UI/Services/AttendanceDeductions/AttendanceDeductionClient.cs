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
        var workDate = date ?? DateOnly.FromDateTime(DateTime.UtcNow);
        var url = $"/api/v1/attendance-deduction/get-effective-policy?organisationId={organisationId}&workDate={workDate:yyyy-MM-dd}";
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
        var url = $"/api/v1/attendance-deduction/get-adjustments?organisationId={organisationId}";
        if (startDate.HasValue) url += $"&fromDate={startDate.Value:yyyy-MM-dd}";
        if (endDate.HasValue) url += $"&toDate={endDate.Value:yyyy-MM-dd}";
        if (!string.IsNullOrWhiteSpace(verificationStatus)) url += $"&verificationStatus={Uri.EscapeDataString(verificationStatus)}";
        if (!string.IsNullOrWhiteSpace(payrollStatus)) url += $"&payrollStatus={Uri.EscapeDataString(payrollStatus)}";

        return await _http.GetFromJsonAsync<List<AttendanceAdjustmentDto>>(url, ct) ?? new();
    }

    public async Task<ServiceResult> VerifyAdjustmentAsync(long adjustmentId, int userId, string note, CancellationToken ct = default)
    {
        using var resp = await _http.PostAsJsonAsync("/api/v1/attendance-deduction/verify-adjustment", new { adjustmentId, userId, comments = note }, ct);
        if (!resp.IsSuccessStatusCode) return ServiceResult.Fail($"Verification request failed ({(int)resp.StatusCode}).");
        return await resp.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct) ?? ServiceResult.Fail("Verification request failed.");
    }

    public async Task<ServiceResult> ExcuseAdjustmentAsync(long adjustmentId, int userId, string note, CancellationToken ct = default)
    {
        using var resp = await _http.PostAsJsonAsync("/api/v1/attendance-deduction/excuse-adjustment", new { adjustmentId, userId, reason = note }, ct);
        if (!resp.IsSuccessStatusCode) return ServiceResult.Fail($"Excuse request failed ({(int)resp.StatusCode}).");
        return await resp.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct) ?? ServiceResult.Fail("Excuse request failed.");
    }

    public async Task<ServiceResult> RejectAdjustmentAsync(long adjustmentId, int userId, string note, CancellationToken ct = default)
    {
        using var resp = await _http.PostAsJsonAsync("/api/v1/attendance-deduction/reject-adjustment", new { adjustmentId, userId, reason = note }, ct);
        if (!resp.IsSuccessStatusCode) return ServiceResult.Fail($"Rejection request failed ({(int)resp.StatusCode}).");
        return await resp.Content.ReadFromJsonAsync<ServiceResult>(cancellationToken: ct) ?? ServiceResult.Fail("Rejection request failed.");
    }

    public async Task<List<AttendanceAdjustment>> EvaluateRangeAsync(int organisationId, DateOnly startDate, DateOnly endDate, int? jobId = null, CancellationToken ct = default)
    {
        using var resp = await _http.PostAsJsonAsync("/api/v1/attendance-deduction/evaluate-range", new
        {
            organisationId,
            individualId = (int?)null,
            jobId,
            fromDate = startDate,
            toDate = endDate
        }, ct);
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadFromJsonAsync<List<AttendanceAdjustment>>(cancellationToken: ct) ?? new();
    }

    public async Task<AttendanceDeductionCalculationPreviewDto?> PreviewCalculationAsync(int organisationId, int jobId, DateOnly workDate, int lateMinutes, string kind, CancellationToken ct = default)
    {
        var url = $"/api/v1/attendance-deduction/preview-calculation?organisationId={organisationId}&jobId={jobId}&workDate={workDate:yyyy-MM-dd}&lateMinutes={lateMinutes}&kind={Uri.EscapeDataString(kind)}";
        return await _http.GetFromJsonAsync<AttendanceDeductionCalculationPreviewDto>(url, ct);
    }
}
