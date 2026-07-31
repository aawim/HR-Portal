using HRM.Components.Shared;
using HRM.DTOs.WorkPlanning;
using HRM.Models;
using HRM.WorkPlanning.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HRM.WorkPlanning.Services
{
    public class ManualWorkAssignmentLookupService : IManualWorkAssignmentLookupService
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;

        public ManualWorkAssignmentLookupService(
            IDbContextFactory<HrmTeContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<List<PlanningProviderLookupDto>> GetPlanningProvidersAsync(
        int organisationBusinessEntityId,
        CancellationToken cancellationToken = default)
        {
            if (organisationBusinessEntityId <= 0)
            {
                return new List<PlanningProviderLookupDto>();
            }

            await using var db =
                await _dbFactory.CreateDbContextAsync(cancellationToken);

            return await db.PlanningProviders
                .AsNoTracking()
                .Where(x =>
                    x.OrganisationBusinessEntityID ==
                        organisationBusinessEntityId &&
                    x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new PlanningProviderLookupDto
                {
                    PlanningProviderId = x.PlanningProviderId,
                    Name = x.Name,
                    Code = x.Code
                })
                .ToListAsync(cancellationToken);
        }
        public async Task<WorkAssignmentPreviewDto?>GetAssignmentPreviewAsync(
         int jobId,
         int workTemplateId,
         DateTime workDate,
         CancellationToken cancellationToken = default)
        {
            if (jobId <= 0 ||
                workTemplateId <= 0 ||
                workDate == default)
            {
                return null;
            }

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            var jobExists =
                await db.Jobs
                    .AsNoTracking()
                    .AnyAsync(job =>
                        job.JobId == jobId &&
                        job.JobStateId ==
                            SharedConfig.JobStates.APPROVED &&
                        job.TerminatedDate == null,
                        cancellationToken);

            if (!jobExists)
            {
                return null;
            }

            var template =
                await db.WorkTemplates
                    .AsNoTracking()
                    .Where(x =>
                        x.WorkTemplateId == workTemplateId &&
                        x.IsActive)
                    .Select(x => new
                    {
                        x.WorkTemplateId,
                        x.Name
                    })
                    .FirstOrDefaultAsync(cancellationToken);

            if (template is null)
            {
                return null;
            }

            var segments =
                await db.WorkTemplateSegments
                    .AsNoTracking()
                    .Where(x =>
                        x.WorkTemplateId == workTemplateId &&
                        x.IsActive)
                    .OrderBy(x =>
                        x.SequenceNumber)
                    .Select(x =>
                        new WorkTemplateSegmentGenerationDto
                        {
                            WorkTemplateSegmentId =
                                x.WorkTemplateSegmentId,

                            WorkTemplateId =
                                x.WorkTemplateId,

                            WorkSegmentTypeId =
                                x.WorkSegmentTypeId,

                            Name =
                                x.Name,

                            Description =
                                x.Description,

                            SequenceNumber =
                                x.SequenceNumber,

                            OffsetMinutes =
                                x.OffsetMinutes ?? 0,

                            DurationMinutes =
                                x.DurationMinutes ?? 0,

                            GraceBeforeMinutes =
                                x.GraceBeforeMinutes ?? 0,

                            GraceAfterMinutes =
                                x.GraceAfterMinutes ?? 0,

                            IsMandatory =
                                x.IsMandatory,

                            RequiresAttendance =
                                x.RequiresAttendance,

                            RequiresLocationValidation =
                                x.RequiresLocationValidation,

                            RequiresDeviceValidation =
                                x.RequiresDeviceValidation
                        })
                    .ToListAsync(cancellationToken);

            if (segments.Count == 0)
            {
                return null;
            }

            var invalidSegment =
                segments.FirstOrDefault(x =>
                    x.DurationMinutes <= 0 ||
                    x.OffsetMinutes < 0);

            if (invalidSegment is not null)
            {
                return null;
            }

            var scheduledStartTime =
                new TimeOnly(8, 0);

            var baseDateTime =
                workDate.Date.Add(
                    scheduledStartTime.ToTimeSpan());

            var previewSegments =
                segments
                    .Select(segment =>
                    {
                        var startDateTime =
                            baseDateTime.AddMinutes(
                                segment.OffsetMinutes);

                        var endDateTime =
                            startDateTime.AddMinutes(
                                segment.DurationMinutes);

                        return new WorkAssignmentSegmentPreviewDto
                        {
                            WorkTemplateSegmentId =
                                segment.WorkTemplateSegmentId,

                            Name =
                                segment.Name,

                            SequenceNumber =
                                segment.SequenceNumber,

                            StartDateTime =
                                startDateTime,

                            EndDateTime =
                                endDateTime,

                            IsMandatory =
                                segment.IsMandatory,

                            RequiresAttendance =
                                segment.RequiresAttendance,

                            RequiresLocationValidation =
                                segment.RequiresLocationValidation,

                            RequiresDeviceValidation =
                                segment.RequiresDeviceValidation
                        };
                    })
                    .OrderBy(x =>
                        x.SequenceNumber)
                    .ToList();

            return new WorkAssignmentPreviewDto
            {
                WorkTemplateId =
                    template.WorkTemplateId,

                TemplateName =
                    template.Name,

                AssignmentBaseDateTime =
                    baseDateTime,

                StartDateTime =
                    previewSegments.Min(x =>
                        x.StartDateTime),

                EndDateTime =
                    previewSegments.Max(x =>
                        x.EndDateTime),

                Segments =
                    previewSegments
            };
        }


        public async Task<IReadOnlyList<ManualAssignmentJobDto>>
    SearchActiveJobsAsync(
        string? searchText,
        int? organisationId,
        CancellationToken cancellationToken = default)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            var search =
                string.IsNullOrWhiteSpace(searchText)
                    ? null
                    : searchText.Trim();

            var query =
                from job in db.Jobs.AsNoTracking()

                join individual in db.Individuals.AsNoTracking()
                    on job.IndividualID equals
                    individual.BusinessEntityId

                join organisation in db.Organisations.AsNoTracking()
                    on job.OrganisationID equals
                    organisation.BusinessEntityID

                join staff in db.Staffs.AsNoTracking()
                    on individual.BusinessEntityId equals
                    staff.IndividualId
                    into staffGroup

                from staff in staffGroup.DefaultIfEmpty()

                let activePosition =
                    (
                        from jobPosition in db.JobPositions
                        join position in db.Positions
                            on jobPosition.PositionId equals
                            position.PositionId

                        where
                            jobPosition.JobId == job.JobId &&
                            jobPosition.ToDate == null

                        orderby jobPosition.JobPositionId descending

                        select position.Name
                    ).FirstOrDefault()

                where
                    job.JobStateId ==
                        SharedConfig.JobStates.APPROVED
                    &&
                    job.TerminatedDate == null

                select new
                {
                    JobId =
                        job.JobId,

                    IndividualId =
                        job.IndividualID,

                    OrganisationId =
                        job.OrganisationID,

                    FirstName =
                        individual.FirstNameEnglish ?? string.Empty,

                    MiddleName =
                        individual.MiddleNameEnglish ?? string.Empty,

                    LastName =
                        individual.LastNameEnglish ?? string.Empty,

                    EmployeeNumber =
                        staff != null
                            ? staff.EmployeeNumber
                            : null,

                    OrganisationName =
                        organisation.OrganisationName ??
                        string.Empty,

                    PositionName =
                        activePosition ?? "No active position"
                };

            if (organisationId.HasValue &&
                organisationId.Value > 0)
            {
                query =
                    query.Where(x =>
                        x.OrganisationId ==
                        organisationId.Value);
            }

            if (search is not null)
            {
                var pattern =
                    $"%{search}%";

                query =
                    query.Where(x =>
                        EF.Functions.Like(
                            x.FirstName,
                            pattern)
                        ||
                        EF.Functions.Like(
                            x.MiddleName,
                            pattern)
                        ||
                        EF.Functions.Like(
                            x.LastName,
                            pattern)
                        ||
                        EF.Functions.Like(
                            x.FirstName + " " +
                            x.MiddleName + " " +
                            x.LastName,
                            pattern)
                        ||
                        (
                            x.EmployeeNumber != null &&
                            EF.Functions.Like(
                                x.EmployeeNumber,
                                pattern)
                        )
                        ||
                        EF.Functions.Like(
                            x.PositionName,
                            pattern)
                        ||
                        EF.Functions.Like(
                            x.OrganisationName,
                            pattern));
            }

            return await query
                .OrderBy(x => x.FirstName)
                .ThenBy(x => x.MiddleName)
                .ThenBy(x => x.LastName)
                .Take(50)
                .Select(x =>
                    new ManualAssignmentJobDto
                    {
                        JobId =
                            x.JobId,

                        IndividualId =
                            x.IndividualId,

                        OrganisationId =
                            x.OrganisationId,

                        EmployeeName =
                            (
                                x.FirstName + " " +
                                x.MiddleName + " " +
                                x.LastName
                            )
                            .Replace("  ", " "),

                        EmployeeNumber =
                            x.EmployeeNumber,

                        PositionName =
                            x.PositionName,

                        OrganisationName =
                            x.OrganisationName
                    })
                .ToListAsync(cancellationToken);
        }


        public async Task<IReadOnlyList<AssignedWorkTemplateDto>>
       GetAvailableTemplatesAsync(
           int organisationId,
           DateTime workDate,
           CancellationToken cancellationToken = default)
        {
            if (organisationId <= 0)
            {
                return [];
            }

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            var selectedDate = workDate.Date;

            return await db.WorkTemplates
                .AsNoTracking()
                .Where(template =>
                    template.IsActive
                    &&
                    (
                        template.OrganisationBusinessEntityId == null
                        ||
                        template.OrganisationBusinessEntityId ==
                            organisationId
                    )
                    &&
                    (
                        template.EffectiveFrom == null
                        ||
                        template.EffectiveFrom.Value.Date <= selectedDate
                    )
                    &&
                    (
                        template.EffectiveTo == null
                        ||
                        template.EffectiveTo.Value.Date >= selectedDate
                    ))
                .OrderBy(template => template.Name)
                .Select(template =>
                    new AssignedWorkTemplateDto
                    {
                        JobWorkTemplateAssignmentId = 0,

                        WorkTemplateId =
                            template.WorkTemplateId,

                        TemplateName =
                            template.Name,

                        EffectiveFrom =
                            template.EffectiveFrom ??
                            DateTime.MinValue,

                        EffectiveTo =
                            template.EffectiveTo
                    })
                .ToListAsync(cancellationToken);
        }

        //public async Task<IReadOnlyList<AssignedWorkTemplateDto>>
        //GetTemplatesForJobAsync(
        //int jobId,
        //DateTime workDate,
        //CancellationToken cancellationToken = default)
        //{
        //    if (jobId <= 0)
        //    {
        //        return [];
        //    }

        //    await using var db =
        //        await _dbFactory.CreateDbContextAsync(
        //            cancellationToken);

        //    var selectedDate =
        //        workDate.Date;

        //    var templates =
        //        await
        //        (
        //            from assignment in
        //                db.JobWorkTemplateAssignments.AsNoTracking()

        //            join template in
        //                db.WorkTemplates.AsNoTracking()

        //                on assignment.WorkTemplateID equals
        //                template.WorkTemplateId

        //            where
        //                assignment.JobID == jobId
        //                &&
        //                assignment.IsActive
        //                &&
        //                assignment.EffectiveFrom.Date <=
        //                    selectedDate
        //                &&
        //                (
        //                    assignment.EffectiveTo == null
        //                    ||
        //                    assignment.EffectiveTo.Value.Date >=
        //                        selectedDate
        //                )
        //                &&
        //                template.IsActive

        //            orderby template.Name

        //            select new AssignedWorkTemplateDto
        //            {
        //                JobWorkTemplateAssignmentId =
        //                    assignment.JobWorkTemplateAssignmentID,

        //                WorkTemplateId =
        //                    assignment.WorkTemplateID,

        //                TemplateName =
        //                    template.Name,

        //                EffectiveFrom =
        //                    assignment.EffectiveFrom,

        //                EffectiveTo =
        //                    assignment.EffectiveTo
        //            }
        //        )
        //        .ToListAsync(cancellationToken);

        //    return templates;
        //}


    }
}
