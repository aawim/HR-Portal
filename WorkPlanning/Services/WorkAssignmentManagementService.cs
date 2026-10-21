using HRM.Components.Shared;
using HRM.DTOs.WorkPlanning;
using HRM.Enum;
using HRM.Models;
using HRM.Models.WorkPlanning;
using HRM.Services.Interfaces;
using HRM.WorkPlanning.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRM.Services.WorkPlanning;

public sealed class WorkAssignmentManagementService
    : IWorkAssignmentManagementService
{
    private const int DepartmentStructureTypeId = 1;

    private readonly IDbContextFactory<HrmTeContext> _dbFactory;
    private readonly IUserAccessService _access;
    private readonly IOperationLogService _operationLogService;
    private readonly ILogger<WorkAssignmentManagementService> _logger;
    private readonly IWorkAssignmentGenerator _generator;

    public WorkAssignmentManagementService(
        IDbContextFactory<HrmTeContext> dbFactory,
        IUserAccessService access,
        IOperationLogService operationLogService,
        ILogger<WorkAssignmentManagementService> logger, IWorkAssignmentGenerator generator)
    {
        _dbFactory = dbFactory;
        _access = access;
        _operationLogService = operationLogService;
        _logger = logger;
        _generator = generator;
    }

    public async Task<List<WorkAssignmentDepartmentDto>>GetDepartmentsAsync(
            CancellationToken cancellationToken = default)
    {
        var org = await HRM.WorkPlanning.Services.WorkPlanningAccess.RequireOrganisationAsync(_access);
        await using var db =
            await _dbFactory.CreateDbContextAsync(cancellationToken);

        return await
        (
            from structure in db.OrganisationStructures.AsNoTracking()

            join structureType in
                    db.OrganisationStructureTypes.AsNoTracking()
                on structure.OrganisationStructureTypeId
                equals structureType.OrganisationStructureTypeId

            where structure.OrganisationBusinessEntityID == org && structure.OrganisationStructureTypeId ==
                      DepartmentStructureTypeId
                  && structureType.TypeName == "Department"

            orderby structure.Name

            select new WorkAssignmentDepartmentDto
            {
                OrganisationStructureId =
                    structure.OrganisationStructureId,

                //OrganisationBusinessEntityId =
                //    structure.OrganisationBusinessEntityId,

                DepartmentName =
                    structure.Name
            }
        )
        .ToListAsync(cancellationToken);
    }

    //public async Task<List<WorkPlanLookupDto>>
    //    GetWorkPlansAsync(
    //        CancellationToken cancellationToken = default)
    //{
    //    await using var db =
    //        await _dbFactory.CreateDbContextAsync(cancellationToken);

    //    var workPlans = await db.WorkPlans
    //        .AsNoTracking()
    //        .Where(x => x.IsValid)
    //        .OrderByDescending(x => x.WorkDate)
    //        .ThenByDescending(x => x.WorkPlanId)
    //        .Select(x => new
    //        {
    //            x.WorkPlanId,
    //            x.WorkDate,
    //            x.IndividualId
    //        })
    //        .ToListAsync(cancellationToken);

    //    return workPlans
    //        .Select(x => new WorkPlanLookupDto
    //        {
    //            WorkPlanId = (int)x.WorkPlanId,

    //            DisplayName =
    //                $"{x.WorkDate:dd MMM yyyy} - " +
    //                $"Plan {x.WorkPlanId}"
    //        })
    //        .ToList();
    //}
    public async Task<List<WorkPlanLookupDto>>
    GetWorkPlansAsync(
        CancellationToken cancellationToken = default)
    {
        var org = await HRM.WorkPlanning.Services.WorkPlanningAccess.RequireOrganisationAsync(_access);
        await using var db =
            await _dbFactory.CreateDbContextAsync(cancellationToken);

        return await
        (
            from workPlan in db.WorkPlans.AsNoTracking()

            join workTemplate in db.WorkTemplates.AsNoTracking()
                on workPlan.WorkTemplateId
                equals workTemplate.WorkTemplateId

            where workPlan.OrganisationBusinessEntityId == org && workPlan.IsValid
                  && workTemplate.IsActive

            orderby workPlan.WorkDate descending,
                    workTemplate.Name

            select new WorkPlanLookupDto
            {
                WorkPlanId =(int)
                    workPlan.WorkPlanId,

                DisplayName =
                    workTemplate.Name
                    + " - "
                    + (workPlan.WorkDate)
            }
        )
        .ToListAsync(cancellationToken);
    }

    public async Task<List<WorkAssignmentGroupDto>>
     GetAssignmentsAsync(
         WorkAssignmentFilterDto filter,
         CancellationToken cancellationToken = default)
    {
        var org = await HRM.WorkPlanning.Services.WorkPlanningAccess.RequireOrganisationAsync(_access);
        ArgumentNullException.ThrowIfNull(filter);

        if (filter.ToDate.Date < filter.FromDate.Date)
        {
            throw new ArgumentException(
                "The end date cannot be before the start date.",
                nameof(filter));
        }

        await using var db =
            await _dbFactory.CreateDbContextAsync(cancellationToken);

        var fromDate = filter.FromDate.Date;
        var toDateExclusive = filter.ToDate.Date.AddDays(1);

        var query =
            from workAssignment in db.WorkAssignments.AsNoTracking()

            join workPlan in db.WorkPlans.AsNoTracking()
                on workAssignment.WorkPlanId
                equals workPlan.WorkPlanId

            join ownerRow in db.WorkAssignmentOwners
                    .AsNoTracking()
                    .Where(x =>
                        x.IsValid &&
                        x.IsCurrentOwner &&
                        !x.RelievedDate.HasValue)
                on workAssignment.WorkAssignmentId
                equals ownerRow.WorkAssignmentId
                into ownerJoin

            from currentOwner in ownerJoin.DefaultIfEmpty()

            join jobRow in db.Jobs.AsNoTracking()
                on currentOwner.JobId
                equals jobRow.JobId
                into jobJoin

            from job in jobJoin.DefaultIfEmpty()

            join departmentRow in db.OrganisationStructures
                    .AsNoTracking()
                    .Where(x =>
                        x.OrganisationStructureTypeId == 1)
                on job.OrganisationStructureId
                equals departmentRow.OrganisationStructureId
                into departmentJoin

            from department in departmentJoin.DefaultIfEmpty()

            where job.OrganisationID == org && workAssignment.IsValid
                  && workPlan.IsValid
                  && !workAssignment.CancelledDate.HasValue

                  && workAssignment.StartDateTime < toDateExclusive
                  && workAssignment.EndDateTime >= fromDate

            select new
            {
                workAssignment.WorkAssignmentId,
                workAssignment.WorkPlanId,
                workAssignment.Name,
                workAssignment.StartDateTime,
                workAssignment.EndDateTime,

                IndividualId =
                    currentOwner == null
                        ? (int?)null
                        : currentOwner.IndividualId,

                DepartmentId =
                    department == null
                        ? (int?)null
                        : department.OrganisationStructureId,

                DepartmentName =
                    department == null
                        ? "Not assigned to a department"
                        : department.Name
            };

        if (filter.WorkPlanId.HasValue &&
            filter.WorkPlanId.Value > 0)
        {
            var workPlanId = filter.WorkPlanId.Value;

            query = query.Where(x =>
                x.WorkPlanId == workPlanId);
        }

        /*
         * A department is derived from the current owner's job.
         * Therefore, selecting a department returns assignments
         * whose current owners belong to that department.
         */
        if (filter.OrganisationStructureId.HasValue &&
            filter.OrganisationStructureId.Value > 0)
        {
            var departmentId =
                filter.OrganisationStructureId.Value;

            query = query.Where(x =>
                x.DepartmentId == departmentId);
        }

        var rows =
            await query.ToListAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(filter.SearchText))
        {
            var searchText =
                filter.SearchText.Trim();

            rows = rows
                .Where(x =>
                    (!string.IsNullOrWhiteSpace(x.Name) &&
                     x.Name.Contains(
                         searchText,
                         StringComparison.OrdinalIgnoreCase))
                    ||
                    (!string.IsNullOrWhiteSpace(x.DepartmentName) &&
                     x.DepartmentName.Contains(
                         searchText,
                         StringComparison.OrdinalIgnoreCase)))
                .ToList();
        }

        var groups = rows
            .GroupBy(x => new
            {
                x.WorkPlanId,
                x.DepartmentId,
                x.DepartmentName,
                AssignmentName = x.Name,
                ScheduledStart = x.StartDateTime,
                ScheduledEnd = x.EndDateTime
            })
            .Select(group =>
            {
                var assignedStaffCount = group
                    .Where(x => x.IndividualId.HasValue)
                    .Select(x => x.IndividualId!.Value)
                    .Distinct()
                    .Count();

                var assignmentCount = group
                    .Select(x => x.WorkAssignmentId)
                    .Distinct()
                    .Count();

                return new WorkAssignmentGroupDto
                {
                    GroupId = (int)
                        group.Min(x =>
                            x.WorkAssignmentId),

                    WorkPlanId = (int)
                        group.Key.WorkPlanId,

                    OrganisationStructureId =
                        group.Key.DepartmentId ?? 0,

                    DepartmentName =
                        group.Key.DepartmentName,

                    AssignmentName =
                        group.Key.AssignmentName,

                    ScheduledStart =
                        group.Key.ScheduledStart,

                    ScheduledEnd =
                        group.Key.ScheduledEnd,

                    RequiredStaffCount =
                        assignmentCount,

                    AssignedStaffCount =
                        assignedStaffCount,

                    Employees =
                        new List<WorkAssignmentEmployeeDto>()
                };
            })
            .OrderBy(x => x.ScheduledStart)
            .ThenBy(x => x.DepartmentName)
            .ThenBy(x => x.AssignmentName)
            .ToList();

        if (!string.IsNullOrWhiteSpace(
                filter.AssignmentStatus))
        {
            groups = groups
                .Where(x =>
                    string.Equals(
                        x.AssignmentStatus,
                        filter.AssignmentStatus,
                        StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        return groups;
    }

    public async Task<List<EligibleAssignmentStaffDto>>
        GetDepartmentStaffAsync(
            int organisationStructureId,
            CancellationToken cancellationToken = default)
    {
        var org = await HRM.WorkPlanning.Services.WorkPlanningAccess.RequireOrganisationAsync(_access);
        if (organisationStructureId <= 0)
        {
            return new List<EligibleAssignmentStaffDto>();
        }

        await using var db =
            await _dbFactory.CreateDbContextAsync(cancellationToken);

        var today =
            DateTime.Today;

        var staffRows = await
        (
            from job in db.Jobs.AsNoTracking()

            join individual in db.Individuals.AsNoTracking()
                on job.IndividualID
                equals individual.BusinessEntityId

            let currentPositionId =
                db.JobPositions
                    .Where(jobPosition =>
                        jobPosition.JobId == job.JobId &&
                        jobPosition.FromDate <= today &&
                        (
                            !jobPosition.ToDate.HasValue ||
                            jobPosition.ToDate.Value >= today
                        ))
                    .OrderByDescending(jobPosition =>
                        jobPosition.FromDate)
                    .ThenByDescending(jobPosition =>
                        jobPosition.JobPositionId)
                    .Select(jobPosition =>
                        (int?)jobPosition.PositionId)
                    .FirstOrDefault()

            join position in db.Positions.AsNoTracking()
                on currentPositionId
                equals (int?)position.PositionId
                into positionJoin

            from position in positionJoin.DefaultIfEmpty()

            where job.OrganisationID == org && job.OrganisationStructureId ==
                      organisationStructureId
                  && !job.TerminatedDate.HasValue

            select new
            {
                IndividualId =
                    individual.BusinessEntityId,

                JobId =
                    job.JobId,

                FirstName =
                    individual.FirstNameEnglish,

                MiddleName =
                    individual.MiddleNameEnglish,

                LastName =
                    individual.LastNameEnglish,

                PositionName =
                    position == null
                        ? null
                        : position.Name
            }
        )
        .ToListAsync(cancellationToken);

        return staffRows
            .GroupBy(x => x.IndividualId)
            .Select(group =>
                group
                    .OrderByDescending(x => x.JobId)
                    .First())
            .Select(x => new EligibleAssignmentStaffDto
            {
                IndividualId =
                    x.IndividualId,

                JobId =
                    x.JobId,

                EmployeeName =
                    BuildFullName(
                        x.FirstName,
                        x.MiddleName,
                        x.LastName),

                JobTitle =
                    string.IsNullOrWhiteSpace(x.PositionName)
                        ? "No active position"
                        : x.PositionName,

                IsSelected =
                    false
            })
            .OrderBy(x => x.EmployeeName)
            .ToList();
    }

    //public async Task<List<WorkAssignmentEmployeeDto>>
    //GetAssignmentEmployeesAsync(
    //    int organisationStructureId,
    //    int? workPlanId,
    //    string assignmentName,
    //    DateTime scheduledStart,
    //    DateTime scheduledEnd,
    //    CancellationToken cancellationToken = default)
    //{
    //    if (organisationStructureId <= 0)
    //    {
    //        return new List<WorkAssignmentEmployeeDto>();
    //    }

    //    await using var db =
    //        await _dbFactory.CreateDbContextAsync(cancellationToken);

    //    var today = DateTime.Today;

    //    var query =
    //        from workAssignment in db.WorkAssignments.AsNoTracking()

    //        join currentOwner in db.WorkAssignmentOwners.AsNoTracking()
    //            on workAssignment.WorkAssignmentId
    //            equals currentOwner.WorkAssignmentId

    //        join job in db.Jobs.AsNoTracking()
    //            on currentOwner.JobId
    //            equals job.JobId

    //        join individual in db.Individuals.AsNoTracking()
    //            on currentOwner.IndividualId
    //            equals individual.BusinessEntityId

    //        let currentPositionId =
    //            db.JobPositions
    //                .Where(jobPosition =>
    //                    jobPosition.JobId == currentOwner.JobId &&
    //                    jobPosition.FromDate <= today &&
    //                    (
    //                        !jobPosition.ToDate.HasValue ||
    //                        jobPosition.ToDate.Value >= today
    //                    ))
    //                .OrderByDescending(jobPosition =>
    //                    jobPosition.FromDate)
    //                .ThenByDescending(jobPosition =>
    //                    jobPosition.JobPositionId)
    //                .Select(jobPosition =>
    //                    (int?)jobPosition.PositionId)
    //                .FirstOrDefault()

    //        join position in db.Positions.AsNoTracking()
    //            on currentPositionId
    //            equals (int?)position.PositionId
    //            into positionJoin

    //        from position in positionJoin.DefaultIfEmpty()

    //        where job.OrganisationID == org && workAssignment.IsValid
    //              && !workAssignment.CancelledDate.HasValue

    //              && currentOwner.IsValid
    //              && currentOwner.IsCurrentOwner
    //              && !currentOwner.RelievedDate.HasValue

    //              && job.OrganisationStructureId ==
    //                 organisationStructureId

    //              && workAssignment.StartDateTime ==
    //                 scheduledStart

    //              && workAssignment.EndDateTime ==
    //                 scheduledEnd

    //        select new
    //        {
    //            workAssignment.WorkAssignmentId,
    //            currentOwner.WorkAssignmentOwnerId,
    //            currentOwner.IndividualId,
    //            currentOwner.JobId,

    //            workAssignment.WorkPlanId,
    //            workAssignment.Name,
    //            workAssignment.StartDateTime,
    //            workAssignment.EndDateTime,

    //            individual.FirstNameEnglish,
    //            individual.MiddleNameEnglish,
    //            individual.LastNameEnglish,

    //            PositionName =
    //                position == null
    //                    ? null
    //                    : position.Name
    //        };

    //    if (workPlanId.HasValue &&
    //        workPlanId.Value > 0)
    //    {
    //        var selectedWorkPlanId =
    //            workPlanId.Value;

    //        query = query.Where(x =>
    //            x.WorkPlanId == selectedWorkPlanId);
    //    }

    //    if (!string.IsNullOrWhiteSpace(assignmentName))
    //    {
    //        var selectedAssignmentName =
    //            assignmentName.Trim();

    //        query = query.Where(x =>
    //            x.Name == selectedAssignmentName);
    //    }

    //    var rows =
    //        await query.ToListAsync(cancellationToken);

    //    var now = DateTime.Now;

    //    return rows
    //        .Select(x => new WorkAssignmentEmployeeDto
    //        {
    //            WorkAssignmentId =
    //                x.WorkAssignmentId,

    //            WorkAssignmentOwnerId =
    //                x.WorkAssignmentOwnerId,

    //            IndividualId =
    //                x.IndividualId,

    //            JobId =
    //                x.JobId,

    //            EmployeeName =
    //                BuildFullName(
    //                    x.FirstNameEnglish,
    //                    x.MiddleNameEnglish,
    //                    x.LastNameEnglish),

    //            JobTitle =
    //                string.IsNullOrWhiteSpace(x.PositionName)
    //                    ? "No active position"
    //                    : x.PositionName,

    //            HasCurrentOwner =
    //                true,

    //            AssignmentState =
    //                GetAssignmentState(
    //                    x.StartDateTime,
    //                    x.EndDateTime,
    //                    now)
    //        })
    //        .OrderBy(x => x.EmployeeName)
    //        .ToList();
    //}


    public async Task<List<WorkAssignmentEmployeeDto>>GetAssignmentEmployeesAsync(
        int organisationStructureId,
        int? workPlanId,
        string assignmentName,
        DateTime scheduledStart,
        DateTime scheduledEnd,
        CancellationToken cancellationToken = default)
    {
        var org = await HRM.WorkPlanning.Services.WorkPlanningAccess.RequireOrganisationAsync(_access);
        await using var db =
            await _dbFactory.CreateDbContextAsync(cancellationToken);

        var today = DateTime.Today;

        var query =
            from workAssignment in db.WorkAssignments.AsNoTracking()

            join owner in db.WorkAssignmentOwners.AsNoTracking()
                on workAssignment.WorkAssignmentId
                equals owner.WorkAssignmentId

            join job in db.Jobs.AsNoTracking()
                on owner.JobId
                equals job.JobId

            join individual in db.Individuals.AsNoTracking()
                on owner.IndividualId
                equals individual.BusinessEntityId

            let positionId =
                db.JobPositions
                    .Where(jobPosition =>
                        jobPosition.JobId == job.JobId &&
                        jobPosition.FromDate <= today &&
                        (
                            !jobPosition.ToDate.HasValue ||
                            jobPosition.ToDate.Value >= today
                        ))
                    .OrderByDescending(jobPosition =>
                        jobPosition.FromDate)
                    .Select(jobPosition =>
                        (int?)jobPosition.PositionId)
                    .FirstOrDefault()

            join position in db.Positions.AsNoTracking()
                on positionId
                equals (int?)position.PositionId
                into positionJoin

            from position in positionJoin.DefaultIfEmpty()

            where job.OrganisationID == org && workAssignment.IsValid
                  && !workAssignment.CancelledDate.HasValue

                  && owner.IsValid
                  && owner.IsCurrentOwner
                  && !owner.RelievedDate.HasValue

                  && job.OrganisationStructureId ==
                     organisationStructureId

                  && workAssignment.WorkPlanId ==
                     workPlanId

                  && workAssignment.Name ==
                     assignmentName

                  && workAssignment.StartDateTime ==
                     scheduledStart

                  && workAssignment.EndDateTime ==
                     scheduledEnd

            select new
            {
                workAssignment.WorkAssignmentId,
                owner.WorkAssignmentOwnerId,
                owner.IndividualId,
                owner.JobId,

                workAssignment.StartDateTime,
                workAssignment.EndDateTime,

                individual.FirstNameEnglish,
                individual.MiddleNameEnglish,
                individual.LastNameEnglish,

                PositionName =
                    position == null
                        ? null
                        : position.Name
            };

        var rows =
            await query.ToListAsync(cancellationToken);

        var now = DateTime.Now;

        return rows
            .Select(x => new WorkAssignmentEmployeeDto
            {
                WorkAssignmentId =
                    x.WorkAssignmentId,

                WorkAssignmentOwnerId =
                    x.WorkAssignmentOwnerId,

                IndividualId =
                    x.IndividualId,

                JobId =
                    x.JobId,

                EmployeeName =
                    BuildFullName(
                        x.FirstNameEnglish,
                        x.MiddleNameEnglish,
                        x.LastNameEnglish),

                JobTitle =
                    string.IsNullOrWhiteSpace(x.PositionName)
                        ? "No active position"
                        : x.PositionName,

                HasCurrentOwner =
                    true,

                AssignmentState =
                    GetAssignmentState(
                        x.StartDateTime,
                        x.EndDateTime,
                        now)
            })
            .OrderBy(x => x.EmployeeName)
            .ToList();
    }





    public async Task<BulkAssignmentResultDto> AssignDepartmentAsync(BulkDepartmentAssignmentRequest request, CancellationToken cancellationToken = default)
    {
        if (request == null) return Failure("The assignment request is required.");
        var ids = request.SelectedIndividualIds?.Where(x => x > 0).Distinct().ToList() ?? [];
        var validation = ValidateBulkRequest(request, ids);
        if (validation != null) return Failure(validation, ids.Count);
        var result = new BulkAssignmentResultDto { TotalSelected = ids.Count, Warnings = [] };
        try
        {
            var org = await HRM.WorkPlanning.Services.WorkPlanningAccess.RequireOrganisationAsync(_access, true);
            var actor = await _access.RequireContextAsync();
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            if (!await db.OrganisationStructures.AnyAsync(x => x.OrganisationStructureId == request.OrganisationStructureId && x.OrganisationBusinessEntityID == org && x.OrganisationStructureTypeId == DepartmentStructureTypeId, cancellationToken))
                return Failure("Select a department in the current organisation.", ids.Count);
            var source = await db.WorkPlans.AsNoTracking().Where(x => x.WorkPlanId == request.WorkPlanId && x.IsValid && x.OrganisationBusinessEntityId == org)
                .Select(x => new { x.WorkTemplateId }).SingleOrDefaultAsync(cancellationToken);
            if (source?.WorkTemplateId == null) return Failure("Choose a source plan with one template. A combined daily plan cannot be used as a template.", ids.Count);
            await HRM.WorkPlanning.Services.WorkPlanningAccess.RequireTemplateAsync(db, _access, source.WorkTemplateId.Value);
            var template = await db.WorkTemplates.AsNoTracking().SingleAsync(x => x.WorkTemplateId == source.WorkTemplateId.Value, cancellationToken);
            if (!template.DefaultStartTime.HasValue || !template.DefaultEndTime.HasValue) return Failure("The source template needs start and end times.", ids.Count);
            var duration = template.DefaultEndTime.Value.ToTimeSpan() - template.DefaultStartTime.Value.ToTimeSpan();
            if (template.EndsNextDay || duration <= TimeSpan.Zero) duration += TimeSpan.FromDays(1);
            if (request.ScheduledEnd != request.ScheduledStart.Add(duration))
                return Failure($"The selected template lasts {duration.TotalHours:0.##} hours. Set the end to {request.ScheduledStart.Add(duration):dd MMM yyyy HH:mm}, or choose a template with the required hours.", ids.Count);
            var staff = await db.Jobs.AsNoTracking().Where(x => ids.Contains(x.IndividualID) && x.OrganisationID == org && x.OrganisationStructureId == request.OrganisationStructureId && x.JobStateId == SharedConfig.JobStates.APPROVED && x.TerminatedDate == null)
                .Select(x => new { x.IndividualID, x.JobId }).ToListAsync(cancellationToken);
            foreach (var id in ids)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var job = staff.Where(x => x.IndividualID == id).OrderByDescending(x => x.JobId).FirstOrDefault();
                if (job == null) { result.SkippedCount++; result.Warnings.Add($"Staff {id} has no approved active job in this department."); continue; }
                var log = await _operationLogService.CreateAsync(db, SharedConfig.OperationLogActionTypes.WORK_ASSIGNMENT_CREATE,
                    $"Department assignment: tenant {org}, job {job.JobId}, template {template.WorkTemplateId}, {request.ScheduledStart:O}–{request.ScheduledEnd:O}.");
                await db.SaveChangesAsync(cancellationToken);
                var generated = await _generator.GenerateAsync(new GenerateWorkPlanRequest {
                    IndividualId = id, JobId = job.JobId, OrganisationBusinessEntityId = org, WorkTemplateId = template.WorkTemplateId,
                    WorkDate = request.ScheduledStart.Date, ScheduledStartTime = request.ScheduledStart,
                    AssignmentTitle = request.AssignmentName.Trim(), Remarks = request.Remarks, GeneratedByUserId = actor.UserId, OperationLogId = log.OperationLogId,
                    GenerationSource = WorkPlanGenerationSource.Manual, AssignmentSource = WorkAssignmentSource.Manual }, cancellationToken);
                if (generated.Success) result.AssignedCount++;
                else { result.SkippedCount++; result.Warnings.Add($"Staff {id}: {generated.Message}"); }
            }
            result.Success = result.AssignedCount > 0;
            result.Message = $"{result.AssignedCount} employee(s) assigned; {result.SkippedCount} skipped. Each employee has their own daily plan and clock segments.";
        }
        catch (UnauthorizedAccessException ex) { result.Success = false; result.Message = ex.Message; }
        return result;
    }
    private static string? ValidateBulkRequest(
        BulkDepartmentAssignmentRequest request,
        IReadOnlyCollection<int> selectedIndividualIds)
    {
        if (request.OrganisationStructureId <= 0)
        {
            return "Select a department.";
        }

        if (!request.WorkPlanId.HasValue ||
            request.WorkPlanId.Value <= 0)
        {
            return "Select a work plan.";
        }

        if (string.IsNullOrWhiteSpace(
                request.AssignmentName))
        {
            return "Enter an assignment name.";
        }

        if (request.ScheduledStart == default)
        {
            return "Enter the assignment start date and time.";
        }

        if (request.ScheduledEnd == default)
        {
            return "Enter the assignment end date and time.";
        }

        if (request.ScheduledEnd <=
            request.ScheduledStart)
        {
            return "The assignment end time must be after its start time.";
        }

        if (selectedIndividualIds.Count == 0)
        {
            return "Select at least one employee.";
        }

        return null;
    }

    private static string BuildFullName(
        string? firstName,
        string? middleName,
        string? lastName)
    {
        return string.Join(
            " ",
            new[]
            {
                firstName,
                middleName,
                lastName
            }
            .Where(x =>
                !string.IsNullOrWhiteSpace(x))
            .Select(x =>
                x!.Trim()));
    }

    private static string GetAssignmentState(
        DateTime startDateTime,
        DateTime endDateTime,
        DateTime currentDateTime)
    {
        if (endDateTime < currentDateTime)
        {
            return "Completed";
        }

        if (startDateTime <= currentDateTime &&
            endDateTime >= currentDateTime)
        {
            return "In Progress";
        }

        return "Scheduled";
    }

    private static BulkAssignmentResultDto Failure(
        string message,
        int totalSelected = 0)
    {
        return new BulkAssignmentResultDto
        {
            Success = false,
            Message = message,
            TotalSelected = totalSelected,
            AssignedCount = 0,
            SkippedCount = 0,
            Warnings = new List<string>()
        };
    }
}
