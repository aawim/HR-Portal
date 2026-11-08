using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HRM.DTOs;
using HRM.DTOs.AttendanceDeductions;
using HRM.Models.AttendanceDeductions;

namespace HRM.Services.Interfaces;

public interface IAttendanceDeductionService
{
    Task<AttendanceDeductionPolicy> GetEffectivePolicyAsync(int organisationId, DateOnly workDate);
    Task<AttendanceAdjustment?> EvaluateDailyDeductionAsync(int organisationId, int individualId, int jobId, DateOnly workDate, CancellationToken ct = default);
    Task<List<AttendanceAdjustment>> EvaluateRangeAsync(int organisationId, int? individualId, int? jobId, DateOnly fromDate, DateOnly toDate, CancellationToken ct = default);
    Task<List<AttendanceAdjustmentDto>> GetAdjustmentsAsync(int organisationId, DateOnly? fromDate = null, DateOnly? toDate = null, string? verificationStatus = null, string? payrollStatus = null);
    Task<ServiceResult> VerifyAdjustmentAsync(long adjustmentId, int userId, string comments);
    Task<ServiceResult> ExcuseAdjustmentAsync(long adjustmentId, int userId, string reason);
    Task<ServiceResult> RejectAdjustmentAsync(long adjustmentId, int userId, string reason);
    Task<List<AttendanceDeductionPolicyDto>> GetPoliciesAsync(int organisationId);
    Task<ServiceResult> SavePolicyAsync(AttendanceDeductionPolicy policy, int userId);
    Task<AttendanceDeductionCalculationPreviewDto> PreviewCalculationAsync(int organisationId, int jobId, DateOnly workDate, int lateMinutes, string kind);
}
