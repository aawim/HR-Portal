using HRM.DTOs.WorkPlanning;
using HRM.Enum;
using HRM.Models;
using HRM.Models.WorkPlanning;
using HRM.WorkPlanning.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HRM.Services.WorkPlanning;

public sealed class WorkAssignmentGenerator : IWorkAssignmentGenerator
{
    private readonly IDbContextFactory<HrmTeContext> _dbFactory;
    private readonly ILogger<WorkAssignmentGenerator> _logger;

    public WorkAssignmentGenerator(
        IDbContextFactory<HrmTeContext> dbFactory,
        ILogger<WorkAssignmentGenerator> logger)
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

        var validationMessage = ValidateRequest(request);

        if (validationMessage is not null)
        {
            return Failure(validationMessage);
        }

        await using var db =
            await _dbFactory.CreateDbContextAsync(cancellationToken);

        await using var transaction =
            await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var template = await db.WorkTemplates
                .AsNoTracking()
                .Where(x =>
                    x.WorkTemplateId == request.WorkTemplateId &&
                    x.IsActive)
                .Select(x => new
                {
                    x.WorkTemplateSegments,
                    x.WorkTemplateId,
                    x.Name,
                    x.Description,
                    x.WorkTemplateTypeId
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (template is null)
            {
                return Failure(
                    $"Work template ID {request.WorkTemplateId} was not found or is inactive.");
            }

            var templateSegments = await db.WorkTemplateSegments
          .AsNoTracking()
          .Where(x =>
              x.WorkTemplateId == request.WorkTemplateId &&
              x.IsActive)
          .OrderBy(x => x.SequenceNumber)
          .ToListAsync(cancellationToken);

            if (templateSegments.Count == 0)
            {
                return Failure(
                    $"Work template '{template.Name}' does not contain any active segments.");
            }

            var individualExists = await db.Individuals
                .AsNoTracking()
                .AnyAsync(
                    x => x.BusinessEntityId == request.IndividualId,
                    cancellationToken);

            if (!individualExists)
            {
                return Failure(
                    $"Individual ID {request.IndividualId} was not found.");
            }

            var organisationExists = await db.Organisations
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.BusinessEntityID ==
                        request.OrganisationBusinessEntityId,
                    cancellationToken);

            if (!organisationExists)
            {
                return Failure(
                    $"Organisation ID {request.OrganisationBusinessEntityId} was not found.");
            }

            var jobExists = await db.Jobs
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.JobId == request.JobId &&
                        x.IndividualID == request.IndividualId &&
                        x.OrganisationID ==
                        request.OrganisationBusinessEntityId,
                    cancellationToken);

            if (!jobExists)
            {
                return Failure(
                    "The selected job does not belong to the selected employee and organisation.");
            }

            var planningProvider =
                await ResolvePlanningProviderAsync(
                    db,
                    request,
                    cancellationToken);

            if (planningProvider is null)
            {
                return Failure(
                    "No active planning provider was found for the organisation.");
            }

            var existingPlan = await db.WorkPlans
                .AsNoTracking()
                .Where(x =>
                    x.WorkTemplateId == request.WorkTemplateId &&
                    x.IndividualId == request.IndividualId &&
                    x.JobId == request.JobId &&
                    x.OrganisationBusinessEntityId ==
                    request.OrganisationBusinessEntityId &&
                    x.WorkDate == request.WorkDate &&
                    x.IsValid)
                .Select(x => new
                {
                    x.WorkPlanId
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (existingPlan is not null)
            {
                return Failure(
                    $"A work plan already exists for this employee on " +
                    $"{request.WorkDate:yyyy-MM-dd}.",
                    existingPlan.WorkPlanId);
            }

            //var assignmentPeriod =
            //    CalculateAssignmentPeriod(
            //        request.WorkDate,
            //        templateSegments);


            //var template = DateTimeCalculationResult
           

            var assignmentPeriod = CalculateAssignmentPeriod(
                    request.WorkDate,
                    new TimeOnly(8, 0),
                    template.WorkTemplateSegments
                        .Where(x => x.IsActive)
                        .ToList());

         

            if (!assignmentPeriod.Success)
            {
                return Failure(assignmentPeriod.Message);
            }

            var now = DateTime.Now;

            var workPlan = new WorkPlan
            {
                IndividualId = request.IndividualId,
                JobId = request.JobId,

                OrganisationBusinessEntityId =
                    request.OrganisationBusinessEntityId,

                PlanningProviderId =
                    planningProvider.PlanningProviderId,

                WorkTemplateId = request.WorkTemplateId,
                WorkDate = request.WorkDate,
                GenerationSource = request.GenerationSource,

                GeneratedDate = now,
                GeneratedByUserId = request.GeneratedByUserId,

                IsFinalized = false,
                FinalizedDate = null,
                FinalizedByUserId = null,

                Remarks = request.Remarks,
                IsValid = true,
                OperationLogId = request.OperationLogId,
                CreatedDate = now,

                PlanGuid = Guid.NewGuid(),
                Version = 1,
                IsGenerated = true,

                IsManual =
                    request.GenerationSource ==
                    WorkPlanGenerationSource.Manual
            };

            db.WorkPlans.Add(workPlan);


            //var plannedStateId = await db.WorkAssignmentStates
            //.AsNoTracking()
            //.Where(x => x.IsActive && x.Code == "PLANNED")
            //.Select(x => x.WorkAssignmentStateId)
            //.SingleOrDefaultAsync(cancellationToken);

            //if (plannedStateId == 0)
            //{
            //    return Failure("Work assignment state 'PLANNED' was not found.");
            //}

            await db.SaveChangesAsync(cancellationToken);

            var workAssignment = new WorkAssignment
            {
                WorkPlanId = workPlan.WorkPlanId,

                WorkTemplateId = request.WorkTemplateId,

                WorkTemplateTypeId = template.WorkTemplateTypeId,

                WorkAssignmentStateId = 1,

                Name = string.IsNullOrWhiteSpace(request.AssignmentTitle)
                   ? template.Name
                   : request.AssignmentTitle.Trim(),

                Code = null,

                Description = string.IsNullOrWhiteSpace(request.AssignmentDescription)
           ? template.Description
           : request.AssignmentDescription.Trim(),

                StartDateTime = assignmentPeriod.StartDateTime,

                EndDateTime = assignmentPeriod.EndDateTime,

                GraceMinutes = 0,

                RequiresAttendance = request.RequiresAttendance,

                RequiresCheckOut = request.RequiresCheckout,

                Priority = request.Priority,

                AssignmentSource = request.AssignmentSource,

                SourceReferenceType = null,

                SourceReferenceId = null,

                LocationName = null,

                Latitude = null,

                Longitude = null,

                AllowedRadiusMeters = null,

                CancelledDate = null,

                CancellationReason = null,

                CancelledByUserId = null,

                IsValid = true,

                OperationLogId = request.OperationLogId,

                CreatedDate = now,

                CreatedByUserId = request.GeneratedByUserId
            };

            db.WorkAssignments.Add(workAssignment);

            await db.SaveChangesAsync(cancellationToken);

            //db.WorkAssignments.Add(workAssignment);

            //await db.SaveChangesAsync(cancellationToken);

            foreach (var templateSegment in templateSegments)
            {
                var segmentPeriod =
                    CalculateSegmentPeriod(
                        workAssignment.StartDateTime,
                        templateSegment);

                if (!segmentPeriod.Success)
                {
                    return Failure(
                        $"Unable to create segment '{templateSegment.Name}'. " +
                        segmentPeriod.Message);
                }

                var assignmentSegment = new WorkAssignmentSegment
                {
                    WorkAssignment = workAssignment,

                    WorkTemplateSegmentId =
                        templateSegment.WorkTemplateSegmentId,

                    WorkSegmentTypeId =
                        templateSegment.WorkSegmentTypeId,

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
                        templateSegment.GraceBeforeMinutes,

                    GraceAfterMinutes =
                        templateSegment.GraceAfterMinutes,

                    IsMandatory =
                        templateSegment.IsMandatory,

                    RequiresAttendance =
                        templateSegment.RequiresAttendance,

                    RequiresLocationValidation =
                        templateSegment.RequiresLocationValidation,

                    RequiresDeviceValidation =
                        templateSegment.RequiresDeviceValidation,

                    IsValid = true,

                    OperationLogId =
                        request.OperationLogId
                };

                db.WorkAssignmentSegments.Add(assignmentSegment);
            }

            var owner = new WorkAssignmentOwner
            {
                WorkAssignmentId =
                    workAssignment.WorkAssignmentId,

                IndividualId = request.IndividualId,
                JobId = request.JobId,

                //AssignedDate = assignmentPeriod.StartDateTime.ToUniversalTime(),
                EffectiveFrom =
                    assignmentPeriod.StartDateTime,
                EffectiveTo = null,

                OwnershipType = WorkOwnershipType.Original,
                IsCurrentOwner = true,
                IsValid = true,
                OperationLogId = request.OperationLogId,
 
            };

            db.WorkAssignmentOwners.Add(owner);

            await db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new GeneratedWorkPlanResult
            {
                Success = true,

                Message =
                    $"Work plan '{template.Name}' was generated successfully.",

                WorkPlanId = workPlan.WorkPlanId,
 
            };
        }
        catch (OperationCanceledException)
        {
            if (transaction.GetDbTransaction().Connection is not null)
            {
                await transaction.RollbackAsync(
                    CancellationToken.None);
            }

            throw;
        }
        catch (Exception exception)
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
                    "An error occurred while rolling back the work-plan transaction.");
            }

            _logger.LogError(
                exception,
                """
                Failed to generate work plan.
                TemplateId: {TemplateId}
                IndividualId: {IndividualId}
                JobId: {JobId}
                WorkDate: {WorkDate}
                """,
                request.WorkTemplateId,
                request.IndividualId,
                request.JobId,
                request.WorkDate);

            var baseException =
                exception.GetBaseException();

            return Failure(
                $"{baseException.GetType().Name}: " +
                $"{baseException.Message}");
        }
    }

    private static string? ValidateRequest(
        GenerateWorkPlanRequest request)
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

        if (request.OperationLogId <= 0)
        {
            return "A valid operation log is required.";
        }

        if (request.WorkDate == default)
        {
            return "A valid work date is required.";
        }

        if (request.Priority < 0)
        {
            return "Priority cannot be less than zero.";
        }

        return null;
    }

    private static async Task<PlanningProvider?>
        ResolvePlanningProviderAsync(
            HrmTeContext db,
            GenerateWorkPlanRequest request,
            CancellationToken cancellationToken)
    {
        if (request.PlanningProviderId.HasValue &&
            request.PlanningProviderId.Value > 0)
        {
            return await db.PlanningProviders
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x =>
                        x.PlanningProviderId ==
                        request.PlanningProviderId.Value &&
                        x.IsActive,
                    cancellationToken);
        }

        var organisationProviderId =
            await db.OrganisationWorkPlanningSettings
                .AsNoTracking()
                .Where(x =>
                    x.OrganisationBusinessEntityId ==
                    request.OrganisationBusinessEntityId &&
                    x.IsActive)
                .Select(x => (int?)x.PlanningProviderId)
                .FirstOrDefaultAsync(cancellationToken);

        if (organisationProviderId.HasValue)
        {
            var organisationProvider =
                await db.PlanningProviders
                    .AsNoTracking()
                    .SingleOrDefaultAsync(
                        x =>
                            x.PlanningProviderId ==
                            organisationProviderId.Value &&
                            x.IsActive,
                        cancellationToken);

            if (organisationProvider is not null)
            {
                return organisationProvider;
            }
        }

        return await db.PlanningProviders
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.PlanningProviderId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    //private static DateTimeCalculationResult
    //    CalculateAssignmentPeriod(
    //        DateOnly workDate,
    //        IReadOnlyCollection<WorkTemplateSegment> segments)
    //{
    //    if (segments.Count == 0)
    //    {
    //        return DateTimeCalculationResult.Fail(
    //            "No template segments were provided.");
    //    }

    //    DateTime? earliestStart = null;
    //    DateTime? latestEnd = null;

    //    foreach (var segment in segments)
    //    {
    //        var segmentPeriod =
    //            CalculateSegmentPeriod(
    //                workDate,
    //                segment);

    //        if (!segmentPeriod.Success)
    //        {
    //            return DateTimeCalculationResult.Fail(
    //                $"Segment '{segment.Name}' is invalid. " +
    //                segmentPeriod.Message);
    //        }

    //        if (!earliestStart.HasValue ||
    //            segmentPeriod.StartDateTime <
    //            earliestStart.Value)
    //        {
    //            earliestStart =
    //                segmentPeriod.StartDateTime;
    //        }

    //        if (!latestEnd.HasValue ||
    //            segmentPeriod.EndDateTime >
    //            latestEnd.Value)
    //        {
    //            latestEnd =
    //                segmentPeriod.EndDateTime;
    //        }
    //    }

    //    if (!earliestStart.HasValue ||
    //     !latestEnd.HasValue)
    //    {
    //        return DateTimeCalculationResult.Fail(
    //            "The assignment period could not be calculated.");
    //    }

    //    if (latestEnd.Value < earliestStart.Value)
    //    {
    //        return DateTimeCalculationResult.Fail(
    //            "The assignment end date and time cannot be before its start date and time.");
    //    }

    //    return DateTimeCalculationResult.Ok(
    //        earliestStart.Value,
    //        latestEnd.Value);
    //}

    private static DateTimeCalculationResult CalculateAssignmentPeriod(
    DateOnly workDate,
    TimeOnly scheduledStartTime,
    IReadOnlyCollection<WorkTemplateSegment> segments)
    {
        if (segments == null || segments.Count == 0)
        {
            return DateTimeCalculationResult.Fail(
                "No template segments were provided.");
        }

        var assignmentBaseDateTime =
            workDate.ToDateTime(scheduledStartTime);

        DateTime? earliestStart = null;
        DateTime? latestEnd = null;

        foreach (var segment in segments
                     .Where(x => x.IsActive)
                     .OrderBy(x => x.SequenceNumber))
        {
            var segmentPeriod = CalculateSegmentPeriod(
                assignmentBaseDateTime,
                segment);

            if (!segmentPeriod.Success)
            {
                return DateTimeCalculationResult.Fail(
                    $"Segment '{segment.Name}' is invalid. " +
                    segmentPeriod.Message);
            }

            if (!earliestStart.HasValue ||
                segmentPeriod.StartDateTime < earliestStart.Value)
            {
                earliestStart = segmentPeriod.StartDateTime;
            }

            if (!latestEnd.HasValue ||
                segmentPeriod.EndDateTime > latestEnd.Value)
            {
                latestEnd = segmentPeriod.EndDateTime;
            }
        }

        if (!earliestStart.HasValue ||
            !latestEnd.HasValue)
        {
            return DateTimeCalculationResult.Fail(
                "The assignment period could not be calculated.");
        }

        if (latestEnd.Value < earliestStart.Value)
        {
            return DateTimeCalculationResult.Fail(
                "The assignment end date and time cannot be before its start date and time.");
        }

        return DateTimeCalculationResult.Ok(
            earliestStart.Value,
            latestEnd.Value);
    }



    //private static DateTimeCalculationResult CalculateSegmentPeriod(
    //DateTime assignmentStartDateTime,
    //DateTime assignmentEndDateTime,
    //WorkTemplateSegment segment)
    //{
    //    var startDateTime =
    //        assignmentStartDateTime.AddMinutes(segment.OffsetMinutes);

    //    /*
    //     * Duration can be null for point-in-time segments such as:
    //     * - Check In
    //     * - Check Out
    //     */
    //    if (!segment.DurationMinutes.HasValue)
    //    {
    //        return DateTimeCalculationResult.Ok(
    //            startDateTime,
    //            startDateTime);
    //    }

    //    if (segment.DurationMinutes.Value < 0)
    //    {
    //        return DateTimeCalculationResult.Fail(
    //            $"Segment '{segment.Name}' cannot have a negative duration.");
    //    }

    //    var endDateTime =
    //        startDateTime.AddMinutes(segment.DurationMinutes.Value);

    //    if (endDateTime > assignmentEndDateTime)
    //    {
    //        return DateTimeCalculationResult.Fail(
    //            $"Segment '{segment.Name}' ends after the assignment ends.");
    //    }

    //    return DateTimeCalculationResult.Ok(
    //        startDateTime,
    //        endDateTime);
    //}

    private static DateTimeCalculationResult CalculateSegmentPeriod(
    DateTime assignmentBaseDateTime,
    WorkTemplateSegment segment)
    {
        if (segment == null)
        {
            return DateTimeCalculationResult.Fail(
                "The template segment is required.");
        }

        if (segment.OffsetMinutes < 0)
        {
            return DateTimeCalculationResult.Fail(
                "The segment offset cannot be negative.");
        }

        if (segment.DurationMinutes.HasValue &&
            segment.DurationMinutes.Value < 0)
        {
            return DateTimeCalculationResult.Fail(
                "The segment duration cannot be negative.");
        }

        var segmentStartDateTime =
            assignmentBaseDateTime.AddMinutes(
                segment.OffsetMinutes);

        var segmentEndDateTime =
            segment.DurationMinutes.HasValue
                ? segmentStartDateTime.AddMinutes(
                    segment.DurationMinutes.Value)
                : segmentStartDateTime;

        return DateTimeCalculationResult.Ok(
            segmentStartDateTime,
            segmentEndDateTime);
    }

    //private static DateTimeCalculationResult CalculateSegmentPeriod(
    //DateOnly workDate,
    //WorkTemplateSegment segment)
    //{
    //    var date =
    //        workDate.ToDateTime(TimeOnly.MinValue);

    //    var startDateTime =
    //        date.AddMinutes(segment.OffsetMinutes);

    //    /*
    //     * DurationMinutes can be null or zero for point-in-time
    //     * segments such as Check In and Check Out.
    //     */
    //    var durationMinutes =
    //        segment.DurationMinutes ?? 0;

    //    if (durationMinutes < 0)
    //    {
    //        return DateTimeCalculationResult.Fail(
    //            $"Segment '{segment.Name}' cannot have a negative duration.");
    //    }

    //    var endDateTime =
    //        startDateTime.AddMinutes(durationMinutes);

    //    return DateTimeCalculationResult.Ok(
    //        startDateTime,
    //        endDateTime);
    //}

    private static GeneratedWorkPlanResult Failure(
        string message,
        long? workPlanId = null)
    {
        return new GeneratedWorkPlanResult
        {
            Success = false,
            Message = message,
            WorkPlanId = workPlanId ?? 0
        };
    }

    private sealed class DateTimeCalculationResult
    {
        public bool Success { get; private init; }

        public string Message { get; private init; }
            = string.Empty;

        public DateTime StartDateTime { get; private init; }

        public DateTime EndDateTime { get; private init; }

        public static DateTimeCalculationResult Ok(
            DateTime startDateTime,
            DateTime endDateTime)
        {
            return new DateTimeCalculationResult
            {
                Success = true,
                StartDateTime = startDateTime,
                EndDateTime = endDateTime
            };
        }

        public static DateTimeCalculationResult Fail(
            string message)
        {
            return new DateTimeCalculationResult
            {
                Success = false,
                Message = message
            };
        }
    }
}