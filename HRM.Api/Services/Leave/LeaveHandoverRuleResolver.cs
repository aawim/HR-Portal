using HRM.DTOs.Leave;
using HRM.Models;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services.Leave;

public sealed class HandoverRuleConflictException(string detail) : InvalidOperationException("Work handover requirements could not be determined. Please contact HR.")
{ public string Detail { get; } = detail; }

public static class LeaveHandoverRuleResolver
{
    public static async Task<EffectiveHandoverRule> ResolveAsync(HrmTeContext db,int jobId,int typeId,DateTime from)
    {
        var job=await db.Jobs.AsNoTracking().SingleOrDefaultAsync(x=>x.JobId==jobId) ?? throw new InvalidOperationException("Staff employment was not found.");
        var day=from.Date;
        var rules=await db.LeaveHandoverRules.AsNoTracking().Include(x=>x.LeaveTypes)
            .Where(x=>x.OrganisationId==job.OrganisationID && x.IsActive && x.Mode!="Inherit" && x.EffectiveFrom<=day && (x.EffectiveTo==null||x.EffectiveTo>=day) &&
                (x.JobTypeId==null||x.JobTypeId==job.JobTypeId) && (x.AllLeaveTypes||x.LeaveTypes.Any(t=>t.IsActive&&t.LeaveTypeId==typeId))).ToListAsync();
        if(rules.Count==0)return new(null,null,"Exempt",null,"No handover rule applies");
        var teams=rules.Any(x=>x.Scope=="Team") ? await db.TeamStaffs.AsNoTracking()
            .Where(x=>x.StaffId==job.IndividualID&&x.IsValid&&x.StartDate.Date<=day&&(x.EndDate==null||x.EndDate.Value.Date>=day)&&x.Team.OrganisationId==job.OrganisationID&&x.Team.IsValid==true&&
                (x.Team.StartDate==null||x.Team.StartDate.Value.Date<=day)&&(x.Team.EndDate==null||x.Team.EndDate.Value.Date>=day)).Select(x=>x.TeamId).Distinct().ToListAsync() : [];
        var structureValid=rules.Any(x=>x.Scope=="Structure")&&await db.OrganisationStructures.AnyAsync(x=>x.OrganisationStructureId==job.OrganisationStructureId&&x.OrganisationBusinessEntityID==job.OrganisationID&&x.OrganisationStructureStateId==3);
        var matches=rules.Where(x=>x.Scope=="Organisation"||x.Scope=="Structure"&&structureValid&&x.OrganisationStructureId==job.OrganisationStructureId||x.Scope=="Team"&&teams.Contains(x.TeamId??0))
            .OrderByDescending(x=>Rank(x.Scope)).ThenByDescending(x=>!x.AllLeaveTypes).ThenByDescending(x=>x.JobTypeId.HasValue).ThenByDescending(x=>x.Priority).ToList();
        if(matches.Count==0)return new(null,null,"Exempt",null,"No handover rule applies");
        var first=matches[0];
        if(matches.Skip(1).Any(x=>Rank(x.Scope)==Rank(first.Scope)&&x.AllLeaveTypes==first.AllLeaveTypes&&x.JobTypeId.HasValue==first.JobTypeId.HasValue&&x.Priority==first.Priority))
            throw new HandoverRuleConflictException("Overlapping rules have equal scope, specificity and priority: "+string.Join(", ",matches.Select(x=>$"{x.Name} (#{x.Id})")));
        return new(first.Id,first.Version,first.Mode,first.Mode=="Required"?first.AfterDays:null,$"{first.Scope}: {first.Name}");
    }
    public static int Rank(string scope)=>scope=="Team"?3:scope=="Structure"?2:1;
}
