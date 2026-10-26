using HRM.Models;
using Microsoft.EntityFrameworkCore;
internal static class AdminAccessAudit
{
    public static async Task RunAsync(HrmTeContext db)
    {
        var people=await db.Individuals.AsNoTracking().Where(x=>x.FirstNameEnglish.Contains("Rasheeda")||x.FirstNameEnglish.Contains("Rasheedha")).Select(x=>new{BusinessEntityID=x.BusinessEntityId,x.FirstNameEnglish,x.LastNameEnglish}).ToListAsync();
        var ids=people.Select(x=>x.BusinessEntityID).ToArray();
        foreach(var p in people)Console.WriteLine($"PERSON {p.BusinessEntityID}: {p.FirstNameEnglish} {p.LastNameEnglish}");
        var users=await db.Users.AsNoTracking().Include(x=>x.UserOrganisation).Where(x=>ids.Contains(x.BusinessEntityID)).ToListAsync();
        foreach(var u in users)
        {
            Console.WriteLine($"ACCOUNT {u.UserId} | person={u.BusinessEntityID} | org={u.UserOrganisation?.BusinessEntityID} userOrg={u.UserOrganisationId} | active={u.IsActive} state={u.LocalUserStateId} adminEnabled={u.AdminPortalAccessEnabled} adminOrg={u.AdminPortalAccessOrganisationId}");
            var effective=await new HRM.Services.EffectivePermissionService(new AuditFactory(db.Database.GetConnectionString()!)).GetEffectivePermissionsAsync(u.UserId);
            Console.WriteLine("EFFECTIVE "+string.Join(", ",effective.Select(x=>x.RoleKey)));
        }
        var day=DateTime.UtcNow.AddHours(5).Date;
        foreach(var j in await db.Jobs.AsNoTracking().Where(x=>ids.Contains(x.IndividualID)).ToListAsync())
        {
            Console.WriteLine($"JOB {j.JobId} | org={j.OrganisationID} | jobType={j.JobTypeId} state={j.JobStateId} joined={j.JoinedDate:yyyy-MM-dd} ended={j.TerminatedDate:yyyy-MM-dd}");
            foreach(var p in await db.JobPositions.AsNoTracking().Where(x=>x.JobId==j.JobId).ToListAsync())Console.WriteLine($"POSITION {p.JobPositionId} | position={p.PositionId} state={p.JobPositionStateId} from={p.FromDate:yyyy-MM-dd} to={p.ToDate:yyyy-MM-dd} eligible={p.JobPositionStateId==4&&p.FromDate.Date<=day&&(!p.ToDate.HasValue||p.ToDate.Value.Date>day)}");
        }
        var userIds=users.Select(x=>x.UserId).ToArray();
        foreach(var r in await db.AdminAccessGrantRequests.AsNoTracking().Include(x=>x.Request).ThenInclude(x=>x.RequestState).Where(x=>userIds.Contains(x.UserId)).ToListAsync()) Console.WriteLine($"REQUEST {r.RequestId} | user={r.UserId} job={r.JobId} open={r.IsOpen} | {r.Request.RequestState.StateName} | groups={r.GroupIdsJson} permissions={r.PermissionIdsJson}");
        var type=await db.RequestTypes.AsNoTracking().Where(x=>x.TypeName.Contains("Admin")&&x.TypeName.Contains("access")).ToListAsync();
        foreach(var t in type)
        {
            Console.WriteLine($"REQUEST TYPE {t.RequestTypeId}: {t.TypeName}");
            foreach(var f in await db.WfWorkflowDefinitions.AsNoTracking().Include(x=>x.Stages).Include(x=>x.WorkflowJobTypes).Where(x=>x.RequestTypeId==t.RequestTypeId&&x.IsActive).ToListAsync())Console.WriteLine($"FLOW {f.WorkflowDefinitionId} | org={f.OrganisationBusinessEntityId} | jobs={string.Join(',',f.WorkflowJobTypes.Where(x=>x.IsActive).Select(x=>x.JobTypeId))} | stages={f.Stages.Count(x=>x.IsActive)} | {f.Name}");
        }
    }
    sealed class AuditFactory(string connection):IDbContextFactory<HrmTeContext>{public HrmTeContext CreateDbContext()=>new(new DbContextOptionsBuilder<HrmTeContext>().UseSqlServer(connection).Options);}
}
