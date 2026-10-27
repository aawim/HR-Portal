using HRM.Models;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;

public static class FormerStaffPolicy
{
    public const string ReadOnlyMessage="Former staff records are read-only. Changes are not permitted, including for superadministrators.";
    public static DateTime Today=>DateTime.UtcNow.AddHours(5).Date;
    public static IQueryable<Job> CurrentJobs(HrmTeContext db)
    {
        var today=Today;
        return db.Jobs.AsNoTracking().Where(j=>j.JobStateId==4&&j.JoinedDate<=today&&(!j.TerminatedDate.HasValue||j.TerminatedDate>today)&&
            (!db.JobPositions.Any(p=>p.JobId==j.JobId&&p.JobPositionStateId==4&&p.FromDate<=today)||
              db.JobPositions.Any(p=>p.JobId==j.JobId&&p.JobPositionStateId==4&&p.FromDate<=today&&(!p.ToDate.HasValue||p.ToDate>today))));
    }
    public static IQueryable<Job> EndedJobs(HrmTeContext db)
    {
        var today=Today;
        return db.Jobs.AsNoTracking().Where(j=>j.JoinedDate<=today&&(j.TerminatedDate.HasValue&&j.TerminatedDate<=today||j.JobStateId==4&&
            db.JobPositions.Any(p=>p.JobId==j.JobId&&p.JobPositionStateId==4&&p.FromDate<=today)&&
            !db.JobPositions.Any(p=>p.JobId==j.JobId&&p.JobPositionStateId==4&&p.FromDate<=today&&(!p.ToDate.HasValue||p.ToDate>today))));
    }
    public static Task<bool> IsFormerAsync(HrmTeContext db,int person,int? org=null,CancellationToken ct=default)=>
        EndedJobs(db).AnyAsync(j=>j.IndividualID==person&&(!org.HasValue||j.OrganisationID==org)&&
            !CurrentJobs(db).Any(a=>a.IndividualID==person&&(!org.HasValue||a.OrganisationID==org)),ct);
}
