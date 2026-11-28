using HRM.Models;
using HRM.Models.Leadership;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;
public static class LeadershipAttendancePolicyResolver
{
 public static async Task<LeadershipAttendancePolicy?> ResolveAsync(HrmTeContext db,int person,int organisation,DateOnly day,CancellationToken ct=default)
 {
  var policies=await (from p in db.LeadershipAttendancePolicies.AsNoTracking() join a in db.LeadershipAppointments.AsNoTracking() on p.LeadershipAppointmentId equals a.LeadershipAppointmentId where a.IndividualId==person&&a.Status=="Active"&&a.EffectiveFrom<=day&&(a.EffectiveTo==null||a.EffectiveTo>=day)&&p.EffectiveFrom<=day&&(p.EffectiveTo==null||p.EffectiveTo>=day) select p).ToListAsync(ct);
  if(policies.Count==0)return null;
  if(policies.Any(x=>x.ManagingOrganisationId!=organisation))throw new InvalidOperationException("Leadership attendance must be managed by its configured administrative organisation.");
  if(policies.Count!=1)throw new InvalidOperationException("Overlapping leadership attendance policies require administrator review.");
  if(policies[0].VerificationMode!="Standard")throw new InvalidOperationException("Unsupported leadership attendance verification mode.");return policies[0];
 }
}
