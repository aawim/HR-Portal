using HRM.Constants;
using HRM.Models.WorkPlanning;
using HRM.Models;
using HRM.Services.Attendance.Processing;
using HRM.Services.Attendance.Repositories;
using HRM.WorkPlanning.Abstractions;
using Microsoft.EntityFrameworkCore;
using HRM.Services.Attendance.Abstraction;

namespace HRM.Services.Attendance.Abstraction.Services
{
    public sealed class AttendanceLogProcessor : IAttendanceLogProcessor
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;

        private readonly IAttendanceDuplicateValidator
            _duplicateValidator;

        private readonly IWorkAssignmentResolver
            _workAssignmentResolver;

        private readonly IAttendanceLogResolutionRepository
            _resolutionRepository;

        private readonly ILogger<AttendanceLogProcessor> _logger;

        public AttendanceLogProcessor(
            IDbContextFactory<HrmTeContext> dbFactory,
            IAttendanceDuplicateValidator duplicateValidator,
            IWorkAssignmentResolver workAssignmentResolver,
            IAttendanceLogResolutionRepository resolutionRepository,
            ILogger<AttendanceLogProcessor> logger)
        {
            _dbFactory = dbFactory;
            _duplicateValidator = duplicateValidator;
            _workAssignmentResolver = workAssignmentResolver;
            _resolutionRepository = resolutionRepository;
            _logger = logger;
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
                /*
                 * Step 1:
                 * Prevent the same attendance event from being processed twice.
                 */
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

                /*
                 * Step 2:
                 * Load the original attendance event.
                 */
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

                /*
                 * Step 3:
                 * Resolve the event against the employee's work assignment.
                 */
                var assignmentResult =
                    await _workAssignmentResolver.ResolveAsync(
                        attendanceLog.IndividualId,
                        attendanceLog.Date,
                        cancellationToken);

                /*
                 * Step 4:
                 * No assignment was found.
                 */
                if (assignmentResult.WorkAssignmentId is null)
                {
                    var noAssignmentResolution =
                        CreateResolution(
                            attendanceLog,
                            AttendanceResolutionStatusIds.NoAssignment,
                            assignmentResult.Message.Length > 0
                                ? assignmentResult.Message
                                : "No active work assignment matched the attendance event.");

                    var savedResolution =
                        await _resolutionRepository.AddAsync(
                            noAssignmentResolution,
                            cancellationToken);

                    return AttendanceProcessingResult.Recorded(
                        savedResolution,
                        savedResolution.ResolutionMessage ??
                        "No matching work assignment was found.");
                }

                /*
                 * Step 5:
                 * An assignment exists, but no segment matched.
                 */
                if (assignmentResult.SegmentName is null)
                {
                    var noSegmentResolution =
                        CreateResolution(
                            attendanceLog,
                            AttendanceResolutionStatusIds.NoSegment,
                            assignmentResult.Message.Length > 0
                                ? assignmentResult.Message
                                : "A work assignment was found, but no segment matched the attendance event.",
                            assignmentResult.WorkPlanId,
                            assignmentResult.WorkAssignmentId);

                    var savedResolution =
                        await _resolutionRepository.AddAsync(
                            noSegmentResolution,
                            cancellationToken);

                    return AttendanceProcessingResult.Recorded(
                        savedResolution,
                        savedResolution.ResolutionMessage ??
                        "No matching assignment segment was found.");
                }

                /*
                 * Step 6:
                 * Assignment and segment were successfully resolved.
                 */
                var resolvedMessage =
                   BuildResolvedMessage(
                       assignmentResult.AssignmentName,
                       assignmentResult.SegmentName,
                       assignmentResult.IsInsideScheduledPeriod,
                       assignmentResult.IsInsideGracePeriod);





                var resolvedRecord =
                     CreateResolution(
                         attendanceLog,
                         AttendanceResolutionStatusIds.Resolved,
                         resolvedMessage,
                         assignmentResult.WorkPlanId,
                         assignmentResult.WorkAssignmentId,
                         assignmentResult.WorkAssignmentSegmentId);

                var savedResolvedRecord =
                    await _resolutionRepository.AddAsync(
                        resolvedRecord,
                        cancellationToken);

                _logger.LogInformation(
                    "Attendance log {AttendanceLogId} for individual " +
                    "{IndividualId} resolved to assignment " +
                    "{WorkAssignmentId} and segment " +
                    "{WorkAssignmentSegmentId}.",
                    attendanceLog.AttendanceLogId,
                    attendanceLog.IndividualId,
                    assignmentResult.WorkAssignmentId,
                    assignmentResult.WorkAssignmentSegmentId);

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

        private static AttendanceLogResolution CreateResolution(
            AttendanceLog attendanceLog,
            int resolutionStatusId,
            string message,
            long? workPlanId = null,
            long? workAssignmentId = null,
            long? workAssignmentSegmentId = null)
        {
            return new AttendanceLogResolution
            {
                AttendanceLogId = attendanceLog.AttendanceLogId,

                WorkPlanId = workPlanId,

                WorkAssignmentId = workAssignmentId,

                WorkAssignmentSegmentId = workAssignmentSegmentId,

                AttendanceResolutionStatusId = resolutionStatusId,

                ResolutionDate = DateTime.Now,

                ResolutionMessage = message,

                IsValid = true,

                CreatedDate = DateTime.Now
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
