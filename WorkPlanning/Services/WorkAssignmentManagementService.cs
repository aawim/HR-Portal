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

    public WorkAssignmentManagementService(
        IDbContextFactory<HrmTeContext> dbFactory,
        IUserAccessService access,
        IOperationLogService operationLogService,
        ILogger<WorkAssignmentManagementService> logger)
    {
        _dbFactory = dbFactory;
        _access = access;
        _operationLogService = operationLogService;
        _logger = logger;
    }

    public async Task<List<WorkAssignmentDepartmentDto>>GetDepartmentsAsync(
            CancellationToken cancellationToken = default)
    {
        await using var db =
            await _dbFactory.CreateDbContextAsync(cancellationToken);

        return await
        (
            from structure in db.OrganisationStructures.AsNoTracking()

            join structureType in
                    db.OrganisationStructureTypes.AsNoTracking()
                on structure.OrganisationStructureTypeId
                equals structureType.OrganisationStructureTypeId

            where structure.OrganisationStructureTypeId ==
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

    public async Task<List<WorkPlanLookupDto>>
        GetWorkPlansAsync(
            CancellationToken cancellationToken = default)
    {
        await using var db =
            await _dbFactory.CreateDbContextAsync(cancellationToken);

        var workPlans = await db.WorkPlans
            .AsNoTracking()
            .Where(x => x.IsValid)
            .OrderByDescending(x => x.WorkDate)
            .ThenByDescending(x => x.WorkPlanId)
            .Select(x => new
            {
                x.WorkPlanId,
                x.WorkDate,
                x.IndividualId
            })
            .ToListAsync(cancellationToken);

        return workPlans
            .Select(x => new WorkPlanLookupDto
            {
                WorkPlanId = (int)x.WorkPlanId,

                DisplayName =
                    $"{x.WorkDate:dd MMM yyyy} - " +
                    $"Plan {x.WorkPlanId}"
            })
            .ToList();
    }

    public async Task<List<WorkAssignmentGroupDto>>
     GetAssignmentsAsync(
         WorkAssignmentFilterDto filter,
         CancellationToken cancellationToken = default)
    {
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

            where workAssignment.IsValid
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

            where job.OrganisationStructureId ==
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

    public async Task<List<WorkAssignmentEmployeeDto>>
    GetAssignmentEmployeesAsync(
        int organisationStructureId,
        int? workPlanId,
        string assignmentName,
        DateTime scheduledStart,
        DateTime scheduledEnd,
        CancellationToken cancellationToken = default)
    {
        if (organisationStructureId <= 0)
        {
            return new List<WorkAssignmentEmployeeDto>();
        }

        await using var db =
            await _dbFactory.CreateDbContextAsync(cancellationToken);

        var today = DateTime.Today;

        var query =
            from workAssignment in db.WorkAssignments.AsNoTracking()

            join currentOwner in db.WorkAssignmentOwners.AsNoTracking()
                on workAssignment.WorkAssignmentId
                equals currentOwner.WorkAssignmentId

            join job in db.Jobs.AsNoTracking()
                on currentOwner.JobId
                equals job.JobId

            join individual in db.Individuals.AsNoTracking()
                on currentOwner.IndividualId
                equals individual.BusinessEntityId

            let currentPositionId =
                db.JobPositions
                    .Where(jobPosition =>
                        jobPosition.JobId == currentOwner.JobId &&
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

            where workAssignment.IsValid
                  && !workAssignment.CancelledDate.HasValue

                  && currentOwner.IsValid
                  && currentOwner.IsCurrentOwner
                  && !currentOwner.RelievedDate.HasValue

                  && job.OrganisationStructureId ==
                     organisationStructureId

                  && workAssignment.StartDateTime ==
                     scheduledStart

                  && workAssignment.EndDateTime ==
                     scheduledEnd

            select new
            {
                workAssignment.WorkAssignmentId,
                currentOwner.WorkAssignmentOwnerId,
                currentOwner.IndividualId,
                currentOwner.JobId,

                workAssignment.WorkPlanId,
                workAssignment.Name,
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

        if (workPlanId.HasValue &&
            workPlanId.Value > 0)
        {
            var selectedWorkPlanId =
                workPlanId.Value;

            query = query.Where(x =>
                x.WorkPlanId == selectedWorkPlanId);
        }

        if (!string.IsNullOrWhiteSpace(assignmentName))
        {
            var selectedAssignmentName =
                assignmentName.Trim();

            query = query.Where(x =>
                x.Name == selectedAssignmentName);
        }

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

    public async Task<BulkAssignmentResultDto>
        AssignDepartmentAsync(
            BulkDepartmentAssignmentRequest request,
            CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            return Failure(
                "The assignment request is required.");
        }

        var selectedIndividualIds =
            request.SelectedIndividualIds?
                .Where(x => x > 0)
                .Distinct()
                .ToList()
            ?? new List<int>();

        var result =
            new BulkAssignmentResultDto
            {
                TotalSelected =
                    selectedIndividualIds.Count,

                Warnings =
                    new List<string>()
            };

        var validationMessage =
            ValidateBulkRequest(
                request,
                selectedIndividualIds);

        if (validationMessage is not null)
        {
            result.Success = false;
            result.Message = validationMessage;

            return result;
        }

        await using var db =
            await _dbFactory.CreateDbContextAsync(cancellationToken);

        await using var transaction =
            await db.Database.BeginTransactionAsync(
                cancellationToken);

        try
        {
            var selectedDepartment =
                await db.OrganisationStructures
                    .AsNoTracking()
                    .Where(x =>
                        x.OrganisationStructureId ==
                            request.OrganisationStructureId &&
                        x.OrganisationStructureTypeId ==
                            DepartmentStructureTypeId)
                    .Select(x => new
                    {
                        x.OrganisationStructureId,
                        x.Name
                    })
                    .SingleOrDefaultAsync(cancellationToken);

            if (selectedDepartment is null)
            {
                return Failure(
                    "The selected department was not found.",
                    selectedIndividualIds.Count);
            }

            var selectedPlan =
                await db.WorkPlans
                    .AsNoTracking()
                    .Where(x =>
                        x.WorkPlanId == request.WorkPlanId &&
                        x.IsValid)
                    .Select(x => new
                    {
                        x.WorkPlanId,
                        x.WorkTemplateId,
                        x.OrganisationBusinessEntityId
                    })
                    .SingleOrDefaultAsync(cancellationToken);

            if (selectedPlan is null)
            {
                return Failure(
                    "The selected work plan was not found.",
                    selectedIndividualIds.Count);
            }

            if (selectedPlan.OrganisationBusinessEntityId !=
                selectedDepartment.OrganisationStructureId)
            {
                return Failure(
                    "The selected work plan does not belong to the department's organisation.",
                    selectedIndividualIds.Count);
            }

            /*
             * Create one audit/operation log for this bulk operation.
             *
             * It is created using the same DbContext and transaction,
             * so it will roll back if assignment creation fails.
             */
            var operationLog =
                await _operationLogService.CreateAsync(
                    db,
                    actionId:
                        SharedConfig.WorkPlaningTypes.WORK_ASSIGNMENT_CREATE,

                    remarks:
                        $"Created work assignment " +
                        $"'{request.AssignmentName.Trim()}' for " +
                        $"{selectedIndividualIds.Count} selected employee(s) " +
                        $"in department '{selectedDepartment.Name}'.");

            var activeStaffRows = await
            (
                from job in db.Jobs.AsNoTracking()

                join individual in db.Individuals.AsNoTracking()
                    on job.IndividualID
                    equals individual.BusinessEntityId

                where selectedIndividualIds.Contains(
                          job.IndividualID)
                      && job.OrganisationStructureId ==
                         request.OrganisationStructureId
                      && !job.TerminatedDate.HasValue

                select new
                {
                    IndividualId =
                        job.IndividualID,

                    JobId =
                        job.JobId,

                    FirstName =
                        individual.FirstNameEnglish,

                    MiddleName =
                        individual.MiddleNameEnglish,

                    LastName =
                        individual.LastNameEnglish
                }
            )
            .ToListAsync(cancellationToken);

            var activeStaff =
                activeStaffRows
                    .GroupBy(x => x.IndividualId)
                    .Select(group =>
                        group
                            .OrderByDescending(x => x.JobId)
                            .First())
                    .ToDictionary(x => x.IndividualId);

            var assignmentName =
                request.AssignmentName.Trim();

            var existingOwnerIds = await
            (
                from existingWorkAssignment in
                    db.WorkAssignments.AsNoTracking()

                join existingOwner in
                        db.WorkAssignmentOwners.AsNoTracking()
                    on existingWorkAssignment.WorkAssignmentId
                    equals existingOwner.WorkAssignmentId

                where existingWorkAssignment.IsValid
                      && !existingWorkAssignment.CancelledDate.HasValue

                      && existingWorkAssignment.WorkPlanId ==
                         request.WorkPlanId!.Value

                      && existingWorkAssignment.Name ==
                         assignmentName

                      && existingWorkAssignment.StartDateTime ==
                         request.ScheduledStart

                      && existingWorkAssignment.EndDateTime ==
                         request.ScheduledEnd

                      && existingOwner.IsValid
                      && existingOwner.IsCurrentOwner
                      && !existingOwner.RelievedDate.HasValue

                      && selectedIndividualIds.Contains(
                          existingOwner.IndividualId)

                select existingOwner.IndividualId
            )
            .Distinct()
            .ToListAsync(cancellationToken);

            var duplicateIndividualIds =
                existingOwnerIds.ToHashSet();

            foreach (var individualId in selectedIndividualIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (!activeStaff.TryGetValue(
                        individualId,
                        out var staff))
                {
                    result.SkippedCount++;

                    result.Warnings.Add(
                        $"Individual {individualId} does not have " +
                        $"an active job in {selectedDepartment.Name}.");

                    continue;
                }

                if (duplicateIndividualIds.Contains(
                        individualId))
                {
                    result.SkippedCount++;

                    result.Warnings.Add(
                        $"{BuildFullName(
                            staff.FirstName,
                            staff.MiddleName,
                            staff.LastName)} already has this assignment.");

                    continue;
                }

                var newWorkAssignment =
                    new WorkAssignment
                    {
                        WorkPlanId =
                            request.WorkPlanId!.Value,

                        WorkTemplateId =(int)
                            selectedPlan.WorkTemplateId,

                        Name =
                            assignmentName,

                        Description =
                            request.Remarks,

                        StartDateTime =
                            request.ScheduledStart,

                        EndDateTime =
                            request.ScheduledEnd,

                        RequiresAttendance =
                            true,

                        RequiresCheckOut =
                            true,

                        Priority =
                            0,

                        CancelledDate =
                            null,

                        CancellationReason =
                            null,

                        CancelledByUserId =
                            null,

                        IsValid =
                            true,

                        OperationLogId =
                            operationLog.OperationLogId
                    };

                db.WorkAssignments.Add(
                    newWorkAssignment);

                var newOwner =
                    new WorkAssignmentOwner
                    {
                        WorkAssignment =
                            newWorkAssignment,

                        IndividualId =
                            staff.IndividualId,

                        JobId =
                            staff.JobId,

                        EffectiveFrom =
                            request.ScheduledStart,

                        EffectiveTo =
                            null,

                        RelievedDate =
                            null,

                        OwnershipType =
                            WorkOwnershipType.Original,

                        IsCurrentOwner =
                            true,

                        IsValid =
                            true,

                        OperationLogId =
                            operationLog.OperationLogId
                    };

                db.WorkAssignmentOwners.Add(
                    newOwner);

                result.AssignedCount++;

                duplicateIndividualIds.Add(
                    individualId);
            }

            if (result.AssignedCount == 0)
            {
                await transaction.RollbackAsync(
                    cancellationToken);

                result.Success = false;
                result.Message =
                    "No employees were assigned.";

                return result;
            }

            await db.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            result.Success = true;

            result.Message =
                result.SkippedCount > 0
                    ? $"{result.AssignedCount} employee(s) assigned and " +
                      $"{result.SkippedCount} employee(s) skipped."
                    : $"{result.AssignedCount} employee(s) assigned successfully.";

            return result;
        }
        catch (OperationCanceledException)
        {
            await transaction.RollbackAsync(
                CancellationToken.None);

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
                    "Failed to roll back the work-assignment transaction.");
            }

            _logger.LogError(
                exception,
                "Failed to create assignments for work plan {WorkPlanId}.",
                request.WorkPlanId);

            result.Success = false;

            result.Message =
                "The assignments could not be created. " +
                exception.GetBaseException().Message;

            return result;
        }
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