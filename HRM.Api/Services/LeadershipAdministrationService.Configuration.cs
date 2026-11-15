using HRM.DTOs.Leadership;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;
public sealed partial class LeadershipAdministrationService
{
 public async Task<LeadershipConfigurationDto> GetConfigurationAsync(CancellationToken ct=default){await RequireAdministratorAsync();await using var db=await factory.CreateDbContextAsync(ct);return new(await db.LeadershipAppointmentTypes.AsNoTracking().Where(x=>x.IsActive).OrderBy(x=>x.Name).Select(x=>new LeadershipLookupOption(x.LeadershipAppointmentTypeId,x.Name)).ToListAsync(ct),await db.Organisations.AsNoTracking().OrderBy(x=>x.OrganisationName).Select(x=>new LeadershipLookupOption(x.BusinessEntityID,x.OrganisationName)).ToListAsync(ct),await db.LeadershipAppointments.AsNoTracking().Where(x=>x.Status=="Active").Select(x=>new LeadershipLookupOption(x.LeadershipAppointmentId,x.PositionTitle+" (#"+x.LeadershipAppointmentId+")")).ToListAsync(ct),await db.LeadershipAppointmentOrganisations.AsNoTracking().Join(db.Organisations,o=>o.OrganisationId,g=>g.BusinessEntityID,(o,g)=>new LeadershipLookupOption(o.LeadershipAppointmentOrganisationId,g.OrganisationName+" — appointment #"+o.LeadershipAppointmentId)).ToListAsync(ct));}
}
