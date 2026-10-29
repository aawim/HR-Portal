using System.Text.Json;
using System.Security.Cryptography;
using HRM.Application.Contracts.Pages;
using HRM.Models;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;
public sealed partial class PositionManagementService
{
    private static T ConvertRequirement<T>(object value)=>JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value))!;
    private static bool RequirementText(string? value,int max,bool required=false)=>value!=null&&value.Length<=max&&(!required||!string.IsNullOrWhiteSpace(value));
    private static bool ValidRequirements(JobMatrixProfile p)
    {
        if(p.CredentialRequirements==null||p.QualificationPathways==null||p.CredentialRequirements.Count>50||p.QualificationPathways.Count>20)return false;
        if(p.CredentialRequirements.Any(x=>x==null||x.Id<0||!RequirementText(x.Name,250,true)||!RequirementText(x.Issuer,250,true)||x.Kind is not("Licence" or "Registration" or "Certification" or "Training")||x.Requirement is not("Mandatory" or "Preferred" or "Conditional")||!RequirementText(x.Condition,2000,x.Requirement=="Conditional")||!RequirementText(x.Equivalents,2000)||!RequirementText(x.SourceReference,2000)||!RequirementText(x.VerificationInstructions,2000)||x.ValidityMonths is <1 or >600||x.TrainingHours is <0 or >10000||x.TrainingHours.HasValue&&decimal.Round(x.TrainingHours.Value,2)!=x.TrainingHours))return false;
        foreach(var x in p.QualificationPathways)
        {
            if(x==null||x.Id<0||!RequirementText(x.Name,250,true)||!RequirementText(x.Conditions,4000)||!RequirementText(x.SourceReference,2000)||x.RelevantExperienceMonths is <0 or >1200||x.PostQualificationMonths is <0 or >1200||x.ManagerialExperienceMonths is <0 or >1200||x.Qualifications==null||x.Qualifications.Count is <1 or >20)return false;
            if(x.Qualifications.Any(q=>q==null||q.Id<0||!RequirementText(q.Qualification,250,true)||!RequirementText(q.FieldOfStudy,1000)||q.MinimumLevel is <1 or >10||q.MaximumLevel is <1 or >10||q.MaximumLevel.HasValue&&(!q.MinimumLevel.HasValue||q.MaximumLevel<q.MinimumLevel)))return false;
        }
        return UniqueIds(p.CredentialRequirements.Select(x=>x.Id))&&UniqueIds(p.QualificationPathways.Select(x=>x.Id))&&UniqueIds(p.QualificationPathways.SelectMany(x=>x.Qualifications).Select(x=>x.Id));
    }
    private static bool UniqueIds(IEnumerable<long> ids){var values=ids.Where(x=>x!=0).ToArray();return values.Distinct().Count()==values.Length;}
    private static async Task LoadRequirements(HrmTeContext db,int id,JobMatrixProfile profile)
    {
        profile.CredentialRequirements=(await db.MatrixCredentialRequirements.AsNoTracking().Where(x=>x.JobMatrixId==id&&x.IsActive).OrderBy(x=>x.Id).ToListAsync()).Select(ConvertRequirement<CredentialRequirementInput>).ToList();
        var pathways=await db.MatrixQualificationPathways.AsNoTracking().Where(x=>x.JobMatrixId==id&&x.IsActive).OrderBy(x=>x.Id).ToListAsync();var ids=pathways.Select(x=>x.Id).ToArray();
        var qualifications=await db.MatrixQualificationConditions.AsNoTracking().Where(x=>ids.Contains(x.PathwayId)&&x.IsActive).OrderBy(x=>x.Id).ToListAsync();
        profile.QualificationPathways=pathways.Select(x=>{var p=ConvertRequirement<QualificationPathwayInput>(x);p.Qualifications=qualifications.Where(q=>q.PathwayId==x.Id).Select(ConvertRequirement<QualificationConditionInput>).ToList();return p;}).ToList();
        profile.RequirementsVersion=profile.CredentialRequirements.Count+profile.QualificationPathways.Count==0?"":Convert.ToHexString(SHA256.HashData(JsonSerializer.SerializeToUtf8Bytes(new{profile.CredentialRequirements,profile.QualificationPathways})));
    }
    private static async Task<bool> RequirementsUnchanged(HrmTeContext db,int id,JobMatrixProfile input)
    {
        var current=new JobMatrixProfile();await LoadRequirements(db,id,current);
        if(current.RequirementsVersion!=input.RequirementsVersion)return false;
        if(input.CredentialRequirements.Any(x=>x.Id!=0&&!current.CredentialRequirements.Any(c=>c.Id==x.Id)))return false;
        foreach(var p in input.QualificationPathways)
        {
            var old=current.QualificationPathways.FirstOrDefault(x=>x.Id==p.Id);
            if(p.Id!=0&&old==null)return false;
            if(p.Qualifications.Any(q=>q.Id!=0&&(old==null||!old.Qualifications.Any(x=>x.Id==q.Id))))return false;
        }
        return true;
    }
    private static async Task SaveRequirements(HrmTeContext db,int id,JobMatrixProfile input)
    {
        var credentials=await db.MatrixCredentialRequirements.Where(x=>x.JobMatrixId==id&&x.IsActive).ToListAsync();
        foreach(var old in credentials)if(!input.CredentialRequirements.Any(x=>x.Id==old.Id))old.IsActive=false;
        foreach(var value in input.CredentialRequirements)
        {
            var row=value.Id==0?new MatrixCredentialRequirement{JobMatrixId=id}:credentials.Single(x=>x.Id==value.Id);
            db.Entry(row).CurrentValues.SetValues(value);if(value.Id==0)db.MatrixCredentialRequirements.Add(row);
        }
        var paths=await db.MatrixQualificationPathways.Where(x=>x.JobMatrixId==id&&x.IsActive).ToListAsync();var ids=paths.Select(x=>x.Id).ToArray();
        var qualifications=await db.MatrixQualificationConditions.Where(x=>ids.Contains(x.PathwayId)&&x.IsActive).ToListAsync();
        foreach(var old in paths)if(!input.QualificationPathways.Any(x=>x.Id==old.Id)){old.IsActive=false;foreach(var q in qualifications.Where(x=>x.PathwayId==old.Id))q.IsActive=false;}
        foreach(var value in input.QualificationPathways)
        {
            var row=value.Id==0?new MatrixQualificationPathway{JobMatrixId=id}:paths.Single(x=>x.Id==value.Id);
            db.Entry(row).CurrentValues.SetValues(value);if(value.Id==0)db.MatrixQualificationPathways.Add(row);await db.SaveChangesAsync();
            foreach(var old in qualifications.Where(x=>x.PathwayId==row.Id))if(!value.Qualifications.Any(x=>x.Id==old.Id))old.IsActive=false;
            foreach(var q in value.Qualifications)
            {
                var condition=q.Id==0?new MatrixQualificationCondition{PathwayId=row.Id}:qualifications.Single(x=>x.Id==q.Id&&x.PathwayId==row.Id);
                db.Entry(condition).CurrentValues.SetValues(q);if(q.Id==0)db.MatrixQualificationConditions.Add(condition);
            }
        }
    }
}
