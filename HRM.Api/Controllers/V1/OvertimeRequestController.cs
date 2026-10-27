// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/overtime-request")]
public sealed class OvertimeRequestController(global::HRM.Services.Contracts.OvertimeRequestServiceContracts.IOperations service) : HrmApiController
{
    [HttpPost("submit")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Submit([FromBody] OvertimeRequestSubmitRequest request)
    {
        return Ok(await service.SubmitAsync(request.date, request.minutes, request.reason));
    }

    public sealed class OvertimeRequestSubmitRequest
    {
        public global::System.DateTime date { get; set; }
        public global::System.Int32 minutes { get; set; }
        public global::System.String reason { get; set; } = default!;
    }

    [HttpGet("load")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Services.OvertimeRow>>> Load([FromQuery] global::System.Boolean pendingOnly = false)
    {
        return Ok(await service.LoadAsync(pendingOnly));
    }

    [HttpPost("act")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> Act([FromBody] OvertimeRequestActRequest request)
    {
        return Ok(await service.ActAsync(request.id, request.expectedState, request.action, request.comment));
    }

    public sealed class OvertimeRequestActRequest
    {
        public global::System.Int32 id { get; set; }
        public global::System.Int32 expectedState { get; set; }
        public global::System.String action { get; set; } = default!;
        public global::System.String? comment { get; set; }
    }

    [HttpPost("record-payment")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> RecordPayment([FromBody] OvertimeRequestRecordPaymentRequest request)
    {
        return Ok(await service.RecordPaymentAsync(request.id, request.reference));
    }

    public sealed class OvertimeRequestRecordPaymentRequest
    {
        public global::System.Int32 id { get; set; }
        public global::System.String reference { get; set; } = default!;
    }

    [HttpPost("process-payroll")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> ProcessPayroll([FromBody] OvertimeRequestProcessPayrollRequest request)
    {
        return Ok(await service.ProcessPayrollAsync(request.requestIds, request.month, request.reference, request.clientBatchId));
    }

    public sealed class OvertimeRequestProcessPayrollRequest
    {
        public global::System.Collections.Generic.IEnumerable<global::System.Int32> requestIds { get; set; } = default!;
        public global::System.DateTime month { get; set; }
        public global::System.String reference { get; set; } = default!;
        public global::System.Guid clientBatchId { get; set; }
    }

    [HttpPost("resubmit-claim")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> ResubmitClaim([FromBody] OvertimeRequestResubmitClaimRequest request)
    {
        return Ok(await service.ResubmitClaimAsync(request.id, request.reason));
    }

    public sealed class OvertimeRequestResubmitClaimRequest
    {
        public global::System.Int32 id { get; set; }
        public global::System.String reason { get; set; } = default!;
    }

    [HttpGet("load-clock-events")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Services.OvertimeClockOption>>> LoadClockEvents([FromQuery] global::System.DateTime date)
    {
        return Ok(await service.LoadClockEventsAsync(date));
    }

    [HttpGet("load-approved-tasks")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Services.OvertimeTaskOption>>> LoadApprovedTasks()
    {
        return Ok(await service.LoadApprovedTasksAsync());
    }

    [HttpPost("submit-claim")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SubmitClaim([FromBody] OvertimeRequestSubmitClaimRequest request)
    {
        return Ok(await service.SubmitClaimAsync(request.startLogId, request.endLogId, request.reason, request.taskId));
    }

    public sealed class OvertimeRequestSubmitClaimRequest
    {
        public global::System.Int32 startLogId { get; set; }
        public global::System.Int32 endLogId { get; set; }
        public global::System.String reason { get; set; } = default!;
        public global::System.Int32? taskId { get; set; } = null;
    }

    [HttpPost("submit-work")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> SubmitWork([FromBody] OvertimeRequestSubmitWorkRequest request)
    {
        return Ok(await service.SubmitWorkAsync(request.kind, request.from, request.to, request.reason, request.tasks, request.proposedStart, request.proposedEnd));
    }

    public sealed class OvertimeRequestSubmitWorkRequest
    {
        public global::System.String kind { get; set; } = default!;
        public global::System.DateTime from { get; set; }
        public global::System.DateTime to { get; set; }
        public global::System.String reason { get; set; } = default!;
        public global::System.Collections.Generic.List<global::System.String> tasks { get; set; } = default!;
        public global::System.DateTime? proposedStart { get; set; } = null;
        public global::System.DateTime? proposedEnd { get; set; } = null;
    }

    [HttpGet("load-work")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.Services.OvertimeWorkRow>>> LoadWork([FromQuery] global::System.Boolean pendingOnly = false)
    {
        return Ok(await service.LoadWorkAsync(pendingOnly));
    }

    [HttpPost("act-work")]
    public async Task<ActionResult<global::HRM.DTOs.ServiceResult>> ActWork([FromBody] OvertimeRequestActWorkRequest request)
    {
        return Ok(await service.ActWorkAsync(request.id, request.expectedState, request.action, request.comment));
    }

    public sealed class OvertimeRequestActWorkRequest
    {
        public global::System.Int32 id { get; set; }
        public global::System.Int32 expectedState { get; set; }
        public global::System.String action { get; set; } = default!;
        public global::System.String? comment { get; set; }
    }

}
