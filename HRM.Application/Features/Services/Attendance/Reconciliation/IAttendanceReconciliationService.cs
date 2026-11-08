using HRM.DTOs.Attendance;

namespace HRM.Services.Attendance.Reconciliation;

public interface IAttendanceReconciliationService
{
    Task<AttendanceReconciliationResult> ReconcileAsync(
        int individualId,
        int jobId,
        int organisationId,
        DateOnly workDate,
        CancellationToken cancellationToken = default);

    Task<AttendanceBatchReconciliationResult> ReconcileRangeAsync(
        AttendanceBatchReconciliationRequest request,
        CancellationToken cancellationToken = default);

    Task<AttendanceReconciliationResult> ApplyManualOverrideAsync(
        AttendanceManualOverrideRequest request,
        CancellationToken cancellationToken = default);
}
