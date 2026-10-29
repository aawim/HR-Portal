using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

internal static class PositionVacancy
{
    // Position and employment end dates are exclusive, matching staff employment checks.
    internal static IQueryable<HRM.Models.JobPosition> Overlapping(HrmTeContext db,DateTime from,DateTime? to,int? excluding=null) =>
        db.JobPositions.Where(a=>a.JobPositionStateId==4&&a.JobPositionId!=excluding&&
            (a.Job.JobStateId==4||a.Job.TerminatedDate.HasValue)&&
            (!to.HasValue||a.FromDate<to.Value&&a.Job.JoinedDate<to.Value)&&
            (!a.ToDate.HasValue||a.ToDate>from)&&
            (!a.Job.TerminatedDate.HasValue||a.Job.TerminatedDate>from));

    internal static IQueryable<Position> Vacant(HrmTeContext db,DateTime day)
    {
        var occupied=Overlapping(db,day,day.AddDays(1)).Select(a=>a.PositionId);
        return db.Positions.Where(p=>!occupied.Contains(p.PositionId));
    }

    internal static async Task RequireAsync(HrmTeContext db,int positionId,DateTime from,DateTime? to,int? excluding=null)
    {
        if(db.Database.IsSqlServer())await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @result int; EXEC @result=sys.sp_getapplock @Resource={"HRM.Position:"+positionId}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=5000; IF @result<0 THROW 51063,'Position assignments are busy. Please try again.',1;");
        if(await Overlapping(db,from,to,excluding).AnyAsync(a=>a.PositionId==positionId))
            throw new InvalidOperationException("This position is already assigned to a job for the selected dates. Select a vacant position.");
    }
}
