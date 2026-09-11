using HRM.Constants;
using HRM.Models;
using HRM.Services.Attendance.Processing;
using HRM.Services.Attendance.Repositories;
using HRM.WorkPlanning.Abstractions;
using Microsoft.EntityFrameworkCore;
using HRM.Enum;
using HRM.Services.Attendance.AttendancePlan;

namespace HRM.Services.Attendance.Abstraction.Services
{
    public sealed class AttendanceLogProcessor : IAttendanceLogProcessor
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;

        private readonly IAttendanceDuplicateValidator _duplicateValidator;

        private readonly IWorkAssignmentResolver _workAssignmentResolver;

        private readonly IAttendanceLogResolutionRepository _resolutionRepository;

        private readonly ILogger<AttendanceLogProcessor> _logger;

        private readonly IAttendanceWorkPlanResolver _workPlanResolver;

        public AttendanceLogProcessor(
            IDbContextFactory<HrmTeContext> dbFactory,
            IAttendanceDuplicateValidator duplicateValidator,
            IWorkAssignmentResolver workAssignmentResolver,
            IAttendanceLogResolutionRepository resolutionRepository,
            ILogger<AttendanceLogProcessor> logger,
               IAttendanceWorkPlanResolver workPlanResolver


            )
        {
            _dbFactory = dbFactory;
            _duplicateValidator = duplicateValidator;
            _workAssignmentResolver = workAssignmentResolver;
            _resolutionRepository = resolutionRepository;
            _logger = logger;
            _workPlanResolver = workPlanResolver;
        }

        

        public async Task<AttendanceProcessingResult> ProcessAsync(
            int attendanceLogId,
            CancellationToken cancellationToken = default)
        {
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

                  var planResult =
                    await _workPlanResolver.ResolveAsync(
                        attendanceLog.IndividualId,
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





                if (existingBoundaryResolution is not null)
                {
                    var duplicateMessage =
                        $"Attendance event was resolved to " +
                        $"'{planResult.SegmentName}' as " +
                        $"{planResult.ClockType}, but that boundary " +
                        $"already has attendance log " +
                        $"{existingBoundaryResolution.AttendanceLogId}.";

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

                    var savedDuplicate =
                        await _resolutionRepository.AddAsync(
                            duplicateResolution,
                            cancellationToken);

                    return AttendanceProcessingResult.Recorded(
                        savedDuplicate,
                        duplicateMessage);



                }

                var resolvedMessage =
                $"Attendance event resolved to " +
                $"'{planResult.SegmentName}' as " +
                $"{planResult.ClockType}.";




                var resolvedRecord =
                     CreateResolution(
                         attendanceLog,
                         AttendanceResolutionStatusIds.Resolved,
                         resolvedMessage,
                         workPlanId:
                             planResult.WorkPlanId,
                         workPlanSegmentId:
                             planResult.WorkPlanSegmentId,
                         jobId:
                             planResult.JobId,
                         clockType:
                             planResult.ClockType);





                var savedResolvedRecord =
                await _resolutionRepository.AddAsync(
                    resolvedRecord,
                    cancellationToken);

                            return AttendanceProcessingResult.Completed(
                                savedResolvedRecord,
                                resolvedMessage);

        
            }
            catch (InvalidOperationException exception)
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

                return AttendanceProcessingResult.Failed(
                    attendanceLogId,
                    "An unexpected error occurred while processing the attendance log.");
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

        private async Task<AttendanceProcessingResult>
            SaveInvalidResolutionAsync(
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

        //private static AttendanceLogResolution CreateResolution(
        //    AttendanceLog attendanceLog,
        //    int resolutionStatusId,
        //    string message,
        //    long? workPlanId = null,
        //    long? workAssignmentId = null,
        //    long? workAssignmentSegmentId = null)
        //{
        //    return new AttendanceLogResolution
        //    {
        //        AttendanceLogId = attendanceLog.AttendanceLogId,

        //        WorkPlanId = workPlanId,

        //        WorkAssignmentId = workAssignmentId,

        //        WorkAssignmentSegmentId = workAssignmentSegmentId,

        //        AttendanceResolutionStatusId = resolutionStatusId,

        //        ResolutionDate = DateTime.Now,

        //        ResolutionMessage = message,

        //        IsValid = true,

        //        CreatedDate = DateTime.Now
        //    };
        //}

        private static AttendanceLogResolution CreateResolution(
    AttendanceLog attendanceLog,
    int resolutionStatusId,
    string message,
    long? workPlanId = null,
    long? workAssignmentId = null,
    long? workAssignmentSegmentId = null,
    int? workPlanSegmentId = null,
    int? jobId = null,
    AttendanceClockType clockType =
        AttendanceClockType.Unresolved)
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

        private static string BuildResolvedMessage(
         string? assignmentName,
         string? segmentName,
         bool isInsideScheduledPeriod,
         bool isInsideGracePeriod)
        {
            var periodDescription =
                isInsideScheduledPeriod
                    ? "inside the scheduled period"
                    : isInsideGracePeriod
                        ? "inside the allowed grace period"
                        : "outside the scheduled period";

            return
                $"Attendance event resolved to assignment '{assignmentName ?? "Unknown"}' " +
                $"and segment '{segmentName ?? "Unknown"}', {periodDescription}.";
        }
    }
}
