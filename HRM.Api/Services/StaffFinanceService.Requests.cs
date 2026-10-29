using System.Data;
using System.Text.Json;
using HRM.Application.Contracts.Pages;
using HRM.Application.Features.StaffFinance;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.Models;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;

public sealed partial class StaffFinanceService
{
    public async Task<List<FinanceInstalmentRow>> PreviewAgreementAsync(FinanceApplication input)
    {
        await ScopeAsync();
        return FinanceCalculations.Schedule(input.Amount,input.FromDate,input.InstalmentCount,input.FrequencyMonths)
            .Select(x=>new FinanceInstalmentRow(0,x.DueDate,x.Amount,0,1,false)).ToList();
    }
    public async Task<ServiceResult> SubmitAsync(FinanceApplication input)
    {
        var actor=await ScopeAsync();await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            if(input.ClientRequestId==Guid.Empty)throw new InvalidOperationException("Refresh the form before submitting.");
            var existing=await db.StaffFinanceRequests.SingleOrDefaultAsync(x=>x.OrganisationId==actor.Org&&x.ClientRequestId==input.ClientRequestId);
            if(existing!=null)return existing.CreatedByUserId==actor.User&&existing.PayloadJson==JsonSerializer.Serialize(input)?ServiceResult.Ok("This request was already submitted."):ServiceResult.Fail("This submission has changed. Refresh and try again.");
            var job=await db.Jobs.SingleOrDefaultAsync(x=>x.JobId==input.JobId&&x.OrganisationID==actor.Org)??throw new UnauthorizedAccessException("Employment unavailable in this organisation.");
            if(!Active(job))throw new InvalidOperationException("Active approved employment is required.");
            if(input.Kind=="PayrollCycle"&&(!actor.Post||job.JobId!=actor.Job||job.IndividualID!=actor.Person))throw new UnauthorizedAccessException("An authorised payroll officer must request a cycle using their own employment context.");
            if(job.IndividualID!=actor.Person&&!actor.Manage)throw new UnauthorizedAccessException("You can submit only for yourself.");
            await ValidateApplicationAsync(db,input,job,actor.Org,false);
            var typeName=TypeName(input.Kind);
            var type=await db.RequestTypes.SingleOrDefaultAsync(x=>x.TypeName==typeName)??throw new InvalidOperationException("This request is not available yet. Please contact HR.");
            var log=await logs.CreateAsync(db,SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE,$"{typeName} submitted; job {job.JobId}, organisation {actor.Org}, user {actor.User}.");
            var request=await requests.CreateAsync(db,new CreateRequestDto{RequestTypeId=type.RequestTypeId,ApplicantBusinessEntityId=job.IndividualID,OrganisationBusinessEntityId=actor.Org,OperationLogId=log.OperationLogId,EffectiveDate=input.FromDate.Date});
            db.StaffFinanceRequests.Add(new(){RequestId=request.RequestId,Request=request,OrganisationId=actor.Org,IndividualId=job.IndividualID,JobId=job.JobId,Kind=input.Kind,PayloadJson=JsonSerializer.Serialize(input),ClientRequestId=input.ClientRequestId,CreatedByUserId=actor.User});
            await db.SaveChangesAsync();await workflow.StartAsync(db,request.RequestId,job.JobId);await tx.CommitAsync();return ServiceResult.Ok($"{request.ReferenceNumber} submitted for approval.");
        }
        catch(InvalidOperationException ex){await tx.RollbackAsync();if(ex is WorkflowConfigurationException config)await config.NotifyAsync(factory);return ServiceResult.Fail(ex.Message);}
        catch(DbUpdateException){await tx.RollbackAsync();return ServiceResult.Fail("This request changed or already exists. Refresh and try again.");}
    }
    private async Task ValidateApplicationAsync(HrmTeContext db,FinanceApplication input,Job job,int org,bool approving)
    {
        _=TypeName(input.Kind);
        if(input.Reason.Trim().Length is <1 or >2000||input.Reference.Length>200||input.FromDate.Year is <2000 or >2100)throw new InvalidOperationException("Enter a valid date, reference (up to 200 characters) and reason (up to 2,000 characters).");
        if(input.Kind is "Allowance" or "Agreement" or "Payment")
        {
            if(input.Amount<=0||input.Amount>999999999||decimal.Round(input.Amount,2)!=input.Amount)throw new InvalidOperationException("Enter a positive amount with up to two decimal places.");
            if(!await db.FinanceCurrencies.AnyAsync(x=>x.CurrencyTypeId==input.CurrencyTypeId))throw new InvalidOperationException("Select a valid currency.");
        }
        if(input.Kind=="PayrollCycle")
        {
            var period=await db.OrganisationPayrollPeriods.SingleOrDefaultAsync(x=>x.OrganisationPayrollPeriodId==input.PayrollPeriodId&&x.OrganisationId==org&&x.IsValid)??throw new InvalidOperationException("Select an active organisation payroll period.");
            if(period.StartDay is <1 or >31||period.EndDay is <1 or >31)throw new InvalidOperationException("The payroll period needs valid start and end days.");
            if(await db.PayrollCycles.AnyAsync(x=>x.OrganisationPayrollPeriodId==period.OrganisationPayrollPeriodId&&x.PayrollMonth.Year==input.FromDate.Year&&x.PayrollMonth.Month==input.FromDate.Month))throw new InvalidOperationException("A payroll cycle already exists for this period and month.");
        }
        else if(input.Kind=="Allowance")
        {
            if(input.FrequencyMonths is not(1 or 6 or 12)||input.ToDate<input.FromDate)throw new InvalidOperationException("Select a valid allowance period and frequency.");
            if(!await db.AllowanceTypes.AnyAsync(x=>x.Id==input.AllowanceTypeId&&x.OrganisationId==org&&x.IsActive))throw new InvalidOperationException("Select an active allowance type.");
            if(await (from p in db.JobPositions join r in db.PositionAllowanceRules on p.PositionId equals r.PositionId
                where p.JobId==job.JobId&&p.JobPositionStateId==4&&r.OrganisationId==org&&r.AllowanceTypeId==input.AllowanceTypeId&&
                    p.FromDate<=(input.ToDate??DateTime.MaxValue)&&(!p.ToDate.HasValue||p.ToDate>input.FromDate)&&
                    r.FromDate<=(input.ToDate??DateTime.MaxValue)&&(!r.ToDate.HasValue||r.ToDate>=input.FromDate)
                select r.Id).AnyAsync())throw new InvalidOperationException("This allowance is already required by the position. Ask HR to review the position allowance rule instead of adding it twice.");
            if(await db.StaffAllowances.AnyAsync(x=>x.JobId==job.JobId&&x.AllowanceTypeId==input.AllowanceTypeId&&x.FromDate<=(input.ToDate??DateTime.MaxValue)&&(!x.ToDate.HasValue||x.ToDate>=input.FromDate)))throw new InvalidOperationException("An approved allowance of this type already overlaps these dates.");
            if(await db.StaffFinancePayrollEntries.AnyAsync(x=>x.JobId==job.JobId&&x.Month>=new DateTime(input.FromDate.Year,input.FromDate.Month,1)))throw new InvalidOperationException("This effective period includes posted payroll. Choose a future period.");
        }
        else if(input.Kind=="Agreement")
        {
            _=FinanceCalculations.Schedule(input.Amount,input.FromDate,input.InstalmentCount,input.FrequencyMonths);
            if(!approving&&input.FromDate.Date<Today||string.IsNullOrWhiteSpace(input.Reference)||input.Category is not("Loan" or "Goods" or "Insurance" or "Other"))throw new InvalidOperationException("Enter a current/future first due date, agreement reference and category.");
            if(!await db.BusinessEntities.AnyAsync(x=>x.BusinessEntityID==input.BusinessEntityId))throw new InvalidOperationException("Select a registered creditor or insurer.");
            if(await db.DeductionAgreements.AnyAsync(x=>x.OrganisationId==org&&x.CreditorBusinessEntityId==input.BusinessEntityId&&x.Reference==input.Reference.Trim()))throw new InvalidOperationException("This creditor agreement reference already exists.");
            var policy=await PolicyAsync(db,org,input.CurrencyTypeId,input.FromDate);
            var agreements=await db.DeductionAgreements.Where(x=>x.OrganisationId==org&&x.IndividualId==job.IndividualID&&x.CurrencyTypeId==input.CurrencyTypeId).ToListAsync();
            decimal outstanding=0;var activeCount=0;foreach(var a in agreements){var due=a.TotalAmount-await PaidAsync(db,a.Id);outstanding+=due;if(due>0)activeCount++;}
            if(outstanding+input.Amount>policy.MaximumOutstanding||activeCount>=policy.MaximumAgreements)throw new InvalidOperationException("This agreement exceeds the configured outstanding-balance or agreement-count limit.");
            await CheckScheduleCapAsync(db,job,input.CurrencyTypeId,FinanceCalculations.Schedule(input.Amount,input.FromDate,input.InstalmentCount,input.FrequencyMonths));
        }
        else if(input.Kind=="EndAllowance")
        {
            var allowance=await db.StaffAllowances.SingleOrDefaultAsync(x=>x.Id==input.AllowanceTypeId&&x.JobId==job.JobId&&x.OrganisationId==org)??throw new InvalidOperationException("Allowance unavailable.");
            if(input.FromDate<allowance.FromDate||input.FromDate.Date<Today||allowance.ToDate.HasValue&&input.FromDate>allowance.ToDate)throw new InvalidOperationException("Choose an end date within the current allowance period, from today onwards.");
            if(await db.StaffFinancePayrollEntries.AnyAsync(x=>x.AllowanceId==allowance.Id&&x.Month.AddMonths(1)>input.FromDate))throw new InvalidOperationException("Posted payroll covers this period. Choose a later end date.");
        }
        else
        {
            var agreement=await db.DeductionAgreements.SingleOrDefaultAsync(x=>x.Id==input.AgreementId&&x.OrganisationId==org&&x.JobId==job.JobId&&x.IndividualId==job.IndividualID)??throw new InvalidOperationException("Agreement unavailable for this employment.");
            var outstanding=agreement.TotalAmount-await PaidAsync(db,agreement.Id);
            if(outstanding<=0)throw new InvalidOperationException("This agreement is fully paid.");
            if(input.Kind=="Payment")
            {
                if(input.CurrencyTypeId!=agreement.CurrencyTypeId||input.Amount>outstanding||input.FromDate.Date>Today||string.IsNullOrWhiteSpace(input.Reference)||!await db.BusinessEntities.AnyAsync(x=>x.BusinessEntityID==input.BusinessEntityId))throw new InvalidOperationException("Select a registered payer, payment reference, actual payment date and an amount within the outstanding balance, in the agreement currency.");
                if(await db.DeductionPayments.AnyAsync(x=>x.AgreementId==agreement.Id&&x.Reference==input.Reference.Trim()))throw new InvalidOperationException("This payment reference has already been recorded.");
            }
            else
            {
                var unpaid=await UnpaidAsync(db,agreement.Id);
                var month=new DateTime(input.FromDate.Year,input.FromDate.Month,1);
                var selected=unpaid.Where(x=>x.Row.DueDate>=month&&x.Row.DueDate<month.AddMonths(1)).ToList();
                if(selected.Count==0||selected.Any(x=>x.Paid>0)||await db.StaffFinancePayrollEntries.AnyAsync(x=>x.JobId==job.JobId&&x.Month==month))throw new InvalidOperationException("Choose an unpaid, unprocessed instalment month.");
                if(unpaid.Any(x=>x.Row.DueDate<month))throw new InvalidOperationException("Resolve overdue instalments before deferring a later month.");
                var remaining=unpaid.Where(x=>x.Row.DueDate>=month.AddMonths(1)).ToList();
                if(remaining.Count==0)throw new InvalidOperationException("No later instalments remain. A term extension is required; contact HR.");
                var schedule=FinanceCalculations.Schedule(unpaid.Sum(x=>x.Row.Amount-x.Paid),remaining[0].Row.DueDate,remaining.Count,agreement.FrequencyMonths);
                await CheckScheduleCapAsync(db,job,agreement.CurrencyTypeId,schedule,agreement.Id);
            }
        }
        if(approving&&!Active(job))throw new InvalidOperationException("Employment ended. This request cannot be approved.");
    }
    private static async Task CheckScheduleCapAsync(HrmTeContext db,Job job,int currency,List<(DateTime DueDate,decimal Amount)> proposed,long? excludeAgreement=null)
    {
        if(!job.BasicSalary.HasValue||job.BasicSalary<=0)throw new InvalidOperationException("A basic salary is required before deduction limits can be checked. Please contact HR.");
        var existing=await (from i in db.DeductionInstalments join a in db.DeductionAgreements on i.AgreementId equals a.Id
            where a.OrganisationId==job.OrganisationID&&a.IndividualId==job.IndividualID&&a.CurrencyTypeId==currency&&a.Id!=excludeAgreement&&!i.IsSuperseded select i).ToListAsync();
        foreach(var group in proposed.GroupBy(x=>new DateTime(x.DueDate.Year,x.DueDate.Month,1)))
        {
            var policy=await PolicyAsync(db,job.OrganisationID,currency,group.Min(x=>x.DueDate));
            decimal other=0;foreach(var i in existing.Where(x=>x.DueDate.Year==group.Key.Year&&x.DueDate.Month==group.Key.Month))other+=i.Amount-await AllocatedAsync(db,i.Id);
            if(other+group.Sum(x=>x.Amount)>FinanceCalculations.MonthlyCap(job.BasicSalary.Value,policy.MaximumPerMonth,policy.MaximumPercentOfBasic,policy.MinimumTakeHome))throw new InvalidOperationException($"The repayment schedule exceeds the deduction limit for {group.Key:MMMM yyyy}. Adjust the term or amount.");
        }
    }
    private sealed record Unpaid(DeductionInstalment Row,decimal Paid);
    private static async Task<List<Unpaid>> UnpaidAsync(HrmTeContext db,long agreementId)
    {
        var rows=await db.DeductionInstalments.Where(x=>x.AgreementId==agreementId&&!x.IsSuperseded).OrderBy(x=>x.DueDate).ToListAsync();var result=new List<Unpaid>();
        foreach(var row in rows){var paid=await AllocatedAsync(db,row.Id);if(paid<row.Amount)result.Add(new(row,paid));}return result;
    }
    public async Task<ServiceResult> ActAsync(int requestId,int expectedState,string action,string? comment)
    {
        if(action is not("Approve" or "Reject" or "Cancel"))return ServiceResult.Fail("Select a valid action.");
        var actor=await ScopeAsync();await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            var row=await db.StaffFinanceRequests.Include(x=>x.Request).ThenInclude(x=>x.RequestState).SingleOrDefaultAsync(x=>x.RequestId==requestId&&x.OrganisationId==actor.Org)??throw new UnauthorizedAccessException("Request unavailable.");
            if(action!="Cancel"&&row.CreatedByUserId==actor.User)throw new UnauthorizedAccessException("You cannot approve or reject a request you submitted.");
            var input=Payload(row);var job=await db.Jobs.SingleAsync(x=>x.JobId==row.JobId&&x.OrganisationID==actor.Org);
            await Processing.ExecuteAsync(db,Subject(row),expectedState,action,comment,actor.User,actor.Person,
                async()=>
                {
                    await ValidateApplicationAsync(db,input,job,actor.Org,true);
                    if(row.Kind is "Agreement" or "Payment" && !await db.StaffFinanceDocuments.AnyAsync(x=>x.RequestId==requestId))throw new InvalidOperationException("Attach the agreement or payment evidence before approval.");
                },()=>ApplyApprovedAsync(db,row,input,actor));
            await tx.CommitAsync();return ServiceResult.Ok("Request action saved.");
        }
        catch(InvalidOperationException ex)
        {
            await tx.RollbackAsync();
            if(ex is WorkflowConfigurationException config)await config.NotifyAsync(factory);
            if(ex is RequestTransitionConfigurationException transition)
            {var row=await db.StaffFinanceRequests.AsNoTracking().SingleAsync(x=>x.RequestId==requestId);var context=await contextResolver.ResolveAsync(db,requestId,row.JobId);await new WorkflowConfigurationException(context,transition.Detail){ConfigurationUrl="/admin/settings/config/state-transitions"}.NotifyAsync(factory);}
            return ServiceResult.Fail(ex.Message);
        }
        catch(DbUpdateException){await tx.RollbackAsync();return ServiceResult.Fail("The request or balance changed. Refresh and try again.");}
    }
    private async Task ApplyApprovedAsync(HrmTeContext db,StaffFinanceRequest row,FinanceApplication input,Scope actor)
    {
        if(row.Kind=="PayrollCycle")
        {
            var period=await db.OrganisationPayrollPeriods.SingleAsync(x=>x.OrganisationPayrollPeriodId==input.PayrollPeriodId&&x.OrganisationId==row.OrganisationId);
            var month=new DateTime(input.FromDate.Year,input.FromDate.Month,1);var startMonth=period.StartDay==1?month:month.AddMonths(-1);
            db.PayrollCycles.Add(new(){RequestId=row.RequestId,OrganisationPayrollPeriodId=period.OrganisationPayrollPeriodId,PayrollMonth=DateOnly.FromDateTime(month),StartDate=new(startMonth.Year,startMonth.Month,Math.Min(period.StartDay,DateTime.DaysInMonth(startMonth.Year,startMonth.Month))),EndDate=new(month.Year,month.Month,Math.Min(period.EndDay,DateTime.DaysInMonth(month.Year,month.Month))),CycleState=4});
        }
        else if(row.Kind=="Allowance")db.StaffAllowances.Add(new(){OrganisationId=row.OrganisationId,IndividualId=row.IndividualId,JobId=row.JobId,RequestId=row.RequestId,AllowanceTypeId=input.AllowanceTypeId,CurrencyTypeId=input.CurrencyTypeId,Amount=input.Amount,FrequencyMonths=input.FrequencyMonths,FromDate=input.FromDate.Date,ToDate=input.ToDate?.Date});
        else if(row.Kind=="EndAllowance") (await db.StaffAllowances.SingleAsync(x=>x.Id==input.AllowanceTypeId)).ToDate=input.FromDate.Date;
        else if(row.Kind=="Agreement")
        {
            var agreement=new DeductionAgreement{OrganisationId=row.OrganisationId,IndividualId=row.IndividualId,JobId=row.JobId,RequestId=row.RequestId,CreditorBusinessEntityId=input.BusinessEntityId,Reference=input.Reference.Trim(),Category=input.Category,CurrencyTypeId=input.CurrencyTypeId,TotalAmount=input.Amount,FirstDueDate=input.FromDate.Date,InstalmentCount=input.InstalmentCount,FrequencyMonths=input.FrequencyMonths};
            db.DeductionAgreements.Add(agreement);await db.SaveChangesAsync();
            foreach(var item in FinanceCalculations.Schedule(input.Amount,input.FromDate,input.InstalmentCount,input.FrequencyMonths))db.DeductionInstalments.Add(new(){AgreementId=agreement.Id,Version=1,DueDate=item.DueDate,Amount=item.Amount,SourceRequestId=row.RequestId});
        }
        else if(row.Kind=="Defer")
        {
            var agreement=await db.DeductionAgreements.SingleAsync(x=>x.Id==input.AgreementId);var unpaid=await UnpaidAsync(db,agreement.Id);var month=new DateTime(input.FromDate.Year,input.FromDate.Month,1);var later=unpaid.Where(x=>x.Row.DueDate>=month.AddMonths(1)).ToList();
            agreement.ScheduleVersion++;foreach(var item in unpaid)item.Row.IsSuperseded=true;
            foreach(var item in FinanceCalculations.Schedule(unpaid.Sum(x=>x.Row.Amount-x.Paid),later[0].Row.DueDate,later.Count,agreement.FrequencyMonths))db.DeductionInstalments.Add(new(){AgreementId=agreement.Id,Version=agreement.ScheduleVersion,DueDate=item.DueDate,Amount=item.Amount,SourceRequestId=row.RequestId});
        }
        else if(row.Kind=="Payment")await AllocatePaymentAsync(db,input.AgreementId,input.Amount,input.FromDate,input.Reference.Trim(),actor.User,row.RequestId,null,input.BusinessEntityId);
        await logs.CreateAsync(db,SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE,$"Finance request {row.RequestId} applied: {row.Kind}; organisation {row.OrganisationId}.");
    }
    private static async Task AllocatePaymentAsync(HrmTeContext db,long agreementId,decimal amount,DateTime paidDate,string reference,int user,int? requestId,long? payrollEntryId,int? payer,long? onlyInstalment=null)
    {
        var unpaid=await UnpaidAsync(db,agreementId);if(onlyInstalment.HasValue)unpaid=unpaid.Where(x=>x.Row.Id==onlyInstalment).ToList();
        if(amount<=0||amount>unpaid.Sum(x=>x.Row.Amount-x.Paid))throw new InvalidOperationException("The outstanding balance changed. Refresh before recording payment.");
        var payment=new DeductionPayment{AgreementId=agreementId,Amount=amount,PaidDate=paidDate.Date,Reference=reference,RecordedByUserId=user,RequestId=requestId,PayrollEntryId=payrollEntryId,PayerBusinessEntityId=payer};db.DeductionPayments.Add(payment);await db.SaveChangesAsync();
        var remaining=amount;foreach(var item in unpaid){var allocated=Math.Min(remaining,item.Row.Amount-item.Paid);if(allocated>0)db.DeductionPaymentAllocations.Add(new(){PaymentId=payment.Id,InstalmentId=item.Row.Id,Amount=allocated});remaining-=allocated;if(remaining==0)break;}await db.SaveChangesAsync();
    }
}
