using HRM.Components.Shared;
using HRM.DTOs.JobPosition;
using HRM.Models;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.JobPosition;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.JobPosition
{
    public class JobPositionService : IJobPosition
    {

        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IUserAccessService _userAccessService;
        private readonly IJobService _jobService;
        private readonly ILogger<ProfileService> _logger;

        public JobPositionService(
        IDbContextFactory<HrmTeContext> dbFactory,
        IUserAccessService userAccessService, IJobService jobService, ILogger<ProfileService> logger)
        {
            _dbFactory = dbFactory;
            _userAccessService = userAccessService;
            _jobService = jobService;
            _logger = logger;
        }

        public async Task<JobPositionDto?> GetCurrentPositionAsync(int StaffId,
            CancellationToken cancellationToken = default)
        {
            if (StaffId <= 0)
            {
                return null;
            }


            var activeJob = _jobService.GetActiveJobAsync(StaffId, cancellationToken);

 
                await using var db =
           await _dbFactory.CreateDbContextAsync(cancellationToken);

                return await
                (
                    from jobPosition in db.JobPositions.AsNoTracking()

                    join position in db.Positions.AsNoTracking()
                        on jobPosition.PositionId equals position.PositionId

                    join state in db.JobPositionStates.AsNoTracking()
                        on jobPosition.JobPositionStateId equals
                           state.JobPositionStateId

                    where jobPosition.JobId == activeJob.Result.JobId
                          && jobPosition.JobPositionStateId ==
                                SharedConfig.JobPositionStates.APPROVED
                          && jobPosition.ToDate == null



                    orderby jobPosition.FromDate descending

                    select new JobPositionDto
                    {
                        JobPositionId =
                            jobPosition.JobPositionId,

                        JobId =
                            jobPosition.JobId,

                        PositionId =
                            jobPosition.PositionId,

                        PositionName =
                            position.Name,

                        FromDate =
                            jobPosition.FromDate,

                        ToDate =
                            jobPosition.ToDate,

                        IsCurrent =
                            jobPosition.ToDate == null,

                        JobPositionStateId =
                            jobPosition.JobPositionStateId,

                        JobPositionStateName =
                            state.StateName
                    }
                )
                .FirstOrDefaultAsync(cancellationToken);
 

       
        }

        public async Task<List<JobPositionHistoryDto>>GetPositionHistoryByJobAsync(int jobId,
        CancellationToken cancellationToken = default)
        {
            if (jobId <= 0)
            {
                return [];
            }

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            var history =
                await
                (
                    from jobPosition in db.JobPositions.AsNoTracking()

                    join job in db.Jobs.AsNoTracking()
                        on jobPosition.JobId equals job.JobId

                    join position in db.Positions.AsNoTracking()
                        on jobPosition.PositionId equals position.PositionId

                    join organisation in db.Organisations.AsNoTracking()
                        on job.OrganisationID equals
                        organisation.BusinessEntityID

                    join structure in db.OrganisationStructures.AsNoTracking()
                        on job.OrganisationStructureId equals
                        structure.OrganisationStructureId
                        into structureGroup

                    from structure in structureGroup.DefaultIfEmpty()

                    join state in db.JobPositionStates.AsNoTracking()
                        on jobPosition.JobPositionStateId equals
                        state.JobPositionStateId
                        into stateGroup

                    from state in stateGroup.DefaultIfEmpty()

                    where jobPosition.JobId == jobId

                    orderby
                        jobPosition.FromDate descending,
                        jobPosition.JobPositionId descending

                    select new JobPositionHistoryDto
                    {
                        JobPositionId =
                            jobPosition.JobPositionId,

                        JobId =
                            job.JobId,

                        PositionId =
                            jobPosition.PositionId,

                        PositionName =
                            position.Name ?? string.Empty,

                        OrganisationId =
                            job.OrganisationID,

                        OrganisationName =
                            organisation.OrganisationName ??
                            string.Empty,

                        OrganisationStructureId =
                            job.OrganisationStructureId,

                        OrganisationStructureName =
                            structure != null
                                ? structure.Name ?? string.Empty
                                : string.Empty,

                        JobPositionStateId =
                            jobPosition.JobPositionStateId,

                        JobPositionStateName =
                            state != null
                                ? state.StateName ?? string.Empty
                                : string.Empty,

                        FromDate =
                            jobPosition.FromDate,

                        ToDate =
                            jobPosition.ToDate,

                        IsCurrent =
                            jobPosition.JobPositionStateId ==
                                SharedConfig.JobPositionStates.APPROVED &&
                            jobPosition.ToDate == null
                    }
                )
                .ToListAsync(cancellationToken);

            foreach (var item in history)
            {
                item.EffectivePeriodText =
                    BuildEffectivePeriodText(
                        item.FromDate,
                        item.ToDate,
                        item.JobPositionStateName);
            }

            return history;
        }


        public async Task<List<JobHistoryWithPositionsDto>>
         GetStaffJobHistoryAsync(
         int individualId,
         int organisationId,
         CancellationToken cancellationToken = default)
        {
            if (individualId <= 0 ||
                organisationId <= 0)
            {
                return [];
            }

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            var jobs =
                await
                (
                    from job in db.Jobs.AsNoTracking()

                    join organisation in db.Organisations.AsNoTracking()
                        on job.OrganisationID equals
                        organisation.BusinessEntityID

                    join structure in db.OrganisationStructures.AsNoTracking()
                        on job.OrganisationStructureId equals
                        structure.OrganisationStructureId
                        into structureGroup

                    from structure in structureGroup.DefaultIfEmpty()

                    join jobState in db.JobStates.AsNoTracking()
                        on job.JobStateId equals jobState.JobStateId
                        into jobStateGroup

                    from jobState in jobStateGroup.DefaultIfEmpty()

                    join jobType in db.JobTypes.AsNoTracking()
                        on job.JobTypeId equals jobType.JobTypeId
                        into jobTypeGroup

                    from jobType in jobTypeGroup.DefaultIfEmpty()

                    where
                        job.IndividualID == individualId &&
                        job.OrganisationID == organisationId

                    orderby
                        job.JoinedDate descending,
                        job.JobId descending

                    select new JobHistoryWithPositionsDto
                    {
                        JobId =
                            job.JobId,

                        IndividualId =
                            job.IndividualID,

                        OrganisationId =
                            job.OrganisationID,

                        OrganisationName =
                            organisation.OrganisationName ??
                            string.Empty,

                        OrganisationStructureId =
                            job.OrganisationStructureId,

                        OrganisationStructureName =
                            structure != null
                                ? structure.Name ?? string.Empty
                                : string.Empty,

                        JobStateId =
                            job.JobStateId,

                        JobStateName =
                            jobState != null
                                ? jobState.StateName ?? string.Empty
                                : string.Empty,

                        JobTypeId =
                            job.JobTypeId,

                        JobTypeName =
                            jobType != null
                                ? jobType.TypeName ?? string.Empty
                                : string.Empty,

                        JoinedDate =
                            job.JoinedDate,

                        TerminatedDate =
                            job.TerminatedDate,

                        IsCurrentJob =
                            job.JobStateId ==
                                SharedConfig.JobStates.APPROVED &&
                            job.TerminatedDate == null
                    }
                )
                .ToListAsync(cancellationToken);

            if (jobs.Count == 0)
            {
                return [];
            }

            var jobIds =
                jobs
                    .Select(job => job.JobId)
                    .ToList();

            var positionHistory =
                await
                (
                    from jobPosition in db.JobPositions.AsNoTracking()

                    join position in db.Positions.AsNoTracking()
                        on jobPosition.PositionId equals position.PositionId

                    join state in db.JobPositionStates.AsNoTracking()
                        on jobPosition.JobPositionStateId equals
                        state.JobPositionStateId
                        into stateGroup

                    from state in stateGroup.DefaultIfEmpty()

                    where jobIds.Contains(jobPosition.JobId)

                    orderby
                        jobPosition.FromDate descending,
                        jobPosition.JobPositionId descending

                    select new JobPositionHistoryDto
                    {
                        JobPositionId =
                            jobPosition.JobPositionId,

                        JobId =
                            jobPosition.JobId,

                        PositionId =
                            jobPosition.PositionId,

                        PositionName =
                            position.Name ?? string.Empty,

                        JobPositionStateId =
                            jobPosition.JobPositionStateId,

                        JobPositionStateName =
                            state != null
                                ? state.StateName ?? string.Empty
                                : string.Empty,

                        FromDate =
                            jobPosition.FromDate,

                        ToDate =
                            jobPosition.ToDate,

                        IsCurrent =
                            jobPosition.JobPositionStateId ==
                                SharedConfig.JobPositionStates.APPROVED &&
                            jobPosition.ToDate == null
                    }
                )
                .ToListAsync(cancellationToken);

            foreach (var position in positionHistory)
            {
                position.EffectivePeriodText =
                    BuildEffectivePeriodText(
                        position.FromDate,
                        position.ToDate,
                        position.JobPositionStateName);
            }

            var positionsByJob =
                positionHistory
                    .GroupBy(position => position.JobId)
                    .ToDictionary(
                        group => group.Key,
                        group => group
                            .OrderByDescending(position =>
                                position.FromDate)
                            .ThenByDescending(position =>
                                position.JobPositionId)
                            .ToList());

            foreach (var job in jobs)
            {
                job.Positions =
                    positionsByJob.TryGetValue(
                        job.JobId,
                        out var positions)
                        ? positions
                        : [];

                job.EmploymentPeriodText =
                    job.TerminatedDate.HasValue
                        ? $"{job.JoinedDate:dd MMM yyyy} – " +
                          $"{job.TerminatedDate.Value:dd MMM yyyy}"
                        : $"{job.JoinedDate:dd MMM yyyy} – Present";
            }

            return jobs;
        }







        private static string BuildEffectivePeriodText(
    DateTime fromDate,
    DateTime? toDate,
    string? stateName)
        {
            if (toDate.HasValue)
            {
                return
                    $"{fromDate:dd MMM yyyy} – " +
                    $"{toDate.Value:dd MMM yyyy}";
            }

            if (!string.IsNullOrWhiteSpace(stateName))
            {
                return
                    $"{fromDate:dd MMM yyyy} – {stateName}";
            }

            return $"{fromDate:dd MMM yyyy} – Present";
        }





















        //public async Task<List<JobPositionHistoryDto>> GetPositionHistoryAsync(
        //int individualId,
        //int organisationId,
        //CancellationToken cancellationToken = default)
        //{
        //    if (individualId <= 0 ||
        //        organisationId <= 0)
        //    {
        //        return [];
        //    }

        //    await using var db =
        //        await _dbFactory.CreateDbContextAsync(
        //            cancellationToken);

        //    var approvedStateId =
        //        SharedConfig.JobPositionStates.APPROVED;

        //    var query =
        //        from jobPosition in db.JobPositions.AsNoTracking()

        //        join job in db.Jobs.AsNoTracking()
        //            on jobPosition.JobId equals job.JobId

        //        join position in db.Positions.AsNoTracking()
        //            on jobPosition.PositionId equals position.PositionId

        //        join organisation in db.Organisations.AsNoTracking()
        //            on job.OrganisationID equals organisation.BusinessEntityID

        //        join structure in db.OrganisationStructures.AsNoTracking()
        //            on job.OrganisationStructureId equals
        //                structure.OrganisationStructureId
        //            into structureGroup

        //        from structure in structureGroup.DefaultIfEmpty()

        //        join state in db.JobPositionStates.AsNoTracking()
        //            on jobPosition.JobPositionStateId equals
        //                state.JobPositionStateId
        //            into stateGroup

        //        from state in stateGroup.DefaultIfEmpty()

        //        where
        //            job.IndividualID == individualId &&
        //            job.OrganisationID == organisationId &&
        //            jobPosition.JobPositionStateId != approvedStateId

        //        select new JobPositionHistoryDto
        //        {
        //            JobPositionId =
        //                jobPosition.JobPositionId,

        //            JobId =
        //                job.JobId,

        //            PositionId =
        //                jobPosition.PositionId,

        //            PositionName =
        //                position.Name ?? string.Empty,

        //            OrganisationId =
        //                job.OrganisationID,

        //            OrganisationName =
        //                organisation.OrganisationName ??
        //                string.Empty,

        //            OrganisationStructureId =
        //                job.OrganisationStructureId,

        //            OrganisationStructureName =
        //                structure != null
        //                    ? structure.Name ?? string.Empty
        //                    : string.Empty,

        //            JobPositionStateId =
        //                jobPosition.JobPositionStateId,

        //            JobPositionStateName =
        //                state != null
        //                    ? state.StateName ?? string.Empty
        //                    : string.Empty,

        //            FromDate =
        //                jobPosition.FromDate,

        //            ToDate =
        //                jobPosition.ToDate,

        //            IsCurrent =
        //                jobPosition.ToDate == null
        //        };

        //    var rawHistory =
        //        await query.ToListAsync(
        //            cancellationToken);

        //    /*
        //     * Protect against duplicate results caused by joins.
        //     * Each JobPositionID should appear only once.
        //     */
        //    var history =
        //        rawHistory
        //            .GroupBy(item =>
        //                item.JobPositionId)
        //            .Select(group =>
        //                group.First())
        //            .OrderByDescending(item =>
        //                item.ToDate ?? DateTime.MaxValue)
        //            .ThenByDescending(item =>
        //                item.FromDate)
        //            .ThenByDescending(item =>
        //                item.JobPositionId)
        //            .ToList();

        //    foreach (var item in history)
        //    {
        //        item.EffectivePeriodText =
        //            BuildEffectivePeriodText(
        //                item.FromDate,
        //                item.ToDate,
        //                item.JobPositionStateName);
        //    }

        //    return history;
        //}


        //private static string BuildEffectivePeriodText(
        //    DateTime fromDate,
        //    DateTime? toDate,
        //    string? stateName)
        //{
        //    if (toDate.HasValue)
        //    {
        //        return
        //            $"{fromDate:dd MMM yyyy} – " +
        //            $"{toDate.Value:dd MMM yyyy}";
        //    }

        //    if (!string.IsNullOrWhiteSpace(stateName))
        //    {
        //        return
        //            $"{fromDate:dd MMM yyyy} – " +
        //            stateName;
        //    }

        //    return $"{fromDate:dd MMM yyyy} – Ongoing";
        //}


    }
}
