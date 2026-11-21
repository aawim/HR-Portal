using HRM.DTOs.Leadership;
using HRM.Models;
using HRM.Services.Contracts;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;

public sealed class LeadershipAuthorityService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access) : LeadershipAuthorityServiceContracts, LeadershipAuthorityServiceContracts.IOperations
{
    public async Task<IReadOnlyList<LeadershipOrganisationAccessDto>> GetCurrentOrganisationAccessAsync(CancellationToken ct = default)
    {
        var actor=await access.RequireContextAsync(); var day=DateOnly.FromDateTime(DateTime.UtcNow); await using var db=await factory.CreateDbContextAsync(ct);
        var links=await (from a in db.LeadershipAppointments.AsNoTracking() join o in db.LeadershipAppointmentOrganisations.AsNoTracking() on a.LeadershipAppointmentId equals o.LeadershipAppointmentId join org in db.Organisations.AsNoTracking() on o.OrganisationId equals org.BusinessEntityID where a.IndividualId==actor.IndividualId && a.Status=="Active" && a.EffectiveFrom<=day && (a.EffectiveTo==null||a.EffectiveTo>=day) && o.EffectiveFrom<=day && (o.EffectiveTo==null||o.EffectiveTo>=day) select new {a.LeadershipAppointmentId,o.LeadershipAppointmentOrganisationId,o.OrganisationId,org.OrganisationName}).ToListAsync(ct);
        var grantIds=links.Select(x=>x.LeadershipAppointmentOrganisationId).ToList(); var grants=await db.LeadershipAuthorityGrants.AsNoTracking().Where(g=>grantIds.Contains(g.LeadershipAppointmentOrganisationId)&&g.IsActive&&g.ValidFrom<=day&&(g.ValidTo==null||g.ValidTo>=day)).ToListAsync(ct);
        return links.Select(x=>new LeadershipOrganisationAccessDto(x.OrganisationId,x.OrganisationName,x.LeadershipAppointmentId,grants.Where(g=>g.LeadershipAppointmentOrganisationId==x.LeadershipAppointmentOrganisationId).Select(g=>g.PermissionCode).Distinct().ToList())).ToList();
    }
    public async Task<bool> HasAuthorityAsync(int organisationId,string permissionCode,DateOnly? onDate=null,CancellationToken ct=default)
    {
        if (organisationId <= 0 || string.IsNullOrWhiteSpace(permissionCode) || permissionCode.Contains('*'))
            return false;
        var actor=await access.RequireContextAsync(); var day=onDate??DateOnly.FromDateTime(DateTime.UtcNow); await using var db=await factory.CreateDbContextAsync(ct);
        return await (from a in db.LeadershipAppointments join o in db.LeadershipAppointmentOrganisations on a.LeadershipAppointmentId equals o.LeadershipAppointmentId join g in db.LeadershipAuthorityGrants on o.LeadershipAppointmentOrganisationId equals g.LeadershipAppointmentOrganisationId where a.IndividualId==actor.IndividualId&&a.Status=="Active"&&a.EffectiveFrom<=day&&(a.EffectiveTo==null||a.EffectiveTo>=day)&&o.OrganisationId==organisationId&&o.EffectiveFrom<=day&&(o.EffectiveTo==null||o.EffectiveTo>=day)&&g.IsActive&&g.PermissionCode==permissionCode&&g.ValidFrom<=day&&(g.ValidTo==null||g.ValidTo>=day) select g.LeadershipAuthorityGrantId).AnyAsync(ct);
    }
    public async Task RequireAuthorityAsync(int organisationId, string permissionCode, DateOnly? onDate = null, CancellationToken ct = default)
    {
        if (!await HasAuthorityAsync(organisationId, permissionCode, onDate, ct))
            throw new UnauthorizedAccessException("The active leadership appointment does not grant this organisation permission.");
    }
    public async Task<IReadOnlyList<LeadershipAppointmentSummaryDto>> GetMyAppointmentsAsync(CancellationToken ct=default)
    { var actor=await access.RequireContextAsync(); await using var db=await factory.CreateDbContextAsync(ct); return await (from a in db.LeadershipAppointments.AsNoTracking() join t in db.LeadershipAppointmentTypes.AsNoTracking() on a.LeadershipAppointmentTypeId equals t.LeadershipAppointmentTypeId where a.IndividualId==actor.IndividualId select new LeadershipAppointmentSummaryDto(a.LeadershipAppointmentId,t.Name,a.PositionTitle,a.Status,a.EffectiveFrom,a.EffectiveTo)).ToListAsync(ct); }
}
