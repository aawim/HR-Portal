using HRM.Models;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services.Leave;

/// <summary>One effective rule source for an organisation's leave applications.</summary>
public static class LeavePolicyResolver
{
    public static async Task<LeaveType?> ResolveAsync(HrmTeContext db, int? typeId, int organisationId, DateTime date)
    {
        var type = await db.LeaveTypes.AsNoTracking().SingleOrDefaultAsync(x => x.LeaveTypeId == typeId && x.IsActive && (x.OrganisationId == organisationId || x.IsGlobal));
        if (type == null) return null;
        var day = date.Date;
        var policies = await db.LeavePolicies.AsNoTracking().Where(x => x.LeaveTypeId == type.LeaveTypeId && x.OrganisationId == organisationId && x.IsActive && x.EffectiveFrom <= day && (x.EffectiveTo == null || x.EffectiveTo >= day))
            .OrderByDescending(x => x.EffectiveFrom).ThenByDescending(x => x.LeavePolicyId).Take(2).ToListAsync();
        if (policies.Count > 1) throw new InvalidOperationException("More than one leave policy applies to this date. Please contact HR.");
        if (policies.Count == 0) return type;
        var policy = policies[0];
        type.IncludeHolidays = policy.IncludeHolidays; type.IncludePay = policy.IncludePay;
        type.PayPercentage = policy.PayPercentage.HasValue ? (double)policy.PayPercentage.Value : null;
        type.RequestTypeId = policy.RequestTypeId; type.IsLocationRequired = policy.IsLocationRequired;
        type.IsStaffWideAvailable = policy.IsStaffWideAvailable; type.ServiceDurationMonths = policy.MinimumServiceMonths ?? type.ServiceDurationMonths;
        return type;
    }
}
