using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;
public sealed record HrRequestGroup(int Id,string Name,int Count,int Processing);
public sealed record HrInboxRow(int Id,string Reference,string Applicant,string Type,string Status,DateTime AppliedAt,bool IsFinal,string? Url);
public sealed record HrRequestStatus(int Id,string Name);
public sealed record HrInboxData(string Organisation,bool OrganisationWide,List<HrRequestGroup> Groups,List<HrInboxRow> Items,int TypeId,int Page,int TotalPages)
{
    public List<HrRequestStatus> Statuses { get; init; } = [];
}
public sealed class HrRequestInboxService(IDbContextFactory<HrmTeContext> factory,IUserAccessService access)
{
    public async Task<HrInboxData> LoadAsync(int? typeId=null,int page=1,string? search=null,string status="all",DateTime? from=null,DateTime? to=null,CancellationToken cancellationToken=default)
    {
        var actor=await access.RequireContextAsync();var org=await access.GetCurrentOrganisationIdAsync();
        if(org<=0||!await access.HasOrganisationAccessAsync(org))throw new UnauthorizedAccessException("Select your current organisation.");
        if(from.HasValue&&(from.Value.Year<1900||from.Value.Year>2200)||to.HasValue&&(to.Value.Year<1900||to.Value.Year>2200)||from.HasValue&&to.HasValue&&to.Value.Date<from.Value.Date)throw new InvalidOperationException("Enter a valid date range.");
        if(search?.Length>250)throw new InvalidOperationException("Search must be 250 characters or fewer.");
        var wide=actor.IsSuperAdministrator||await access.HasPermissionAsync("Admin")||await access.HasPermissionAsync("Requests.View");
        await using var db=await factory.CreateDbContextAsync(cancellationToken);
        var statuses=await db.RequestStates.AsNoTracking().OrderBy(s=>s.StateName).Select(s=>new HrRequestStatus(s.RequestStateId,s.StateName)).ToListAsync(cancellationToken);
        int? stateId=null;
        if(status is not ("all" or "processing" or "final"))
        {
            if(!int.TryParse(status,out var id)||!statuses.Any(s=>s.Id==id))throw new InvalidOperationException("Select a valid request status.");
            stateId=id;
        }
        var source=db.Requests.AsNoTracking().Where(r=>r.OrganisationBusinessEntityID==org&&(wide||
            db.WfRequestApprovals.Any(a=>a.ApproverUserId==actor.UserId&&a.RequestStageInstance.WorkflowRequestInstance.RequestId==r.RequestId)||
            db.RequestNotifications.Any(n=>n.UserId==actor.UserId&&n.RequestId==r.RequestId)));
        if(from.HasValue){var start=from.Value.Date.AddHours(-5);source=source.Where(r=>r.ApplicationDate>=start);}
        if(to.HasValue){var end=to.Value.Date.AddDays(1).AddHours(-5);source=source.Where(r=>r.ApplicationDate<end);}
        if(status=="processing")source=source.Where(r=>!r.RequestState.IsFinalState);
        else if(status=="final")source=source.Where(r=>r.RequestState.IsFinalState);
        else if(stateId.HasValue)source=source.Where(r=>r.RequestStateId==stateId.Value);
        var text=search?.Trim();
        if(!string.IsNullOrWhiteSpace(text))source=source.Where(r=>(r.ReferenceNumber??"").Contains(text)||r.RequestType.TypeName.Contains(text)||
            db.Individuals.Any(i=>i.BusinessEntityId==r.ApplicantBusinessEntityId&&((i.FirstNameEnglish??"")+" "+(i.LastNameEnglish??"")).Contains(text)));
        var grouped=await source.GroupBy(r=>new{r.RequestTypeId,r.RequestType.TypeName}).Select(g=>new{Id=g.Key.RequestTypeId,Name=g.Key.TypeName,Count=g.Count(),Processing=g.Count(r=>!r.RequestState.IsFinalState)}).OrderBy(g=>g.Name).ToListAsync(cancellationToken);
        var groups=grouped.Select(g=>new HrRequestGroup(g.Id,g.Name,g.Count,g.Processing)).ToList();
        var type=Math.Max(0,typeId.GetValueOrDefault());
        var count=type==0?groups.Sum(g=>g.Count):groups.FirstOrDefault(g=>g.Id==type)?.Count??0;
        var pages=Math.Max(1,(int)Math.Ceiling(count/25d));page=Math.Clamp(page,1,pages);
        var rows=await source.Where(r=>type==0||r.RequestTypeId==type).OrderByDescending(r=>r.ApplicationDate).ThenByDescending(r=>r.RequestId).Skip((page-1)*25).Take(25)
            .Select(r=>new{r.RequestId,r.ReferenceNumber,Type=r.RequestType.TypeName,r.RequestType.ViewUrl,r.RequestState.StateName,r.RequestState.IsFinalState,r.RequestState.IsProcessingState,r.ApplicationDate,
                Applicant=db.Individuals.Where(i=>i.BusinessEntityId==r.ApplicantBusinessEntityId).Select(i=>(i.FirstNameEnglish??"")+" "+(i.LastNameEnglish??"")).FirstOrDefault(),IsLeave=db.LeaveRequests.Any(l=>l.RequestId==r.RequestId)}).ToListAsync(cancellationToken);
        var items=rows.Select(r=>new HrInboxRow(r.RequestId,r.ReferenceNumber??$"#{r.RequestId}",r.Applicant??"",r.Type,
            (r.IsProcessingState&&!r.IsFinalState?"Processing · ":"")+r.StateName,r.ApplicationDate.AddHours(5),r.IsFinalState,RequestUrl(r.ViewUrl,r.RequestId,r.IsLeave))).ToList();
        return new(await db.Organisations.Where(o=>o.BusinessEntityID==org).Select(o=>o.OrganisationName).SingleAsync(cancellationToken),wide,groups,items,type,page,pages){Statuses=statuses};
    }
    private static string? RequestUrl(string? url,int id,bool leave)
    {
        if(leave)return $"/admin/my/workspace?requestId={id}";
        if(string.IsNullOrWhiteSpace(url)||!url.StartsWith('/')||url.StartsWith("//")||url.Contains('\\')||url.Any(char.IsControl))return null;
        url=url.Replace("{requestId}",id.ToString(),StringComparison.OrdinalIgnoreCase).Replace("{id}",id.ToString(),StringComparison.OrdinalIgnoreCase);
        if(url.Contains('{')||url.Contains('}'))return null;
        return QueryHelpers.AddQueryString(url,"requestId",id.ToString());
    }
}
