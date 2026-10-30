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

                await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);


                try
                {

                    if (existingBoundaryResolution is not null)
                    {

                        var existingLogTime =
                            existingBoundaryResolution.AttendanceLog.Date;
                        // Duplicate punches must not move an on-time arrival later.
                        // Keep the first check-in and last check-out for this exact boundary.
                        var keepExisting = planResult.ClockType == AttendanceClockType.CheckIn
                            ? existingLogTime <= attendanceLog.Date
                            : existingLogTime >= attendanceLog.Date;
                        if (keepExisting)
                        {

                            var duplicateMessage =
                               $"Attendance event matched " +
                               $"'{planResult.SegmentName}' " +
                               $"{planResult.ClockType}, but attendance log " +
                               $"{existingBoundaryResolution.AttendanceLogId} " +
                               $"already records the {(planResult.ClockType == AttendanceClockType.CheckIn ? "earliest check-in" : "latest check-out")} for this boundary.";

                            var duplicateResolution =
                            CreateResolution(
                                attendanceLog,
                                AttendanceResolutionStatusIds.DuplicatePunch,
                                duplicateMessage,
                                workPlanId:
                                    planResult.WorkPlanId,
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



                            //var savedDuplicate =
                            //    await _resolutionRepository.AddAsync(
                            //        duplicateResolution,
                            //        cancellationToken);

                            //return AttendanceProcessingResult.Recorded(
                            //    savedDuplicate,
                            //    duplicateMessage);

                        }


                        //await _resolutionRepository
                        //    .MarkAsDuplicateAsync(
                        //        existingBoundaryResolution.AttendanceLogResolutionId,
                        //        $"Replaced by attendance log " +
                        //        $"{attendanceLog.AttendanceLogId}, which is closer " +
                        //        $"to the scheduled boundary.",
                        //        cancellationToken);

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
                            $"{(planResult.ClockType == AttendanceClockType.CheckIn ? "earlier check-in" : "later check-out")} for this boundary.";
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
                        workPlanSegmentId: planResult.WorkPlanSegmentId,
                        jobId: planResult.JobId,
                        clockType: planResult.ClockType);


                    context.AttendanceLogResolutions.Add(resolvedRecord);

                    // When official and duty boundaries coincide, record the duty
                    // clock as well (official is primary, duty is secondary).
                    if (planResult.SecondaryWorkPlanSegmentId.HasValue &&
                        planResult.SecondaryClockType != AttendanceClockType.Unresolved)
                    {
                        var secondaryMessage =
                            $"Attendance event resolved to " +
                            $"'{planResult.SecondarySegmentName}' as " +
                            $"{planResult.SecondaryClockType}.";
                        var secondaryRecord = CreateResolution(
                            attendanceLog,
                            AttendanceResolutionStatusIds.Resolved,
                            secondaryMessage,
                            workPlanId: planResult.WorkPlanId,
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
