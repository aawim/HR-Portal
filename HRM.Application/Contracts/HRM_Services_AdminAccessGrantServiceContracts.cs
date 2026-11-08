using System.Data;
using System.Text.Json;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.ApprovalFlow;
using HRM.DTOs.UserContext;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
namespace HRM.Services.Contracts;
public abstract class AdminAccessGrantServiceContracts
{
    public const string TypeName = "Admin access grant";

    public const string PageUrl = "/admin/settings/admin-access";

public interface IOperations
{
Task<AdminAccessGrantData> LoadAsync(int? organisationId = null)
;
Task<ServiceResult> SubmitAsync(int organisationId, int userId, IEnumerable<int> groupIds, IEnumerable<int> permissionIds, string reason)
;
Task<ServiceResult> ActAsync(int organisationId, int id, int expectedState, string action, string? comment)
;
Task<ServiceResult> RevokeAsync(int organisationId, int userId, string reason)
;
}
}
