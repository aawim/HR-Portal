using HRM.Models;
using HRM.Models.WorkPlanning;
using Microsoft.EntityFrameworkCore;
namespace HRM.Helpers;

public sealed record StaffScheduleCandidate(JobWorkTemplateAssignment Assignment, int Rank, int Priority, string Source);
public static class EffectiveStaffSchedules
{
    public static async Task<List<StaffScheduleCandidate>> LoadAsync(HrmTeContext db, Job job, DateTime from, DateTime to, CancellationToken ct=default, int? excludingStaffAssignment=null)
    {
        var direct = await db.JobWorkTemplateAssignments.AsNoTracking().Include(a=>a.WorkTemplate).ThenInclude(t=>t.WorkTemplateSegments).ThenInclude(s=>s.WorkSegmentType)
            .Where(a=>a.JobID==job.JobId && a.IsActive && (!excludingStaffAssignment.HasValue || a.JobWorkTemplateAssignmentID!=excludingStaffAssignment) && a.EffectiveFrom<=to && (!a.EffectiveTo.HasValue || a.EffectiveTo>=from)).ToListAsync(ct);
        var result = direct.Select(a=>new StaffScheduleCandidate(a,0,int.MaxValue,"Staff override")).ToList();
        var legacy = await db.JobWorkTemplates.AsNoTracking().Include(a=>a.WorkTemplate).ThenInclude(t=>t.WorkTemplateSegments).ThenInclude(s=>s.WorkSegmentType)
            .Where(a=>a.JobId==job.JobId && a.IsActive && a.EffectiveFrom<=to && (!a.EffectiveTo.HasValue || a.EffectiveTo>=from)).ToListAsync(ct);
        foreach(var a in legacy)
        {
            var mask=(a.Sunday?1:0)+(a.Monday?2:0)+(a.Tuesday?4:0)+(a.Wednesday?8:0)+(a.Thursday?16:0)+(a.Friday?32:0)+(a.Saturday?64:0);
            result.Add(new(new() {JobWorkTemplateAssignmentID=int.MinValue,JobID=job.JobId,WorkTemplateID=a.WorkTemplateId,WorkTemplate=a.WorkTemplate,SchedulePurpose="Official",ScheduleMode="Weekly",WorkingDaysMask=mask,EffectiveFrom=a.EffectiveFrom,EffectiveTo=a.EffectiveTo,IsActive=true},0,a.Priority,"Legacy staff schedule"));
        }
        if(db.Model.FindEntityType(typeof(ScopeScheduleAssignment))==null) return Valid(result,job.OrganisationID);
        var rows=await db.ScopeScheduleAssignments.AsNoTracking().Include(a=>a.WorkTemplate).ThenInclude(t=>t.WorkTemplateSegments).ThenInclude(s=>s.WorkSegmentType)
            .Where(a=>a.OrganisationId==job.OrganisationID && a.IsActive && a.EffectiveFrom<=to && (!a.EffectiveTo.HasValue || a.EffectiveTo>=from)).ToListAsync(ct);
        if(rows.Count==0) return Valid(result,job.OrganisationID);
        var members=await db.GroupStaffs.AsNoTracking().Include(m=>m.Group).Where(m=>m.StaffIndividualId==job.IndividualID && m.Group.BusinessEntityOrganisationId==job.OrganisationID &&
            m.EffectiveStartDate<=to && (!m.EffectiveEndDate.HasValue || m.EffectiveEndDate>=from)).ToListAsync(ct);
        var structures=await db.OrganisationStructures.AsNoTracking().Where(s=>s.OrganisationBusinessEntityID==job.OrganisationID && s.OrganisationStructureStateId==3).ToDictionaryAsync(s=>s.OrganisationStructureId,s=>s.Name,ct);
        var relations=await db.OrganisationStructureRelations.AsNoTracking().Where(r=>r.OrganisationStructure.OrganisationBusinessEntityID==job.OrganisationID && r.ParentOrganisationStructure.OrganisationBusinessEntityID==job.OrganisationID &&
            r.ActiveFromDate<=to && (!r.ActiveToDate.HasValue || r.ActiveToDate>=from)).ToListAsync(ct);
        foreach(var row in rows)
        {
            var first=row.EffectiveFrom.Date<from.Date?from.Date:row.EffectiveFrom.Date;
            var last=row.EffectiveTo?.Date??to.Date; if(last>to.Date) last=to.Date;
            if(row.GroupId.HasValue)
            {
                foreach(var m in members.Where(m=>m.GroupId==row.GroupId)) Add(row,1,"Group: "+m.Group.Name,Max(first,m.EffectiveStartDate.Date),Min(last,m.EffectiveEndDate?.Date??to.Date));
            }
            else if(row.OrganisationStructureId.HasValue)
            {
                var paths=new Queue<(int Node,int Depth,DateTime First,DateTime Last,HashSet<int> Seen)>();
                paths.Enqueue((job.OrganisationStructureId,0,first,last,new()));
                while(paths.Count>0)
                {
                    var path=paths.Dequeue();
                    if(path.Last<path.First || path.Depth>100 || !structures.ContainsKey(path.Node) || path.Seen.Contains(path.Node)) continue;
                    if(path.Node==row.OrganisationStructureId) { Add(row,2+path.Depth,"Structure: "+structures[path.Node],path.First,path.Last); continue; }
                    var seen=new HashSet<int>(path.Seen) {path.Node};
                    foreach(var parent in relations.Where(r=>r.OrganisationStructureId==path.Node))
                        paths.Enqueue((parent.ParentOrganisationStructureId,path.Depth+1,Max(path.First,parent.ActiveFromDate.Date),Min(path.Last,parent.ActiveToDate?.Date??to.Date),seen));
                }
            }
            else Add(row,1000,"Organisation default",first,last);
        }
        return Valid(result,job.OrganisationID);
        void Add(ScopeScheduleAssignment a,int rank,string source,DateTime first,DateTime last)
        {
            if(last<first) return;
            result.Add(new(new() {JobWorkTemplateAssignmentID=-a.Id,JobID=job.JobId,WorkTemplateID=a.WorkTemplateId,WorkTemplate=a.WorkTemplate,SchedulePurpose=a.SchedulePurpose,
                ScheduleMode=a.ScheduleMode,WorkingDaysMask=a.WorkingDaysMask,RotationAnchorDate=a.RotationAnchorDate,RotationTemplateIds=a.RotationTemplateIds,
                EffectiveFrom=first,EffectiveTo=last,IsActive=true},rank,a.Priority,source));
        }
    }
    public static HRM.DTOs.StaffSchedule.CurrentWorkTemplateAssignmentDto ToDto(StaffScheduleCandidate candidate)
    {
        var a=candidate.Assignment;var t=a.WorkTemplate;
        return new() {JobWorkTemplateAssignmentId=a.JobWorkTemplateAssignmentID,JobId=a.JobID,WorkTemplateId=a.WorkTemplateID,TemplateName=t.Name,TemplateCode=t.Code,
            TemplateTypeName=t.WorkTemplateType?.Name??"",SchedulePurpose=a.SchedulePurpose,ScheduleMode=a.ScheduleMode,WorkingDaysMask=a.WorkingDaysMask,
            RotationAnchorDate=a.RotationAnchorDate,RotationTemplateIds=a.RotationTemplateIds,EndsNextDay=t.EndsNextDay,EffectiveFrom=a.EffectiveFrom,EffectiveTo=a.EffectiveTo,IsActive=a.IsActive,
            IsInherited=a.JobWorkTemplateAssignmentID<=0,InheritedFrom=candidate.Source,
            DefaultStartTime=(a.ScheduledStartTime??t.DefaultStartTime).HasValue?DateTime.MinValue.Add((a.ScheduledStartTime??t.DefaultStartTime)!.Value.ToTimeSpan()):null,
            DefaultEndTime=t.DefaultEndTime.HasValue?DateTime.MinValue.Add(t.DefaultEndTime.Value.ToTimeSpan()):null,
            MandatoryCheckInOffsets=t.WorkTemplateSegments.Where(s=>s.IsActive && s.IsMandatory && s.RequiresAttendance && (s.WorkSegmentType.Code is "CHECK_IN" or "DUTY_CHECK_IN" or "BREAK_END")).OrderBy(s=>s.SequenceNumber).Select(s=>s.OffsetMinutes).ToList(),
            MandatoryCheckOutOffsets=t.WorkTemplateSegments.Where(s=>s.IsActive && s.IsMandatory && s.RequiresAttendance && (s.WorkSegmentType.Code is "CHECK_OUT" or "DUTY_CHECK_OUT" or "BREAK_START")).OrderBy(s=>s.SequenceNumber).Select(s=>s.OffsetMinutes).ToList()};
    }
    private static DateTime Max(DateTime a,DateTime b)=>a>b?a:b;
    private static DateTime Min(DateTime a,DateTime b)=>a<b?a:b;
    private static List<StaffScheduleCandidate> Valid(List<StaffScheduleCandidate> rows,int org)=>rows.Where(r=>r.Assignment.WorkTemplate.IsActive && (r.Assignment.WorkTemplate.IsGlobal || r.Assignment.WorkTemplate.OrganisationBusinessEntityId==org)).ToList();
    public static List<StaffScheduleCandidate> On(IEnumerable<StaffScheduleCandidate> candidates,DateTime day)
    {
        var active=candidates.Where(c=>c.Assignment.EffectiveFrom.Date<=day.Date && (!c.Assignment.EffectiveTo.HasValue || c.Assignment.EffectiveTo.Value.Date>=day.Date) &&
            (!c.Assignment.WorkTemplate.EffectiveFrom.HasValue || c.Assignment.WorkTemplate.EffectiveFrom.Value.Date<=day.Date) && (!c.Assignment.WorkTemplate.EffectiveTo.HasValue || c.Assignment.WorkTemplate.EffectiveTo.Value.Date>=day.Date))
            .OrderBy(c=>c.Rank).ThenByDescending(c=>c.Priority).ThenByDescending(c=>c.Assignment.EffectiveFrom).ThenByDescending(c=>c.Assignment.JobWorkTemplateAssignmentID).ToList();
        return active.Where(c=>c.Assignment.SchedulePurpose=="Official").Take(1)
            .Concat(active.Where(c=>c.Assignment.SchedulePurpose=="Duty").GroupBy(c=>c.Assignment.WorkTemplateID).Select(g=>g.First())).ToList();
    }
}
