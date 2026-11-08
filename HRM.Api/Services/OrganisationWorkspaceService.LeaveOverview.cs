using HRM.DTOs;
using HRM.Models;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;

public partial class OrganisationWorkspaceService
{
    public async Task<OrganisationLeaveOverviewData> LeaveOverviewAsync(OrganisationLeaveOverviewFilter filter,CancellationToken cancellationToken=default)
    {
        await RequireAsync(filter.OrganisationId);var actor=await access.RequireContextAsync();
        if(!actor.IsSuperAdministrator&&filter.OrganisationId!=await access.GetCurrentOrganisationIdAsync())throw new UnauthorizedAccessException("Use your current organisation.");
        if(filter.Year is <1900 or >2200||filter.Month is <1 or >12||filter.DepartmentId<0||filter.JobTypeId<0||filter.Search.Length>200)throw new ArgumentException("Select a valid month and staff filter.");
        await using var db=await factory.CreateDbContextAsync(cancellationToken);var ct=cancellationToken;var org=filter.OrganisationId;
        var yearStart=new DateTime(filter.Year,1,1);var yearEnd=yearStart.AddYears(1);
        var monthStart=new DateTime(filter.Year,filter.Month,1);var monthEnd=monthStart.AddMonths(1);var today=Today;
        var wide=actor.IsSuperAdministrator||await access.HasPermissionAsync("Admin")||await access.HasPermissionAsync("Leave.View");
        var data=new OrganisationLeaveOverviewData{OrganisationWide=wide};
        var own=actor.IndividualId;var search=filter.Search.Trim();
        var assigned=db.WfRequestApprovals.Where(a=>a.ApproverUserId==actor.UserId&&a.Decision=="Pending"&&a.RequestStageInstance.StageStatus=="Pending"&&
            a.RequestStageInstance.WorkflowRequestInstance.CurrentRequestStageInstanceId==a.RequestStageInstanceId)
            .Select(a=>a.RequestStageInstance.WorkflowRequestInstance.RequestId);
        var flowing=db.WfRequestInstances.Where(w=>w.CompletedAt==null&&w.CurrentRequestStageInstanceId!=null&&(w.WorkflowStatus=="InProgress"||w.WorkflowStatus=="Returned")).Select(w=>w.RequestId);
        data.ProcessingLeaves=await db.LeaveRequests.AsNoTracking().Where(l=>l.Request.OrganisationBusinessEntityID==org&&l.Leave.Job.OrganisationID==org&&
            l.Request.RequestState.IsProcessingState&&!l.Request.RequestState.IsFinalState&&l.Leave.LeaveStateId!=4&&l.Leave.LeaveStateId!=5&&l.Leave.LeaveStateId!=6&&flowing.Contains(l.RequestId)&&
            (wide||l.Leave.Job.IndividualID==own||assigned.Contains(l.RequestId))&&
            (filter.DepartmentId==0||l.Leave.Job.OrganisationStructureId==filter.DepartmentId)&&(filter.JobTypeId==0||l.Leave.Job.JobTypeId==filter.JobTypeId)&&
            (search==""||l.Leave.Job.OrganisationStructure.Name.Contains(search)))
            .OrderByDescending(l=>l.Request.ApplicationDate).ThenByDescending(l=>l.RequestId)
            .Select(l=>new OrganisationProcessingLeave(l.RequestId,l.Request.ReferenceNumber??"#"+l.RequestId,
                ((l.Leave.Job.Individual.FirstNameEnglish??"")+" "+(l.Leave.Job.Individual.MiddleNameEnglish??"")+" "+(l.Leave.Job.Individual.LastNameEnglish??"")).Replace("  "," ").Trim(),
                l.Leave.LeaveType.Name,l.Request.RequestState.StateName,l.Leave.FromDate,l.Leave.ToDate,l.Leave.Duration,l.Leave.Job.IndividualID==own||assigned.Contains(l.RequestId))).ToListAsync(ct);

        var departments=db.OrganisationStructureSupervisors.Where(s=>s.SupervisorStaffId==own&&s.IsActive&&s.IsValid&&s.OrganisationStructure.OrganisationBusinessEntityID==org).Select(s=>s.OrganisationStructureId);
        var teams=db.TeamStaffs.Where(t=>t.StaffId==own&&t.IsSuperVisor&&t.IsValid&&t.StartDate<=today&&(!t.EndDate.HasValue||t.EndDate>=today)&&t.Team.OrganisationId==org&&t.Team.IsValid==true&&
            (!t.Team.StartDate.HasValue||t.Team.StartDate<=today)&&(!t.Team.EndDate.HasValue||t.Team.EndDate>=today)).Select(t=>t.TeamId);
        var reports=db.TeamStaffs.Where(t=>teams.Contains(t.TeamId)&&t.IsValid&&t.StartDate<=today&&(!t.EndDate.HasValue||t.EndDate>=today)).Select(t=>t.StaffId);
        var staff=FormerStaffPolicy.CurrentJobs(db).Where(j=>j.OrganisationID==org&&(wide||j.IndividualID==own||departments.Contains(j.OrganisationStructureId)||reports.Contains(j.IndividualID))&&
            (filter.DepartmentId==0||j.OrganisationStructureId==filter.DepartmentId)&&(filter.JobTypeId==0||j.JobTypeId==filter.JobTypeId)&&
            (search==""||j.OrganisationStructure.Name.Contains(search))).Select(j=>j.IndividualID);
        var plans=await db.BusinessEntitySchedules.AsNoTracking().Where(p=>staff.Contains(p.BusinessEntityId)&&p.IsActive&&p.IsLinked&&p.Schedule.IsValid&&p.Schedule.ScheduleType.Name=="Planned leave"&&
            p.Schedule.StartDate<yearEnd&&p.Schedule.EndDate>=yearStart&&p.Schedule.CalendarSchedules.Any(c=>c.Calendar.BusinessEntityCalendars.Any(b=>b.BusinessEntityId==p.BusinessEntityId&&b.IsLinked)))
            .Select(p=>new{Person=p.BusinessEntityId,From=p.Schedule.StartDate,To=p.Schedule.EndDate}).Distinct().ToListAsync(ct);
        for(var month=1;month<=12;month++){var from=new DateTime(filter.Year,month,1);var to=from.AddMonths(1);data.MonthlyPlans.Add(new(month,plans.Where(p=>p.From<to&&p.To>=from).Select(p=>p.Person).Distinct().Count()));}

        var calendars=await db.BusinessEntityCalendars.AsNoTracking().Where(b=>b.BusinessEntityId==org&&b.IsLinked&&b.StartDate<monthEnd&&(!b.EndDate.HasValue||b.EndDate>=monthStart))
            .Select(b=>new{Id=b.CalenderId,b.Calender.Name,b.StartDate,b.EndDate}).ToListAsync(ct);
        data.Calendars=calendars.Select(c=>c.Name).Distinct().Order().ToList();var calendarIds=calendars.Select(c=>c.Id).ToArray();
        var events=await db.CalendarSchedules.AsNoTracking().Where(c=>calendarIds.Contains(c.CalendarId)&&c.Schedule.IsValid&&c.Schedule.StartDate<monthEnd&&c.Schedule.EndDate>=monthStart)
            .Select(c=>new{c.CalendarId,c.ScheduleId,c.Schedule.Name,c.Schedule.StartDate,c.Schedule.EndDate}).ToListAsync(ct);
        foreach(var e in events)foreach(var c in calendars.Where(c=>c.Id==e.CalendarId))
        {
            var from=new[]{e.StartDate.Date,c.StartDate.Date,monthStart}.Max();var to=new[]{e.EndDate.Date,c.EndDate?.Date??monthEnd.AddDays(-1),monthEnd.AddDays(-1)}.Min();
            if(from<=to)data.CalendarEntries.Add(new($"schedule:{e.ScheduleId}:{from:yyyyMMdd}",e.Name,from,to,c.Name));
        }
        var elements=await db.Elements.AsNoTracking().Where(e=>e.IsValid&&(e.Date>=monthStart&&e.Date<monthEnd||e.Date==null&&e.DayOfWeekId!=null)&&e.Calendars.Any(c=>calendarIds.Contains(c.CalenderId)))
            .Select(e=>new{e.ElementId,Title=e.Description??e.SpecialDayType.Name,e.Date,Weekday=e.DayOfWeek==null?null:e.DayOfWeek.Name,CalendarIds=e.Calendars.Select(c=>c.CalenderId).ToList()}).ToListAsync(ct);
        foreach(var e in elements)for(var day=monthStart;day<monthEnd;day=day.AddDays(1))
        {
            if(e.Date?.Date!=day&&!(e.Date==null&&System.Enum.TryParse<System.DayOfWeek>(e.Weekday,true,out var weekday)&&day.DayOfWeek==weekday))continue;
            var linked=calendars.Where(c=>e.CalendarIds.Contains(c.Id)&&c.StartDate.Date<=day&&(!c.EndDate.HasValue||c.EndDate.Value.Date>=day)).ToList();
            if(linked.Count>0)data.CalendarEntries.Add(new($"element:{e.ElementId}:{day:yyyyMMdd}",e.Title,day,day,string.Join(", ",linked.Select(c=>c.Name).Distinct())));
        }
        data.CalendarEntries=data.CalendarEntries.DistinctBy(e=>e.Key).OrderBy(e=>e.From).ThenBy(e=>e.Title).ToList();return data;
    }
}
