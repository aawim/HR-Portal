using HRM.Components.Shared;
using HRM.DTOs.WorkPlanning;
using HRM.Enum;
using HRM.Models;
using HRM.Services.Interfaces;
using HRM.WorkPlanning.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HRM.WorkPlanning.Services
{
    public class ManualWorkAssignmentService : IManualWorkAssignmentService
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IWorkAssignmentGenerator _generator;
        private readonly IOperationLogService _operationLogService;
        private readonly ILogger<ManualWorkAssignmentService> _logger;

        public ManualWorkAssignmentService(
            IDbContextFactory<HrmTeContext> dbFactory,
            IWorkAssignmentGenerator generator,
            IOperationLogService operationLogService,
            ILogger<ManualWorkAssignmentService> logger)
        {
            _dbFactory = dbFactory;
            _generator = generator;
            _operationLogService = operationLogService;
            _logger = logger;
        }

        public async Task<GeneratedWorkPlanResult> GenerateAsync(
       ManualWorkAssignmentRequest request,
       int generatedByUserId,
       CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                return Failure(
                    "The manual work-assignment request is required.");
            }

            var validationMessage =
                ValidateRequest(request, generatedByUserId);

            if (validationMessage is not null)
            {
                return Failure(validationMessage);
            }

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            try
            {
                /*
                 * The generator uses a different DbContext, therefore save
                 * the operation log before invoking it.
                 */
                var operationLog =
                    await _operationLogService.CreateAsync(
                        db,
                        actionId:
                            SharedConfig.OperationLogActionTypes.WORK_ASSIGNMENT_CREATE,
                        remarks:
                            $"Manual work assignment generation for " +
                            $"individual {request.IndividualId}, " +
                            $"job {request.JobId}, " +
                            $"date {request.WorkDate:yyyy-MM-dd}.");

                await db.SaveChangesAsync(cancellationToken);

                var generationRequest =
                    new GenerateWorkPlanRequest
                    {
                        IndividualId = request.IndividualId,

                        JobId = request.JobId,

                        OrganisationBusinessEntityId = request.OrganisationBusinessEntityId,

                        WorkTemplateId = request.WorkTemplateId,
 
                        WorkDate = request.WorkDate.Date,

                        GenerationSource = WorkPlanGenerationSource.Manual,

                        GeneratedByUserId = generatedByUserId,

                        OperationLogId = operationLog.OperationLogId,

                        AssignmentTitle = Normalize(request.AssignmentTitle),

                        AssignmentDescription = Normalize(request.AssignmentDescription),

                        Remarks = Normalize(request.Remarks),

                        RequiresAttendance = request.RequiresAttendance,

                        RequiresCheckout = request.RequiresCheckout,

                        Priority = request.Priority,

                        AssignmentSource = WorkAssignmentSource.Manual, // string.IsNullOrWhiteSpace(request.AssignmentSource) ? "Manual" : request.AssignmentSource.Trim()
                    };

                var result =
                    await _generator.GenerateAsync(
                        generationRequest,
                        cancellationToken);

                if (!result.Success)
                {
                    _logger.LogWarning(
                        """
                    Manual work-assignment generation failed.
                    IndividualId: {IndividualId}
                    JobId: {JobId}
                    WorkTemplateId: {WorkTemplateId}
                    WorkDate: {WorkDate}
                    Message: {Message}
                    """,
                        request.IndividualId,
                        request.JobId,
                        request.WorkTemplateId,
                        request.WorkDate,
                        result.Message);
                }

                return result;
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
                Manual work-assignment generation failed unexpectedly.
                IndividualId: {IndividualId}
                JobId: {JobId}
                WorkTemplateId: {WorkTemplateId}
                WorkDate: {WorkDate}
                """,
                    request.IndividualId,
                    request.JobId,
                    request.WorkTemplateId,
                    request.WorkDate);

                return Failure(
                    "An unexpected error occurred while generating " +
                    "the manual work assignment.");
            }
        }

        private static string? ValidateRequest(
            ManualWorkAssignmentRequest request,
            int generatedByUserId)
        {
            if (generatedByUserId <= 0)
            {
                return "The current user could not be identified.";
            }

            if (request.IndividualId <= 0)
            {
                return "A valid employee is required.";
            }

            if (request.JobId <= 0)
            {
                return "A valid job is required.";
            }

            if (request.OrganisationBusinessEntityId <= 0)
            {
                return "A valid organisation is required.";
            }

            if (request.WorkTemplateId <= 0)
            {
                return "A valid work template is required.";
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

        private static string? Normalize(string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

        private static GeneratedWorkPlanResult Failure(
            string message)
        {
            return new GeneratedWorkPlanResult
            {
                Success = false,
                Message = message,
                WorkPlanId = 0,
                WorkAssignmentId = 0,
                Version = 0
            };
        }





    }
}
