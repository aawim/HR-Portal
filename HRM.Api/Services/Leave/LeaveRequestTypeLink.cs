using HRM.Components.Shared;
using HRM.Models;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services.Leave;

public static class LeaveRequestTypeLink
{
    public static readonly int[] ExcludedIds = [SharedConfig.RequestTypes.USER_REGISTRATION,
        SharedConfig.RequestTypes.LEAVE_CHANGE_REQUEST, SharedConfig.RequestTypes.EARLY_RETURN_REQUEST, SharedConfig.RequestTypes.LEAVE_EXTENSION_REQUEST];
    public static async Task<string?> ValidateAsync(HrmTeContext db, int? requestTypeId, CancellationToken ct = default)
    {
        if (requestTypeId is not > 0 || ExcludedIds.Contains(requestTypeId.Value) ||
            !await db.RequestTypes.AnyAsync(x => x.RequestTypeId == requestTypeId.Value, ct))
            return "Select a valid leave application request type.";
        return null;
    }
}
