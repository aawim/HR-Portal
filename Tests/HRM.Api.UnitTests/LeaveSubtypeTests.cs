using HRM.DTOs.Leave;
using HRM.DTOs.LeaveTypes;
using HRM.Models;
using HRM.Models.LeaveTypes;
using HRM.Services;
using HRM.Services.Interfaces;
using HRM.Services.Leave;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace HRM.Api.UnitTests;

public sealed class LeaveSubtypeTests
{
    private readonly QueryFactory factory=new(ignoreTransactions:true);
    private readonly FakeAccess access=new();
    private readonly EphemeralDataProtectionProvider protection=new();
    private readonly DateTime start=new(FormerStaffPolicy.Today.Year,1,1);
    private static readonly byte[] Pdf="%PDF-1.7\ncertificate"u8.ToArray();
    private async Task Seed()
    {
        await using var db=factory.CreateDbContext();
        db.Organisations.Add(new(){BusinessEntityID=10,OrganisationName="Tenant"});
        db.LeaveTypes.Add(new(){LeaveTypeId=9,Name="Sick Leave",Code="SL",OrganisationId=1,IsGlobal=true,Duration=30});
        db.Jobs.Add(new(){JobId=1,IndividualID=100,OrganisationID=10,JobStateId=4,JoinedDate=start});
        db.JobLeaveTypes.Add(new(){JobLeaveTypeId=1,JobId=1,LeaveTypeId=9,RemainingDays=30,IsValid=true,EffectiveFromDate=start});
        db.LeaveSubtypes.AddRange(new(){Id=1,OrganisationId=10,LeaveTypeId=9,Name="With certificate",Days=15,CertificateRule="Required"},new(){Id=2,OrganisationId=10,LeaveTypeId=9,Name="Without certificate",Days=15,CertificateRule="Without"});
        await db.SaveChangesAsync();
    }
    private LeaveApplicationDto Form(int days=1,bool certificate=false)=>new(){LeaveSubtypeId=1,FromDate=start.AddDays(10),ToDate=start.AddDays(9+days),Duration=days,CertificateContent=certificate?Pdf:null,CertificateFileName=certificate?"certificate.pdf":null};
    private async Task AddUsage(int days,int state=2)
    {
        await using var db=factory.CreateDbContext();var balance=await db.JobLeaveTypes.SingleAsync();
        var usage=await LeaveSubtypeRules.ReserveAsync(db,balance,10,Form(days),protection);
        usage!.Leave=new(){LeaveId=1,JobId=1,LeaveTypeId=9,Duration=days,FromDate=start.AddDays(10),ToDate=start.AddDays(9+days),LeaveStateId=state};
        db.LeaveSubtypeUsages.Add(usage);await db.SaveChangesAsync();
    }
    [Fact]public async Task Attachment_routes_the_reservation_and_cannot_be_spoofed_by_selected_id_or_certificate_number()
    {
        await Seed();await using var db=factory.CreateDbContext();var balance=await db.JobLeaveTypes.SingleAsync();
        var noFile=Form();noFile.MedicalCertificateNo="claimed certificate";
        Assert.Equal(2,(await LeaveSubtypeRules.ReserveAsync(db,balance,10,noFile,protection))!.SubtypeId);
        var withFile=Form(certificate:true);withFile.LeaveSubtypeId=2;
        var usage=(await LeaveSubtypeRules.ReserveAsync(db,balance,10,withFile,protection))!;
        Assert.Equal(1,usage.SubtypeId);Assert.NotEqual(Pdf,usage.ProtectedCertificate);
        Assert.Equal(Pdf,protection.CreateProtector(LeaveSubtypeRules.ProtectionPurpose).Unprotect(usage.ProtectedCertificate!));
    }
    [Fact]public async Task Exhausted_without_certificate_quota_requires_a_real_attachment_and_keeps_parent_cap()
    {
        await Seed();await AddUsage(15);await using var db=factory.CreateDbContext();var balance=await db.JobLeaveTypes.SingleAsync();
        var error=await Assert.ThrowsAsync<LeaveSubtypeValidationException>(()=>LeaveSubtypeRules.ReserveAsync(db,balance,10,Form(),protection));Assert.Contains("Attach",error.Message);
        Assert.Equal(1,(await LeaveSubtypeRules.ReserveAsync(db,balance,10,Form(certificate:true),protection))!.SubtypeId);
        balance.RemainingDays=0;
        await Assert.ThrowsAsync<LeaveSubtypeValidationException>(()=>LeaveSubtypeRules.ReserveAsync(db,balance,10,Form(certificate:true),protection));
    }
    [Theory][InlineData(5)][InlineData(6)]
    public async Task Cancelled_and_rejected_reservations_release_their_subtype_quota(int closedState)
    {
        await Seed();await AddUsage(15);await using var db=factory.CreateDbContext();var balance=await db.JobLeaveTypes.SingleAsync();
        Assert.Equal(0,(await LeaveSubtypeRules.OptionsAsync(db,balance,10,start.AddDays(10))).Single(x=>x.Id==2).RemainingDays);
        (await db.Leaves.SingleAsync()).LeaveStateId=closedState;await db.SaveChangesAsync();
        Assert.Equal(15,(await LeaveSubtypeRules.OptionsAsync(db,balance,10,start.AddDays(10))).Single(x=>x.Id==2).RemainingDays);
    }
    [Fact]public async Task Early_return_frees_days_and_extensions_and_date_corrections_cannot_exceed_quota_or_reset_the_period()
    {
        await Seed();await AddUsage(10);await using var db=factory.CreateDbContext();var leave=await db.Leaves.SingleAsync();
        await Assert.ThrowsAsync<LeaveSubtypeValidationException>(()=>LeaveSubtypeRules.RequireChangeAsync(db,leave,leave.FromDate,leave.ToDate.AddDays(6),16));
        await Assert.ThrowsAsync<LeaveSubtypeValidationException>(()=>LeaveSubtypeRules.RequireChangeAsync(db,leave,start.AddYears(1),start.AddYears(1).AddDays(1),2));
        leave.Duration=5;await db.SaveChangesAsync();var balance=await db.JobLeaveTypes.SingleAsync();
        Assert.Equal(10,(await LeaveSubtypeRules.OptionsAsync(db,balance,10,start.AddDays(10))).Single(x=>x.Id==2).RemainingDays);
        Assert.Equal(10,(await LeaveSubtypeRules.OptionsAsync(db,balance,10,start.AddYears(1).AddDays(10))).Single(x=>x.Id==2).RemainingDays);
        var nextYear=Form();nextYear.FromDate=start.AddYears(1);nextYear.ToDate=nextYear.FromDate;
        await Assert.ThrowsAsync<LeaveSubtypeValidationException>(()=>LeaveSubtypeRules.ReserveAsync(db,balance,10,nextYear,protection));
    }
    [Theory][InlineData("bad.pdf","not a pdf")][InlineData("unsafe.html","%PDF-")]
    public void Invalid_certificate_formats_are_rejected(string name,string content)=>Assert.Throws<LeaveSubtypeValidationException>(()=>LeaveSubtypeRules.CertificateType(System.Text.Encoding.UTF8.GetBytes(content),name));
    [Fact]public async Task Configuration_requires_permission_exact_parent_allocation_and_immutable_used_rules()
    {
        await Seed();var service=new LeaveTypeCatalogueService(factory,access,StubProxy.For<IOperationLogService>((_,_)=>Task.FromResult(new OperationLog{OperationLogId=1})));
        var settings=await service.GetSubtypesAsync(9);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.SaveSubtypesAsync(settings));
        access.Permissions.Add("Leave.Configuration.Manage");settings.Subtypes[0].Days=20;
        Assert.False((await service.SaveSubtypesAsync(settings)).Success);
        settings.Subtypes[1].Days=10;Assert.True((await service.SaveSubtypesAsync(settings)).Success);
        await AddUsage(1);settings=await service.GetSubtypesAsync(9);settings.Subtypes[0].Days=15;settings.Subtypes[1].Days=15;
        Assert.False((await service.SaveSubtypesAsync(settings)).Success);
        access.Organisation=20;Assert.Empty((await service.GetSubtypesAsync(9)).Subtypes);
    }
    [Fact]public async Task Certificate_download_enforces_request_access_and_tenant_scope()
    {
        await Seed();await using(var db=factory.CreateDbContext())
        {
            var balance=await db.JobLeaveTypes.SingleAsync();var usage=(await LeaveSubtypeRules.ReserveAsync(db,balance,10,Form(certificate:true),protection))!;
            usage.Leave=new(){LeaveId=1,JobId=1,LeaveTypeId=9,Duration=1,LeaveStateId=2};db.LeaveSubtypeUsages.Add(usage);
            db.Requests.Add(new(){RequestId=10,OrganisationBusinessEntityID=10,ApplicantBusinessEntityId=100});db.LeaveRequests.Add(new(){LeaveId=1,RequestId=10});await db.SaveChangesAsync();
        }
        var allowed=false;var business=StubProxy.For<IBusinessAccessService>((_,_)=>Task.FromResult(new HRM.DTOs.Security.AccessResultDto{Allowed=allowed}));
        var service=new LeaveTypeCatalogueService(factory,access,null,business,protection);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.GetCertificateAsync(1));allowed=true;
        Assert.Equal(Pdf,(await service.GetCertificateAsync(1))!.Content);
        access.Organisation=20;await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.GetCertificateAsync(1));
    }
}
