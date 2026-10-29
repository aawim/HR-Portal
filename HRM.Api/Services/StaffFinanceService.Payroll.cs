using System.Data;
using HRM.Application.Contracts.Pages;
using HRM.Application.Features.StaffFinance;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.Models;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;

public sealed partial class StaffFinanceService
{
    public async Task<List<FinanceOption>> GetPayrollCyclesAsync(DateTime month)
    {
        var actor=await ScopeAsync();if(!actor.Post)throw new UnauthorizedAccessException("Finance payroll permission is required.");
        await using var db=await factory.CreateDbContextAsync();
        return await db.PayrollCycles.Where(x=>x.PayrollMonth.Year==month.Year&&x.PayrollMonth.Month==month.Month&&x.OrganisationPayrollPeriod.OrganisationId==actor.Org)
            .OrderByDescending(x=>x.RequestId).Select(x=>new FinanceOption(x.RequestId,x.Request.ReferenceNumber??("Cycle "+x.RequestId))).ToListAsync();
    }
    public async Task<FinancePayrollPreview> PreviewPayrollAsync(DateTime month,List<int> jobIds)
    {
        var actor=await ScopeAsync();if(!actor.Post)throw new UnauthorizedAccessException("Finance payroll permission is required.");
        await using var db=await factory.CreateDbContextAsync();return await PreviewPayrollAsync(db,actor,month,jobIds);
    }
    private static bool DueMonth(DateTime month,DateTime start,int frequency) => ((month.Year-start.Year)*12+month.Month-start.Month)%frequency==0;
    private static async Task<FinancePayrollPreview> PreviewPayrollAsync(HrmTeContext db,Scope actor,DateTime month,List<int> jobIds)
    {
        if(month.Year is <2000 or >2100||jobIds.Count is <1 or >500)throw new InvalidOperationException("Choose a payroll month and 1–500 employment records.");
        month=new(month.Year,month.Month,1);var last=month.AddMonths(1).AddDays(-1);var ids=jobIds.Distinct().ToArray();
        var jobs=await db.Jobs.Where(x=>ids.Contains(x.JobId)&&x.OrganisationID==actor.Org).ToListAsync();
        if(jobs.Count!=ids.Length)throw new UnauthorizedAccessException("Some employment records are unavailable in this organisation.");
        var lines=new List<FinancePayrollLine>();var issues=new List<string>();
        var types=await db.AllowanceTypes.Where(x=>x.OrganisationId==actor.Org).ToDictionaryAsync(x=>x.Id,x=>x.Name);
        foreach(var job in jobs)
        {
            var name=await NameAsync(db,job.IndividualID);
            if(!Active(job)){issues.Add($"{name}: active employment is required to process payroll.");continue;}
            if(job.JoinedDate.Date>last)continue;
            var allowances=await db.StaffAllowances.Where(x=>x.JobId==job.JobId&&x.OrganisationId==actor.Org&&x.FromDate<=last&&(!x.ToDate.HasValue||x.ToDate>=month)).ToListAsync();
            foreach(var a in allowances.Where(x=>DueMonth(month,x.FromDate,x.FrequencyMonths)))
            {
                var start=a.FromDate.Date>job.JoinedDate.Date?a.FromDate.Date:job.JoinedDate.Date;
                var amount=FinanceCalculations.Prorate(a.Amount,month,start,a.ToDate);
                if(amount>0)lines.Add(new(job.JobId,job.IndividualID,name,$"A:{a.Id}","Allowance",types[a.AllowanceTypeId],a.CurrencyTypeId,amount,AllowanceId:a.Id));
            }
            // Required entitlements are derived from approved position periods; no duplicate manual assignment is needed.
            var positions=await db.JobPositions.Where(x=>x.JobId==job.JobId&&x.JobPositionStateId==4&&x.FromDate<=last&&(!x.ToDate.HasValue||x.ToDate>month)).ToListAsync();
            var positionIds=positions.Select(x=>x.PositionId).ToArray();var rules=await db.PositionAllowanceRules.Where(x=>x.OrganisationId==actor.Org&&positionIds.Contains(x.PositionId)&&x.FromDate<=last&&(!x.ToDate.HasValue||x.ToDate>=month)).ToListAsync();
            var daily=new Dictionary<(long Rule,int Position),decimal>();
            for(var day=month;day<=last;day=day.AddDays(1))
            {
                if(day<job.JoinedDate.Date)continue;
                var candidates=(from p in positions from r in rules where r.PositionId==p.PositionId&&p.FromDate.Date<=day&&(!p.ToDate.HasValue||p.ToDate.Value.Date>day)&&r.FromDate.Date<=day&&(!r.ToDate.HasValue||r.ToDate.Value.Date>=day)&&DueMonth(month,r.FromDate,r.FrequencyMonths) select new{P=p,R=r}).ToList();
                foreach(var group in candidates.GroupBy(x=>new{x.R.AllowanceTypeId,x.R.CurrencyTypeId}))
                {
                    var policy=group.First().R.MultiplePositionPolicy;
                    var selected=policy=="Stack"?group.ToList():policy=="Highest"?group.OrderByDescending(x=>x.R.Amount).ThenBy(x=>x.P.JobPositionId).Take(1).ToList():group.OrderBy(x=>x.P.JobPositionId).Take(1).ToList();
                    foreach(var c in selected){var key=(c.R.Id,c.P.JobPositionId);daily[key]=daily.GetValueOrDefault(key)+c.R.Amount/DateTime.DaysInMonth(month.Year,month.Month);}
                }
            }
            foreach(var item in daily)
            {
                var r=rules.Single(x=>x.Id==item.Key.Rule);var amount=decimal.Round(item.Value,2,MidpointRounding.AwayFromZero);
                if(amount>0)lines.Add(new(job.JobId,job.IndividualID,name,$"P:{r.Id}:{item.Key.Position}","Allowance",types[r.AllowanceTypeId]+" · Required position allowance",r.CurrencyTypeId,amount,PositionRuleId:r.Id,JobPositionId:item.Key.Position));
            }
            var due=await (from i in db.DeductionInstalments join a in db.DeductionAgreements on i.AgreementId equals a.Id where a.JobId==job.JobId&&a.OrganisationId==actor.Org&&!i.IsSuperseded&&i.DueDate<=last select new{I=i,A=a}).ToListAsync();
            foreach(var d in due)
            {
                var outstanding=d.I.Amount-await AllocatedAsync(db,d.I.Id);
                if(outstanding>0)lines.Add(new(job.JobId,job.IndividualID,name,$"D:{d.I.Id}","Deduction",d.A.Reference+" · "+d.I.DueDate.ToString("dd MMM yyyy"),d.A.CurrencyTypeId,outstanding,d.I.Id));
            }
            foreach(var currency in lines.Where(x=>x.JobId==job.JobId&&x.Kind=="Deduction").GroupBy(x=>x.CurrencyTypeId))
            {
                try
                {
                    var policy=await PolicyAsync(db,actor.Org,currency.Key,last);
                    if(!job.BasicSalary.HasValue||currency.Sum(x=>x.Amount)>FinanceCalculations.MonthlyCap(job.BasicSalary??0,policy.MaximumPerMonth,policy.MaximumPercentOfBasic,policy.MinimumTakeHome))issues.Add($"{name}: deductions exceed the payroll limit or basic salary is unavailable. Resolve the repayment schedule before posting.");
                }
                catch(InvalidOperationException ex){issues.Add($"{name}: {ex.Message}");}
            }
        }
        var posted=await db.StaffFinancePayrollEntries.Where(x=>x.OrganisationId==actor.Org&&x.Month==month).Select(x=>x.SourceKey).ToListAsync();
        if(lines.Any(x=>posted.Contains(x.SourceKey)))issues.Add("Some selected entries have already been posted for this month. Select only unprocessed staff.");
        var ordered=lines.OrderBy(x=>x.SourceKey,StringComparer.Ordinal).ToList();
        var fingerprint=Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new{Month=month,Lines=ordered})));
        return new(month,ordered,issues,fingerprint);
    }
    public async Task<ServiceResult> PostPayrollAsync(FinancePayrollInput input)
    {
        var actor=await ScopeAsync();if(!actor.Post)throw new UnauthorizedAccessException("Finance payroll permission is required.");
        if(input.ClientBatchId==Guid.Empty||string.IsNullOrWhiteSpace(input.Reference)||input.Reference.Length>200)return ServiceResult.Fail("Enter a payroll reference and refresh the submission.");
        await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var month=new DateTime(input.Month.Year,input.Month.Month,1);
            var existing=await db.StaffFinancePayrollBatches.SingleOrDefaultAsync(x=>x.OrganisationId==actor.Org&&x.ClientBatchId==input.ClientBatchId);
            if(existing!=null)
            {
                return existing.PostedByUserId==actor.User&&existing.Month==month&&existing.Reference==input.Reference.Trim()&&existing.PayrollCycleRequestId==input.PayrollCycleRequestId&&existing.SelectionJson==System.Text.Json.JsonSerializer.Serialize(input.JobIds.Distinct().Order())&&existing.Fingerprint==input.ExpectedFingerprint?ServiceResult.Ok("This payroll batch was already posted."):ServiceResult.Fail("This payroll submission changed. Refresh and try again.");
            }
            if(!await db.PayrollCycles.AnyAsync(x=>x.RequestId==input.PayrollCycleRequestId&&x.PayrollMonth.Year==month.Year&&x.PayrollMonth.Month==month.Month&&x.OrganisationPayrollPeriod.OrganisationId==actor.Org))throw new InvalidOperationException("Select a payroll cycle for this organisation and month.");
            var preview=await PreviewPayrollAsync(db,actor,input.Month,input.JobIds);
            if(preview.Issues.Count>0)throw new InvalidOperationException(string.Join(" ",preview.Issues));
            if(preview.Lines.Count==0)throw new InvalidOperationException("There are no eligible payroll entries.");
            if(preview.Fingerprint!=input.ExpectedFingerprint)throw new InvalidOperationException("The payroll amounts changed after preview. Preview again before posting.");
            if(preview.Lines.Any(x=>x.IndividualId==actor.Person))throw new InvalidOperationException("Another authorised payroll officer must process your own allowances and deductions.");
            var batch=new StaffFinancePayrollBatch{OrganisationId=actor.Org,Month=month,PayrollCycleRequestId=input.PayrollCycleRequestId,ClientBatchId=input.ClientBatchId,Reference=input.Reference.Trim(),PostedByUserId=actor.User,PostedAtUtc=DateTime.UtcNow,SelectionJson=System.Text.Json.JsonSerializer.Serialize(input.JobIds.Distinct().Order()),Fingerprint=preview.Fingerprint};db.StaffFinancePayrollBatches.Add(batch);await db.SaveChangesAsync();
            foreach(var line in preview.Lines)
            {
                var entry=new StaffFinancePayrollEntry{BatchId=batch.Id,OrganisationId=actor.Org,JobId=line.JobId,IndividualId=line.IndividualId,Month=month,SourceKey=line.SourceKey,Kind=line.Kind,Description=line.Description,CurrencyTypeId=line.CurrencyTypeId,Amount=line.Amount,InstalmentId=line.InstalmentId,AllowanceId=line.AllowanceId,PositionRuleId=line.PositionRuleId,JobPositionId=line.JobPositionId};db.StaffFinancePayrollEntries.Add(entry);await db.SaveChangesAsync();
                if(line.InstalmentId.HasValue){var instalment=await db.DeductionInstalments.SingleAsync(x=>x.Id==line.InstalmentId);await AllocatePaymentAsync(db,instalment.AgreementId,line.Amount,Today,batch.Reference,actor.User,null,entry.Id,null,instalment.Id);}
            }
            await logs.CreateAsync(db,SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE,$"Finance payroll batch {batch.Id} posted; cycle {input.PayrollCycleRequestId}; month {month:yyyy-MM}; tenant {actor.Org}; reference {batch.Reference}.");
            foreach(var person in preview.Lines.Select(x=>x.IndividualId).Distinct())
            {
                var users=await db.Users.Where(x=>x.BusinessEntityID==person&&x.IsActive).Select(x=>x.UserId).ToListAsync();
                foreach(var user in users)db.StaffNotifications.Add(new(){UserId=user,IndividualId=person,OrganisationId=actor.Org,Kind="Payroll",Message=$"Allowances and deductions processed for {month:MMMM yyyy}. Reference: {batch.Reference}",Url=PageUrl,CreatedAtUtc=DateTime.UtcNow});
            }
            await db.SaveChangesAsync();await tx.CommitAsync();return ServiceResult.Ok("Payroll allowances and deductions posted. These records are locked.");
        }
        catch(InvalidOperationException ex){await tx.RollbackAsync();return ServiceResult.Fail(ex.Message);}
        catch(DbUpdateException){await tx.RollbackAsync();return ServiceResult.Fail("Some entries were already processed or changed. Refresh before posting again.");}
    }
    public async Task<List<FinancePayrollHistory>> PayrollHistoryAsync(int? staffId=null)
    {
        var actor=await ScopeAsync();if(staffId.HasValue&&staffId!=actor.Person&&!actor.Manage&&!actor.Post)throw new UnauthorizedAccessException();
        var all=!staffId.HasValue&&(actor.Manage||actor.Post);var person=staffId??actor.Person;await using var db=await factory.CreateDbContextAsync();
        var entries=await db.StaffFinancePayrollEntries.AsNoTracking().Where(x=>x.OrganisationId==actor.Org&&(all||x.IndividualId==person)).OrderByDescending(x=>x.Month).Take(2000).ToListAsync();
        var ids=entries.Select(x=>x.BatchId).Distinct().ToArray();var batches=await db.StaffFinancePayrollBatches.Where(x=>ids.Contains(x.Id)).OrderByDescending(x=>x.PostedAtUtc).ToListAsync();var result=new List<FinancePayrollHistory>();
        foreach(var batch in batches)
        {var lines=new List<FinancePayrollLine>();foreach(var e in entries.Where(x=>x.BatchId==batch.Id))lines.Add(new(e.JobId,e.IndividualId,await NameAsync(db,e.IndividualId),e.SourceKey,e.Kind,e.Description,e.CurrencyTypeId,e.Amount,e.InstalmentId,e.AllowanceId,e.PositionRuleId,e.JobPositionId));result.Add(new(batch.Id,batch.Month,batch.Reference,batch.PostedAtUtc,lines));}return result;
    }
}
