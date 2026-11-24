namespace HRM.Application.Contracts.Positions;

public sealed record PositionContractDto(
    long DocumentId,
    string DocumentNumber,
    string Title,
    int JobPositionId,
    string PositionName,
    int CurrentVersionNumber,
    string WorkflowStatus,
    bool IsLocked,
    DateTime CreatedAtUtc,
    DateTime? AcceptedAtUtc,
    string? AcceptedByName,
    string? UnlockRequestReason,
    string? UnlockRequestedByName,
    DateTime? UnlockRequestedAtUtc,
    DateTime? UnlockedAtUtc,
    string? UnlockedByName,
    string? StaffDecisionNote);

public sealed record RegisterPositionContractInput(long DocumentId, int JobPositionId, string PositionName);
public sealed record RequestContractUnlockInput(long DocumentId, string Reason);
public sealed record RespondContractUnlockInput(long DocumentId, bool Approve, string? Note);
