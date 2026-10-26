using System.Linq.Expressions;
using HRM.Models;
namespace HRM.Helpers;

/// <summary>Position periods include FromDate and exclude ToDate, using the tenant business date.</summary>
public static class PositionLifecycle
{
    public static bool Contains(DateTime from, DateTime? to, DateTime date)
        => from.Date <= date.Date && (!to.HasValue || date.Date < to.Value.Date);
    public static Expression<Func<JobPosition, bool>> ApprovedOn(DateTime date)
    {
        var day = date.Date;
        return p => p.JobPositionStateId == 4 && p.FromDate.Date <= day && (!p.ToDate.HasValue || p.ToDate.Value.Date > day);
    }
}
