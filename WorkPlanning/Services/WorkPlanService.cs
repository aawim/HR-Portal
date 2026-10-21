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
        private readonly IWorkAssignmentGenerator _assignmentGenerator;
        private readonly IWorkPlanGenerator _planGenerator;
        public string searchText = string.Empty;
        public WorkPlanService(
            IDbContextFactory<HrmTeContext> dbFactory, IOperationLogService logService, IUserAccessService userAccessService,
            IWorkAssignmentGenerator assignmentGenerator, IWorkPlanGenerator planGenerator)
        {
            _dbFactory = dbFactory;
            _logService = logService;
            _userAccessService = userAccessService;
            _assignmentGenerator = assignmentGenerator;
            _planGenerator = planGenerator;
        }

        public async Task<List<WorkPlanEmployeeDto>> SearchEmployeesAsync(string searchText)
        {
            var org = await WorkPlanningAccess.RequireOrganisationAsync(_userAccessService);
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

                where job.OrganisationID == org && job.JobStateId == approvedJobStateId
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
            if (request == null || request.WorkDate == default || request.JobId <= 0 || request.IndividualId <= 0)
            { result.Errors.Add("Select a staff member, job and work date."); return result; }
            try
            {
                var org = await WorkPlanningAccess.RequireOrganisationAsync(_userAccessService, true);
                if (org != request.OrganisationBusinessEntityId) throw new UnauthorizedAccessException("Select the current organisation.");
                await using var db = await _dbFactory.CreateDbContextAsync();
                await WorkPlanningAccess.RequireJobAsync(db, _userAccessService, request.JobId);
                if (request.IsManual || request.WorkPlanGenerationSource == WorkPlanGenerationSource.Manual)
                {
                    await WorkPlanningAccess.RequireTemplateAsync(db, _userAccessService, request.WorkTemplateId);
                    var context = await _userAccessService.RequireContextAsync();
                    var log = await _logService.CreateAsync(db, SharedConfig.OperationLogActionTypes.WORK_ASSIGNMENT_CREATE,
                        $"Manual plan: job {request.JobId}, template {request.WorkTemplateId}, date {request.WorkDate:yyyy-MM-dd}.");
                    await db.SaveChangesAsync();
                    var generated = await _assignmentGenerator.GenerateAsync(new GenerateWorkPlanRequest {
                        IndividualId = request.IndividualId, JobId = request.JobId, OrganisationBusinessEntityId = org,
                        WorkTemplateId = request.WorkTemplateId, WorkDate = request.WorkDate.Date,
                        GeneratedByUserId = context.UserId, OperationLogId = log.OperationLogId, Remarks = request.Remarks,
                        GenerationSource = WorkPlanGenerationSource.Manual, AssignmentSource = WorkAssignmentSource.Manual });
                    if (!generated.Success) { result.Errors.Add(generated.Message); return result; }
                    result.WorkPlanId = generated.WorkPlanId;
                }
                else
                {
                    var plan = await _planGenerator.GenerateOrGetAsync(request.IndividualId, request.JobId, org, DateOnly.FromDateTime(request.WorkDate));
                    if (plan == null) { result.Errors.Add("No work is scheduled on this date. Use a manual assignment for extra duty."); return result; }
                    result.WorkPlanId = plan.WorkPlanId;
                }
                var saved = await db.WorkPlans.AsNoTracking().Where(x => x.WorkPlanId == result.WorkPlanId)
                    .Select(x => new { x.PlanGuid, Count = x.WorkPlanSegments.Count(s => s.IsValid) }).SingleAsync();
                result.Success = true; result.PlanGuid = saved.PlanGuid; result.GeneratedSegmentCount = saved.Count;
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or InvalidOperationException)
            { result.Errors.Add(ex.Message); }
            return result;
        }
        public async Task<WorkPlanListDto?> GetWorkPlanDetailAsync(int workPlanId)
        {
            var org = await WorkPlanningAccess.RequireOrganisationAsync(_userAccessService);
            await using var db =
                await _dbFactory.CreateDbContextAsync();

            return await db.WorkPlans
                .AsNoTracking()
                .Where(x =>
                    x.WorkPlanId == workPlanId && x.OrganisationBusinessEntityId == org &&
                    x.IsValid)
                .Select(x => new WorkPlanListDto
                {
                    WorkPlanId = x.WorkPlanId,

                    IndividualId = x.IndividualId,

                    JobId = x.JobId,

                    OrganisationBusinessEntityId = x.OrganisationBusinessEntityId,

                    WorkDate = x.WorkDate,

                    WorkTemplateId = x.WorkTemplateId,

                    WorkTemplateName = x.WorkTemplate != null ? x.WorkTemplate.Name : "Manual Plan",

                    GenerationSource = x.GenerationSource,

                    GeneratedDate = x.GeneratedDate,

                    IsGenerated = x.IsGenerated,

                    IsFinalized = x.IsFinalized,

                    IsManual = x.IsManual,

                    IsValid = x.IsValid,

                    PlanGuid = x.PlanGuid,

                    Version = x.Version,

                    Remarks = x.Remarks,

                    Segments = x.WorkPlanSegments
                        .Where(s => s.IsValid)
                        .OrderBy(s => s.SequenceNumber)
                        .ThenBy(s => s.StartDateTime)
                        .Select(s => new WorkPlanSegmentDto
                        {
                            WorkPlanSegmentId =
                                s.WorkPlanSegmentId,

                            WorkPlanId =
                                x.WorkPlanId,

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
            var org = await WorkPlanningAccess.RequireOrganisationAsync(_userAccessService);
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

               where wp.IsValid && wp.OrganisationBusinessEntityId == org

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
                        x.WorkPlan.GeneratedDate,
                    Version = x.WorkPlan.Version,
                })
                .ToListAsync();
        }




        public async Task<List<WorkTemplateLookupDto>>GetActiveTemplatesAsync()
        {
            var org = await WorkPlanningAccess.RequireOrganisationAsync(_userAccessService);
            await using var db =
                await _dbFactory.CreateDbContextAsync();

            return await db.WorkTemplates
                .AsNoTracking()
                .Where(x => x.IsActive && (x.IsGlobal || x.OrganisationBusinessEntityId == org))
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
            var org = await WorkPlanningAccess.RequireOrganisationAsync(_userAccessService);
            await using var db =
                await _dbFactory.CreateDbContextAsync();

            return await db.PlanningProviders
                .AsNoTracking()
                .Where(x => x.IsActive && x.OrganisationBusinessEntityID == org)
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
