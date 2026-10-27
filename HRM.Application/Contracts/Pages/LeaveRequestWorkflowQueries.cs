namespace HRM.Application.Contracts.Pages;

public sealed record LeaveRequestWorkflowSummary(int LeaveId, int RequestId, string Reference, int StateId,
    string Status, bool IsProcessing, bool IsFinal, string? ReturnReason, bool HasHandover,
    long? WorkflowInstanceId, string? WorkflowStatus, string? CurrentStage, bool WorkflowCompleted);

/// <summary>Only returns workflow information for leaves owned by the authenticated employee.</summary>
public interface ILeaveRequestWorkflowQueries
{
    Task<List<RequestStateOption>> GetStatesAsync(CancellationToken ct = default);
    Task<List<LeaveRequestWorkflowSummary>> GetMineAsync(IReadOnlyList<int> leaveIds, CancellationToken ct = default);
}
