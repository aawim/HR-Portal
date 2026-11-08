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
        throw new RequestTransitionConfigurationException($"Transition not allowed for {type?.Trim() ?? $"Request Type {requestType}"}: {current?.StateName.Trim() ?? from.ToString()} → {target?.Trim() ?? to.ToString()}. Configure this From State / To State in Settings > State Transitions.");
    }
}
