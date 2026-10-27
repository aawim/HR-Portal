using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HRM.Models;
using HRM.Models.Archives;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;




public sealed partial class LeaveSetConfigurationService
{
    public async Task<LeaveSetContentsData> LoadContentsAsync(int id)
    {
        var a=await Scope();await using var db=await factory.CreateDbContextAsync();
        var set=await db.LeaveSets.AsNoTracking().SingleOrDefaultAsync(s=>s.LeaveSetId==id&&s.OrganisationId==a.Org)??throw new UnauthorizedAccessException("Leave set unavailable in this organisation.");
        var types=await db.LeaveTypes.AsNoTracking().Where(t=>t.OrganisationId==a.Org||t.IsGlobal).OrderBy(t=>t.Name).Select(t=>new LeaveSetTypeOption(t.LeaveTypeId,t.Name)).ToListAsync();
        var ids=types.Select(t=>t.Id).ToList();
        var selected=await db.LeaveSetLeaveTypes.Where(x=>x.LeaveSetId==id&&x.IsValid&&ids.Contains(x.LeaveTypeId)).Select(x=>x.LeaveTypeId).Distinct().ToListAsync();
        return new(set.Name,set.OperationLogId,a.Super||!set.IsGlobal&&!set.IsPublic,types,selected);
    }
    public async Task SaveContentsAsync(int id,IReadOnlyCollection<int> typeIds,int expectedAudit)
    {
        var a=await Scope();var ids=typeIds.Distinct().ToList();
        await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var set=await db.LeaveSets.SingleOrDefaultAsync(s=>s.LeaveSetId==id&&s.OrganisationId==a.Org)??throw new UnauthorizedAccessException("Leave set unavailable in this organisation.");
        if(!a.Super&&(set.IsGlobal||set.IsPublic))throw new UnauthorizedAccessException("Only Super Administrators can change shared leave sets.");
        if(set.OperationLogId!=expectedAudit)throw new InvalidOperationException("This leave set changed. Refresh before saving.");
        if(await db.LeaveTypes.CountAsync(t=>ids.Contains(t.LeaveTypeId)&&(t.OrganisationId==a.Org||t.IsGlobal))!=ids.Count)throw new UnauthorizedAccessException("Choose leave types available to this organisation.");
        if(ids.Count==0&&await db.JobTypeLeaveSets.AnyAsync(x=>x.OrganisationId==a.Org&&x.LeaveSetId==id&&x.IsActive))throw new InvalidOperationException("A set assigned to Job Types must contain at least one leave type.");
        var links=await db.LeaveSetLeaveTypes.Where(x=>x.LeaveSetId==id).ToListAsync();
        var audit=await logs.CreateAsync(db,HRM.Components.Shared.SharedConfig.OperationLogActionTypes.LEAVE_SET_LEAVE_TYPE_UDPATE,$"Leave set {id}, organisation {a.Org}: leave types {string.Join(",",ids.Order())}.");
        foreach(var group in links.GroupBy(x=>x.LeaveTypeId))
        {
            var first=true;foreach(var link in group.OrderByDescending(x=>x.IsValid).ThenBy(x=>x.LeaveSetLeaveTypeId)){link.IsValid=ids.Contains(link.LeaveTypeId)&&first;link.OperationLogId=audit.OperationLogId;first=false;}
        }
        foreach(var type in ids.Where(i=>!links.Any(l=>l.LeaveTypeId==i)))db.LeaveSetLeaveTypes.Add(new(){LeaveSetId=id,LeaveTypeId=type,IsValid=true,OperationLogId=audit.OperationLogId});
        set.OperationLogId=audit.OperationLogId;await db.SaveChangesAsync();await tx.CommitAsync();
    }
    private static string Version(IEnumerable<JobTypeLeaveSet> rows)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(rows.OrderBy(x=>x.Id).Select(x=>new{x.Id,x.LeaveSetId,x.IsActive,x.OperationLogId})))));
    public async Task<JobTypeLeaveSetData> LoadJobTypeAsync(int jobTypeId)
    {
        var a=await Scope();await using var db=await factory.CreateDbContextAsync();
        var job=await db.JobTypes.Where(j=>j.JobTypeId==jobTypeId&&j.IsActive!=false).Select(j=>j.TypeName).SingleOrDefaultAsync()??throw new InvalidOperationException("Select an active Job Type.");
        var sets=await db.LeaveSets.AsNoTracking().Where(s=>s.OrganisationId==a.Org).OrderBy(s=>s.Name).Select(s=>new{s.LeaveSetId,s.Name}).ToListAsync();
        var setIds=sets.Select(s=>s.LeaveSetId).ToList();
        var contents=await db.LeaveSetLeaveTypes.AsNoTracking().Where(l=>setIds.Contains(l.LeaveSetId)&&l.IsValid&&(l.LeaveType.OrganisationId==a.Org||l.LeaveType.IsGlobal)).Select(l=>new{l.LeaveSetId,l.LeaveTypeId,l.LeaveType.Name}).ToListAsync();
        var links=await db.JobTypeLeaveSets.AsNoTracking().Where(l=>l.OrganisationId==a.Org&&l.JobTypeId==jobTypeId).ToListAsync();
        return new(await db.Organisations.Where(o=>o.BusinessEntityID==a.Org).Select(o=>o.OrganisationName).SingleAsync(),job,
            sets.Select(s=>new JobTypeSetOption(s.LeaveSetId,s.Name,contents.Where(c=>c.LeaveSetId==s.LeaveSetId).DistinctBy(c=>c.LeaveTypeId).Select(c=>new LeaveSetTypeOption(c.LeaveTypeId,c.Name)).OrderBy(t=>t.Name).ToList())).ToList(),links.Where(l=>l.IsActive).Select(l=>l.LeaveSetId).ToList(),Version(links));
    }
    public async Task SaveJobTypeAsync(int jobTypeId,IReadOnlyCollection<int> setIds,string expectedVersion)
    {
        var a=await Scope();var ids=setIds.Distinct().ToList();await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if(!await db.JobTypes.AnyAsync(j=>j.JobTypeId==jobTypeId&&j.IsActive!=false))throw new InvalidOperationException("Select an active Job Type.");
        if(await db.LeaveSets.CountAsync(s=>ids.Contains(s.LeaveSetId)&&s.OrganisationId==a.Org)!=ids.Count)throw new UnauthorizedAccessException("Choose leave sets owned by the current organisation.");
        var nonEmpty=await db.LeaveSetLeaveTypes.Where(l=>ids.Contains(l.LeaveSetId)&&l.IsValid&&(l.LeaveType.OrganisationId==a.Org||l.LeaveType.IsGlobal)).Select(l=>l.LeaveSetId).Distinct().CountAsync();
        if(nonEmpty!=ids.Count)throw new InvalidOperationException("Add leave types to every selected set before assigning it.");
        var rows=await db.JobTypeLeaveSets.Where(r=>r.OrganisationId==a.Org&&r.JobTypeId==jobTypeId).ToListAsync();
        if(Version(rows)!=expectedVersion)throw new InvalidOperationException("Job Type leave set assignments changed. Refresh before saving.");
        var audit=await logs.CreateAsync(db,HRM.Components.Shared.SharedConfig.OperationLogActionTypes.LEAVE_SET_UDPATE,$"Job Type {jobTypeId}, organisation {a.Org}: assigned leave sets {string.Join(",",ids.Order())}.");
        foreach(var row in rows){row.IsActive=ids.Contains(row.LeaveSetId);row.OperationLogId=audit.OperationLogId;}
        foreach(var id in ids.Where(i=>!rows.Any(r=>r.LeaveSetId==i)))db.JobTypeLeaveSets.Add(new(){OrganisationId=a.Org,JobTypeId=jobTypeId,LeaveSetId=id,IsActive=true,OperationLogId=audit.OperationLogId});
        await db.SaveChangesAsync();await tx.CommitAsync();
    }
}
