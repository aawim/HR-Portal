 
using HRM.DTOs.WorkPlanning;
using HRM.Enum;
using HRM.Models;
using Microsoft.EntityFrameworkCore;
namespace HRM.WorkPlanning.Abstractions
{
    public class WorkPlanningTestService : IWorkPlanningTestService
    {
        private const string StandardOfficeTemplateCode =
        "STANDARD_OFFICE_0800_1600";
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IWorkAssignmentGenerator _generator;

        public WorkPlanningTestService(
        IDbContextFactory<HrmTeContext> dbFactory,
        IWorkAssignmentGenerator generator)
        {
            _dbFactory = dbFactory;
            _generator = generator;
        }
        public async Task<WorkPlanTestResult> GenerateStandardOfficePlanAsync(
            int organisationId,
            int individualId,
            int operationLogId,
            DateTime workDate,
            CancellationToken cancellationToken = default)
        {
            try
            {
                await using var db =
                    await _dbFactory.CreateDbContextAsync(cancellationToken);

                var template = await db.WorkTemplates
                    .AsNoTracking()
                    .Where(x =>
                        x.Code == StandardOfficeTemplateCode &&
                        x.IsActive)
                    .Select(x => new
                    {
                        x.WorkTemplateId,
                        x.Name
                    })
                    .SingleOrDefaultAsync(cancellationToken);

                if (template is null)
                {
                    return new WorkPlanTestResult
                    {
                        Success = false,
                        Message =
                            $"Template '{StandardOfficeTemplateCode}' was not found."
                    };
                }

                var date = DateOnly.FromDateTime(workDate.Date);

                var job = await db.Jobs
                    .AsNoTracking()
                    .Where(x =>
                        x.IndividualID == individualId &&
                        x.OrganisationID == organisationId &&
                        x.JobStateId == 4 &&
                        x.TerminatedDate == null)
                    .Select(x => new
                    {
                        x.JobId
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (job is null)
                {
                    return new WorkPlanTestResult
                    {
                        Success = false,
                        Message =
                            "No active job was found for the selected employee and organisation."
                    };
                }

                var existingPlan = await db.WorkPlans
                    .AsNoTracking()
                    .Where(x =>
                        x.WorkTemplateId == template.WorkTemplateId &&
                        x.IndividualId == individualId &&
                        x.JobId == job.JobId &&
                        x.OrganisationBusinessEntityId == organisationId &&
                        x.WorkDate == date &&
                        x.IsValid)
                    .Select(x => new
                    {
                        x.WorkPlanId
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (existingPlan is not null)
                {
                    var existingAssignment = await db.WorkAssignments
                        .AsNoTracking()
                        .Where(x =>
                            x.WorkPlanId == existingPlan.WorkPlanId)
                        .OrderBy(x => x.WorkAssignmentId)
                        .Select(x => new
                        {
                            x.WorkAssignmentId,
                            x.Name,
                            x.StartDateTime,
                            x.EndDateTime
                        })
                        .FirstOrDefaultAsync(cancellationToken);

                    return new WorkPlanTestResult
                    {
                        Success = false,
                        Message =
                            "A work plan already exists for this employee, template and date.",
                        WorkPlanId = existingPlan.WorkPlanId,
                        WorkAssignmentId =
                            existingAssignment?.WorkAssignmentId,
                        AssignmentTitle =
                            existingAssignment?.Name,
                        StartDateTime =
                            existingAssignment?.StartDateTime,
                        EndDateTime =
                            existingAssignment?.EndDateTime
                    };
                }

                var request = new GenerateWorkPlanRequest
                {
                    WorkTemplateId = template.WorkTemplateId,
                    OrganisationBusinessEntityId = organisationId,
                    IndividualId = individualId,
                    JobId = job.JobId,
                    WorkDate = date,
                    GenerationSource = WorkPlanGenerationSource.Manual,
                    AssignmentSource = WorkAssignmentSource.Template,
                    OperationLogId = operationLogId
                };

                var generatedResult = await _generator.GenerateAsync(
                    request,
                    cancellationToken = default);

                if (!generatedResult.Success)
                {
                    return new WorkPlanTestResult
                    {
                        Success = false,
                        Message = string.IsNullOrWhiteSpace(generatedResult.Message)
                            ? "The generator returned an unsuccessful result without an error message."
                            : generatedResult.Message
                    };
                }

                await using var verificationDb =
                    await _dbFactory.CreateDbContextAsync(cancellationToken);

                var workPlanId = generatedResult.WorkPlanId;

                var assignment = await verificationDb.WorkAssignments
                    .AsNoTracking()
                    .Where(x => x.WorkPlanId == workPlanId)
                    .OrderBy(x => x.WorkAssignmentId)
                    .Select(x => new
                    {
                        x.WorkAssignmentId,
                        x.Name,
                        x.StartDateTime,
                        x.EndDateTime
                    })
                    .FirstOrDefaultAsync(cancellationToken);

                if (assignment is null)
                {
                    return new WorkPlanTestResult
                    {
                        Success = false,
                        Message =
                            "The work plan was created, but no assignment was found.",
                        WorkPlanId = workPlanId
                    };
                }

                var segmentCount =
                    await verificationDb.WorkAssignmentSegments
                        .AsNoTracking()
                        .CountAsync(
                            x => x.WorkAssignmentId ==
                                 assignment.WorkAssignmentId,
                            cancellationToken);

                var ownerCount =
                    await verificationDb.WorkAssignmentOwners
                        .AsNoTracking()
                        .CountAsync(
                            x => x.WorkAssignmentId ==
                                 assignment.WorkAssignmentId,
                            cancellationToken);

                return new WorkPlanTestResult
                {
                    Success = true,
                    Message =
                        "The standard office work plan was generated successfully.",
                    WorkPlanId = workPlanId,
                    WorkAssignmentId = assignment.WorkAssignmentId,
                    TemplateName = template.Name,
                    AssignmentTitle = assignment.Name,
                    StartDateTime = assignment.StartDateTime,
                    EndDateTime = assignment.EndDateTime,
                    SegmentCount = segmentCount,
                    OwnerCount = ownerCount
                };
            }
            catch (Exception exception)
            {
                var baseException = exception.GetBaseException();

                return new WorkPlanTestResult
                {
                    Success = false,
                    Message =
                        $"{baseException.GetType().Name}: {baseException.Message}"
                };
            }
        }



    }
}
 
