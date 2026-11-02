using HRM.Constants;
using HRM.Models;
using HRM.Services.Attendance.Processing;
using HRM.Services.Attendance.Repositories;
using HRM.WorkPlanning.Abstractions;
using Microsoft.EntityFrameworkCore;
using HRM.Enum;
using HRM.Services.Attendance.AttendancePlan;
using Microsoft.Data.SqlClient;

namespace HRM.Services.Attendance.Abstraction.Services
{
    public sealed class AttendanceLogProcessor : IAttendanceLogProcessor
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;

        private readonly IAttendanceDuplicateValidator _duplicateValidator;
 
        private readonly IAttendanceLogResolutionRepository _resolutionRepository;

        private readonly ILogger<AttendanceLogProcessor> _logger;

        private readonly IAttendanceWorkPlanResolver _workPlanResolver;

        public AttendanceLogProcessor(
            IDbContextFactory<HrmTeContext> dbFactory,
            IAttendanceDuplicateValidator duplicateValidator,
 
            IAttendanceLogResolutionRepository resolutionRepository,
            ILogger<AttendanceLogProcessor> logger,
               IAttendanceWorkPlanResolver workPlanResolver


            )
        {
            _dbFactory = dbFactory;
            _duplicateValidator = duplicateValidator;
 
            _resolutionRepository = resolutionRepository;
            _logger = logger;
            _workPlanResolver = workPlanResolver;
        }

        

        public async Task<AttendanceProcessingResult> ProcessAsync(
            int attendanceLogId,
            CancellationToken cancellationToken = default)
        {


            //using var context = _dbFactory.CreateDbContext();

            await using var context = await _dbFactory.CreateDbContextAsync(cancellationToken);

            if (attendanceLogId <= 0)
            {
                return AttendanceProcessingResult.Failed(
                    attendanceLogId,
                    "Attendance log ID must be greater than zero.");
            }

            try
            {
        
                var duplicateResult =
                    await _duplicateValidator.ValidateAsync(
                        attendanceLogId,
                        cancellationToken);




                if (duplicateResult.IsDuplicate &&
                    duplicateResult.ExistingResolution is not null)
                {
                    return AttendanceProcessingResult.AlreadyExists(
                        duplicateResult.ExistingResolution);
                }
           

                var attendanceLog = await LoadAttendanceLogAsync(attendanceLogId,cancellationToken);

                if (attendanceLog is null)
                {
                    return AttendanceProcessingResult.Failed(
                        attendanceLogId,
                        $"Attendance log {attendanceLogId} was not found.");
                }

                if (attendanceLog.IndividualId <= 0)
                {
                    return await SaveInvalidResolutionAsync(
                        attendanceLog,
                        "The attendance log does not contain a valid individual.",
                        cancellationToken);
                }

                if (attendanceLog.Date == default)
                {
                    return await SaveInvalidResolutionAsync(
                        attendanceLog,
                        "The attendance log does not contain a valid date and time.",
                        cancellationToken);
                }



                var attendanceDate = attendanceLog.Date.Date;
                var nextDate = attendanceDate.AddDays(1);

                var jobId = await context.Jobs
                    .AsNoTracking()
                    .Where(x =>
                        x.IndividualID == attendanceLog.IndividualId &&
                        x.OrganisationID == attendanceLog.OrganisationId &&
                        x.OrganisationStructureId ==
                            attendanceLog.OrganisationStructureId &&

                        // Job started on or before attendance date
                        x.JoinedDate < nextDate &&

                        // Job was active on attendance date
                        (x.TerminatedDate == null ||
                         x.TerminatedDate >= attendanceDate))
                    .OrderByDescending(x => x.JoinedDate)
                    .Select(x => (int?)x.JobId)
                    .FirstOrDefaultAsync(cancellationToken);






                if (!jobId.HasValue)
                {
                    var resolution =
                        CreateResolution(
                            attendanceLog,
                            AttendanceResolutionStatusIds.Invalid,
                            "No active job was found for this attendance event.");

                    var savedResolution =
                        await _resolutionRepository.AddAsync(
                            resolution,
                            cancellationToken);

                    return AttendanceProcessingResult.Recorded(
                        savedResolution,
                        "No active job was found for this attendance event.");
                }


                var planResult =
                   await _workPlanResolver.ResolveAsync(
                       attendanceLog.IndividualId,
                       jobId.Value,
                       attendanceLog.OrganisationId,
                       attendanceLog.Date,
                       cancellationToken);



                if (!planResult.IsResolved)
                {

                    var resolutionStatusId =
                          planResult.State switch
                          {
                              AttendancePlanResolutionState.NoWorkPlan
                                  => AttendanceResolutionStatusIds.NoWorkPlan,

                              AttendancePlanResolutionState.NoSegment
                                  => AttendanceResolutionStatusIds.NoSegment,

                              AttendancePlanResolutionState.OutsideResolutionWindow
                                  => AttendanceResolutionStatusIds.OutsideResolutionWindow,

                              _ => AttendanceResolutionStatusIds.Invalid
                          };


                    var unresolvedResolution =
                   CreateResolution(
                       attendanceLog,
                       resolutionStatusId,
                       planResult.Message,
                       workPlanId: planResult.WorkPlanId,
                       jobId: planResult.JobId,
                       clockType: AttendanceClockType.Unresolved);

                                var savedResolution =
                                    await _resolutionRepository.AddAsync(
                                        unresolvedResolution,
                                        cancellationToken);

                                return AttendanceProcessingResult.Recorded(
                                    savedResolution,
                                    planResult.Message);
                }

                var existingBoundaryResolution =
                 await _resolutionRepository
                     .GetResolvedBoundaryAsync(
                         planResult.WorkPlanId!.Value,
                         planResult.WorkPlanSegmentId!.Value,
                         planResult.ClockType,
                         cancellationToken);
                var primaryLink = await ResolveAssignmentLinkAsync(
                    context,
                    planResult.WorkPlanId,
                    planResult.WorkPlanSegmentId,
                    cancellationToken);

                await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);


                try
                {

                    if (existingBoundaryResolution is not null)
                    {

                        var existingLogTime =
                            existingBoundaryResolution.AttendanceLog.Date;
                        // Duplicate punches must keep the clock event nearest the
                        // scheduled boundary. A later morning punch is a better
                        // check-in than an early swipe two hours before the shift.
                        // Check-outs still keep the latest punch for the boundary.
                        var boundaryTime =
                            planResult.BoundaryTime ?? attendanceLog.Date;
                        var keepExisting = planResult.ClockType == AttendanceClockType.CheckIn
                            ? Math.Abs((existingLogTime - boundaryTime).TotalMinutes) <=
                              Math.Abs((attendanceLog.Date - boundaryTime).TotalMinutes)
                            : existingLogTime >= attendanceLog.Date;
                        if (keepExisting)
                        {

                            var duplicateMessage =
                               $"Attendance event matched " +
                               $"'{planResult.SegmentName}' " +
                               $"{planResult.ClockType}, but attendance log " +
                               $"{existingBoundaryResolution.AttendanceLogId} " +
                               $"already records the {(planResult.ClockType == AttendanceClockType.CheckIn ? "closest check-in" : "latest check-out")} for this boundary.";

                            var duplicateResolution =
                            CreateResolution(
                                attendanceLog,
                                AttendanceResolutionStatusIds.DuplicatePunch,
                                duplicateMessage,
                                workPlanId:
                                    planResult.WorkPlanId,
                                workAssignmentId:
                                    primaryLink.WorkAssignmentId,
                                workAssignmentSegmentId:
                                    primaryLink.WorkAssignmentSegmentId,
                                workPlanSegmentId:
                                    planResult.WorkPlanSegmentId,
                                jobId:
                                    planResult.JobId,
                                clockType:
                                    planResult.ClockType);


                            context.AttendanceLogResolutions.Add(duplicateResolution);

                            await context.SaveChangesAsync(cancellationToken);

                            await transaction.CommitAsync(cancellationToken);

                            return AttendanceProcessingResult.Recorded(
                                duplicateResolution,
                                duplicateMessage);

                        }

                        var existingResolution =
           await context.AttendanceLogResolutions
               .FirstOrDefaultAsync(
                   x => x.AttendanceLogResolutionId ==
                        existingBoundaryResolution.AttendanceLogResolutionId,
                   cancellationToken);

                        if (existingResolution is null)
                        {
                            throw new InvalidOperationException(
                                "Existing boundary resolution could not be found.");
                        }

                        existingResolution.AttendanceResolutionStatusId =
                            AttendanceResolutionStatusIds.DuplicatePunch;

                        existingResolution.ResolutionMessage =
                            $"Replaced by attendance log " +
                            $"{attendanceLog.AttendanceLogId}, which records the " +
                            $"{(planResult.ClockType == AttendanceClockType.CheckIn ? "closer check-in" : "later check-out")} for this boundary.";
                    }


                    var resolvedMessage =
                    $"Attendance event resolved to " +
                    $"'{planResult.SegmentName}' as " +
                    $"{planResult.ClockType}.";

                    var resolvedRecord = CreateResolution(
                        attendanceLog,
                        AttendanceResolutionStatusIds.Resolved,
                        resolvedMessage,
                        workPlanId: planResult.WorkPlanId,
                        workAssignmentId: primaryLink.WorkAssignmentId,
                        workAssignmentSegmentId: primaryLink.WorkAssignmentSegmentId,
                        workPlanSegmentId: planResult.WorkPlanSegmentId,
                        jobId: planResult.JobId,
                        clockType: planResult.ClockType);


                    context.AttendanceLogResolutions.Add(resolvedRecord);

                    // When official and duty boundaries coincide, record the duty
                    // clock as well (official is primary, duty is secondary).
                    if (planResult.SecondaryWorkPlanSegmentId.HasValue &&
                        planResult.SecondaryClockType != AttendanceClockType.Unresolved)
                    {
                        var secondaryLink = await ResolveAssignmentLinkAsync(
                            context,
                            planResult.WorkPlanId,
                            planResult.SecondaryWorkPlanSegmentId,
                            cancellationToken);
                        var existingSecondaryResolution =
                            await _resolutionRepository.GetResolvedBoundaryAsync(
                                planResult.WorkPlanId!.Value,
                                planResult.SecondaryWorkPlanSegmentId.Value,
                                planResult.SecondaryClockType,
                                cancellationToken);
                        var boundaryTime = planResult.BoundaryTime ?? attendanceLog.Date;
                        var keepExistingSecondary = existingSecondaryResolution is not null &&
                            (planResult.SecondaryClockType == AttendanceClockType.CheckIn
                                ? Math.Abs((existingSecondaryResolution.AttendanceLog.Date - boundaryTime).TotalMinutes) <=
                                  Math.Abs((attendanceLog.Date - boundaryTime).TotalMinutes)
                                : existingSecondaryResolution.AttendanceLog.Date >= attendanceLog.Date);
                        if (existingSecondaryResolution is not null && !keepExistingSecondary)
                        {
                            var trackedSecondary = await context.AttendanceLogResolutions
                                .FirstOrDefaultAsync(
                                    x => x.AttendanceLogResolutionId == existingSecondaryResolution.AttendanceLogResolutionId,
                                    cancellationToken);
                            if (trackedSecondary is not null)
                            {
                                trackedSecondary.AttendanceResolutionStatusId = AttendanceResolutionStatusIds.DuplicatePunch;
                                trackedSecondary.ResolutionMessage =
                                    $"Replaced by attendance log {attendanceLog.AttendanceLogId}, which records the " +
                                    $"{(planResult.SecondaryClockType == AttendanceClockType.CheckIn ? "closer check-in" : "later check-out")} for this boundary.";
                            }
                        }
                        var secondaryStatusId = keepExistingSecondary
                            ? AttendanceResolutionStatusIds.DuplicatePunch
                            : AttendanceResolutionStatusIds.Resolved;
                        var secondaryMessage = keepExistingSecondary
                            ? $"Attendance event matched '{planResult.SecondarySegmentName}' {planResult.SecondaryClockType}, but attendance log {existingSecondaryResolution!.AttendanceLogId} already records the {(planResult.SecondaryClockType == AttendanceClockType.CheckIn ? "closest check-in" : "latest check-out")} for this boundary."
                            : $"Attendance event resolved to '{planResult.SecondarySegmentName}' as {planResult.SecondaryClockType}.";
                        var secondaryRecord = CreateResolution(
                            attendanceLog,
                            secondaryStatusId,
                            secondaryMessage,
                            workPlanId: planResult.WorkPlanId,
                            workAssignmentId: secondaryLink.WorkAssignmentId,
                            workAssignmentSegmentId: secondaryLink.WorkAssignmentSegmentId,
                            workPlanSegmentId: planResult.SecondaryWorkPlanSegmentId,
                            jobId: planResult.JobId,
                            clockType: planResult.SecondaryClockType);
                        context.AttendanceLogResolutions.Add(secondaryRecord);
                    }

                    // Save both the old resolution update and new resolution.
                    await context.SaveChangesAsync(cancellationToken);

                    // Commit only when everything succeeds.
                    await transaction.CommitAsync(cancellationToken);

                    return AttendanceProcessingResult.Completed(
                        resolvedRecord,
                        resolvedMessage);


                }
                catch
                {
                    await transaction.RollbackAsync(CancellationToken.None);
                    throw;

                }


               

                //var resolvedMessage =
                //    $"Attendance event resolved to " +
                //    $"'{planResult.SegmentName}' as " +
                //    $"{planResult.ClockType}.";




                //var resolvedRecord =
                //     CreateResolution(
                //         attendanceLog,
                //         AttendanceResolutionStatusIds.Resolved,
                //         resolvedMessage,
                //         workPlanId:
                //             planResult.WorkPlanId,
                //         workPlanSegmentId:
                //             planResult.WorkPlanSegmentId,
                //         jobId:
                //             planResult.JobId,
                //         clockType:
                //             planResult.ClockType);





                //var savedResolvedRecord =
                //await _resolutionRepository.AddAsync(
                //    resolvedRecord,
                //    cancellationToken);

                //            return AttendanceProcessingResult.Completed(
                //                savedResolvedRecord,
                //                resolvedMessage);

        
            }
            catch (DbUpdateException exception)
                 when (exception.InnerException is SqlException sqlException &&
                       (sqlException.Number == 2601 ||
                        sqlException.Number == 2627))
            {
                _logger.LogWarning(
                    exception,
                    "Attendance log {AttendanceLogId} could not be processed.",
                    attendanceLogId);

                /*
                 * This can happen when two requests attempt to process
                 * the same event at almost the same time.
                 */
                var existingResolution =
                    await _resolutionRepository
                        .GetByAttendanceLogIdAsync(
                            attendanceLogId,
                            cancellationToken);

                if (existingResolution is not null)
                {
                    return AttendanceProcessingResult.AlreadyExists(
                        existingResolution);
                }

                return AttendanceProcessingResult.Failed(
                    attendanceLogId,
                    exception.Message);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unexpected error while processing attendance log " +
                    "{AttendanceLogId}.",
                    attendanceLogId);


                var errorMessage = exception.ToString();


                return AttendanceProcessingResult.Failed(
               attendanceLogId,
               errorMessage);

                //return AttendanceProcessingResult.Failed(
                //    attendanceLogId,
                //    "An unexpected error occurred while processing the attendance log.");
            }
        }

        private async Task<AttendanceLog?> LoadAttendanceLogAsync(
            int attendanceLogId,
            CancellationToken cancellationToken)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            return await db.AttendanceLogs
                .AsNoTracking()
                .SingleOrDefaultAsync(
                    x => x.AttendanceLogId == attendanceLogId,
                    cancellationToken);
        }

        private async Task<AttendanceProcessingResult>SaveInvalidResolutionAsync(
                AttendanceLog attendanceLog,
                string message,
                CancellationToken cancellationToken)
        {
            var invalidResolution =
                CreateResolution(
                    attendanceLog,
                    AttendanceResolutionStatusIds.Invalid,
                    message);

            var savedResolution =
                await _resolutionRepository.AddAsync(
                    invalidResolution,
                    cancellationToken);

            return AttendanceProcessingResult.Recorded(
                savedResolution,
                message);
        }

        private static async Task<(long? WorkAssignmentId, long? WorkAssignmentSegmentId)> ResolveAssignmentLinkAsync(
            HrmTeContext db,
            long? workPlanId,
            int? workPlanSegmentId,
            CancellationToken cancellationToken)
        {
            if (!workPlanId.HasValue || !workPlanSegmentId.HasValue)
            {
                return (null, null);
            }

            var planSegment = await db.WorkPlanSegments
                .AsNoTracking()
                .Where(x => x.WorkPlanId == workPlanId.Value && x.WorkPlanSegmentId == workPlanSegmentId.Value && x.IsValid)
                .Select(x => new { x.SchedulePurpose, x.WorkTemplateSegmentId, x.WorkSegmentTypeId, x.StartDateTime })
                .SingleOrDefaultAsync(cancellationToken);

            if (planSegment is null)
            {
                return (null, null);
            }

            var match = await db.WorkAssignmentSegments
                .AsNoTracking()
                .Where(x =>
                    x.IsValid &&
                    x.WorkAssignment.IsValid &&
                    x.WorkAssignment.WorkPlanId == workPlanId.Value &&
                    x.WorkAssignment.SchedulePurpose == planSegment.SchedulePurpose &&
                    x.WorkTemplateSegmentId == planSegment.WorkTemplateSegmentId &&
                    x.WorkSegmentTypeId == planSegment.WorkSegmentTypeId)
                .OrderBy(x => x.StartDateTime == planSegment.StartDateTime ? 0 : 1)
                .Select(x => new { x.WorkAssignmentId, x.WorkAssignmentSegmentId })
                .FirstOrDefaultAsync(cancellationToken);

            return (match?.WorkAssignmentId, match?.WorkAssignmentSegmentId);
        }

        private static AttendanceLogResolution CreateResolution(
            AttendanceLog attendanceLog,
            int resolutionStatusId,
            string message,
            long? workPlanId = null,
            long? workAssignmentId = null,
            long? workAssignmentSegmentId = null,
            int? workPlanSegmentId = null,
            int? jobId = null,
            AttendanceClockType clockType = AttendanceClockType.Unresolved)
        {
            return new AttendanceLogResolution
            {
                AttendanceLogId =
                    attendanceLog.AttendanceLogId,

                WorkPlanId =
                    workPlanId,

                WorkAssignmentId =
                    workAssignmentId,

                WorkAssignmentSegmentId =
                    workAssignmentSegmentId,

                WorkPlanSegmentId =
                    workPlanSegmentId,

                JobId =
                    jobId,

                AttendanceResolutionStatusId =
                    resolutionStatusId,

                AttendanceClockTypeId =
                    (int)clockType,

                ResolutionDate =
                    DateTime.Now,

                ResolutionMessage =
                    message,

                IsValid =
                    true,

                CreatedDate =
                    DateTime.Now
            };
        }

    
    }
}
