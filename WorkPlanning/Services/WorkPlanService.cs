using Azure.Core;
using HRM.Components.Shared;
using HRM.DTOs.WorkPlanning;
using HRM.Enum;
using HRM.Models;
using HRM.Models.WorkPlanning;
using HRM.Services.Interfaces;
using HRM.WorkPlanning.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HRM.WorkPlanning.Services
{
    public class WorkPlanService : IWorkPlanService
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IOperationLogService _logService;
        private readonly IUserAccessService _userAccessService;
        public string searchText = string.Empty;
        public WorkPlanService(
            IDbContextFactory<HrmTeContext> dbFactory, IOperationLogService logService, IUserAccessService userAccessService    )
        {
            _dbFactory = dbFactory;
            _logService = logService;
            _userAccessService = userAccessService;
        }

        public async Task<List<WorkPlanEmployeeDto>> SearchEmployeesAsync(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
                return [];

            searchText = searchText.Trim();

            await using var db =
                await _dbFactory.CreateDbContextAsync();

            var approvedJobStateId =
                SharedConfig.JobStates.APPROVED;

            return await
            (
                from job in db.Jobs.AsNoTracking()

                join individual in db.Individuals.AsNoTracking()
                    on job.IndividualID
                    equals individual.BusinessEntityId

                join staff in db.Staffs.AsNoTracking()
                    on individual.BusinessEntityId
                    equals staff.IndividualId
                    into staffJoin

                from staff in staffJoin.DefaultIfEmpty()

                join organisation in db.Organisations.AsNoTracking()
                    on job.OrganisationID
                    equals organisation.BusinessEntityID
                    into organisationJoin

                from organisation in organisationJoin.DefaultIfEmpty()

                join structure in db.OrganisationStructures.AsNoTracking()
                    on job.OrganisationStructureId
                    equals structure.OrganisationStructureId
                    into structureJoin

                from structure in structureJoin.DefaultIfEmpty()

                let fullName =
                    ((individual.FirstNameEnglish ?? string.Empty) + " " +
                      (individual.LastNameEnglish ?? string.Empty))
                    .Trim()

                where job.JobStateId == approvedJobStateId
                      && job.TerminatedDate == null
                      && (
                          fullName.Contains(searchText) ||
                          (staff != null &&
                           staff.EmployeeNumber.Contains(searchText))
                      )

                orderby fullName

                select new WorkPlanEmployeeDto
                {
                    IndividualId =
                        individual.BusinessEntityId,

                    JobId =
                        job.JobId,

                    OrganisationId =
                        job.OrganisationID,

                    Name =
                        fullName,

                    EmployeeNumber =
                        staff != null
                            ? staff.EmployeeNumber
                            : null,

                    PositionName =
                        structure != null
                            ? structure.Name
                            : null,

                    OrganisationName =
                        organisation != null
                            ? organisation.OrganisationName
                            : null
                }
            )
            .Take(20)
            .ToListAsync();
        }

        public async Task<GenerateWorkPlanResultDto> GenerateWorkPlanAsync(GenerateWorkPlanDto request)
        {
            var result = new GenerateWorkPlanResultDto();

            await using var db =
                await _dbFactory.CreateDbContextAsync();

            await using var transaction =
                await db.Database.BeginTransactionAsync();

            try
            {

                var userContext = await _userAccessService.GetContextAsync();

                if (userContext == null || userContext.UserId <= 0)
                {
                    throw new UnauthorizedAccessException(
                        "The current user account could not be identified.");
                }


                if (!request.PlanningProviderId.HasValue)
                {
                    throw new InvalidOperationException(
                        "Planning provider is required.");
                }


                var workDate = request.WorkDate.Date;

                // 1. Validate the employee's job.
                var job = await db.Jobs
                    .AsNoTracking()
                    .Where(x =>
                        x.JobId == request.JobId &&
                        x.IndividualID == request.IndividualId &&
                        x.OrganisationID ==
                            request.OrganisationBusinessEntityId &&
                        x.TerminatedDate == null)
                    .Select(x => new
                    {
                        x.JobId,
                        x.IndividualID,
                        x.OrganisationID
                    })
                    .SingleOrDefaultAsync();

                if (job == null)
                {
                    result.Errors.Add(
                        "The selected active job could not be found.");

                    return result;
                }

                // 2. Validate the template.
                var template = await db.WorkTemplates
                    .AsNoTracking()
                    .Where(x =>
                        x.WorkTemplateId == request.WorkTemplateId &&
                        x.IsActive)
                    .Select(x => new
                    {
                        x.WorkTemplateId,
                        x.Name
                    })
                    .SingleOrDefaultAsync();

                if (template == null)
                {
                    result.Errors.Add(
                        "The selected work template could not be found.");

                    return result;
                }

                // 3. Load the template segments.
                var templateSegments = await db.WorkTemplateSegments
                    .AsNoTracking()
                    .Where(x =>
                        x.WorkTemplateId == request.WorkTemplateId &&
                        x.IsActive)
                    .OrderBy(x => x.SequenceNumber)
                    .Select(x => new
                    {
                        x.WorkTemplateSegmentId,
                        x.WorkSegmentTypeId,
                        x.Name,
                        x.Description,
                        x.SequenceNumber,
                        x.OffsetMinutes,
                        x.DurationMinutes,
                        x.GraceBeforeMinutes,
                        x.GraceAfterMinutes,
                        x.IsMandatory,
                        x.RequiresAttendance,
                        x.RequiresLocationValidation,
                        x.RequiresDeviceValidation,
                        x.IsPaid
                    })
                    .ToListAsync();

                if (templateSegments.Count == 0)
                {
                    result.Errors.Add(
                        "The selected template has no active segments.");

                    return result;
                }

                // 4. Check whether an active plan already exists.
    

                DateOnly targetDate = DateOnly.FromDateTime(workDate);

                var existingPlan = await db.WorkPlans
                         .AsNoTracking()
                         .Where(x =>
                             x.IndividualId == request.IndividualId &&
                             x.JobId == request.JobId &&
                             // 2. Compare matching DateOnly structures
                             x.WorkDate == request.WorkDate &&
                             x.IsValid)
                         .Select(x => new
                         {
                             x.WorkPlanId,
                             x.PlanGuid,
                             x.Version,
                             x.IsFinalized
                         })
                         .OrderByDescending(x => x.Version)
                         .FirstOrDefaultAsync();

                if (existingPlan?.IsFinalized == true)
                {
                    result.Errors.Add(
                        "A finalized work plan already exists for this date.");

                    return result;
                }

                var planGuid =
                    existingPlan?.PlanGuid ?? Guid.NewGuid();

                var version =
                    existingPlan == null
                        ? 1
                        : existingPlan.Version + 1;

                // Optional:
                // Invalidate the previous active version.
                if (existingPlan != null)
                {
                    var previousPlan = await db.WorkPlans
                        .SingleAsync(x =>
                            x.WorkPlanId == existingPlan.WorkPlanId);

                    previousPlan.IsValid = false;
                }



                var operationLog = await _logService.CreateAsync(
                 db,
                 actionId: SharedConfig.OperationLogActionTypes.WORK_PLAN_CREATE_CREATE,
                 remarks: "Work Plan Create");


                // 5. Create the plan header.
                var workPlan = new WorkPlan
                {
                    IndividualId = request.IndividualId,
                    JobId = request.JobId,

                    OrganisationBusinessEntityId =
                        request.OrganisationBusinessEntityId,

                    PlanningProviderId = request.PlanningProviderId.Value,

                    WorkDate = request.WorkDate,

                    GenerationSource = request.IsManual ? WorkPlanGenerationSource.Manual : WorkPlanGenerationSource.Template,


                    GeneratedDate = DateTime.UtcNow,

                    GeneratedByUserId = userContext.UserId,
            

                    IsFinalized = false,
                    FinalizedDate = null,
                    FinalizedByUserId = null,

                    Remarks = request.Remarks,

                    IsValid = true,

                    OperationLogId = operationLog.OperationLogId,

                    CreatedDate = DateTime.UtcNow,

                    WorkTemplateId = request.WorkTemplateId,

                    PlanGuid = planGuid,

                    Version = version,

                    IsGenerated = false,

                    IsManual = request.IsManual
                };

                db.WorkPlans.Add(workPlan);

                await db.SaveChangesAsync();

                // 6. Generate actual dated segments.
                var assignmentBaseDateTime = workDate;

                var generatedSegments = new List<WorkPlanSegment>();

                foreach (var templateSegment in templateSegments)
                {
                    var offsetMinutes =
                        templateSegment.OffsetMinutes ?? 0;

                    var durationMinutes =
                        templateSegment.DurationMinutes ?? 0;

                    if (offsetMinutes < 0)
                    {
                        result.Errors.Add(
                            $"Segment '{templateSegment.Name}' has an invalid offset.");

                        await transaction.RollbackAsync();
                        return result;
                    }

                    if (durationMinutes < 0)
                    {
                        result.Errors.Add(
                            $"Segment '{templateSegment.Name}' has an invalid duration.");

                        await transaction.RollbackAsync();
                        return result;
                    }

                    var segmentStartDateTime =
                        assignmentBaseDateTime.AddMinutes(
                            offsetMinutes);

                    var segmentEndDateTime =
                        segmentStartDateTime.AddMinutes(
                            durationMinutes);

                    var workPlanSegment = new WorkPlanSegment
                    {
                        WorkPlanId = (int)
                            workPlan.WorkPlanId,

                        WorkTemplateSegmentId =
                            templateSegment.WorkTemplateSegmentId,

                        WorkSegmentTypeId =
                            templateSegment.WorkSegmentTypeId,

                        Name =
                            templateSegment.Name ?? string.Empty,

                        Description =
                            templateSegment.Description,

                        SequenceNumber =
                            templateSegment.SequenceNumber,

                        StartDateTime =
                            segmentStartDateTime,

                        EndDateTime =
                            segmentEndDateTime,

                        GraceBeforeMinutes =
                            templateSegment.GraceBeforeMinutes ?? 0,

                        GraceAfterMinutes =
                            templateSegment.GraceAfterMinutes ?? 0,

                        IsMandatory =
                            templateSegment.IsMandatory,

                        RequiresAttendance =
                            templateSegment.RequiresAttendance,

                        RequiresLocationValidation =
                            templateSegment.RequiresLocationValidation,

                        RequiresDeviceValidation =
                            templateSegment.RequiresDeviceValidation,

                        IsPaid =
                            templateSegment.IsPaid,

                        IsCompleted = false,

                        AttendanceId = null,

                        IsValid = true,

                        OperationLogId =
                            request.OperationLogId,

                        CreatedDate =
                            DateTime.UtcNow,

                        UpdatedDate = null
                    };

                    generatedSegments.Add(workPlanSegment);
                }

                db.WorkPlanSegments.AddRange(generatedSegments);

                workPlan.IsGenerated = true;

                await db.SaveChangesAsync();

                await transaction.CommitAsync();

                result.Success = true;
                result.WorkPlanId = workPlan.WorkPlanId;
                result.PlanGuid = workPlan.PlanGuid;
                result.GeneratedSegmentCount = generatedSegments.Count;

                return result;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                result.Errors.Add(
                    ex.InnerException?.Message ?? ex.Message);

                return result;
            }


        }

      
        public async Task<WorkPlanListDto?> GetWorkPlanAsync(int workPlanId)
        {

            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.WorkPlans
                .AsNoTracking()
                .Where(x =>
                    x.WorkPlanId == workPlanId &&
                    x.IsValid)
                .Select(x => new WorkPlanListDto
                {
                    WorkPlanId = x.WorkPlanId,
                    IndividualId = x.IndividualId,
                    JobId = x.JobId,

                    OrganisationBusinessEntityId =
                        x.OrganisationBusinessEntityId,

                    WorkDate =   x.WorkDate ,

                    //WorkDate = x.WorkDate.HasValue? x.WorkDate.Value.ToDateTime(TimeOnly.MinValue) : DateTime.MinValue,

                    WorkTemplateId =
                        x.WorkTemplateId,

                    WorkTemplateName =
                        x.WorkTemplate != null
                            ? x.WorkTemplate.Name
                            : string.Empty,

                    GenerationSource = x.GenerationSource,

                    GeneratedDate =
                        x.GeneratedDate,

                    IsGenerated =
                        x.IsGenerated,

                    IsFinalized =
                        x.IsFinalized,

                    IsManual =
                        x.IsManual,

                    IsValid =
                        x.IsValid,

                    PlanGuid =
                        x.PlanGuid,

                    Version =
                        x.Version,

                    Remarks =
                        x.Remarks,

                    Segments = x.WorkPlanSegments
                        .Where(s => s.IsValid)
                        .OrderBy(s => s.SequenceNumber)
                        .Select(s => new WorkPlanSegmentDto
                        {
                            WorkPlanSegmentId =
                                s.WorkPlanSegmentId,

                            WorkPlanId =(int)
                                s.WorkPlanId,

                            WorkTemplateSegmentId =
                                s.WorkTemplateSegmentId,

                            WorkSegmentTypeId =
                                s.WorkSegmentTypeId,

                            WorkSegmentTypeName =
                                s.WorkSegmentType != null
                                    ? s.WorkSegmentType.Name
                                    : string.Empty,

                            Name =
                                s.Name ?? string.Empty,

                            Description =
                                s.Description,

                            SequenceNumber =
                                s.SequenceNumber,

                            StartDateTime =
                                s.StartDateTime,

                            EndDateTime =
                                s.EndDateTime,

                            GraceBeforeMinutes =
                                s.GraceBeforeMinutes,

                            GraceAfterMinutes =
                                s.GraceAfterMinutes,

                            IsMandatory =
                                s.IsMandatory,

                            RequiresAttendance =
                                s.RequiresAttendance,

                            RequiresLocationValidation =
                                s.RequiresLocationValidation,

                            RequiresDeviceValidation =
                                s.RequiresDeviceValidation,

                            IsPaid =
                                s.IsPaid,

                            IsCompleted =
                                s.IsCompleted,

                            AttendanceId =
                                s.AttendanceId
                        })
                        .ToList()
                })
                .SingleOrDefaultAsync();
        }

        public async Task<List<WorkPlanListDto>> GetWorkPlansAsync(
         //int individualId,
         DateTime fromDate,
         DateTime toDate)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync();

            var startFilter =
                DateOnly.FromDateTime(fromDate.Date);

            var endFilter =
                DateOnly.FromDateTime(toDate.Date);

            var query =
               from wp in db.WorkPlans.AsNoTracking()

               join individual in db.Individuals
                   on wp.IndividualId equals individual.BusinessEntityId

               join templateItem in db.WorkTemplates
                   on wp.WorkTemplateId equals templateItem.WorkTemplateId
                   into templateGroup

               from template in templateGroup.DefaultIfEmpty()

               where wp.IsValid

               select new
               {
                   WorkPlan = wp,
                   Individual = individual,
                   Template = template
               };

            if (fromDate != DateTime.MinValue)
            {
                //var startDate = DateOnly.FromDateTime(fromDate.Date);

                query = query.Where(x =>
                    x.WorkPlan.WorkDate >= fromDate);
            }

            if (toDate != DateTime.MinValue)
            {
                //var endDate = DateOnly.FromDateTime(toDate.Date);

                query = query.Where(x =>
                    x.WorkPlan.WorkDate <= toDate);
            }

            return await query
                .OrderByDescending(x => x.WorkPlan.WorkDate)
                .ThenBy(x => x.Individual.FirstNameEnglish)
                .ThenBy(x => x.Individual.MiddleNameEnglish)
                .ThenBy(x => x.Individual.LastNameEnglish)
                .Select(x => new WorkPlanListDto
                {
                    WorkPlanId = x.WorkPlan.WorkPlanId,
                    IndividualId = x.WorkPlan.IndividualId,
                    JobId = x.WorkPlan.JobId,

                    EmployeeName =
                        ((x.Individual.FirstNameEnglish ?? string.Empty) + " " +
                         (x.Individual.MiddleNameEnglish ?? string.Empty) + " " +
                         (x.Individual.LastNameEnglish ?? string.Empty))
                        .Trim(),

                    OrganisationBusinessEntityId =
                        x.WorkPlan.OrganisationBusinessEntityId,

                    PlanningProviderId =
                        x.WorkPlan.PlanningProviderId,

                    WorkTemplateId =
                        x.WorkPlan.WorkTemplateId,

                    WorkTemplateName =
                        x.Template != null
                            ? x.Template.Name
                            : "Manual Plan",

                    WorkDate =
                        x.WorkPlan.WorkDate,

                    IsManual =
                        x.WorkPlan.IsManual,

                    IsGenerated =
                        x.WorkPlan.IsGenerated,

                    IsFinalized =
                        x.WorkPlan.IsFinalized,

                    Remarks =
                        x.WorkPlan.Remarks,

                    GeneratedDate =
                        x.WorkPlan.GeneratedDate
                })
                .ToListAsync();
        }
        public async Task<List<WorkTemplateLookupDto>>GetActiveTemplatesAsync()
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync();

            return await db.WorkTemplates
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new WorkTemplateLookupDto
                {
                    WorkTemplateId =
                        x.WorkTemplateId,

                    Name =
                        x.Name,

                    Description =
                        x.Description
                })
                .ToListAsync();
        }

        public async Task<List<PlanningProviderLookupDto>>GetPlanningProvidersAsync()
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync();

            return await db.PlanningProviders
                .AsNoTracking()
                .Where(x => x.IsActive)
                .OrderBy(x => x.Name)
                .Select(x => new PlanningProviderLookupDto
                {
                    PlanningProviderId =
                        x.PlanningProviderId,

                    Name =
                        x.Name,

                    Description =
                        x.Description
                })
                .ToListAsync();
        }


 
    }
}
