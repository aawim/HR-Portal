using HRM.DTOs.LeaveTypes;
using HRM.Models.LeaveTypes;
using HRM.Services.Leave;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;

public partial class LeaveTypeCatalogueService
{
    public async Task<LeaveSubtypeSettings> GetSubtypesAsync(int leaveTypeId)
    {
        var a=await Scope();await using var db=await factory.CreateDbContextAsync();
        var parent=await db.LeaveTypes.AsNoTracking().SingleOrDefaultAsync(x=>x.LeaveTypeId==leaveTypeId&&(x.IsGlobal||x.OrganisationId==a.Org))??throw new UnauthorizedAccessException();
        return new(){LeaveTypeId=leaveTypeId,LeaveTypeName=parent.Name,TotalDays=parent.Duration,
            Subtypes=await db.LeaveSubtypes.Where(x=>x.OrganisationId==a.Org&&x.LeaveTypeId==leaveTypeId).OrderBy(x=>x.Id)
                .Select(x=>new LeaveSubtypeInput{Id=x.Id,Name=x.Name,Days=x.Days,CertificateRule=x.CertificateRule,IsActive=x.IsActive,Version=x.Version,InUse=db.LeaveSubtypeUsages.Any(u=>u.SubtypeId==x.Id)}).ToListAsync()};
    }
    public async Task<ServiceResult<int>> SaveSubtypesAsync(LeaveSubtypeSettings request)
    {
        var a=await Scope(true);await using var db=await factory.CreateDbContextAsync();
        await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var parent=await db.LeaveTypes.SingleOrDefaultAsync(x=>x.LeaveTypeId==request.LeaveTypeId&&x.IsActive&&(x.IsGlobal||x.OrganisationId==a.Org))??throw new UnauthorizedAccessException();
        if(request.Subtypes.Count>30)return ServiceResult<int>.Failure("Configure at most 30 sub-types.");
        var active=request.Subtypes.Where(x=>x.IsActive).ToList();
        if(request.Subtypes.Any(x=>string.IsNullOrWhiteSpace(x.Name)||x.Name.Trim().Length>150||x.Days<=0||x.Days>3660||decimal.Round(x.Days,2)!=x.Days||x.CertificateRule is not ("Any" or "Required" or "Without"))||
            request.Subtypes.Select(x=>x.Name.Trim().ToUpperInvariant()).Distinct().Count()!=request.Subtypes.Count)
            return ServiceResult<int>.Failure("Use unique sub-type names, positive day allocations and a valid certificate rule.");
        if(active.Count>0&&parent.Duration.HasValue&&active.Sum(x=>x.Days)!=parent.Duration.Value)
            return ServiceResult<int>.Failure($"Active sub-type allocations must total {parent.Duration} days. They divide the parent entitlement; they do not add days.");
        if(active.Any(x=>x.CertificateRule!="Any")&&(active.Count!=2||active.Count(x=>x.CertificateRule=="Required")!=1||active.Count(x=>x.CertificateRule=="Without")!=1))
            return ServiceResult<int>.Failure("Certificate routing needs exactly two active sub-types: certificate required and without certificate.");
        var rows=await db.LeaveSubtypes.Where(x=>x.OrganisationId==a.Org&&x.LeaveTypeId==parent.LeaveTypeId).ToListAsync();
        if(request.Subtypes.Where(x=>x.Id!=0).Select(x=>x.Id).Distinct().Count()!=request.Subtypes.Count(x=>x.Id!=0)||request.Subtypes.Any(x=>x.Id!=0&&!rows.Any(r=>r.Id==x.Id)))throw new UnauthorizedAccessException();
        foreach(var row in rows)
        {
            var input=request.Subtypes.SingleOrDefault(x=>x.Id==row.Id);
            if(input!=null&&row.Version!=input.Version)return ServiceResult<int>.Failure("Sub-types changed. Refresh before saving.");
            if(await db.LeaveSubtypeUsages.AnyAsync(x=>x.SubtypeId==row.Id)&&
                (input==null||input.Days!=row.Days||input.CertificateRule!=row.CertificateRule||input.IsActive!=row.IsActive))
                return ServiceResult<int>.Failure("An allocated sub-type is already in use. Its allocation and certificate rule cannot be changed.");
        }
        await (logs??throw new InvalidOperationException("Audit service unavailable.")).CreateAsync(db,HRM.Components.Shared.SharedConfig.OperationLogActionTypes.LEAVE_TYPE_UDPATE,$"Configured sub-types for leave type {parent.LeaveTypeId}, organisation {a.Org}: "+string.Join("; ",active.Select(x=>$"{x.Name}: {x.Days} days, {x.CertificateRule}")));
        foreach(var row in rows.Where(x=>!request.Subtypes.Any(i=>i.Id==x.Id))){row.IsActive=false;row.Version++;}
        foreach(var input in request.Subtypes)
        {
            var row=rows.SingleOrDefault(x=>x.Id==input.Id)??new LeaveSubtype{OrganisationId=a.Org,LeaveTypeId=parent.LeaveTypeId};
            row.Name=input.Name.Trim();row.Days=input.Days;row.CertificateRule=input.CertificateRule;row.IsActive=input.IsActive;row.Version++;
            if(row.Id==0)db.LeaveSubtypes.Add(row);
        }
        await db.SaveChangesAsync();await tx.CommitAsync();return ServiceResult<int>.Successful(parent.LeaveTypeId,"Leave sub-types saved.");
    }
    public async Task<LeaveCertificateDownload?> GetCertificateAsync(int leaveId)
    {
        var a=await Scope();await using var db=await factory.CreateDbContextAsync();
        var requestId=await db.LeaveRequests.Where(x=>x.LeaveId==leaveId&&x.Leave.Job.OrganisationID==a.Org).Select(x=>(int?)x.RequestId).SingleOrDefaultAsync();
        if(requestId==null||business==null||!(await business.CanViewLeaveAsync(requestId.Value)).Allowed)throw new UnauthorizedAccessException();
        var usage=await db.LeaveSubtypeUsages.AsNoTracking().SingleOrDefaultAsync(x=>x.LeaveId==leaveId);
        if(usage==null)return null;
        var bytes=usage.ProtectedCertificate==null?[]:(protection??throw new InvalidOperationException("Certificate storage unavailable.")).CreateProtector(LeaveSubtypeRules.ProtectionPurpose).Unprotect(usage.ProtectedCertificate);
        return new(usage.CertificateName??"",usage.ContentType??"application/octet-stream",bytes,usage.SubtypeName);
    }
}
