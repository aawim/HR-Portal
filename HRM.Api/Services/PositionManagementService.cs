using System.Data;
using HRM.Application.Contracts.Pages;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;
public sealed class PositionManagementService(IDbContextFactory<HrmTeContext> factory,IUserAccessService access,IOperationLogService logs):IPositionManagement
{
    private async Task<(int Org,bool Manage,bool Super)> ScopeAsync()
    {
        var actor=await access.RequireContextAsync();var org=await access.GetCurrentOrganisationIdAsync();
        var manage=actor.IsSuperAdministrator||await access.HasPermissionAsync("Positions.Configure");
        if(org<=0||!await access.HasOrganisationAccessAsync(org)||!manage&&!await access.HasPermissionAsync("Position.Details")&&!await access.HasPermissionAsync("Staff.Positions.Manage"))throw new UnauthorizedAccessException("Position management permission is required in your current organisation.");
        return(org,manage,actor.IsSuperAdministrator);
    }
    public async Task<PositionManagementData> LoadAsync()
    {
        var actor=await ScopeAsync();await using var db=await factory.CreateDbContextAsync();var today=DateTime.UtcNow.AddHours(5).Date;
        var frameworks=await db.PositionFrameworks.OrderBy(x=>x.FrameworkId).Select(x=>new PositionFrameworkOption(x.FrameworkId,x.Code,x.Name,x.IsActive)).ToListAsync();
        var matrices=await(from m in db.PositionMatrices join j in db.FrameworkJobMatrices on m.JobMatrixId equals j.JobMatrixId join t in db.FrameworkJobTitles on m.JobTitleId equals t.JobTitleId
            where m.OrganisationId==actor.Org&&m.IsActive&&m.EffectiveFrom<=today&&(!m.EffectiveTo.HasValue||m.EffectiveTo>=today)&&j.IsActive&&t.IsActive
            orderby t.Name select new PositionMatrixOption(m.PositionMatrixId,t.Name+" · "+j.MatrixCode+" · Step "+m.JobMatrixStepId,j.BasicSalary,m.ApprovedSlots)).ToListAsync();
        var rows=await db.Positions.Where(x=>x.OrganisationId==actor.Org||x.IsGlobal||x.OrganisationId==null&&db.JobPositions.Any(a=>a.PositionId==x.PositionId&&a.Job.OrganisationID==actor.Org)).OrderBy(x=>x.Name).ToListAsync();var positions=new List<ManagedPosition>();
        foreach(var p in rows)
        {
            var count=await db.JobPositions.Where(x=>x.PositionId==p.PositionId&&x.Job.OrganisationID==actor.Org&&x.JobPositionStateId==4&&x.FromDate<=today&&(!x.ToDate.HasValue||x.ToDate>today)&&x.Job.JobStateId==4&&x.Job.JoinedDate<=today&&(!x.Job.TerminatedDate.HasValue||x.Job.TerminatedDate>today)).Select(x=>x.Job.IndividualID).Distinct().CountAsync();
            positions.Add(new(p.PositionId,p.Name,p.DhivehiName,p.Descriptions,p.IsActive!=false,p.IsGlobal,p.PositionFrameworkId,p.PositionMatrixId,frameworks.FirstOrDefault(x=>x.Id==p.PositionFrameworkId)?.Name??"Unclassified existing position",count,actor.Manage&&(p.OrganisationId==actor.Org&&!p.IsGlobal||actor.Super&&p.IsGlobal)));
        }
        return new(frameworks,matrices,positions,actor.Manage,actor.Super);
    }
    public async Task<ServiceResult> SaveAsync(PositionInput input)
    {
        var actor=await ScopeAsync();if(!actor.Manage)throw new UnauthorizedAccessException("Position configuration permission is required.");
        if(string.IsNullOrWhiteSpace(input.Name)||input.Name.Trim().Length>250||input.DhivehiName?.Length>250||input.Description?.Length>4000)return ServiceResult.Fail("Enter a position name up to 250 characters and description up to 4,000 characters.");
        if(input.Global&&!actor.Super)throw new UnauthorizedAccessException("Only a superadministrator can manage global positions.");
        await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var framework=await db.PositionFrameworks.SingleOrDefaultAsync(x=>x.FrameworkId==input.FrameworkId&&x.IsActive)??throw new InvalidOperationException("Select an active position framework.");
        if(framework.Code=="NEW_PAY")
        {
            var today=DateTime.UtcNow.AddHours(5).Date;
            if(input.Global||!await db.PositionMatrices.AnyAsync(x=>x.PositionMatrixId==input.MatrixId&&x.OrganisationId==actor.Org&&x.IsActive&&x.EffectiveFrom<=today&&(!x.EffectiveTo.HasValue||x.EffectiveTo>=today)))return ServiceResult.Fail("Select an active government position matrix belonging to your organisation.");
        }
        else if(input.MatrixId.HasValue)return ServiceResult.Fail("Custom/legacy positions do not use a government matrix link.");
        var p=input.Id==0?new Position{OrganisationId=actor.Org}:await db.Positions.SingleOrDefaultAsync(x=>x.PositionId==input.Id)??throw new UnauthorizedAccessException();
        if(input.Id!=0&&!(p.OrganisationId==actor.Org&&!p.IsGlobal||actor.Super&&p.IsGlobal))throw new UnauthorizedAccessException("Position unavailable for editing in this organisation.");
        if(input.Id!=0&&(p.IsGlobal!=input.Global||p.PositionFrameworkId!=input.FrameworkId||p.PositionMatrixId!=input.MatrixId)&&await db.JobPositions.AnyAsync(x=>x.PositionId==p.PositionId))return ServiceResult.Fail("This position has employment history. Create a new position to change its framework, matrix or global scope.");
        if(await db.Positions.AnyAsync(x=>x.PositionId!=input.Id&&x.OrganisationId==(input.Global?null:actor.Org)&&x.Name==input.Name.Trim()&&x.PositionFrameworkId==input.FrameworkId))return ServiceResult.Fail("This position name already exists in the selected framework.");
        var log=await logs.CreateAsync(db,SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE,$"Position {input.Id} configured in organisation {actor.Org}; framework {input.FrameworkId}, matrix {input.MatrixId}; active {input.Active}.");
        p.Name=input.Name.Trim();p.DhivehiName=input.DhivehiName?.Trim();p.Descriptions=input.Description?.Trim();p.IsActive=input.Active;p.IsGlobal=input.Global;p.OrganisationId=input.Global?null:actor.Org;p.PositionFrameworkId=input.FrameworkId;p.PositionMatrixId=input.MatrixId;p.OperationLogId=log.OperationLogId;
        if(input.Id==0)db.Positions.Add(p);await db.SaveChangesAsync();await tx.CommitAsync();return ServiceResult.Ok("Position saved. Existing staff assignments are unchanged.");
    }
}
