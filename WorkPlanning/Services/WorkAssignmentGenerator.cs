using Azure.Core;
using HRM.Components.Shared;
using HRM.DTOs.WorkPlanning;
using HRM.Enum;
using HRM.Models;
using HRM.Models.WorkPlanning;
using HRM.WorkPlanning.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HRM.Services.WorkPlanning;

public  class WorkAssignmentGeneratorService : IWorkAssignmentGenerator
{
    private const int AssignedWorkAssignmentStateId = 1;

    /*
     * Temporary default because JobWorkTemplateAssignments does not
     * currently contain a scheduled start-time column.
     *
     * Later, this can be replaced by:
     *
     * JobWorkTemplateAssignment.ScheduledStartTime
     */
    private static readonly TimeOnly DefaultScheduledStartTime =
        new(8, 0);

    private readonly IDbContextFactory<HrmTeContext> _dbFactory;
    private readonly ILogger<WorkAssignmentGeneratorService> _logger;

    public WorkAssignmentGeneratorService(
        IDbContextFactory<HrmTeContext> dbFactory,
        ILogger<WorkAssignmentGeneratorService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<GeneratedWorkPlanResult> GenerateAsync(
        GenerateWorkPlanRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Failure(
                "The work-plan generation request is required.");
        }

        var requestValidationMessage =
            ValidateRequest(request);

        if (requestValidationMessage is not null)
        {
            return Failure(requestValidationMessage);
        }

        await using var db =
            await _dbFactory.CreateDbContextAsync(
                cancellationToken);

        try
        {
            /*
             * Load the template and all active template segments
             * using a non-tracked projection.
             */
            var template = await LoadTemplateAsync(
                db,
                request.WorkTemplateId,
                cancellationToken);

            if (template is null)
            {
                return Failure(
                    $"Work template ID {request.WorkTemplateId} " +
                    "was not found or is inactive.");
            }

            if (template.Segments.Count == 0)
            {
                return Failure(
                    $"Work template '{template.Name}' does not " +
                    "contain any active segments.");
            }

            var segmentValidationMessage =
                ValidateTemplateSegments(
                    template.Segments);

            if (segmentValidationMessage is not null)
            {
                return Failure(segmentValidationMessage);
            }

            /*
             * Validate the employee, job and organisation using
             * one database query.
             */
            var job = await LoadValidJobAsync(
                db,
                request,
                cancellationToken);

            if (job is null)
            {
                return Failure(
                    "The selected job does not belong to the " +
                    "selected employee and organisation.");
            }

            if (job.JobStateId !=
                SharedConfig.JobStates.APPROVED)
            {
                return Failure(
                    "The selected job is not approved.");
            }

            if (job.TerminatedDate.HasValue)
            {
                return Failure(
                    "An assignment cannot be generated for a " +
                    "terminated job.");
            }

            /*
             * Validate that the template is assigned to the selected
             * job for the requested work date.
             */
            var jobTemplateAssignment =
                await ResolveJobTemplateAssignmentAsync(
                    db,
                    request,
                    cancellationToken);

            if (jobTemplateAssignment is null)
            {
                return Failure(
                    "The selected work template is not actively " +
                    "assigned to this job for the requested date.");
            }

            var planningProvider =
                await ResolvePlanningProviderAsync(
                    db,
                    request,
                    cancellationToken);

            if (planningProvider is null)
            {
                return Failure(
                    "No active planning provider is configured " +
                    "for the selected organisation.");
            }

            /*
             * Only one active work plan should exist for the same:
             *
             * Employee + Job + Organisation + Date
             *
             * WorkTemplateId must not be included because otherwise
             * different templates could create duplicate assignments.
             */
            var existingPlan =
                await FindExistingActivePlanAsync(
                    db,
                    request,
                    cancellationToken);

            if (existingPlan is not null)
            {
                var state =
                    existingPlan.IsFinalized
                        ? "finalized"
                        : "active";

                return Failure(
                    $"A {state} work assignment already exists " +
                    $"for this employee on " +
                    $"{request.WorkDate:yyyy-MM-dd}.",
                    existingPlan.WorkPlanId);
            }

            /*
             * Version is calculated from all previous versions,
             * including invalidated versions.
             */
            var nextVersion =
                await GetNextVersionAsync(
                    db,
                    request,
                    cancellationToken);

            /*
             * The current database has no scheduled-start-time field.
             * Therefore 08:00 is used as the assignment base time.
             */
            var scheduledStartTime =
                DefaultScheduledStartTime;

            var assignmentPeriod =
                CalculateAssignmentPeriod(
                    DateOnly.FromDateTime(
                        request.WorkDate),
                    scheduledStartTime,
                    template.Segments);

            if (!assignmentPeriod.Success)
            {
                return Failure(
                    assignmentPeriod.Message);
            }

            /*
             * Start the transaction only after all read-only validation
             * has completed.
             */
            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                var result =
                    await CreateWorkPlanAndAssignmentAsync(
                        db,
                        request,
                        template,
                        planningProvider,
                        assignmentPeriod,
                        nextVersion,
                        cancellationToken);

                await transaction.CommitAsync(
                    cancellationToken);

                _logger.LogInformation(
                    """
                    Work assignment generated successfully.
                    WorkPlanId: {WorkPlanId}
                    WorkAssignmentId: {WorkAssignmentId}
                    TemplateId: {TemplateId}
                    IndividualId: {IndividualId}
                    JobId: {JobId}
                    WorkDate: {WorkDate}
                    Version: {Version}
                    GenerationSource: {GenerationSource}
                    """,
                    result.WorkPlanId,
                    result.WorkAssignmentId,
                    request.WorkTemplateId,
                    request.IndividualId,
                    request.JobId,
                    request.WorkDate.Date,
                    result.Version,
                    request.GenerationSource);

                return result;
            }
            catch (OperationCanceledException)
            {
                await RollbackSafelyAsync(
                    transaction);

                throw;
            }
            catch (DbUpdateException exception)
            {
                await RollbackSafelyAsync(
                    transaction);
                var databaseError =
                    exception.InnerException?.Message
                    ?? exception.Message;


                _logger.LogError(
                    exception,
                    """
                    A database error occurred while generating
                    a work assignment.
                    TemplateId: {TemplateId}
                    IndividualId: {IndividualId}
                    JobId: {JobId}
                    WorkDate: {WorkDate}
                    """,
                    request.WorkTemplateId,
                    request.IndividualId,
                    request.JobId,
                    request.WorkDate);

                //return Failure(
                //    "The work assignment could not be saved. " +
                //    "It may already exist or contain invalid data.");


                return GeneratedWorkPlanResult.Failure(
                     "The work assignment could not be saved. " +
                     databaseError);



            }
            catch (Exception exception)
            {
                await RollbackSafelyAsync(
                    transaction);

                _logger.LogError(
                    exception,
                    """
                    An unexpected error occurred while generating
                    a work assignment.
                    TemplateId: {TemplateId}
                    IndividualId: {IndividualId}
                    JobId: {JobId}
                    WorkDate: {WorkDate}
                    """,
                    request.WorkTemplateId,
                    request.IndividualId,
                    request.JobId,
                    request.WorkDate);

                return Failure(
                    "An unexpected error occurred while generating " +
                    "the work assignment.");
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                """
                Work-assignment validation failed unexpectedly.
                TemplateId: {TemplateId}
                IndividualId: {IndividualId}
                JobId: {JobId}
                WorkDate: {WorkDate}
                """,
                request.WorkTemplateId,
                request.IndividualId,
                request.JobId,
                request.WorkDate);

            return Failure(
                "The work-assignment request could not be processed.");
        }
    }

    private static async Task<GeneratedWorkPlanResult>
        CreateWorkPlanAndAssignmentAsync(
            HrmTeContext db,
            GenerateWorkPlanRequest request,
            TemplateGenerationModel template,
            PlanningProvider planningProvider,
            DateTimeCalculationResult assignmentPeriod,
            int nextVersion,
            CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var workPlan = new WorkPlan
        {
            IndividualId =
                request.IndividualId,

            JobId =
                request.JobId,

            OrganisationBusinessEntityId =
                request.OrganisationBusinessEntityId,

            PlanningProviderId =
                planningProvider.PlanningProviderId,

            WorkTemplateId =
                request.WorkTemplateId,

            WorkDate =
                request.WorkDate.Date,

            GenerationSource =
                request.GenerationSource,

            GeneratedDate =
                now,

            GeneratedByUserId =
                request.GeneratedByUserId,

            IsGenerated =
                true,

            IsManual =
                request.GenerationSource ==
                WorkPlanGenerationSource.Manual,

            IsFinalized =
                false,

            FinalizedDate =
                null,

            FinalizedByUserId =
                null,

            Remarks =
                NullIfWhiteSpace(
                    request.Remarks),

            IsValid =
                true,

            OperationLogId =
                request.OperationLogId,

            CreatedDate =
                now,

            PlanGuid =
                Guid.NewGuid(),

            Version =
                nextVersion
        };

        db.WorkPlans.Add(workPlan);

        /*
         * WorkPlanId is database-generated, so save the WorkPlan first.
         */
        await db.SaveChangesAsync(
            cancellationToken);

        var workAssignment = new WorkAssignment
        {
            WorkPlanId =
                workPlan.WorkPlanId,

            WorkTemplateId =
                template.WorkTemplateId,

            WorkTemplateTypeId =
                template.WorkTemplateTypeId,

            WorkAssignmentStateId =
                AssignedWorkAssignmentStateId,

            Name =
                string.IsNullOrWhiteSpace(
                    request.AssignmentTitle)
                    ? template.Name
                    : request.AssignmentTitle.Trim(),

            Code =
                null,

            Description =
                string.IsNullOrWhiteSpace(
                    request.AssignmentDescription)
                    ? template.Description
                    : request.AssignmentDescription.Trim(),

            StartDateTime =
                assignmentPeriod.StartDateTime,

            EndDateTime =
                assignmentPeriod.EndDateTime,

            GraceMinutes =
                0,

            RequiresAttendance =
                request.RequiresAttendance,

            RequiresCheckOut =
                request.RequiresCheckout,

            Priority =
                request.Priority,

            AssignmentSource =
                request.AssignmentSource,

            SourceReferenceType =
                null,

            SourceReferenceId =
                null,

            LocationName =
                null,

            Latitude =
                null,

            Longitude =
                null,

            AllowedRadiusMeters =
                null,

            CancelledDate =
                null,

            CancellationReason =
                null,

            CancelledByUserId =
                null,

            IsValid =
                true,

            OperationLogId =
                request.OperationLogId,

            CreatedDate =
                now,

            

            CreatedByUserId = request.GeneratedByUserId,
            
        };

        db.WorkAssignments.Add(
            workAssignment);

        /*
         * WorkAssignmentId is database-generated.
         */
        await db.SaveChangesAsync(
            cancellationToken);

        var assignmentSegments =
            BuildAssignmentSegments(
                request,
                template,
                workAssignment,
                assignmentPeriod.AssignmentBaseDateTime);




         db.WorkAssignmentSegments.AddRange(assignmentSegments);

        //await db.SaveChangesAsync();


        var owner = new WorkAssignmentOwner
        {
            //WorkAssignmentId =
            //    workAssignment.WorkAssignmentId,

            WorkAssignment = workAssignment,

            IndividualId =
                request.IndividualId,

            JobId =
                request.JobId,

            OwnershipType =
                WorkOwnershipType.Original,

            AssignedDate =
                now,

            AssignedByUserId =
                request.GeneratedByUserId,

            EffectiveFrom =
                assignmentPeriod.StartDateTime,

            EffectiveTo = null,

            RelievedDate = null,

            RelievedByUserId = null,

            ReliefReason = null,

            IsCurrentOwner = true,

            IsValid = true,

            OperationLogId =
                request.OperationLogId
        };

        db.WorkAssignmentOwners.Add(owner);
  

        await db.SaveChangesAsync(cancellationToken);

        return new GeneratedWorkPlanResult
        {
            Success = true,

            Message =
                $"Work assignment '{workAssignment.Name}' " +
                "was generated successfully.",

            WorkPlanId =
                workPlan.WorkPlanId,

            WorkAssignmentId =
                workAssignment.WorkAssignmentId,

            Version =
                workPlan.Version
        };
    }

    private static List<WorkAssignmentSegment>
        BuildAssignmentSegments(
            GenerateWorkPlanRequest request,
            TemplateGenerationModel template,
            WorkAssignment workAssignment,
            DateTime assignmentBaseDateTime)
    {
        var assignmentSegments =
            new List<WorkAssignmentSegment>();

        foreach (var templateSegment in
                 template.Segments.OrderBy(
                     x => x.SequenceNumber))
        {
            var segmentPeriod =
                CalculateSegmentPeriod(
                    assignmentBaseDateTime,
                    templateSegment);

            if (!segmentPeriod.Success)
            {
                throw new InvalidOperationException(
                    $"Unable to create segment " +
                    $"'{templateSegment.Name}'. " +
                    segmentPeriod.Message);
            }

            var assignmentSegment =
                new WorkAssignmentSegment
                {
                    WorkAssignment = workAssignment,

                    //WorkAssignmentId =
                    //    workAssignment.WorkAssignmentId,

                    WorkTemplateSegmentId =
                        templateSegment
                            .WorkTemplateSegmentId,

                    WorkSegmentTypeId =
                        templateSegment
                            .WorkSegmentTypeId,

                    Name =
                        templateSegment.Name,

                    Description =
                        templateSegment.Description,

                    SequenceNumber =
                        templateSegment.SequenceNumber,

                    StartDateTime =
                        segmentPeriod.StartDateTime,

                    EndDateTime =
                        segmentPeriod.EndDateTime,

                    GraceBeforeMinutes =
                        templateSegment
                            .GraceBeforeMinutes,

                    GraceAfterMinutes =
                        templateSegment
                            .GraceAfterMinutes,

                    IsMandatory =
                        templateSegment.IsMandatory,

                    RequiresAttendance =
                        templateSegment
                            .RequiresAttendance,

                    RequiresLocationValidation =
                        templateSegment
                            .RequiresLocationValidation,

                    RequiresDeviceValidation =
                        templateSegment
                            .RequiresDeviceValidation,



                    IsValid =
                        true,

                    OperationLogId =
                        request.OperationLogId
                };

            assignmentSegments.Add(
                assignmentSegment);
        }

        return assignmentSegments;
    }

    private static async Task<TemplateGenerationModel?>
        LoadTemplateAsync(
            HrmTeContext db,
            int workTemplateId,
            CancellationToken cancellationToken)
    {
        return await db.WorkTemplates
            .AsNoTracking()
            .Where(x =>
                x.WorkTemplateId == workTemplateId &&
                x.IsActive)
            .Select(x => new TemplateGenerationModel
            {
                WorkTemplateId =
                    x.WorkTemplateId,

                WorkTemplateTypeId =
                    x.WorkTemplateTypeId,

                Name =
                    x.Name,

                Description =
                    x.Description,

                Segments = x.WorkTemplateSegments
                    .Where(segment =>
                        segment.IsActive)
                    .OrderBy(segment =>
                        segment.SequenceNumber)
                    .Select(segment =>
                        new TemplateSegmentGenerationModel
                        {
                            WorkTemplateSegmentId = segment.WorkTemplateSegmentId,

                            WorkSegmentTypeId = segment.WorkSegmentTypeId,

                            Name = segment.Name,

                            Description = segment.Description,

                            SequenceNumber = segment.SequenceNumber ,

                            OffsetMinutes = segment.OffsetMinutes ?? 0,

                            DurationMinutes = segment.DurationMinutes ?? 0,

                            GraceBeforeMinutes = segment.GraceBeforeMinutes ?? 0,

                            GraceAfterMinutes = segment.GraceAfterMinutes ?? 0,

                            IsMandatory = segment.IsMandatory,

                            RequiresAttendance = segment.RequiresAttendance,

                            RequiresLocationValidation = segment.RequiresLocationValidation,

                            RequiresDeviceValidation =segment.RequiresDeviceValidation
                        })
                    .ToList()
            })
            .SingleOrDefaultAsync(
                cancellationToken);
    }

    private static async Task<JobGenerationModel?>
        LoadValidJobAsync(
            HrmTeContext db,
            GenerateWorkPlanRequest request,
            CancellationToken cancellationToken)
    {
        return await db.Jobs
            .AsNoTracking()
            .Where(x =>
                x.JobId == request.JobId &&
                x.IndividualID ==
                    request.IndividualId &&
                x.OrganisationID ==
                    request.OrganisationBusinessEntityId)
            .Select(x => new JobGenerationModel
            {
                JobId =
                    x.JobId,

                IndividualId =
                    x.IndividualID,

                OrganisationId =
                    x.OrganisationID,

                JobStateId =
                    x.JobStateId,

                TerminatedDate =
                    x.TerminatedDate
            })
            .SingleOrDefaultAsync(
                cancellationToken);
    }

    private static async Task<JobTemplateAssignmentModel?>ResolveJobTemplateAssignmentAsync(
            HrmTeContext db,
            GenerateWorkPlanRequest request,
            CancellationToken cancellationToken)
    {
        //var workDate = request.WorkDate.Date;

        DateOnly workDate = DateOnly.FromDateTime(request.WorkDate.Date);

        return await db.JobWorkTemplateAssignments
            .AsNoTracking()
            .Where(x =>
                x.JobID == request.JobId &&
                x.WorkTemplateID ==
                    request.WorkTemplateId &&
                x.IsActive &&
                x.EffectiveFrom <= request.WorkDate.Date &&
                (
                    x.EffectiveTo == null ||
                    x.EffectiveTo >= request.WorkDate.Date
                ))
            .OrderByDescending(x =>
                x.EffectiveFrom)
            .Select(x => new JobTemplateAssignmentModel
            {
                JobWorkTemplateAssignmentId =
                    x.JobWorkTemplateAssignmentID,

                JobId =
                    x.JobID,

                WorkTemplateId =
                    x.WorkTemplateID,

                EffectiveFrom = x.EffectiveFrom,

                EffectiveTo = x.EffectiveTo.HasValue ? x.EffectiveTo.Value : null
            })
            .FirstOrDefaultAsync(cancellationToken);
    }

   

    private static async Task<PlanningProvider?>ResolvePlanningProviderAsync(
        HrmTeContext db,
        GenerateWorkPlanRequest request,
        CancellationToken cancellationToken)
    {
        var effectiveDate = request.WorkDate;

        var configuredProviderId =
            await db.OrganisationWorkPlanningSettings
                .AsNoTracking()
                .Where(x =>
                    x.OrganisationBusinessEntityId == request.OrganisationBusinessEntityId &&
                    x.IsActive &&
                    (!x.EffectiveFrom.HasValue ||
                     x.EffectiveFrom.Value.Date <=
                        effectiveDate) &&
                    (!x.EffectiveTo.HasValue ||
                     x.EffectiveTo.Value.Date >=
                        effectiveDate))
                .OrderByDescending(x =>
                    x.EffectiveFrom)
                .ThenByDescending(x =>
                    x.OrganisationWorkPlanningSettingId)
                .Select(x =>
                    (int?)x.PlanningProviderId)
                .FirstOrDefaultAsync(
                    cancellationToken);

        if (!configuredProviderId.HasValue)
        {
            return null;
        }

        return await db.PlanningProviders
            .AsNoTracking()
            .SingleOrDefaultAsync(
                x =>
                    x.PlanningProviderId ==
                        configuredProviderId.Value &&
                    x.OrganisationBusinessEntityID == request.OrganisationBusinessEntityId &&
                    x.IsActive,
                cancellationToken);
    }

    private static async Task<ExistingPlanModel?>FindExistingActivePlanAsync(
            HrmTeContext db,
            GenerateWorkPlanRequest request,
            CancellationToken cancellationToken)
    {
        var workDate =
            request.WorkDate.Date;

        return await db.WorkPlans
            .AsNoTracking()
            .Where(x =>
                x.IndividualId ==
                    request.IndividualId &&
                x.JobId ==
                    request.JobId &&
                x.OrganisationBusinessEntityId ==
                    request
                        .OrganisationBusinessEntityId &&
                x.WorkDate == workDate &&
                x.IsValid)
            .Select(x => new ExistingPlanModel
            {
                WorkPlanId =
                    x.WorkPlanId,

                Version =
                    x.Version,

                IsFinalized =
                    x.IsFinalized
            })
            .SingleOrDefaultAsync(
                cancellationToken);
    }

    private static async Task<int>GetNextVersionAsync(
            HrmTeContext db,
            GenerateWorkPlanRequest request,
            CancellationToken cancellationToken)
    {
        var workDate =
            request.WorkDate.Date;

        var latestVersion =
            await db.WorkPlans
                .AsNoTracking()
                .Where(x =>
                    x.IndividualId ==
                        request.IndividualId &&
                    x.JobId ==
                        request.JobId &&
                    x.OrganisationBusinessEntityId ==
                        request
                            .OrganisationBusinessEntityId &&
                    x.WorkDate == workDate)
                .MaxAsync(
                    x => (int?)x.Version,
                    cancellationToken)
            ?? 0;

        return latestVersion + 1;
    }

    private static string? ValidateRequest(GenerateWorkPlanRequest request)
    {
        if (request.WorkTemplateId <= 0)
        {
            return "A valid work template is required.";
        }

        if (request.OrganisationBusinessEntityId <= 0)
        {
            return "A valid organisation is required.";
        }

        if (request.IndividualId <= 0)
        {
            return "A valid individual is required.";
        }

        if (request.JobId <= 0)
        {
            return "A valid job is required.";
        }

        if (request.GeneratedByUserId <= 0)
        {
            return "The user generating the assignment is required.";
        }

        if (request.OperationLogId <= 0)
        {
            return "A valid operation log is required.";
        }

        if (request.WorkDate == default)
        {
            return "A valid work date is required.";
        }

        if (!System.Enum.IsDefined(
                typeof(WorkPlanGenerationSource),
                request.GenerationSource))
        {
            return "A valid generation source is required.";
        }

        if (request.Priority < 0)
        {
            return "Priority cannot be less than zero.";
        }

        return null;
    }

    private static string? ValidateTemplateSegments(IReadOnlyCollection<TemplateSegmentGenerationModel>
            segments)
    {
        var duplicateSequence =
            segments
                .GroupBy(x =>
                    x.SequenceNumber)
                .FirstOrDefault(group =>
                    group.Count() > 1);

        if (duplicateSequence is not null)
        {
            return
                $"The template contains duplicate sequence " +
                $"number {duplicateSequence.Key}.";
        }

        foreach (var segment in segments)
        {
            if (segment.SequenceNumber <= 0)
            {
                return
                    $"Segment '{segment.Name}' has an invalid " +
                    "sequence number.";
            }

            if (segment.OffsetMinutes < 0)
            {
                return
                    $"Segment '{segment.Name}' has a negative " +
                    "offset.";
            }

            if (segment.DurationMinutes < 0 )
            {
                //return
                //    $"Segment '{segment.Name}' must have a " +
                //    "duration greater than zero.";

                return
                    $"Segment '{segment.Name}' " +
                    $"(ID: {segment.WorkTemplateSegmentId}) " +
                    $"has DurationMinutes = " +
                    $"{segment.DurationMinutes.ToString() ?? "NULL"}";


            }

            if (segment.GraceBeforeMinutes < 0)
            {
                return
                    $"Segment '{segment.Name}' has invalid " +
                    "grace-before minutes.";
            }

            if (segment.GraceAfterMinutes < 0)
            {
                return
                    $"Segment '{segment.Name}' has invalid " +
                    "grace-after minutes.";
            }
        }

        return null;
    }

    private static DateTimeCalculationResult
        CalculateAssignmentPeriod(
            DateOnly workDate,
            TimeOnly scheduledStartTime,
            IReadOnlyCollection<TemplateSegmentGenerationModel>
                segments)
    {
        if (segments.Count == 0)
        {
            return DateTimeCalculationResult.Fail(
                "No template segments were provided.");
        }

        /*
         * This is an internal calculated value only.
         * It is not a database column.
         */
        var assignmentBaseDateTime =
            workDate.ToDateTime(
                scheduledStartTime);

        DateTime? earliestStart = null;
        DateTime? latestEnd = null;

        foreach (var segment in
                 segments.OrderBy(
                     x => x.SequenceNumber))
        {
            var segmentPeriod =
                CalculateSegmentPeriod(
                    assignmentBaseDateTime,
                    segment);

            if (!segmentPeriod.Success)
            {
                return DateTimeCalculationResult.Fail(
                    $"Segment '{segment.Name}' is invalid. " +
                    segmentPeriod.Message);
            }

            if (!earliestStart.HasValue ||
                segmentPeriod.StartDateTime <
                earliestStart.Value)
            {
                earliestStart =
                    segmentPeriod.StartDateTime;
            }

            if (!latestEnd.HasValue ||
                segmentPeriod.EndDateTime >
                latestEnd.Value)
            {
                latestEnd =
                    segmentPeriod.EndDateTime;
            }
        }

        if (!earliestStart.HasValue ||
            !latestEnd.HasValue)
        {
            return DateTimeCalculationResult.Fail(
                "The assignment period could not be calculated.");
        }

        if (latestEnd.Value <=
            earliestStart.Value)
        {
            return DateTimeCalculationResult.Fail(
                "The assignment end date and time must be " +
                "after its start date and time.");
        }

        return DateTimeCalculationResult.Ok(
            assignmentBaseDateTime,
            earliestStart.Value,
            latestEnd.Value);
    }

    private static DateTimeCalculationResult
        CalculateSegmentPeriod(
            DateTime assignmentBaseDateTime,
            TemplateSegmentGenerationModel segment)
    {
        if (segment.OffsetMinutes < 0)
        {
            return DateTimeCalculationResult.Fail(
                "The segment offset cannot be negative.");
        }

        if (segment.DurationMinutes < 0 )
        {
            return DateTimeCalculationResult.Fail(
                "The segment duration must be greater than zero.");
        }

        var startDateTime =
            assignmentBaseDateTime.AddMinutes(
                segment.OffsetMinutes);

        var endDateTime =
            startDateTime.AddMinutes(
                segment.DurationMinutes);

        return DateTimeCalculationResult.Ok(
            assignmentBaseDateTime,
            startDateTime,
            endDateTime);
    }

    private async Task RollbackSafelyAsync(
        IDbContextTransaction transaction)
    {
        try
        {
            await transaction.RollbackAsync(
                CancellationToken.None);
        }
        catch (Exception rollbackException)
        {
            _logger.LogError(
                rollbackException,
                "An error occurred while rolling back the " +
                "work-assignment transaction.");
        }
    }

    private static string? NullIfWhiteSpace(
        string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }

    private static GeneratedWorkPlanResult Failure(
        string message,
        long? workPlanId = null)
    {
        return new GeneratedWorkPlanResult
        {
            Success =
                false,

            Message =
                message,

            WorkPlanId =
                workPlanId ?? 0,

            WorkAssignmentId =
                0,

            Version =
                0
        };
    }

    private sealed class TemplateGenerationModel
    {
        public int WorkTemplateId { get; init; }

        public int WorkTemplateTypeId { get; init; }

        public string Name { get; init; } =
            string.Empty;

        public string? Description { get; init; }

        public List<TemplateSegmentGenerationModel>
            Segments
        { get; init; } = [];
    }

    private sealed class TemplateSegmentGenerationModel
    {
        public int WorkTemplateSegmentId { get; init; }

        public int WorkSegmentTypeId { get; init; }

        public string Name { get; init; } =
            string.Empty;

        public string? Description { get; init; }

        public int SequenceNumber { get; init; }

        public int OffsetMinutes { get; init; }

        public int DurationMinutes { get; init; }

        public int GraceBeforeMinutes { get; init; }

        public int GraceAfterMinutes { get; init; }

        public bool IsMandatory { get; init; }

        public bool RequiresAttendance { get; init; }

        public bool RequiresLocationValidation { get; init; }

        public bool RequiresDeviceValidation { get; init; }
    }

    private sealed class JobGenerationModel
    {
        public int JobId { get; init; }

        public int IndividualId { get; init; }

        public int OrganisationId { get; init; }

        public int JobStateId { get; init; }

        public DateTime? TerminatedDate { get; init; }
    }

    private sealed class JobTemplateAssignmentModel
    {
        public int JobWorkTemplateAssignmentId { get; init; }

        public int JobId { get; init; }

        public int WorkTemplateId { get; init; }

        public DateTime EffectiveFrom { get; init; }

        public DateTime? EffectiveTo { get; init; }
    }

    private sealed class ExistingPlanModel
    {
        public long WorkPlanId { get; init; }

        public int Version { get; init; }

        public bool IsFinalized { get; init; }
    }

    private sealed class DateTimeCalculationResult
    {
        public bool Success { get; private init; }

        public string Message { get; private init; } =
            string.Empty;

        public DateTime AssignmentBaseDateTime
        {
            get;
            private init;
        }

        public DateTime StartDateTime
        {
            get;
            private init;
        }

        public DateTime EndDateTime
        {
            get;
            private init;
        }

        public static DateTimeCalculationResult Ok(
            DateTime assignmentBaseDateTime,
            DateTime startDateTime,
            DateTime endDateTime)
        {
            return new DateTimeCalculationResult
            {
                Success =
                    true,

                AssignmentBaseDateTime =
                    assignmentBaseDateTime,

                StartDateTime =
                    startDateTime,

                EndDateTime =
                    endDateTime
            };
        }

        public static DateTimeCalculationResult Fail(
            string message)
        {
            return new DateTimeCalculationResult
            {
                Success =
                    false,

                Message =
                    message
            };
        }
    }
}