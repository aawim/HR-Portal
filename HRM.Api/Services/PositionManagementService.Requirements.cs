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
    private static string? RequirementsError(JobMatrixProfile p)
    {
        if(p.CredentialRequirements==null||p.QualificationPathways==null||p.CredentialRequirements.Count>50||p.QualificationPathways.Count>20)return "Use up to 50 credential requirements and 20 qualification pathways.";
        for(var i=0;i<p.CredentialRequirements.Count;i++)
        {
            var x=p.CredentialRequirements[i];var row=$"Credential requirement {i+1}: ";
            if(x==null||x.Id<0)return row+"remove the invalid row and add it again.";
            if(!RequirementText(x.Name,250,true))return row+"enter a credential / training name (up to 250 characters).";
            if(!RequirementText(x.Issuer,250,true))return row+"enter the issuing / recognising authority (up to 250 characters).";
            if(x.Kind is not("Licence" or "Registration" or "Certification" or "Training"))return row+"select a valid credential type.";
            if(x.Requirement is not("Mandatory" or "Preferred" or "Conditional"))return row+"select Mandatory, Preferred or Conditional.";
            if(!RequirementText(x.Condition,2000,x.Requirement=="Conditional"))return row+"enter a condition for a Conditional requirement (up to 2,000 characters).";
            if(!RequirementText(x.Equivalents,2000)||!RequirementText(x.SourceReference,2000)||!RequirementText(x.VerificationInstructions,2000))return row+"keep equivalents, source and verification notes within 2,000 characters each.";
            if(x.ValidityMonths is <1 or >600)return row+"validity must be 1–600 months, or left blank.";
            if(x.TrainingHours is <0 or >10000||x.TrainingHours.HasValue&&decimal.Round(x.TrainingHours.Value,2)!=x.TrainingHours)return row+"training hours must be 0–10,000 with up to two decimal places, or left blank.";
        }
        for(var i=0;i<p.QualificationPathways.Count;i++)
        {
            var x=p.QualificationPathways[i];var row=$"Qualification pathway {i+1}: ";
            if(x==null||x.Id<0)return row+"remove the invalid row and add it again.";
            if(!RequirementText(x.Name,250,true))return row+"enter a pathway name (up to 250 characters).";
            if(!RequirementText(x.Conditions,4000)||!RequirementText(x.SourceReference,2000))return row+"keep conditions within 4,000 characters and source within 2,000 characters.";
            if(x.RelevantExperienceMonths is <0 or >1200)return row+"relevant experience must be 0–1,200 months.";
            if(x.PostQualificationMonths is <0 or >1200)return row+"post-qualification experience must be 0–1,200 months.";
            if(x.ManagerialExperienceMonths is <0 or >1200)return row+"managerial experience must be 0–1,200 months.";
            if(x.Qualifications==null||x.Qualifications.Count is <1 or >20)return row+"add between 1 and 20 qualifications, or remove the unused pathway.";
            for(var j=0;j<x.Qualifications.Count;j++)
            {
                var q=x.Qualifications[j];var field=row+$"qualification {j+1}: ";
                if(q==null||q.Id<0)return field+"remove the invalid row and add it again.";
                if(!RequirementText(q.Qualification,250,true))return field+"enter a qualification type / description (up to 250 characters).";
                if(!RequirementText(q.FieldOfStudy,1000))return field+"keep the field of study within 1,000 characters.";
                if(q.MinimumLevel is <1 or >10||q.MaximumLevel is <1 or >10)return field+"MNQF levels must be between 1 and 10.";
                if(q.MaximumLevel.HasValue&&!q.MinimumLevel.HasValue)return field+"select an MNQF level before selecting an upper level.";
                if(q.MaximumLevel<q.MinimumLevel)return field+"the upper MNQF level cannot be below the selected level.";
            }
        }
        if(!UniqueIds(p.CredentialRequirements.Select(x=>x.Id))||!UniqueIds(p.QualificationPathways.Select(x=>x.Id))||!UniqueIds(p.QualificationPathways.SelectMany(x=>x.Qualifications).Select(x=>x.Id)))return "Duplicate requirement rows were submitted. Reload the Job Matrix and try again.";
        return null;
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
