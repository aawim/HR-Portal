using System.Data;
using System.Text.Json;
using HRM.Application.Contracts.Pages;
using HRM.Application.Features.StaffFinance;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.Models;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;

public sealed partial class StaffFinanceService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access,
    IRequestService requests, IOperationLogService logs, IWorkflowEngine workflow, IWorkflowApproverResolver resolver,
    IWorkflowRequestContextResolver contextResolver) : IStaffFinance
{
    public const string PageUrl = "/online/allowances-deductions";
    public const string AdminUrl = "/admin/payroll/finance";
    private static DateTime Today => DateTime.UtcNow.AddHours(5).Date;
    private WorkflowRequestProcessing Processing => new(resolver, contextResolver);
    private sealed record Scope(int User, int Person, int Org, int Job, bool Manage, bool Configure, bool Post);
    private async Task<Scope> ScopeAsync()
    {
        var actor = await access.RequireContextAsync(); var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org)) throw new UnauthorizedAccessException("Select your current organisation.");
        return new(actor.UserId, actor.IndividualId, org, actor.ActiveJob?.JobId ?? 0,
            actor.IsSuperAdministrator || await access.HasPermissionAsync("StaffFinance.Manage"),
            actor.IsSuperAdministrator || await access.HasPermissionAsync("StaffFinance.Configure"),
            actor.IsSuperAdministrator || await access.HasPermissionAsync("StaffFinance.Payroll"));
    }
    private static FinanceApplication Payload(StaffFinanceRequest row) => JsonSerializer.Deserialize<FinanceApplication>(row.PayloadJson) ?? throw new InvalidOperationException("The saved request is unavailable.");
    private static WorkflowRequestSubject Subject(StaffFinanceRequest row) => new(row.Request,row.JobId,row.IndividualId,row.OrganisationId);
    private static bool Active(Job j) => j.JobStateId == 4 && j.JoinedDate.Date <= Today && (!j.TerminatedDate.HasValue || j.TerminatedDate.Value.Date > Today);
    private static string TypeName(string kind) => kind switch
    {
        "Allowance" => "Staff Allowance Request", "Agreement" => "Salary Deduction Agreement Request",
        "Defer" => "Deduction Deferral Request", "Payment" => "Deduction External Payment Request",
        "EndAllowance" => "Allowance End Request", "PayrollCycle" => "Finance Payroll Cycle Request", _ => throw new InvalidOperationException("Select a valid finance request type.")
    };
    public async Task<FinanceConfiguration> GetConfigurationAsync()
    {
        var actor = await ScopeAsync(); await using var db = await factory.CreateDbContextAsync();
        var types = await db.AllowanceTypes.Where(x=>x.OrganisationId==actor.Org&&x.IsActive).OrderBy(x=>x.Name).Select(x=>new FinanceOption(x.Id,x.Name)).ToListAsync();
        var positions = actor.Configure ? await db.Positions.Where(x=>(x.OrganisationId==actor.Org||x.IsGlobal)&&x.IsActive==true).OrderBy(x=>x.Name).Select(x=>new FinanceOption(x.PositionId,x.Name)).ToListAsync() : [];
        var currencies = await db.FinanceCurrencies.OrderBy(x=>x.Name).Select(x=>new FinanceOption(x.CurrencyTypeId,x.Name)).ToListAsync();
        var rules = actor.Configure ? await (from r in db.PositionAllowanceRules join p in db.Positions on r.PositionId equals p.PositionId join a in db.AllowanceTypes on r.AllowanceTypeId equals a.Id
            where r.OrganisationId==actor.Org orderby r.FromDate descending select new FinanceRuleRow(r.Id,new(r.PositionId,r.AllowanceTypeId,r.CurrencyTypeId,r.Amount,r.FrequencyMonths,r.FromDate,r.ToDate,r.MultiplePositionPolicy),p.Name,a.Name)).ToListAsync() : [];
        var policies = actor.Configure ? await db.DeductionPolicies.Where(x=>x.OrganisationId==actor.Org).OrderByDescending(x=>x.FromDate).Select(x=>new DeductionPolicyInput(x.CurrencyTypeId,x.MaximumPerMonth,x.MaximumPercentOfBasic,x.MinimumTakeHome,x.MaximumOutstanding,x.MaximumAgreements,x.FromDate)).ToListAsync() : [];
        return new(types,positions,currencies,rules,policies,actor.Configure)
        {
            TypeDefinitions=actor.Configure?await db.AllowanceTypes.Where(x=>x.OrganisationId==actor.Org).OrderBy(x=>x.Name).Select(x=>new AllowanceTypeInput(x.Id,x.Name,x.IsActive)).ToListAsync():[],
            PayrollPeriods=actor.Post?await db.OrganisationPayrollPeriods.Where(x=>x.OrganisationId==actor.Org&&x.IsValid).Select(x=>new FinanceOption(x.OrganisationPayrollPeriodId,"Period "+x.OrganisationPayrollPeriodId+": day "+x.StartDay+"–"+x.EndDay)).ToListAsync():[]
        };
    }
    public async Task<ServiceResult> SaveAllowanceTypeAsync(AllowanceTypeInput input)
    {
        var actor=await ScopeAsync(); if(!actor.Configure) throw new UnauthorizedAccessException("Finance configuration permission is required.");
        if(string.IsNullOrWhiteSpace(input.Name)||input.Name.Trim().Length>150) return ServiceResult.Fail("Enter an allowance name of up to 150 characters.");
        await using var db=await factory.CreateDbContextAsync();
        var row=input.Id==0?new AllowanceType{OrganisationId=actor.Org}:await db.AllowanceTypes.SingleOrDefaultAsync(x=>x.Id==input.Id&&x.OrganisationId==actor.Org)??throw new UnauthorizedAccessException();
        // Names are retained on posted payroll entries; types are deactivated rather than deleted.
        row.Name=input.Name.Trim();row.IsActive=input.IsActive;if(input.Id==0)db.AllowanceTypes.Add(row);
        await logs.CreateAsync(db,SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE,$"Allowance type {input.Id}: {row.Name}; active {row.IsActive}; organisation {actor.Org}.");
        try { await db.SaveChangesAsync();return ServiceResult.Ok("Allowance type saved."); } catch(DbUpdateException){return ServiceResult.Fail("This name already exists or the record changed. Refresh and try again.");}
    }
    public async Task<ServiceResult> AddPositionRuleAsync(PositionAllowanceInput input)
    {
        var actor=await ScopeAsync();if(!actor.Configure)throw new UnauthorizedAccessException("Finance configuration permission is required.");
        if(input.Amount<=0||decimal.Round(input.Amount,2)!=input.Amount||input.Amount>999999999||input.FrequencyMonths is not(1 or 6 or 12)||input.FromDate.Date<Today||input.ToDate<input.FromDate||input.MultiplePositionPolicy is not("Once" or "Highest" or "Stack"))return ServiceResult.Fail("Enter a future/current effective period, amount, frequency and multiple-position policy.");
        await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if(!await db.Positions.AnyAsync(x=>x.PositionId==input.PositionId&&(x.OrganisationId==actor.Org||x.IsGlobal)&&x.IsActive==true)||!await db.AllowanceTypes.AnyAsync(x=>x.Id==input.AllowanceTypeId&&x.OrganisationId==actor.Org&&x.IsActive)||!await db.FinanceCurrencies.AnyAsync(x=>x.CurrencyTypeId==input.CurrencyTypeId))return ServiceResult.Fail("Select an active position, allowance type and currency.");
        if(await db.PositionAllowanceRules.AnyAsync(x=>x.OrganisationId==actor.Org&&x.PositionId==input.PositionId&&x.AllowanceTypeId==input.AllowanceTypeId&&x.FromDate<=(input.ToDate??DateTime.MaxValue)&&(!x.ToDate.HasValue||x.ToDate>=input.FromDate)))return ServiceResult.Fail("This position already has an overlapping allowance rule. Use non-overlapping effective periods.");
        if(await db.PositionAllowanceRules.AnyAsync(x=>x.OrganisationId==actor.Org&&x.AllowanceTypeId==input.AllowanceTypeId&&x.MultiplePositionPolicy!=input.MultiplePositionPolicy))return ServiceResult.Fail("Use the same multiple-position policy for this allowance type.");
        if(await db.PositionAllowanceRules.AnyAsync(x=>x.OrganisationId==actor.Org&&x.AllowanceTypeId==input.AllowanceTypeId&&(x.CurrencyTypeId!=input.CurrencyTypeId||x.FrequencyMonths!=input.FrequencyMonths)))return ServiceResult.Fail("Position rules for an allowance type must use the same currency and payment frequency.");
        db.PositionAllowanceRules.Add(new(){OrganisationId=actor.Org,PositionId=input.PositionId,AllowanceTypeId=input.AllowanceTypeId,CurrencyTypeId=input.CurrencyTypeId,Amount=input.Amount,FrequencyMonths=input.FrequencyMonths,FromDate=input.FromDate.Date,ToDate=input.ToDate?.Date,MultiplePositionPolicy=input.MultiplePositionPolicy,CreatedByUserId=actor.User,CreatedAtUtc=DateTime.UtcNow});
        await logs.CreateAsync(db,SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE,$"Position allowance rule added: organisation {actor.Org}, position {input.PositionId}, type {input.AllowanceTypeId}, amount {input.Amount}.");await db.SaveChangesAsync();await tx.CommitAsync();return ServiceResult.Ok("Required position allowance rule saved.");
    }
    public async Task<ServiceResult> AddDeductionPolicyAsync(DeductionPolicyInput input)
    {
        var actor=await ScopeAsync();if(!actor.Configure)throw new UnauthorizedAccessException("Finance configuration permission is required.");
        if(input.FromDate.Date<Today||input.MaximumPerMonth<=0||input.MaximumPercentOfBasic is <=0 or >100||input.MinimumTakeHome<0||input.MaximumOutstanding<=0||input.MaximumAgreements is <1 or >100)return ServiceResult.Fail("Enter valid limits and an effective date from today onwards.");
        await using var db=await factory.CreateDbContextAsync();
        if(!await db.FinanceCurrencies.AnyAsync(x=>x.CurrencyTypeId==input.CurrencyTypeId))return ServiceResult.Fail("Select a valid currency.");
        if(await db.DeductionPolicies.AnyAsync(x=>x.OrganisationId==actor.Org&&x.CurrencyTypeId!=input.CurrencyTypeId))return ServiceResult.Fail("Deduction policies must use this organisation's existing basic-salary currency. Currency conversion is not supported.");
        if(await db.DeductionPolicies.AnyAsync(x=>x.OrganisationId==actor.Org&&x.CurrencyTypeId==input.CurrencyTypeId&&x.FromDate==input.FromDate.Date))return ServiceResult.Fail("A policy already exists for this date and currency.");
        db.DeductionPolicies.Add(new(){OrganisationId=actor.Org,CurrencyTypeId=input.CurrencyTypeId,MaximumPerMonth=input.MaximumPerMonth,MaximumPercentOfBasic=input.MaximumPercentOfBasic,MinimumTakeHome=input.MinimumTakeHome,MaximumOutstanding=input.MaximumOutstanding,MaximumAgreements=input.MaximumAgreements,FromDate=input.FromDate.Date,CreatedByUserId=actor.User});
        await logs.CreateAsync(db,SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE,$"Deduction policy added: organisation {actor.Org}, currency {input.CurrencyTypeId}, from {input.FromDate:yyyy-MM-dd}.");await db.SaveChangesAsync();return ServiceResult.Ok("Deduction policy saved; previous policies remain in history.");
    }
    private static async Task<decimal> PaidAsync(HrmTeContext db,long agreementId) => await db.DeductionPayments.Where(x=>x.AgreementId==agreementId).SumAsync(x=>(decimal?)x.Amount)??0;
    public async Task<ServiceResult> EndPositionRuleAsync(long id,DateTime lastEntitlementDate)
    {
        var actor=await ScopeAsync();if(!actor.Configure)throw new UnauthorizedAccessException("Finance configuration permission is required.");
        await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var row=await db.PositionAllowanceRules.SingleOrDefaultAsync(x=>x.Id==id&&x.OrganisationId==actor.Org)??throw new UnauthorizedAccessException();
        if(lastEntitlementDate.Date<Today||lastEntitlementDate.Date<row.FromDate||row.ToDate.HasValue&&lastEntitlementDate.Date>row.ToDate.Value.Date)return ServiceResult.Fail("Choose a last entitlement date within the rule period, from today onwards.");
        if(await db.StaffFinancePayrollEntries.AnyAsync(x=>x.PositionRuleId==id&&x.Month.AddMonths(1)>lastEntitlementDate.Date))return ServiceResult.Fail("Posted payroll covers this period. Choose an end date after the posted month.");
        row.ToDate=lastEntitlementDate.Date;await logs.CreateAsync(db,SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE,$"Position allowance rule {id} ends {lastEntitlementDate:yyyy-MM-dd}; tenant {actor.Org}.");await db.SaveChangesAsync();await tx.CommitAsync();return ServiceResult.Ok("Position rule ended. Add a new rule for any later period.");
    }
    private static async Task<decimal> AllocatedAsync(HrmTeContext db,long instalmentId) => await db.DeductionPaymentAllocations.Where(x=>x.InstalmentId==instalmentId).SumAsync(x=>(decimal?)x.Amount)??0;
    private static async Task<DeductionPolicy> PolicyAsync(HrmTeContext db,int org,int currency,DateTime day) => await db.DeductionPolicies.Where(x=>x.OrganisationId==org&&x.CurrencyTypeId==currency&&x.FromDate<=day.Date).OrderByDescending(x=>x.FromDate).FirstOrDefaultAsync()??throw new InvalidOperationException("Deduction limits are not available for this currency and date. Please contact HR.");
    private static async Task<bool> AssignedAsync(HrmTeContext db,int requestId,int user) => await db.WfRequestApprovals.AnyAsync(x=>x.ApproverUserId==user&&x.RequestStageInstance.WorkflowRequestInstance.RequestId==requestId);
    private static async Task<bool> CanReadAsync(HrmTeContext db,StaffFinanceRequest row,Scope actor) => row.OrganisationId==actor.Org&&(row.IndividualId==actor.Person||actor.Manage||actor.Post||await AssignedAsync(db,row.RequestId,actor.User));
}
