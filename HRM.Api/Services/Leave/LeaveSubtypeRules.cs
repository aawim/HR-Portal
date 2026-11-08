using HRM.DTOs.Leave;
using HRM.DTOs.LeaveTypes;
using HRM.Models;
using HRM.Models.LeaveTypes;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services.Leave;
public sealed class LeaveSubtypeValidationException(string message):InvalidOperationException(message);

public static class LeaveSubtypeRules
{
    public const string ProtectionPurpose="HRM.LeaveSubtypeCertificates.v1";
    public const int MaximumCertificateBytes=2*1024*1024;
    public static Task LockAsync(HrmTeContext db,int jobId)=>!db.Database.IsSqlServer()?Task.CompletedTask:
        db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource={"HRM.LeaveBalance:"+jobId},@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=10000; IF @r<0 THROW 51000,'Leave balance is busy. Please retry.',1;");
    public static DateTime Period(DateTime anchor,DateTime day)
    {
        var years=day.Year-anchor.Year;var result=anchor.Date.AddYears(years);return result>day.Date?anchor.Date.AddYears(years-1):result;
    }
    public static async Task<List<LeaveSubtypeOption>> OptionsAsync(HrmTeContext db,JobLeaveType balance,int org,DateTime day)
    {
        var job=await db.Jobs.AsNoTracking().SingleAsync(x=>x.JobId==balance.JobId);
        var period=Period(balance.EffectiveFromDate??job.JoinedDate,FormerStaffPolicy.Today);
        var types=await db.LeaveSubtypes.AsNoTracking().Where(x=>x.OrganisationId==org&&x.LeaveTypeId==balance.LeaveTypeId&&x.IsActive).OrderBy(x=>x.Id).ToListAsync();
        var result=new List<LeaveSubtypeOption>();
        foreach(var type in types)
        {
            var used=await UsedAsync(db,balance.JobLeaveTypeId,type.Id,period);
            result.Add(new(type.Id,type.Name,type.Days,Math.Max(0,Math.Min(type.Days-used,balance.RemainingDays??0)),type.CertificateRule));
        }
        return result;
    }
    private static Task<decimal> UsedAsync(HrmTeContext db,int balance,int subtype,DateTime period,int excluding=0)=>db.LeaveSubtypeUsages
        .Where(x=>x.JobLeaveTypeId==balance&&x.SubtypeId==subtype&&x.PeriodStart==period&&x.LeaveId!=excluding&&x.Leave.LeaveStateId!=5&&x.Leave.LeaveStateId!=6)
        .SumAsync(x=>(decimal)x.Leave.Duration);
    public static string? CertificateType(byte[]? content,string? name)
    {
        if(content==null||content.Length==0)return null;
        if(content.Length>MaximumCertificateBytes||string.IsNullOrWhiteSpace(name)||name.Length>180)throw new LeaveSubtypeValidationException("Attach a PDF, PNG or JPEG certificate up to 2 MB.");
        var ext=Path.GetExtension(name).ToLowerInvariant();
        if(ext==".pdf"&&content.AsSpan().StartsWith("%PDF-"u8))return "application/pdf";
        if(ext==".png"&&content.AsSpan().StartsWith(new byte[]{137,80,78,71,13,10,26,10}))return "image/png";
        if(ext is ".jpg" or ".jpeg"&&content.AsSpan().StartsWith(new byte[]{255,216,255}))return "image/jpeg";
        throw new LeaveSubtypeValidationException("The certificate must be a valid PDF, PNG or JPEG file.");
    }
    public static async Task<LeaveSubtypeUsage?> ReserveAsync(HrmTeContext db,JobLeaveType balance,int org,LeaveApplicationDto model,IDataProtectionProvider? protection)
    {
        var options=await OptionsAsync(db,balance,org,model.FromDate);
        var mime=CertificateType(model.CertificateContent,model.CertificateFileName);
        if(options.Count==0)
        {
            if(model.LeaveSubtypeId.HasValue||mime!=null)throw new LeaveSubtypeValidationException("Sub-types are not configured for this leave type.");
            return null;
        }
        var routed=options.Any(x=>x.CertificateRule!="Any");
        var selected=routed?options.SingleOrDefault(x=>x.CertificateRule==(mime==null?"Without":"Required")):options.SingleOrDefault(x=>x.Id==model.LeaveSubtypeId);
        if(selected==null)throw new LeaveSubtypeValidationException("Select a valid leave sub-type.");
        if(model.Duration>selected.RemainingDays)throw new LeaveSubtypeValidationException(mime==null&&routed?
            "There are not enough days without a certificate. Attach a medical certificate to use the certificate-required balance.":$"There are not enough days remaining in {selected.Name}.");
        var job=await db.Jobs.SingleAsync(x=>x.JobId==balance.JobId);var anchor=balance.EffectiveFromDate??job.JoinedDate;
        var period=Period(anchor,FormerStaffPolicy.Today);
        if(model.FromDate.Date<period||model.ToDate.Date>=period.AddYears(1))throw new LeaveSubtypeValidationException($"Select dates within this balance's current entitlement year: {period:dd MMM yyyy} to {period.AddYears(1).AddDays(-1):dd MMM yyyy}.");
        model.LeaveSubtypeId=selected.Id;
        return new(){JobLeaveTypeId=balance.JobLeaveTypeId,SubtypeId=selected.Id,SubtypeName=selected.Name,DaysLimit=selected.AllocatedDays,PeriodStart=period,
            CertificateName=mime==null?null:Path.GetFileName(model.CertificateFileName),ContentType=mime,
            ProtectedCertificate=mime==null?null:(protection??throw new InvalidOperationException("Certificate storage unavailable.")).CreateProtector(ProtectionPurpose).Protect(model.CertificateContent!)};
    }
    public static async Task RequireChangeAsync(HrmTeContext db,Leaf leave,DateTime from,DateTime to,int duration)
    {
        var usage=await db.LeaveSubtypeUsages.SingleOrDefaultAsync(x=>x.LeaveId==leave.LeaveId);if(usage==null)return;
        await LockAsync(db,leave.JobId);
        if(from.Date<usage.PeriodStart||to.Date>=usage.PeriodStart.AddYears(1))throw new LeaveSubtypeValidationException("The revised leave must stay in its original entitlement year.");
        if(await UsedAsync(db,usage.JobLeaveTypeId,usage.SubtypeId,usage.PeriodStart,leave.LeaveId)+duration>usage.DaysLimit)
            throw new LeaveSubtypeValidationException("The revised leave exceeds its sub-type balance.");
    }
}
