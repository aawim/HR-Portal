using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HRM.Api.Infrastructure;
using HRM.DTOs;
using HRM.DTOs.AttendanceDeductions;
using HRM.Models.AttendanceDeductions;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace HRM.Api.Controllers.V1;

[Route("api/v1/attendance-deductions")]
public sealed class AttendanceDeductionController(
    IAttendanceDeductionService service,
    IUserAccessService access,
    ILogger<AttendanceDeductionController> logger) : HrmApiController
{
    private async Task<int> ResolveOrganisationIdAsync(int organisationId)
    {
        if (organisationId > 0) return organisationId;
        try
        {
            var currentOrg = await access.GetCurrentOrganisationIdAsync();
            if (currentOrg > 0) return currentOrg;
        }
        catch { }

        var context = await access.GetContextAsync();
        return context?.Organisation?.OrganisationBusinessEntityId
            ?? context?.ActiveJob?.OrganisationId
            ?? 1;
    }

    [HttpGet("policy")]
    public async Task<ActionResult<AttendanceDeductionPolicy>> GetEffectivePolicy(
        [FromQuery] int organisationId = 0,
        [FromQuery] DateOnly? effectiveDate = null)
    {
        try
        {
            var orgId = await ResolveOrganisationIdAsync(organisationId);
            var date = effectiveDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var policy = await service.GetEffectivePolicyAsync(orgId, date);
            return Ok(policy);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load effective attendance deduction policy for OrganisationId={OrgId}", organisationId);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpGet("policies")]
    public async Task<ActionResult<List<AttendanceDeductionPolicyDto>>> GetPolicies([FromQuery] int organisationId = 0)
    {
        var orgId = await ResolveOrganisationIdAsync(organisationId);
        var policies = await service.GetPoliciesAsync(orgId);
        return Ok(policies);
    }

    [HttpPost("save-policy")]
    public async Task<ActionResult<ServiceResult>> SavePolicy(
        [FromBody] SavePolicyRequest request)
    {
        var orgId = await ResolveOrganisationIdAsync(request.Policy.OrganisationId);
        request.Policy.OrganisationId = orgId;
        var result = await service.SavePolicyAsync(request.Policy, request.UserId);
        return Ok(result);
    }

    [HttpPost("evaluate-day")]
    public async Task<ActionResult<AttendanceAdjustment?>> EvaluateDay(
        [FromBody] EvaluateDayRequest request,
        CancellationToken ct = default)
    {
        var orgId = await ResolveOrganisationIdAsync(request.OrganisationId);
        var adjustment = await service.EvaluateDailyDeductionAsync(
            orgId,
            request.IndividualId,
            request.JobId,
            request.WorkDate,
            ct);
        return Ok(adjustment);
    }

    [HttpPost("evaluate-range")]
    public async Task<ActionResult<List<AttendanceAdjustment>>> EvaluateRange(
        [FromBody] EvaluateRangeRequest request,
        CancellationToken ct = default)
    {
        var orgId = await ResolveOrganisationIdAsync(request.OrganisationId);
        var adjustments = await service.EvaluateRangeAsync(
            orgId,
            request.IndividualId,
            request.JobId,
            request.StartDate,
            request.EndDate,
            ct);
        return Ok(adjustments);
    }

    [HttpGet("adjustments")]
    public async Task<ActionResult<List<AttendanceAdjustmentDto>>> GetAdjustments(
        [FromQuery] int organisationId = 0,
        [FromQuery] DateOnly? startDate = null,
        [FromQuery] DateOnly? endDate = null,
        [FromQuery] string? verificationStatus = null,
        [FromQuery] string? payrollStatus = null)
    {
        try
        {
            var orgId = await ResolveOrganisationIdAsync(organisationId);
            var list = await service.GetAdjustmentsAsync(
                orgId,
                startDate,
                endDate,
                verificationStatus,
                payrollStatus);
            return Ok(list);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load attendance adjustments for OrganisationId={OrgId}", organisationId);
            return StatusCode(500, new { message = ex.Message });
        }
    }

    [HttpPost("verify")]
    public async Task<ActionResult<ServiceResult>> Verify([FromBody] ActionAdjustmentRequest request)
    {
        var result = await service.VerifyAdjustmentAsync(request.AdjustmentId, request.UserId, request.Note ?? string.Empty);
        return Ok(result);
    }

    [HttpPost("excuse")]
    public async Task<ActionResult<ServiceResult>> Excuse([FromBody] ActionAdjustmentRequest request)
    {
        var result = await service.ExcuseAdjustmentAsync(request.AdjustmentId, request.UserId, request.Note ?? string.Empty);
        return Ok(result);
    }

    [HttpPost("reject")]
    public async Task<ActionResult<ServiceResult>> Reject([FromBody] ActionAdjustmentRequest request)
    {
        var result = await service.RejectAdjustmentAsync(request.AdjustmentId, request.UserId, request.Note ?? string.Empty);
        return Ok(result);
    }

    [HttpPost("preview")]
    public async Task<ActionResult<AttendanceDeductionCalculationPreviewDto>> PreviewCalculation([FromBody] PreviewCalcRequest request)
    {
        var orgId = await ResolveOrganisationIdAsync(request.OrganisationId);
        var preview = await service.PreviewCalculationAsync(
            orgId,
            request.JobId,
            request.WorkDate,
            request.LateMinutes,
            request.Kind);
        return Ok(preview);
    }

    public sealed record SavePolicyRequest(AttendanceDeductionPolicy Policy, int UserId);
    public sealed record EvaluateDayRequest(int OrganisationId, int IndividualId, int JobId, DateOnly WorkDate);
    public sealed record EvaluateRangeRequest(int OrganisationId, int? IndividualId, int? JobId, DateOnly StartDate, DateOnly EndDate);
    public sealed record ActionAdjustmentRequest(long AdjustmentId, int UserId, string? Note = null);
    public sealed record PreviewCalcRequest(int OrganisationId, int JobId, DateOnly WorkDate, int LateMinutes, string Kind);
}
