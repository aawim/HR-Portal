using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using HRM.Models.Payroll;

namespace HRM.Application.Features.Payroll;

public interface IPayrollQueueService
{
    Task<long> EnqueueJobAsync(int organisationId, int? payrollRunId, PayrollJobType jobType, string? payloadJson = null, int priority = 100);
    Task<PayrollBackgroundJob?> ClaimNextJobAsync(string workerInstanceId, TimeSpan leaseDuration, CancellationToken cancellationToken = default);
    Task<bool> RenewLeaseAsync(long jobId, string workerInstanceId, long fencingToken, TimeSpan leaseDuration);
    Task CompleteJobAsync(long jobId, string workerInstanceId, long fencingToken);
    Task FailJobAsync(long jobId, string workerInstanceId, long fencingToken, string errorMessage, bool allowRetry = true);
    Task<int> RecoverExpiredJobsAsync();
}

public interface IPayrollSnapshotService
{
    Task<List<EmployeePayrollInputItem>> CreateOrGetRunSnapshotsAsync(int organisationId, int payrollRunId, DateOnly periodStart, DateOnly periodEnd);
    Task<List<PayrollInputSnapshot>> GetSnapshotsForRunAsync(int payrollRunId);
}

public interface IPayrollProcessor
{
    Task ProcessRunBatchesAsync(int organisationId, int payrollRunId, CancellationToken cancellationToken = default);
    Task ProcessBatchAsync(int organisationId, int payrollRunId, int batchId, CancellationToken cancellationToken = default);
}

public interface IPayrollApprovalService
{
    Task<bool> SubmitForReviewAsync(int organisationId, int payrollRunId, int userId, string? comment);
    Task<bool> ApprovePayrollAsync(int organisationId, int payrollRunId, int userId, string? comment);
    Task<bool> FinalisePayrollAsync(int organisationId, int payrollRunId, int userId, string? comment);
    Task<bool> RejectPayrollAsync(int organisationId, int payrollRunId, int userId, string reason);
}

public interface IPayrollPaymentService
{
    Task<PaymentBatchDto> PreparePaymentBatchAsync(int organisationId, int payrollRunId, string bankCode, string formatCode, int userId);
    Task<string> ExportBankFileAsync(int paymentBatchId, int organisationId, int userId);
    Task<bool> ReconcilePaymentBatchAsync(int paymentBatchId, int organisationId, int userId, string reference);
}

public interface IPayslipService
{
    Task<List<PayslipDto>> GetEmployeePayslipsAsync(int individualId, int organisationId);
    Task<PayslipDto?> GetPayslipDetailAsync(int payrollEmployeeId, int organisationId, int requestingIndividualId, bool isPrivileged);
}
