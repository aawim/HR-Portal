using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public static class RequestTransitionPolicy
{
    public static async Task RequireAllowedAsync(HrmTeContext db, int requestType, int from, int to, CancellationToken ct = default)
    {
        var current = await db.RequestStates.AsNoTracking().Where(x => x.RequestStateId == from)
            .Select(x => new { x.StateName, x.IsFinalState }).SingleOrDefaultAsync(ct);
        if (current?.IsFinalState == true)
            throw new InvalidOperationException($"Request is already final ({current.StateName.Trim()}); no further status changes are allowed.");
        if (from != to && await db.RequestTypesAllowedRequestStateTransitions.AsNoTracking()
            .AnyAsync(x => x.RequestTypeId == requestType && x.FromRequestStateId == from && x.ToRequestStateId == to, ct)) return;
        var target = await db.RequestStates.AsNoTracking().Where(x => x.RequestStateId == to).Select(x => x.StateName).SingleOrDefaultAsync(ct);
        var type = await db.RequestTypes.AsNoTracking().Where(x => x.RequestTypeId == requestType).Select(x => x.TypeName).SingleOrDefaultAsync(ct);
        var typeName = type?.Trim() ?? $"Request Type {requestType}";
        var fromName = current?.StateName.Trim() ?? from.ToString();
        var toName = target?.Trim() ?? to.ToString();
        throw new RequestTransitionConfigurationException(
            $"Missing transition. Enter in this order: 1. Request Type = {typeName} (ID {requestType}); " +
            $"2. From State = {fromName} (ID {from}); 3. To State = {toName} (ID {to}); " +
            "4. Select Allow transition; 5. Retry the original request action.");
    }
}
