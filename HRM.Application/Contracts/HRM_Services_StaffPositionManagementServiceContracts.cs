using System.Data;
using System.Text.Json;
using HRM.Components.Shared;
using HRM.Services.Interfaces;
using HRM.DTOs;
namespace HRM.Services.Contracts;
public abstract class StaffPositionManagementServiceContracts
{
    public sealed record Choice(int Id, string Name)
    {
        public HRM.Application.Contracts.Pages.PositionPayInfo? Pay { get; init; }
    }

    public sealed record Assignment(int Id, int JobId, int PositionId, string Name, DateTime From,
        DateTime? To, int StateId, string State, int AuditId)
    {
        public string Version => $"{PositionId}:{From.Ticks}:{To?.Ticks}:{StateId}:{AuditId}";
        public string? PendingAction { get; init; }
        public bool HasApprovedRemoval { get; init; }
        public HRM.Application.Contracts.Pages.PositionPayInfo? Pay { get; init; }
    }

    public sealed record Data(bool CanManage, List<Choice> Jobs, List<Choice> Positions, List<Assignment> Assignments);

    public const string RequestsPageUrl = "/online/staff-position-requests";

public interface IOperations
{
Task<Data> LoadAsync(int staffId)
;
Task SaveAsync(int staffId, int jobId, int positionId, DateTime from, DateTime? to,
        string reason, Assignment? original = null, bool change = false, bool remove = false)
;
Task<List<StaffPositionRequestRow>> LoadRequestsAsync(int? staffId = null, bool pendingOnly = false)
;
Task<ServiceResult> ActOnRequestAsync(int id, int expectedState, string action, string? comment)
;
}
}
