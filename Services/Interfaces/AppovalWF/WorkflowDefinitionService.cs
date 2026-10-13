using HRM.Models;
using HRM.Models.Workflow;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Interfaces.AppovalWF
{

    public sealed class WorkflowDefinitionService : IWorkflowDefinitionService
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;

        public WorkflowDefinitionService(IDbContextFactory<HrmTeContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }


        public async Task<WfWorkflowDefinition> ResolveAsync(
              int requestTypeId,
              int organisationBusinessEntityId,
              int jobTypeId,
              DateOnly effectiveDate,
              CancellationToken cancellationToken = default)
        {
            if (requestTypeId <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(requestTypeId));

            if (organisationBusinessEntityId <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(organisationBusinessEntityId));

            if (jobTypeId <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(jobTypeId));

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            // -------------------------------------------------
            // 1. Validate JobType
            // -------------------------------------------------

            var jobTypeExists = await db.Set<JobType>()
                .AsNoTracking()
                .AnyAsync(x =>
                    (x.JobTypeId == jobTypeId && x.IsActive == true),
                    cancellationToken);

            if (!jobTypeExists)
            {
                throw new InvalidOperationException(
                    $"JobTypeID {jobTypeId} is invalid or inactive.");
            }

            // -------------------------------------------------
            // 2. Load eligible workflow definitions
            // -------------------------------------------------

            var date = effectiveDate.ToDateTime(TimeOnly.MinValue);

            // 1. Convert the DateTime parameter to DateOnly upfront
            var searchDate = DateOnly.FromDateTime(date);

            // 2. Execute the query using the matched parameter type
            var definitions = await db.Set<WfWorkflowDefinition>()
                .AsNoTracking()
                .Where(x =>
                    x.RequestTypeId == requestTypeId &&
                    x.IsActive &&
                    (
                        x.OrganisationBusinessEntityId == organisationBusinessEntityId ||
                        x.OrganisationBusinessEntityId == null
                    ) &&
                    (
                        x.EffectiveFrom == null || x.EffectiveFrom <= searchDate
                    ) &&
                    (
                        x.EffectiveTo == null || x.EffectiveTo >= searchDate
                    ))
                .ToListAsync(cancellationToken);




            if (definitions.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No active workflow found for " +
                    $"RequestTypeID {requestTypeId}.");
            }

            // -------------------------------------------------
            // 3. Load employment type mappings
            // -------------------------------------------------

            var definitionIds = definitions
                .Select(x => x.WorkflowDefinitionId)
                .ToList();

            var mappings = await db.Set<WfWorkflowJobType>()
                .AsNoTracking()
                .Where(x =>
                    definitionIds.Contains(x.WorkflowDefinitionId) &&
                    x.IsActive)
                .ToListAsync(cancellationToken);

            // -------------------------------------------------
            // 4. Select best matching workflow
            // -------------------------------------------------

            // Priority:
            // 1 = Organisation + JobType
            // 2 = Organisation + Default
            // 3 = Global + JobType
            // 4 = Global + Default

            var matches = definitions
                .Select(definition =>
                {
                    var definitionMappings = mappings
                        .Where(x =>
                            x.WorkflowDefinitionId ==
                                definition.WorkflowDefinitionId)
                        .ToList();

                    bool matchesJobType =
                        definitionMappings.Any(x =>
                            x.JobTypeId == jobTypeId);

                    bool isDefault =
                        definitionMappings.Count == 0;

                    bool isOrganisationSpecific =
                        definition.OrganisationBusinessEntityId ==
                            organisationBusinessEntityId;

                    int priority;

                    if (isOrganisationSpecific && matchesJobType)
                        priority = 1;
                    else if (isOrganisationSpecific && isDefault)
                        priority = 2;
                    else if (matchesJobType)
                        priority = 3;
                    else if (isDefault)
                        priority = 4;
                    else
                        priority = int.MaxValue;

                    return new
                    {
                        Definition = definition,
                        Priority = priority
                    };
                })
                .Where(x => x.Priority != int.MaxValue)
                .OrderBy(x => x.Priority)
                .ThenByDescending(x =>
                    x.Definition.VersionNumber)
                .ToList();

            if (matches.Count == 0)
            {
                throw new InvalidOperationException(
                    "No workflow matches the selected employment type.");
            }

            var selected = matches[0];

            // Detect ambiguous definitions at the same
            // priority and version.
            var conflict = matches.Skip(1).Any(x =>
                x.Priority == selected.Priority &&
                x.Definition.VersionNumber ==
                    selected.Definition.VersionNumber);

            if (conflict)
            {
                throw new InvalidOperationException(
                    "Multiple workflow definitions have the same " +
                    "priority and version. Review the configuration.");
            }

            // -------------------------------------------------
            // 5. Validate active stages
            // -------------------------------------------------

            var stages = await db.Set<WfWorkflowStage>()
                .AsNoTracking()
                .Where(x =>
                    x.WorkflowDefinitionId ==
                        selected.Definition.WorkflowDefinitionId &&
                    x.IsActive)
                .OrderBy(x => x.StageOrder)
                .ToListAsync(cancellationToken);

            if (stages.Count == 0)
            {
                throw new InvalidOperationException(
                    $"WorkflowDefinitionID " +
                    $"{selected.Definition.WorkflowDefinitionId} " +
                    "has no active stages.");
            }

            // Check duplicate stage order
            if (stages.GroupBy(x => x.StageOrder)
                .Any(g => g.Count() > 1))
            {
                throw new InvalidOperationException(
                    "Workflow contains duplicate StageOrder values.");
            }

            // -------------------------------------------------
            // 6. Return selected definition
            // -------------------------------------------------

            return selected.Definition;
        }


    }
}
