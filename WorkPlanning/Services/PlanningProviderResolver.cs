using HRM.Models;
using HRM.WorkPlanning.Abstractions;
using HRM.WorkPlanning.Results;
using Microsoft.EntityFrameworkCore;

namespace HRM.WorkPlanning.Services
{
    public sealed class PlanningProviderResolver : IPlanningProviderResolver
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private const string LegacyShiftCode = "LEGACY_SHIFT";
        private const string WorkPlanningCode = "WORK_PLANNING";
        public PlanningProviderResolver(
            IDbContextFactory<HrmTeContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

       public async Task<PlanningProviderResolution> ResolveAsync(int organisationBusinessEntityId,DateOnly workDate,
       CancellationToken cancellationToken = default)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync(cancellationToken);

            var setting = await db.OrganisationWorkPlanningSettings
                .AsNoTracking()
                .Where(x =>
                    x.OrganisationBusinessEntityId ==
                        organisationBusinessEntityId &&
                    x.IsActive &&
                    (!x.EffectiveFromDate.HasValue ||
                     x.EffectiveFromDate.Value <= workDate) &&
                    (!x.EffectiveToDate.HasValue ||
                     x.EffectiveToDate.Value >= workDate))
                .OrderByDescending(x => x.EffectiveFromDate)
                .Select(x => new
                {
                    x.PlanningProviderId,
                    ProviderCode = x.PlanningProvider.Code
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (setting != null)
            {
                return CreateResolution(
                    setting.PlanningProviderId,
                    setting.ProviderCode);
            }

            var legacyProvider = await db.PlanningProviders
                .AsNoTracking()
                .Where(x =>
                    x.Code == LegacyShiftCode &&
                    x.IsActive)
                .Select(x => new
                {
                    x.PlanningProviderId,
                    x.Code
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (legacyProvider == null)
            {
                throw new InvalidOperationException(
                    $"The '{LegacyShiftCode}' planning provider is missing or inactive.");
            }

            return CreateResolution(
                legacyProvider.PlanningProviderId,
                legacyProvider.Code);
        }

        private static PlanningProviderResolution CreateResolution(
            int planningProviderId,
            string providerCode)
        {
            return new PlanningProviderResolution
            {
                PlanningProviderId = planningProviderId,
                Code = providerCode,

            };
        }









    }

}