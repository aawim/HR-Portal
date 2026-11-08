using HRM.Application.Contracts.Pages;
using HRM.Models;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;
public sealed partial class PositionManagementService
{
    private static JobMatrixProfile Profile(FrameworkJobMatrix x)=>new(){JobFamilyId=x.JobFamilyId,ProfessionId=x.ProfessionId,Description=x.Description??"",PublicServiceRank=x.PublicServiceRank??"",InstitutionalCategory=x.InstitutionalCategory??"",GradeCode=x.GradeCode??"",JobFamily=x.JobFamilyName??"",MisscoCode=x.MisscoCode??"",SkillLevel=x.SkillLevel,AttendanceBenefitDaily=x.AttendanceBenefitDaily,RevisedDate=x.RevisedDate,LicensesAndTraining=x.LicensesAndTraining??"",FieldsOfStudy=x.FieldsOfStudy??"",ExperienceAreas=x.ExperienceAreas??""};
    private static bool ValidProfile(JobMatrixProfile? p)=>p!=null&&p.PublicServiceRank?.Length<=50&&p.InstitutionalCategory?.Length<=100&&p.GradeCode?.Length<=50&&p.JobFamily?.Length<=250&&p.MisscoCode?.Length<=10&&p.SkillLevel is not >9&&(!p.AttendanceBenefitDaily.HasValue||Money(p.AttendanceBenefitDaily.Value))&&new[]{p.Description,p.LicensesAndTraining,p.FieldsOfStudy,p.ExperienceAreas,p.Qualifications,p.Duties}.All(x=>x!=null&&x.Length<=20000);
    private static void ApplyProfile(FrameworkJobMatrix row,JobMatrixProfile p)
    {
        row.JobFamilyId=p.JobFamilyId;row.ProfessionId=p.ProfessionId;row.Description=p.Description;row.PublicServiceRank=p.PublicServiceRank;row.InstitutionalCategory=p.InstitutionalCategory;row.GradeCode=p.GradeCode;row.JobFamilyName=p.JobFamily;row.MisscoCode=p.MisscoCode;row.SkillLevel=p.SkillLevel;row.AttendanceBenefitDaily=p.AttendanceBenefitDaily;row.RevisedDate=p.RevisedDate?.Date;row.LicensesAndTraining=p.LicensesAndTraining;row.FieldsOfStudy=p.FieldsOfStudy;row.ExperienceAreas=p.ExperienceAreas;
    }
    private static async Task<bool> ProfileTextsMatch(HrmTeContext db,int id,JobMatrixProfile p)
    {
        var duties=await db.FrameworkMatrixDuties.Where(x=>x.JobMatrixId==id).OrderBy(x=>x.SequenceNumber).Select(x=>x.DutyDescription).ToListAsync();
        var qualifications=await db.FrameworkMatrixQualifications.Where(x=>x.JobMatrixId==id).OrderBy(x=>x.PathwayNumber).Select(x=>x.RequirementText).ToListAsync();
        return (duties.Count==0||string.Join("\n",duties)==p.Duties)&&(qualifications.Count==0||string.Join("\nOR\n",qualifications)==p.Qualifications);
    }
    private static async Task SaveProfileText(HrmTeContext db,int id,JobMatrixProfile p)
    {
        if(!string.IsNullOrWhiteSpace(p.Duties)&&!await db.FrameworkMatrixDuties.AnyAsync(x=>x.JobMatrixId==id))db.FrameworkMatrixDuties.Add(new(){JobMatrixId=id,SequenceNumber=1,DutyDescription=p.Duties});
        if(!string.IsNullOrWhiteSpace(p.Qualifications)&&!await db.FrameworkMatrixQualifications.AnyAsync(x=>x.JobMatrixId==id))db.FrameworkMatrixQualifications.Add(new(){JobMatrixId=id,PathwayNumber=1,RequirementText=p.Qualifications});
    }
}
