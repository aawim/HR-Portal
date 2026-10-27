using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Leave;
using HRM.DTOs.UserContext;
using HRM.Services.Interfaces;
namespace HRM.Services.Leave.Contracts;
public abstract class LeaveHandoverServiceContracts
{

public interface IOperations
{
Task<List<LeaveHandoverInboxItem>> InboxAsync(int? leaveId = null)
;
Task<List<LeaveHandoverInboxItem>> GetAssignedInboxAsync()
;
Task<ServiceResult> DecideAsync(int leaveId, int revision, int? taskId, bool approve, string? comments)
;
Task<ServiceResult> ReviseAsync(int leaveId, int revision, List<LeaveHandoverTaskInput> tasks)
;
}
}
