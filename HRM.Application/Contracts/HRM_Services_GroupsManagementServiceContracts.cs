using System.Data;
using HRM.Components.Shared;
using HRM.Services.Interfaces;
namespace HRM.Services.Contracts;
public abstract class GroupsManagementServiceContracts
{

public interface IOperations
{
Task<OperationalGroupsData> LoadAsync(int? selected = null)
;
Task<int> RequireGroupAsync(int id, bool write = false)
;
Task<bool> CanManageGroupAsync(int id)
;
Task SaveGroupAsync(int org, int id, string name, int type, int structure, int expectedAudit)
;
Task DeleteGroupAsync(int org, int id, int expectedAudit)
;
Task SaveTypeAsync(int org, int id, string name, string? originalName)
;
Task DeleteTypeAsync(int org,int id,string originalName)
;
Task AssignLeaveSetAsync(int groupId,int leaveSetId)
;
}
}
