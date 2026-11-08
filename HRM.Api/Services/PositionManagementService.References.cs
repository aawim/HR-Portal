using HRM.Application.Contracts.Pages;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;
public sealed partial class PositionManagementService
{
    private async Task<JobMatrixInput?> ResolveReferenceAsync(JobMatrixInput input)
    {
        await using var db=await factory.CreateDbContextAsync();
        if(input.Profile?.ProfessionId==null)
        {
            if(input.Id==0||input.Profile==null)return null;
            var legacy=await db.FrameworkJobMatrices.SingleOrDefaultAsync(x=>x.JobMatrixId==input.Id&&x.ProfessionId==null);
            return legacy==null||legacy.JobFamilyId!=input.Profile.JobFamilyId?null:input with{Name=legacy.Name};
        }
        var reference=await(from p in db.PayProfessions join f in db.PayJobFamilies on p.JobFamilyId equals f.JobFamilyId where p.Id==input.Profile.ProfessionId&&p.JobFamilyId==input.Profile.JobFamilyId&&p.IsActive&&f.IsActive select new{p.Name,Family=f.Name,p.JobFamilyId}).SingleOrDefaultAsync();
        return reference==null?null:input with{Name=reference.Name,Profile=input.Profile with{JobFamily=reference.Family}};
    }
}
