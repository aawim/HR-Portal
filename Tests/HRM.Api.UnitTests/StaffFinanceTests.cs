using System.Text.Json;
using HRM.Application.Contracts.Pages;
using HRM.Application.Features.StaffFinance;
using HRM.DTOs;
using HRM.DTOs.ApprovalFlow;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Models.Workflow;
using HRM.Services;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using Microsoft.EntityFrameworkCore;
using Xunit;
namespace HRM.Api.UnitTests;

public sealed class StaffFinanceTests
{
    private static DateTime Today=>DateTime.UtcNow.AddHours(5).Date;
    private static DateTime Month=>new(Today.Year,Today.Month,1);
    [Theory]
    [InlineData(100,3,1)] [InlineData(12000,12,1)] [InlineData(999.99,4,6)] [InlineData(300,3,12)]
    public void Repayment_schedule_preserves_total_and_frequency(decimal total,int count,int months)
    {
        var rows=FinanceCalculations.Schedule(total,new(2026,1,31),count,months);
        Assert.Equal(total,rows.Sum(x=>x.Amount));Assert.Equal(count,rows.Count);
        for(var i=0;i<count;i++){Assert.Equal(new DateTime(2026,1,31).AddMonths(i*months),rows[i].DueDate);Assert.True(rows[i].Amount>0);}
    }
    [Theory]
    [InlineData(0,12,1)] [InlineData(100,0,1)] [InlineData(100,121,1)] [InlineData(100,12,3)] [InlineData(.01,12,1)] [InlineData(1.001,2,1)]
    public void Invalid_repayment_terms_are_rejected(decimal total,int count,int frequency)=>Assert.Throws<InvalidOperationException>(()=>FinanceCalculations.Schedule(total,Today,count,frequency));
    [Fact] public void Deferral_example_keeps_total_debt()
    {var remaining=FinanceCalculations.Schedule(11000,new(2026,3,1),10,1);Assert.All(remaining,x=>Assert.Equal(1100,x.Amount));Assert.Equal(12000,1000+remaining.Sum(x=>x.Amount));}
    [Fact] public void Proration_and_take_home_limits_are_exact()
    {
        Assert.Equal(160,FinanceCalculations.Prorate(310,new(2026,1,1),new(2026,1,16),null));
        Assert.Equal(0,FinanceCalculations.Prorate(310,new(2026,1,1),new(2026,2,1),null));
        Assert.Equal(500,FinanceCalculations.MonthlyCap(1000,800,60,500));
        Assert.Equal(0,FinanceCalculations.MonthlyCap(100,800,60,500));
    }
    private sealed class Fixture
    {
        public QueryFactory Factory {get;}=new(ignoreTransactions:true);
        public FakeAccess Access {get;}=new();
        public StaffFinanceService Service {get;}
        public Fixture()
        {
            Service=new(Factory,Access,StubProxy.For<IRequestService>((_,_)=>throw new NotSupportedException()),
                StubProxy.For<IOperationLogService>((_,_)=>Task.FromResult(new OperationLog{OperationLogId=1})),
                StubProxy.For<IWorkflowEngine>((_,_)=>throw new NotSupportedException()),
                StubProxy.For<IWorkflowApproverResolver>((_,_)=>Task.FromResult<IReadOnlyList<WorkflowApproverCandidate>>(new List<WorkflowApproverCandidate>{new(1,1,1,true)})),
                StubProxy.For<IWorkflowRequestContextResolver>((_,_)=>Task.FromResult(new WorkflowRequestContextDto())));
        }
        public async Task Seed()
        {
            await using var db=Factory.CreateDbContext();
            db.Organisations.Add(new(){BusinessEntityID=10,OrganisationName="Tenant"});
            db.Individuals.AddRange(new(){BusinessEntityId=100,FirstNameEnglish="HR"},new(){BusinessEntityId=200,FirstNameEnglish="Staff"});
            db.Jobs.Add(new(){JobId=10,IndividualID=200,OrganisationID=10,JobStateId=4,JoinedDate=Today.AddYears(-1),BasicSalary=6000});
            db.FinanceCurrencies.Add(new(){CurrencyTypeId=1,Name="MVR"});db.AllowanceTypes.Add(new(){Id=1,OrganisationId=10,Name="Phone"});
            db.Positions.Add(new(){PositionId=1,OrganisationId=10,IsActive=true,Name="Officer"});
            db.JobPositions.Add(new(){JobPositionId=1,JobId=10,PositionId=1,JobPositionStateId=4,FromDate=Month});
            db.PositionAllowanceRules.Add(new(){Id=1,OrganisationId=10,PositionId=1,AllowanceTypeId=1,CurrencyTypeId=1,Amount=300,FrequencyMonths=1,FromDate=Month,MultiplePositionPolicy="Once"});
            db.DeductionPolicies.Add(new(){Id=1,OrganisationId=10,CurrencyTypeId=1,MaximumPerMonth=1500,MaximumPercentOfBasic=50,MinimumTakeHome=2000,MaximumOutstanding=20000,MaximumAgreements=5,FromDate=Month.AddYears(-1)});
            db.Users.Add(new(){UserId=1,BusinessEntityID=100,Username="hr",IsActive=true,LocalUserStateId=1,UserOrganisation=new UserOrganisation{BusinessEntityID=10}});
            db.RequestStates.AddRange(new(){RequestStateId=7,StateName="Pending HR Approval",IsProcessingState=true},new(){RequestStateId=4,StateName="Approved",IsFinalState=true},new(){RequestStateId=6,StateName="Rejected",IsFinalState=true});
            db.RequestTypes.Add(new(){RequestTypeId=501,TypeName="Deduction Deferral Request"});
            db.Requests.Add(new(){RequestId=1000,OrganisationBusinessEntityID=10,ApplicantBusinessEntityId=200,RequestStateId=4});
            db.OrganisationPayrollPeriods.Add(new(){OrganisationPayrollPeriodId=1,OrganisationId=10,StartDay=1,EndDay=31,IsValid=true});
            db.PayrollCycles.Add(new(){RequestId=1000,OrganisationPayrollPeriodId=1,PayrollMonth=DateOnly.FromDateTime(Month),StartDate=DateOnly.FromDateTime(Month),EndDate=DateOnly.FromDateTime(Month.AddMonths(1).AddDays(-1))});
            await db.SaveChangesAsync();
        }
        public async Task AddAgreement(decimal total=1200)
        {
            await using var db=Factory.CreateDbContext();
            db.DeductionAgreements.Add(new(){Id=1,OrganisationId=10,IndividualId=200,JobId=10,RequestId=1000,CreditorBusinessEntityId=100,Reference="LOAN-1",CurrencyTypeId=1,TotalAmount=total,FrequencyMonths=1,InstalmentCount=12,FirstDueDate=Month});
            for(var i=0;i<12;i++)db.DeductionInstalments.Add(new(){Id=i+1,AgreementId=1,Version=1,DueDate=Month.AddMonths(i),Amount=total/12,SourceRequestId=1000});await db.SaveChangesAsync();
        }
        public async Task AddWorkflow(FinanceApplication input)
        {
            await using var db=Factory.CreateDbContext();
            var request=new Request{RequestId=2000,RequestTypeId=501,RequestStateId=7,ApplicantBusinessEntityId=200,OrganisationBusinessEntityID=10,ReferenceNumber="FIN-2000"};
            var definition=new WfWorkflowDefinition{WorkflowDefinitionId=1};var stage=new WfWorkflowStage{WorkflowStageId=1,WorkflowDefinition=definition,RequestStateId=7,StageOrder=1};definition.Stages.Add(stage);
            var instance=new WfRequestInstance{WorkflowRequestInstanceId=1,RequestId=2000,WorkflowDefinition=definition};var opened=new WfRequestStageInstance{RequestStageInstanceId=1,WorkflowRequestInstance=instance,WorkflowStage=stage};opened.Approvals.Add(new(){ApproverUserId=1});instance.CurrentStage=opened;
            db.WfRequestInstances.Add(instance);db.StaffFinanceRequests.Add(new(){RequestId=2000,Request=request,OrganisationId=10,IndividualId=200,JobId=10,Kind=input.Kind,PayloadJson=JsonSerializer.Serialize(input),CreatedByUserId=2,ClientRequestId=Guid.NewGuid()});
            db.RequestTypesAllowedRequestStateTransitions.Add(new(){RequestTypeId=501,FromRequestStateId=7,ToRequestStateId=4});await db.SaveChangesAsync();
        }
    }
    [Fact] public async Task Finance_permissions_and_tenant_boundaries_are_enforced()
    {
        var f=new Fixture();await f.Seed();
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>f.Service.AddPositionRuleAsync(new(1,1,1,300,1,Today,null)));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>f.Service.PreviewPayrollAsync(Month,[10]));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>f.Service.LoadAsync(200));
        f.Access.Permissions.Add("StaffFinance.Payroll");f.Access.Organisation=20;
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>f.Service.PreviewPayrollAsync(Month,[10]));
    }
    [Theory] [InlineData("Once",300)] [InlineData("Highest",500)] [InlineData("Stack",800)]
    public async Task Multiple_position_entitlements_follow_policy(string policy,decimal expected)
    {
        var f=new Fixture();await f.Seed();f.Access.Permissions.Add("StaffFinance.Payroll");
        await using(var db=f.Factory.CreateDbContext())
        {
            (await db.PositionAllowanceRules.SingleAsync()).MultiplePositionPolicy=policy;
            db.Positions.Add(new(){PositionId=2,OrganisationId=10,IsActive=true,Name="Additional"});db.JobPositions.Add(new(){JobPositionId=2,JobId=10,PositionId=2,JobPositionStateId=4,FromDate=Month});
            db.PositionAllowanceRules.Add(new(){Id=2,OrganisationId=10,PositionId=2,AllowanceTypeId=1,CurrencyTypeId=1,Amount=500,FrequencyMonths=1,FromDate=Month,MultiplePositionPolicy=policy});await db.SaveChangesAsync();
        }
        var preview=await f.Service.PreviewPayrollAsync(Month,[10]);Assert.Empty(preview.Issues);Assert.Equal(expected,preview.Lines.Sum(x=>x.Amount));
    }
    [Theory] [InlineData(1,6)] [InlineData(2,1)]
    public async Task Once_per_person_rules_cannot_mix_currency_or_payment_frequency(int currency,int frequency)
    {
        var f=new Fixture();await f.Seed();f.Access.Permissions.Add("StaffFinance.Configure");
        await using(var db=f.Factory.CreateDbContext()){db.Positions.Add(new(){PositionId=2,OrganisationId=10,Name="Other",IsActive=true});db.FinanceCurrencies.Add(new(){CurrencyTypeId=2,Name="USD"});await db.SaveChangesAsync();}
        var result=await f.Service.AddPositionRuleAsync(new(2,1,currency,100,frequency,Today,null));
        Assert.False(result.Success);Assert.Contains("same currency",result.Message);
    }
    [Fact] public async Task Ended_positions_stop_required_allowances()
    {
        var f=new Fixture();await f.Seed();f.Access.Permissions.Add("StaffFinance.Payroll");await using(var db=f.Factory.CreateDbContext()){(await db.JobPositions.SingleAsync()).ToDate=Month;await db.SaveChangesAsync();}
        Assert.Empty((await f.Service.PreviewPayrollAsync(Month,[10])).Lines);
    }
    [Fact] public async Task Legacy_payroll_cycles_use_period_tenant_without_needing_request_organisation()
    {
        var f=new Fixture();await f.Seed();f.Access.Permissions.Add("StaffFinance.Payroll");
        await using(var db=f.Factory.CreateDbContext()){(await db.Requests.SingleAsync(x=>x.RequestId==1000)).OrganisationBusinessEntityID=null;await db.SaveChangesAsync();}
        Assert.Equal(1000,Assert.Single(await f.Service.GetPayrollCyclesAsync(Month)).Id);
        f.Access.Organisation=20;Assert.Empty(await f.Service.GetPayrollCyclesAsync(Month));
    }
    [Fact] public async Task Payroll_cycle_is_created_only_at_final_workflow_approval()
    {
        var f=new Fixture();await f.Seed();await f.AddWorkflow(new(){Kind="PayrollCycle",JobId=10,PayrollPeriodId=1,FromDate=Month.AddMonths(1),Reason="Next payroll period"});
        await using(var db=f.Factory.CreateDbContext())Assert.False(await db.PayrollCycles.AnyAsync(x=>x.RequestId==2000));
        Assert.True((await f.Service.ActAsync(2000,7,"Approve","Confirmed")).Success);
        await using(var db=f.Factory.CreateDbContext()){var cycle=await db.PayrollCycles.SingleAsync(x=>x.RequestId==2000);Assert.Equal(1,cycle.OrganisationPayrollPeriodId);Assert.Equal(DateOnly.FromDateTime(Month.AddMonths(1)),cycle.PayrollMonth);Assert.Equal(4,(await db.Requests.SingleAsync(x=>x.RequestId==2000)).RequestStateId);}
    }
    [Fact] public async Task Payroll_posts_once_allocates_deductions_and_rejects_changed_retry()
    {
        var f=new Fixture();await f.Seed();await f.AddAgreement();f.Access.Permissions.Add("StaffFinance.Payroll");
        var preview=await f.Service.PreviewPayrollAsync(Month,[10]);Assert.Equal(100,Assert.Single(preview.Lines,x=>x.Kind=="Deduction").Amount);
        var input=new FinancePayrollInput(Month,1000,"PAY-1",Guid.NewGuid(),[10],preview.Fingerprint);
        Assert.True((await f.Service.PostPayrollAsync(input)).Success);Assert.True((await f.Service.PostPayrollAsync(input)).Success);Assert.False((await f.Service.PostPayrollAsync(input with{Reference="changed"})).Success);
        await using var db=f.Factory.CreateDbContext();Assert.Single(await db.StaffFinancePayrollBatches.ToListAsync());Assert.Equal(2,await db.StaffFinancePayrollEntries.CountAsync());Assert.Equal(100,await db.DeductionPayments.SumAsync(x=>x.Amount));Assert.Equal(100,await db.DeductionPaymentAllocations.SumAsync(x=>x.Amount));
        Assert.False((await f.Service.PostPayrollAsync(input with{ClientBatchId=Guid.NewGuid()})).Success);
    }
    [Fact] public async Task Payroll_rechecks_preview_and_deduction_cap()
    {
        var f=new Fixture();await f.Seed();f.Access.Permissions.Add("StaffFinance.Payroll");var preview=await f.Service.PreviewPayrollAsync(Month,[10]);
        await using(var db=f.Factory.CreateDbContext()){(await db.PositionAllowanceRules.SingleAsync()).Amount=400;await db.SaveChangesAsync();}
        Assert.False((await f.Service.PostPayrollAsync(new(Month,1000,"PAY",Guid.NewGuid(),[10],preview.Fingerprint))).Success);
        await f.AddAgreement(24000);Assert.NotEmpty((await f.Service.PreviewPayrollAsync(Month,[10])).Issues);
    }
    [Fact] public async Task Staff_cannot_request_a_duplicate_of_a_required_position_allowance()
    {
        var f=new Fixture();await f.Seed();f.Access.Actor!.IndividualId=200;
        var result=await f.Service.SubmitAsync(new(){Kind="Allowance",JobId=10,AllowanceTypeId=1,FromDate=Today,Reason="Phone allowance"});
        Assert.False(result.Success);Assert.Contains("already required",result.Message);
    }
    [Fact] public async Task Payroll_officer_cannot_post_their_own_entries()
    {
        var f=new Fixture();await f.Seed();f.Access.Permissions.Add("StaffFinance.Payroll");f.Access.Actor!.IndividualId=200;
        var preview=await f.Service.PreviewPayrollAsync(Month,[10]);
        Assert.False((await f.Service.PostPayrollAsync(new(Month,1000,"SELF",Guid.NewGuid(),[10],preview.Fingerprint))).Success);
        await using var db=f.Factory.CreateDbContext();Assert.Empty(await db.StaffFinancePayrollEntries.ToListAsync());
    }
    [Fact] public async Task Staff_allowance_amount_is_decided_by_independent_hr_and_original_request_stays_unpriced()
    {
        var f=new Fixture();await f.Seed();await using(var db=f.Factory.CreateDbContext()){db.AllowanceTypes.Add(new(){Id=2,OrganisationId=10,Name="Subscription"});await db.SaveChangesAsync();}
        await f.AddWorkflow(new(){Kind="Allowance",JobId=10,AllowanceTypeId=2,FromDate=Today,Reason="Subscription required"});
        Assert.False((await f.Service.ActAsync(2000,7,"Approve","Approve")).Success);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>f.Service.AssessAllowanceAsync(2000,7,111,1));
        f.Access.Permissions.Add("StaffFinance.Assess");
        Assert.True((await f.Service.AssessAllowanceAsync(2000,7,111,1)).Success);
        Assert.True((await f.Service.AssessAllowanceAsync(2000,7,123.45m,1)).Success);
        Assert.True((await f.Service.ActAsync(2000,7,"Approve","HR assessed")).Success);
        await using(var db=f.Factory.CreateDbContext()){Assert.Equal(123.45m,(await db.StaffAllowances.SingleAsync()).Amount);Assert.Equal(2,await db.AllowanceAssessments.CountAsync());Assert.Equal(0,JsonSerializer.Deserialize<FinanceApplication>((await db.StaffFinanceRequests.SingleAsync()).PayloadJson)!.Amount);}
        Assert.False((await f.Service.AssessAllowanceAsync(2000,4,200,1)).Success);
    }
    [Fact] public async Task Staff_cannot_choose_the_requested_allowance_amount_through_the_api()
    {
        var f=new Fixture();await f.Seed();f.Access.Actor!.IndividualId=200;
        var result=await f.Service.SubmitAsync(new(){Kind="Allowance",JobId=10,AllowanceTypeId=1,Amount=999,CurrencyTypeId=1,FromDate=Today,Reason="Forged amount"});
        Assert.False(result.Success);Assert.Contains("without an amount",result.Message);
    }
    [Fact] public async Task Evidence_upload_checks_content_instead_of_trusting_file_name()
    {
        var f=new Fixture();await f.Seed();await f.AddWorkflow(new(){Kind="Payment",JobId=10,Reason="Receipt",FromDate=Today});f.Access.Actor!.IndividualId=200;
        var result=await f.Service.UploadDocumentAsync(new(2000,"fake.pdf","application/pdf","<script>bad</script>"u8.ToArray()));
        Assert.False(result.Success);await using var db=f.Factory.CreateDbContext();Assert.Empty(await db.StaffFinanceDocuments.ToListAsync());
    }
    [Fact] public async Task Deferral_reversions_only_unpaid_instalments_and_preserves_debt()
    {
        var f=new Fixture();await f.Seed();await f.AddAgreement();
        await f.AddWorkflow(new(){Kind="Defer",JobId=10,AgreementId=1,FromDate=Month,Reason="Unable to pay this month"});
        Assert.True((await f.Service.ActAsync(2000,7,"Approve","Confirmed")).Success);
        await using var db=f.Factory.CreateDbContext();Assert.Equal(2,(await db.DeductionAgreements.SingleAsync()).ScheduleVersion);
        Assert.Equal(12,await db.DeductionInstalments.CountAsync(x=>x.IsSuperseded));var rows=await db.DeductionInstalments.Where(x=>!x.IsSuperseded).ToListAsync();Assert.Equal(11,rows.Count);Assert.Equal(1200,rows.Sum(x=>x.Amount));Assert.DoesNotContain(rows,x=>x.DueDate==Month);
    }
    [Fact] public async Task External_payment_requires_evidence_and_allocates_oldest_debt()
    {
        var f=new Fixture();await f.Seed();await f.AddAgreement();
        await using(var db=f.Factory.CreateDbContext()){db.BusinessEntities.Add(new(){BusinessEntityID=900});await db.SaveChangesAsync();}
        await f.AddWorkflow(new(){Kind="Payment",JobId=10,AgreementId=1,BusinessEntityId=900,CurrencyTypeId=1,Amount=150,FromDate=Today,Reference="BANK-1",Reason="Paid by a family member"});
        Assert.False((await f.Service.ActAsync(2000,7,"Approve","Verified")).Success);
        await using(var db=f.Factory.CreateDbContext()){db.StaffFinanceDocuments.Add(new(){RequestId=2000,Name="receipt.pdf",ContentType="application/pdf",Content="%PDF-1.7"u8.ToArray()});await db.SaveChangesAsync();}
        Assert.True((await f.Service.ActAsync(2000,7,"Approve","Funds verified")).Success);
        await using(var db=f.Factory.CreateDbContext()){var payment=await db.DeductionPayments.SingleAsync();Assert.Equal(900,payment.PayerBusinessEntityId);Assert.Equal(150,payment.Amount);Assert.Equal(100,(await db.DeductionPaymentAllocations.SingleAsync(x=>x.InstalmentId==1)).Amount);Assert.Equal(50,(await db.DeductionPaymentAllocations.SingleAsync(x=>x.InstalmentId==2)).Amount);}
    }
    [Fact] public async Task Self_and_submitting_officer_cannot_approve_and_documents_are_private()
    {
        var f=new Fixture();await f.Seed();await f.AddAgreement();await f.AddWorkflow(new(){Kind="Defer",JobId=10,AgreementId=1,FromDate=Month,Reason="Defer"});
        await using(var db=f.Factory.CreateDbContext()){(await db.StaffFinanceRequests.SingleAsync()).CreatedByUserId=1;db.StaffFinanceDocuments.Add(new(){Id=1,RequestId=2000,Name="private.pdf",Content="%PDF-1.7"u8.ToArray()});await db.SaveChangesAsync();}
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>f.Service.ActAsync(2000,7,"Approve","Yes"));
        f.Access.Actor!.UserId=99;await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>f.Service.DownloadDocumentAsync(1));
    }
}
