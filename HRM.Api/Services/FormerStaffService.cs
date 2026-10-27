using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
namespace HRM.Services;





public sealed partial class FormerStaffService(IDbContextFactory<HrmTeContext> factory,IUserAccessService access)
:global::HRM.Services.Contracts.FormerStaffServiceContracts,global::HRM.Services.Contracts.FormerStaffServiceContracts.IOperations{
    private async Task<int> ScopeAsync()
    {
        var actor=await access.RequireContextAsync();var org=await access.GetCurrentOrganisationIdAsync();
        if(org<=0||!await access.HasOrganisationAccessAsync(org))throw new UnauthorizedAccessException("Select your current organisation.");
        if(!actor.IsSuperAdministrator&&!actor.IsHRStaff&&!actor.IsHRHead&&!actor.IsHRManager&&!actor.IsOrganisationAdmin&&!await access.HasPermissionAsync("Admin")&&!await access.HasPermissionAsync("Staff.Details"))throw new UnauthorizedAccessException("You do not have permission to view former staff records.");
        return org;
    }
    public async Task<FormerStaffData> SearchAsync(string? search=null,CancellationToken ct=default,FormerStaffFilters? filters=null)
    {
        filters??=new();
        if(search?.Length>250)throw new InvalidOperationException("Search must be 250 characters or fewer.");
        if(filters.From.HasValue&&(filters.From.Value.Year<1900||filters.From.Value.Year>2200)||filters.To.HasValue&&(filters.To.Value.Year<1900||filters.To.Value.Year>2200)||filters.From.HasValue&&filters.To.HasValue&&filters.From.Value.Date>filters.To.Value.Date)throw new InvalidOperationException("Enter a valid employment end-date range.");
        var org=await ScopeAsync();await using var db=await factory.CreateDbContextAsync(ct);
        var current=FormerStaffPolicy.CurrentJobs(db);var endedJobs=FormerStaffPolicy.EndedJobs(db);
        // Display and filter the same latest ended employment, rather than silently matching an older department.
        var latest=endedJobs.Where(j=>j.OrganisationID==org&&!current.Any(a=>a.IndividualID==j.IndividualID&&a.OrganisationID==org)&&
            !endedJobs.Any(n=>n.OrganisationID==org&&n.IndividualID==j.IndividualID&&(n.JoinedDate>j.JoinedDate||n.JoinedDate==j.JoinedDate&&n.JobId>j.JobId)));
        var departments=await latest.Select(j=>new{Id=j.OrganisationStructureId,Name=j.OrganisationStructure.Name}).Distinct().OrderBy(x=>x.Name).Select(x=>new FormerStaffChoice(x.Id,x.Name)).ToListAsync(ct);
        var types=await latest.Select(j=>new{Id=j.JobTypeId,Name=j.JobType.TypeName}).Distinct().OrderBy(x=>x.Name).Select(x=>new FormerStaffChoice(x.Id,x.Name)).ToListAsync(ct);
        var verified=await db.VerifiedStates.AsNoTracking().OrderBy(x=>x.StateName).Select(x=>new FormerStaffChoice(x.VerifiedStateId,x.StateName.Trim())).ToListAsync(ct);
        var states=await db.BusinessEntityStates.AsNoTracking().OrderBy(x=>x.StateName).Select(x=>new FormerStaffChoice(x.BusinessEntityStateId,x.StateName.Trim())).ToListAsync(ct);
        if(filters.DepartmentId.HasValue&&!departments.Any(x=>x.Id==filters.DepartmentId))throw new InvalidOperationException("Select a department from the current organisation.");
        if(filters.JobTypeId.HasValue&&!types.Any(x=>x.Id==filters.JobTypeId))throw new InvalidOperationException("Select an available Job Type.");
        if(filters.VerifiedStateId.HasValue&&filters.VerifiedStateId!=-1&&!verified.Any(x=>x.Id==filters.VerifiedStateId))throw new InvalidOperationException("Select a valid verification state.");
        if(filters.BusinessEntityStateId.HasValue&&filters.BusinessEntityStateId!=-1&&!states.Any(x=>x.Id==filters.BusinessEntityStateId))throw new InvalidOperationException("Select a valid business entity state.");
        var jobs=latest.Select(j=>new{j.JobId,j.IndividualID,j.OrganisationStructureId,j.JobTypeId,j.JoinedDate,Ended=j.TerminatedDate??db.JobPositions.Where(p=>p.JobId==j.JobId&&p.JobPositionStateId==4).Max(p=>p.ToDate),Department=j.OrganisationStructure.Name,Type=j.JobType.TypeName});
        if(filters.DepartmentId.HasValue)jobs=jobs.Where(j=>j.OrganisationStructureId==filters.DepartmentId);
        if(filters.JobTypeId.HasValue)jobs=jobs.Where(j=>j.JobTypeId==filters.JobTypeId);
        if(filters.From.HasValue){var from=filters.From.Value.Date;jobs=jobs.Where(j=>j.Ended.HasValue&&j.Ended.Value>=from);}
        if(filters.To.HasValue){var end=filters.To.Value.Date.AddDays(1);jobs=jobs.Where(j=>j.Ended.HasValue&&j.Ended.Value<end);}
        var source=db.Individuals.AsNoTracking().Where(i=>jobs.Any(j=>j.IndividualID==i.BusinessEntityId));
        if(filters.VerifiedStateId.HasValue)source=source.Where(i=>db.BusinessEntities.Any(b=>b.BusinessEntityID==i.BusinessEntityId&&(filters.VerifiedStateId==-1?b.VerifiedStateId==null:b.VerifiedStateId==filters.VerifiedStateId)));
        if(filters.BusinessEntityStateId.HasValue)source=source.Where(i=>db.BusinessEntities.Any(b=>b.BusinessEntityID==i.BusinessEntityId&&(filters.BusinessEntityStateId==-1?b.BusinessEntityStateId==null:b.BusinessEntityStateId==filters.BusinessEntityStateId)));
        foreach(var word in (search??"").Trim().Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries))
        {
            var term=word;
            source=source.Where(i=>((i.FirstNameEnglish??"")+" "+(i.MiddleNameEnglish??"")+" "+(i.LastNameEnglish??"")).Contains(term)||db.Idcards.Any(c=>c.BusinessEntityId==i.BusinessEntityId&&c.IdcardNumber.Contains(term))||db.Passports.Any(c=>c.BusinessEntityId==i.BusinessEntityId&&c.PassportNumber.Contains(term))||db.Staffs.Any(s=>s.IndividualId==i.BusinessEntityId&&(s.EmployeeNumber??"").Contains(term)));
        }
        var persons=await source.OrderBy(i=>i.FirstNameEnglish).Select(i=>new{i.BusinessEntityId,i.FirstNameEnglish,i.MiddleNameEnglish,i.LastNameEnglish,
            Verified=db.BusinessEntities.Where(b=>b.BusinessEntityID==i.BusinessEntityId).Select(b=>b.VerifiedStateId).FirstOrDefault(),
            State=db.BusinessEntities.Where(b=>b.BusinessEntityID==i.BusinessEntityId).Select(b=>b.BusinessEntityStateId).FirstOrDefault()}).ToListAsync(ct);
        var result=new List<FormerStaffRow>();
        foreach(var person in persons)
        {
            var job=await jobs.SingleAsync(j=>j.IndividualID==person.BusinessEntityId,ct);
            result.Add(new(person.BusinessEntityId,string.Join(" ",new[]{person.FirstNameEnglish,person.MiddleNameEnglish,person.LastNameEnglish}.Where(x=>!string.IsNullOrWhiteSpace(x))),await db.Staffs.Where(s=>s.IndividualId==person.BusinessEntityId).Select(s=>s.EmployeeNumber).FirstOrDefaultAsync(ct)??"",await db.Idcards.Where(c=>c.BusinessEntityId==person.BusinessEntityId).Select(c=>c.IdcardNumber).FirstOrDefaultAsync(ct)??"",await db.Passports.Where(c=>c.BusinessEntityId==person.BusinessEntityId).Select(c=>c.PassportNumber).FirstOrDefaultAsync(ct)??"",job.Department,job.Type,job.JoinedDate,job.Ended)
            {DepartmentId=job.OrganisationStructureId,JobTypeId=job.JobTypeId,VerifiedStateId=person.Verified,BusinessEntityStateId=person.State,VerificationState=ChoiceName(verified,person.Verified),EntityState=ChoiceName(states,person.State)});
        }
        var description=$"Department: {FilterName(departments,filters.DepartmentId)} · Job Type: {FilterName(types,filters.JobTypeId)} · Ended: {filters.From?.ToString("dd MMM yyyy")??"Any"} – {filters.To?.ToString("dd MMM yyyy")??"Any"} · Verification: {FilterName(verified,filters.VerifiedStateId)} · Entity state: {FilterName(states,filters.BusinessEntityStateId)}"+(string.IsNullOrWhiteSpace(search)?"":" · Search: "+search.Trim());
        return new(await db.Organisations.Where(o=>o.BusinessEntityID==org).Select(o=>o.OrganisationName).SingleAsync(ct),result){Departments=departments,JobTypes=types,VerifiedStates=verified,EntityStates=states,FilterDescription=description};
    }
    private static string ChoiceName(List<FormerStaffChoice> choices,int? id)=>id.HasValue?choices.FirstOrDefault(x=>x.Id==id)?.Name??$"State {id}":"Not set";
    private static string FilterName(List<FormerStaffChoice> choices,int? id)=>!id.HasValue?"All":id==-1?"Not set":choices.FirstOrDefault(x=>x.Id==id)?.Name??id.ToString()!;
    public async Task<AttendanceExportReport> DetailAsync(int person,CancellationToken ct=default)
    {
        var org=await ScopeAsync();await using var db=await factory.CreateDbContextAsync(ct);
        if(!await FormerStaffPolicy.IsFormerAsync(db,person,org,ct))throw new UnauthorizedAccessException("Former staff record unavailable in the current organisation.");
        var individual=await db.Individuals.AsNoTracking().SingleAsync(i=>i.BusinessEntityId==person,ct);
        var jobs=db.Jobs.AsNoTracking().Where(j=>j.IndividualID==person&&j.OrganisationID==org);var jobIds=await jobs.Select(j=>j.JobId).ToListAsync(ct);
        var tables=new List<AttendanceExportTable>();
        async Task Add<T>(string title,IQueryable<T> source)where T:class
        {
            var rows=await source.AsNoTracking().ToListAsync(ct);
            var fields=db.Model.FindEntityType(typeof(T))!.GetProperties().Where(p=>p.PropertyInfo!=null&&p.ClrType!=typeof(byte[])&&!p.Name.Contains("Json",StringComparison.OrdinalIgnoreCase)&&!p.Name.Contains("OperationLog",StringComparison.OrdinalIgnoreCase)).ToArray();
            tables.Add(new(title,fields.Select(p=>System.Text.RegularExpressions.Regex.Replace(p.Name,"([a-z])([A-Z])","$1 $2")).ToArray(),rows.Select(r=>fields.Select(p=>Format(p.PropertyInfo!.GetValue(r))).ToArray()).ToList()));
        }
        await Add("Personal information",db.Individuals.Where(i=>i.BusinessEntityId==person));
        await Add("Identity cards",db.Idcards.Where(i=>i.BusinessEntityId==person));await Add("Passports",db.Passports.Where(i=>i.BusinessEntityId==person));
        await Add("Staff registration",db.Staffs.Where(i=>i.IndividualId==person));
        await Add("Contacts",db.BussinessEntityContactInformations.Where(i=>i.BussinessEntityId==person));
        var locations=db.BusinessEntityLocations.Where(i=>i.BusinessEntityId==person).Select(i=>i.LocationId);
        await Add("Address records",db.BusinessEntityLocations.Where(i=>i.BusinessEntityId==person));await Add("Addresses",db.AddressInstances.Where(i=>locations.Contains(i.LocationId)));
        var employment=await jobs.Select(j=>new{j.JobId,j.JoinedDate,j.TerminatedDate,Department=j.OrganisationStructure.Name,Type=j.JobType.TypeName,State=j.JobState.StateName,j.BasicSalary,j.Sapnumber}).ToListAsync(ct);
        tables.Add(new("Employment history",["Job","Department","Job type","Joined","Ended","State","Basic salary","SAP number"],employment.Select(j=>new object?[]{j.JobId,j.Department,j.Type,j.JoinedDate.ToString("yyyy-MM-dd"),j.TerminatedDate?.ToString("yyyy-MM-dd"),j.State,j.BasicSalary,j.Sapnumber}).ToList()));
        var positions=await db.JobPositions.Where(p=>jobIds.Contains(p.JobId)).Select(p=>new{p.JobPositionId,p.JobId,p.Position.Name,p.FromDate,p.ToDate,State=p.JobPositionState.StateName}).ToListAsync(ct);
        tables.Add(new("Position history",["Assignment","Job","Position","From","To","State"],positions.Select(p=>new object?[]{p.JobPositionId,p.JobId,p.Name,p.FromDate.ToString("yyyy-MM-dd"),p.ToDate?.ToString("yyyy-MM-dd"),p.State}).ToList()));
        var balances=await db.JobLeaveTypes.Where(l=>jobIds.Contains(l.JobId)).Select(l=>new{l.JobId,Type=l.LeaveType.Name,l.RemainingDays,l.EffectiveFromDate,l.EffectiveToDate,l.IsValid}).ToListAsync(ct);
        tables.Add(new("Leave entitlements",["Job","Leave type","Balance","Effective from","Effective to","Valid"],balances.Select(l=>new object?[]{l.JobId,l.Type,l.RemainingDays,l.EffectiveFromDate?.ToString("yyyy-MM-dd"),l.EffectiveToDate?.ToString("yyyy-MM-dd"),l.IsValid}).ToList()));
        var leaves=await db.Leaves.Where(l=>jobIds.Contains(l.JobId)).Select(l=>new{l.LeaveId,l.JobId,Type=l.LeaveType.Name,l.FromDate,l.ToDate,l.Duration,State=l.LeaveState.StateName}).ToListAsync(ct);
        tables.Add(new("Leave history",["Leave","Job","Type","From","To","Days","State"],leaves.Select(l=>new object?[]{l.LeaveId,l.JobId,l.Type,l.FromDate.ToString("yyyy-MM-dd"),l.ToDate.ToString("yyyy-MM-dd"),l.Duration,l.State}).ToList()));
        var requests=await db.Requests.Where(r=>r.ApplicantBusinessEntityId==person&&r.OrganisationBusinessEntityID==org).OrderByDescending(r=>r.ApplicationDate).Select(r=>new{r.RequestId,r.ReferenceNumber,Type=r.RequestType.TypeName,State=r.RequestState.StateName,r.ApplicationDate,r.RequestEffectiveDate,r.StateChangeRemarks}).ToListAsync(ct);
        tables.Add(new("Request history",["Request","Reference","Type","State","Applied","Effective","Remarks"],requests.Select(r=>new object?[]{r.RequestId,r.ReferenceNumber,r.Type,r.State,r.ApplicationDate.AddHours(5).ToString("yyyy-MM-dd HH:mm"),r.RequestEffectiveDate?.ToString("yyyy-MM-dd"),r.StateChangeRemarks}).ToList()));
        await Add("Overtime claims",db.OvertimeRequests.Where(r=>r.IndividualId==person&&r.OrganisationId==org));
        await Add("Payroll allocation",db.OvertimePayrollItems.Where(r=>r.IndividualId==person&&r.Batch.OrganisationId==org));
        await Add("Work plans",db.WorkPlans.Where(r=>r.IndividualId==person&&r.OrganisationBusinessEntityId==org));
        await Add("Position salaries",db.JobPositionBasicSalaries.Where(r=>jobIds.Contains(r.JobPoistion.JobId)));
        await Add("Team memberships",db.TeamStaffs.Where(r=>r.StaffId==person&&r.Team.OrganisationId==org));
        await Add("Group memberships",db.GroupStaffs.Where(r=>r.StaffIndividualId==person&&r.Group.BusinessEntityOrganisationId==org));
        var addressIds=db.AddressInstances.Where(i=>locations.Contains(i.LocationId)).Select(i=>i.AddressBaseId);
        await Add("Address details",db.AddressBases.Where(i=>addressIds.Contains(i.AddressBaseId)));
        var docIds=db.BusinessEntitiesDocuments.Where(r=>r.BusinessEntityId==person).Select(r=>r.DocumentId);
        await Add("Documents",db.Documents.Where(r=>docIds.Contains(r.DocumentId)));
        return new("Former staff · "+string.Join(" ",new[]{individual.FirstNameEnglish,individual.MiddleNameEnglish,individual.LastNameEnglish}.Where(x=>!string.IsNullOrWhiteSpace(x))),"Read-only employee record · "+await db.Organisations.Where(o=>o.BusinessEntityID==org).Select(o=>o.OrganisationName).SingleAsync(ct),tables);
    }
    private static object? Format(object? value)=>value switch{DateTime d=>d.ToString("yyyy-MM-dd HH:mm",CultureInfo.InvariantCulture),DateOnly d=>d.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),_=>value};
}
