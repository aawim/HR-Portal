using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
namespace HRM.Services;

public sealed record FormerStaffRow(int Id,string Name,string EmployeeNumber,string NationalId,string Passport,string Department,string JobType,DateTime Joined,DateTime? Ended);
public sealed record FormerStaffData(string Organisation,List<FormerStaffRow> Staff);
public sealed class FormerStaffService(IDbContextFactory<HrmTeContext> factory,IUserAccessService access)
{
    private async Task<int> ScopeAsync()
    {
        var actor=await access.RequireContextAsync();var org=await access.GetCurrentOrganisationIdAsync();
        if(org<=0||!await access.HasOrganisationAccessAsync(org))throw new UnauthorizedAccessException("Select your current organisation.");
        if(!actor.IsSuperAdministrator&&!actor.IsHRStaff&&!actor.IsHRHead&&!actor.IsHRManager&&!actor.IsOrganisationAdmin&&!await access.HasPermissionAsync("Admin")&&!await access.HasPermissionAsync("Staff.Details"))throw new UnauthorizedAccessException("You do not have permission to view former staff records.");
        return org;
    }
    public async Task<FormerStaffData> SearchAsync(string? search=null,CancellationToken ct=default)
    {
        var org=await ScopeAsync();await using var db=await factory.CreateDbContextAsync(ct);
        var current=FormerStaffPolicy.CurrentJobs(db);
        var people=FormerStaffPolicy.EndedJobs(db).Where(j=>j.OrganisationID==org&&!current.Any(a=>a.IndividualID==j.IndividualID&&a.OrganisationID==org)).Select(j=>j.IndividualID).Distinct();
        var source=db.Individuals.AsNoTracking().Where(i=>people.Contains(i.BusinessEntityId));
        foreach(var word in (search??"").Trim().Split((char[]?)null,StringSplitOptions.RemoveEmptyEntries))
        {
            var term=word;
            source=source.Where(i=>((i.FirstNameEnglish??"")+" "+(i.MiddleNameEnglish??"")+" "+(i.LastNameEnglish??"")).Contains(term)||db.Idcards.Any(c=>c.BusinessEntityId==i.BusinessEntityId&&c.IdcardNumber.Contains(term))||db.Passports.Any(c=>c.BusinessEntityId==i.BusinessEntityId&&c.PassportNumber.Contains(term))||db.Staffs.Any(s=>s.IndividualId==i.BusinessEntityId&&(s.EmployeeNumber??"").Contains(term)));
        }
        var persons=await source.OrderBy(i=>i.FirstNameEnglish).Select(i=>new{i.BusinessEntityId,i.FirstNameEnglish,i.MiddleNameEnglish,i.LastNameEnglish}).ToListAsync(ct);var result=new List<FormerStaffRow>();
        foreach(var person in persons)
        {
            var job=await FormerStaffPolicy.EndedJobs(db).Where(j=>j.OrganisationID==org&&j.IndividualID==person.BusinessEntityId).OrderByDescending(j=>j.JoinedDate).Select(j=>new{j.JobId,j.JoinedDate,j.TerminatedDate,Department=j.OrganisationStructure.Name,Type=j.JobType.TypeName}).FirstAsync(ct);
            var end=job.TerminatedDate??await db.JobPositions.Where(p=>p.JobId==job.JobId&&p.JobPositionStateId==4).MaxAsync(p=>p.ToDate,ct);
            result.Add(new(person.BusinessEntityId,string.Join(" ",new[]{person.FirstNameEnglish,person.MiddleNameEnglish,person.LastNameEnglish}.Where(x=>!string.IsNullOrWhiteSpace(x))),await db.Staffs.Where(s=>s.IndividualId==person.BusinessEntityId).Select(s=>s.EmployeeNumber).FirstOrDefaultAsync(ct)??"",await db.Idcards.Where(c=>c.BusinessEntityId==person.BusinessEntityId).Select(c=>c.IdcardNumber).FirstOrDefaultAsync(ct)??"",await db.Passports.Where(c=>c.BusinessEntityId==person.BusinessEntityId).Select(c=>c.PassportNumber).FirstOrDefaultAsync(ct)??"",job.Department,job.Type,job.JoinedDate,end));
        }
        return new(await db.Organisations.Where(o=>o.BusinessEntityID==org).Select(o=>o.OrganisationName).SingleAsync(ct),result);
    }
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
