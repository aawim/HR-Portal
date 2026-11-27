using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Helpers;

public static class ActiveOrganisationFilter
{
    public static Task<List<int>> InactiveEntityStateIdsAsync(HrmTeContext db, CancellationToken ct = default) =>
        db.BusinessEntityStates.AsNoTracking()
            .Where(s => s.StateName.Contains("Deleted") || s.StateName.Contains("Inactive") || s.StateName.Contains("Closed") || s.StateName.Contains("Disabled"))
            .Select(s => s.BusinessEntityStateId)
            .ToListAsync(ct);

    public static IQueryable<Organisation> ActiveOrganisations(HrmTeContext db, List<int> inactiveEntityStateIds) =>
        db.Organisations.AsNoTracking()
            .Where(o => !db.BusinessEntities.Any(b => b.BusinessEntityID == o.BusinessEntityID && b.BusinessEntityStateId.HasValue && inactiveEntityStateIds.Contains(b.BusinessEntityStateId.Value)));
}
