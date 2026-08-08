using HRM.Enum;
using HRM.Models;
using HRM.Services.Interfaces.LeaveType;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services
{
    public class LeaveFrameworkResolver : ILeaveFrameworkResolver
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;

        public LeaveFrameworkResolver(
            IDbContextFactory<HrmTeContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }
        public async Task<LeaveFrameworkType>
       GetOrganisationFrameworkAsync(
           int organisationId,
           CancellationToken cancellationToken = default)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            var today =
                DateTime.Today;

            var framework =
                await db.LeaveFrameworkConfigurations
                    .AsNoTracking()
                    .Where(x =>
                        x.OrganisationId ==
                            organisationId &&
                        x.IsActive &&
                        x.EffectiveFrom <= today &&
                        (
                            x.EffectiveTo == null ||
                            x.EffectiveTo >= today
                        ))
                    .OrderByDescending(x =>
                        x.EffectiveFrom)
                    .Select(x =>
                        (LeaveFrameworkType?)x.FrameworkType)
                    .FirstOrDefaultAsync(
                        cancellationToken);

            return framework ??
                   LeaveFrameworkType.Legacy;
        }

        public async Task<LeaveFrameworkType> ResolveAsync(
            int organisationId,
            int? legacyLeaveTypeId,
            int? leaveDefinitionId,
            CancellationToken cancellationToken = default)
        {
            var framework =
                await GetOrganisationFrameworkAsync(
                    organisationId,
                    cancellationToken);

            if (framework !=
                LeaveFrameworkType.Hybrid)
            {
                return framework;
            }

            if (leaveDefinitionId.HasValue)
            {
                return LeaveFrameworkType.PolicyBased;
            }

            return LeaveFrameworkType.Legacy;
        }

    }
}
