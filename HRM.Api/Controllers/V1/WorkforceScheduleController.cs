using HRM.Api.Infrastructure;
using HRM.WorkPlanning.Abstractions;
using HRM.Services.Interfaces;
using HRM.WorkPlanning.Services;
using HRM.Components.Shared;
using HRM.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRM.Api.Controllers.V1;

[Route("api/v1/workforce-schedule")]
public sealed class WorkforceScheduleController(IWorkforceRosterService rosters, IWorkforcePolicyManagementService policies, IWorkforcePayrollExportService payroll, IWorkforceScheduleReviewService reviews, IUserAccessService access, IOperationLogService logs) : HrmApiController
{
    [HttpPost("rosters/draft")]
    public async Task<ActionResult<WorkforceRosterDraft>> CreateDraft([FromBody] CreateWorkforceRosterRequest request, CancellationToken cancellationToken)
    {
        var user = await access.RequireContextAsync();
        var organisationId = await WorkPlanningAccess.RequireOrganisationAsync(access, write: true);
        if (request.OrganisationId != organisationId) return Forbid();
        var logId = await logs.CreateLogAsync(SharedConfig.OperationLogActionTypes.WORK_ASSIGNMENT_CREATE, "Created workforce roster draft.");
        return Ok(await rosters.CreateDraftAsync(request with { ActorUserId = user.UserId, OperationLogId = logId }, cancellationToken));
    }

    [HttpGet("roster-versions/{rosterVersionId:int}/validation")]
    public async Task<ActionResult<WorkforceRosterValidation>> Validate(int rosterVersionId, CancellationToken cancellationToken)
    {
        var organisationId = await WorkPlanningAccess.RequireOrganisationAsync(access);
        return Ok(await rosters.ValidateAsync(rosterVersionId, organisationId, cancellationToken));
    }

    [HttpPost("roster-versions/{rosterVersionId:int}/occurrences")]
    public async Task<ActionResult<long>> AddOccurrence(int rosterVersionId, [FromBody] AddDutyOccurrenceRequest request, CancellationToken cancellationToken)
    {
        var organisationId = await WorkPlanningAccess.RequireOrganisationAsync(access, write: true);
        if (request.RosterVersionId != rosterVersionId || request.OrganisationId != organisationId) return Forbid();
        var logId = await logs.CreateLogAsync(SharedConfig.OperationLogActionTypes.WORK_ASSIGNMENT_CREATE, "Added workforce duty occurrence.");
        return Ok(await rosters.AddOccurrenceAsync(request with { OperationLogId = logId }, cancellationToken));
    }

    [HttpPost("duty-occurrences/{dutyOccurrenceId:long}/assignments")]
    public async Task<ActionResult<long>> Assign(int dutyOccurrenceId, [FromBody] AssignDutyRequest request, CancellationToken cancellationToken)
    {
        var user = await access.RequireContextAsync();
        var organisationId = await WorkPlanningAccess.RequireOrganisationAsync(access, write: true);
        if (request.DutyOccurrenceId != dutyOccurrenceId || request.OrganisationId != organisationId) return Forbid();
        var logId = await logs.CreateLogAsync(SharedConfig.OperationLogActionTypes.WORK_ASSIGNMENT_CREATE, "Assigned staff to workforce duty.");
        return Ok(await rosters.AssignAsync(request with { ActorUserId = user.UserId, OperationLogId = logId }, cancellationToken));
    }

    [HttpPost("roster-versions/{rosterVersionId:int}/publish")]
    public async Task<IActionResult> Publish(int rosterVersionId, [FromBody] PublishRequest request, CancellationToken cancellationToken)
    {
        var user = await access.RequireContextAsync();
        await WorkPlanningAccess.RequireOrganisationAsync(access, write: true);
        var logId = await logs.CreateLogAsync(SharedConfig.OperationLogActionTypes.WORK_ASSIGNMENT_CREATE, "Published workforce roster.");
        await rosters.PublishAsync(rosterVersionId, user.UserId, logId, cancellationToken);
        return NoContent();
    }

    public sealed record PublishRequest(int OperationLogId);

    [HttpGet("duty-types")]
    public async Task<ActionResult<IReadOnlyList<WorkforceDutyTypeRow>>> GetDutyTypes(CancellationToken cancellationToken)
        => Ok(await policies.GetDutyTypesAsync(await WorkPlanningAccess.RequireOrganisationAsync(access), cancellationToken));

    [HttpPost("duty-types")]
    public async Task<ActionResult<int>> CreateDutyType([FromBody] DutyTypeInput input, CancellationToken cancellationToken)
    {
        var organisationId = await WorkPlanningAccess.RequireOrganisationAsync(access, write: true); if (input.OrganisationId != organisationId) return Forbid();
        var logId = await logs.CreateLogAsync(SharedConfig.OperationLogActionTypes.WORK_ASSIGNMENT_CREATE, "Created workforce duty type.");
        return Ok(await policies.CreateDutyTypeAsync(input with { OperationLogId = logId }, cancellationToken));
    }

    [HttpGet("policies")]
    public async Task<ActionResult<IReadOnlyList<WorkforcePolicyRow>>> GetPolicies(CancellationToken cancellationToken)
        => Ok(await policies.GetPoliciesAsync(await WorkPlanningAccess.RequireOrganisationAsync(access), cancellationToken));

    [HttpPost("policies")]
    public async Task<ActionResult<int>> CreatePolicy([FromBody] WorkforcePolicyInput input, CancellationToken cancellationToken)
    {
        var organisationId = await WorkPlanningAccess.RequireOrganisationAsync(access, write: true); if (input.OrganisationId != organisationId) return Forbid();
        var logId = await logs.CreateLogAsync(SharedConfig.OperationLogActionTypes.WORK_ASSIGNMENT_CREATE, "Created workforce policy.");
        return Ok(await policies.CreatePolicyAsync(input with { OperationLogId = logId }, cancellationToken));
    }

    [HttpGet("policy-assignments")]
    public async Task<ActionResult<IReadOnlyList<WorkforcePolicyAssignmentRow>>> GetPolicyAssignments(CancellationToken cancellationToken)
        => Ok(await policies.GetAssignmentsAsync(await WorkPlanningAccess.RequireOrganisationAsync(access), cancellationToken));

    [HttpPost("policy-assignments")]
    public async Task<ActionResult<int>> CreatePolicyAssignment([FromBody] WorkforcePolicyAssignmentInput input, CancellationToken cancellationToken)
    {
        var organisationId = await WorkPlanningAccess.RequireOrganisationAsync(access, write: true); if (input.OrganisationId != organisationId) return Forbid();
        var logId = await logs.CreateLogAsync(SharedConfig.OperationLogActionTypes.WORK_ASSIGNMENT_CREATE, "Assigned workforce policy.");
        return Ok(await policies.CreateAssignmentAsync(input with { OperationLogId = logId }, cancellationToken));
    }

    [HttpPost("payroll-exports")]
    public async Task<ActionResult<IReadOnlyList<WorkforcePayrollExportLine>>> PreparePayrollExport([FromBody] PayrollExportRequest request, CancellationToken cancellationToken)
    {
        if (!await access.HasPermissionAsync(Permissions.PayrollExport)) return Forbid();
        var organisationId = await WorkPlanningAccess.RequireOrganisationAsync(access); if (request.OrganisationId != organisationId) return Forbid();
        return Ok(await payroll.PrepareAsync(organisationId, request.Reference, cancellationToken));
    }
    public sealed record PayrollExportRequest(int OrganisationId, string Reference);

    [HttpPost("payroll-corrections")]
    public async Task<ActionResult<long>> CreatePayrollCorrection([FromBody] WorkforcePayrollCorrection correction, CancellationToken cancellationToken)
    {
        if (!await access.HasPermissionAsync(Permissions.PayrollFinalise)) return Forbid();
        var organisationId = await WorkPlanningAccess.RequireOrganisationAsync(access, write: true); if (correction.OrganisationId != organisationId) return Forbid();
        var logId = await logs.CreateLogAsync(SharedConfig.OperationLogActionTypes.WORK_ASSIGNMENT_CREATE, "Created payroll correction adjustment.");
        return Ok(await payroll.CreateCorrectionAsync(correction with { OperationLogId = logId }, cancellationToken));
    }

    [HttpGet("sessions")]
    public async Task<ActionResult<IReadOnlyList<WorkforceSessionReviewRow>>> GetSessions(CancellationToken cancellationToken)
        => Ok(await reviews.GetSessionsAsync(await WorkPlanningAccess.RequireOrganisationAsync(access), cancellationToken));

    [HttpGet("ledger")]
    public async Task<ActionResult<IReadOnlyList<WorkforceLedgerReviewRow>>> GetLedger(CancellationToken cancellationToken)
        => Ok(await reviews.GetLedgerAsync(await WorkPlanningAccess.RequireOrganisationAsync(access), cancellationToken));
}
