
using HRM.Models;
using HRM.Models.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Text;

namespace HRM.Services.Interfaces.AppovalWF
{
    public sealed class WorkflowDefinitionService
        : IWorkflowDefinitionService
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;

        public WorkflowDefinitionService(
            IDbContextFactory<HrmTeContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<WfWorkflowDefinition> ResolveAsync(
            int requestTypeId,
            int organisationBusinessEntityId,
            int? jobTypeId,
            DateOnly effectiveDate,
            CancellationToken cancellationToken = default)
        {
            if (requestTypeId <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(requestTypeId));

            if (organisationBusinessEntityId <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(organisationBusinessEntityId));

            if (jobTypeId.HasValue && jobTypeId.Value <= 0)
                throw new ArgumentOutOfRangeException(
                    nameof(jobTypeId));

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);





            //var debug = new StringBuilder();

            //var entityType = db.Model.FindEntityType(
            //    typeof(WfWorkflowDefinition));

            //if (entityType == null)
            //{
            //    throw new InvalidOperationException(
            //        "WfWorkflowDefinition is not registered in EF Core.");
            //}

            //var table = StoreObjectIdentifier.Table(
            //    entityType.GetTableName()!,
            //    entityType.GetSchema());

            //debug.AppendLine("========== ENTITY INFORMATION ==========");
            //debug.AppendLine($"Entity: {entityType.ClrType.Name}");
            //debug.AppendLine($"Table: {entityType.GetTableName()}");
            //debug.AppendLine($"Schema: {entityType.GetSchema()}");

            //debug.AppendLine();
            //debug.AppendLine("========== MAPPED PROPERTIES ==========");

            //foreach (var property in entityType.GetProperties())
            //{
            //    debug.AppendLine(
            //        $"Property: {property.Name}, " +
            //        $"Column: {property.GetColumnName(table)}, " +
            //        $"Shadow: {property.IsShadowProperty()}");
            //}

            //debug.AppendLine();
            //debug.AppendLine("========== FOREIGN KEYS ==========");

            //foreach (var fk in entityType.GetForeignKeys())
            //{
            //    debug.AppendLine(
            //        $"FK Properties: " +
            //        $"{string.Join(", ", fk.Properties.Select(p => p.Name))}");

            //    debug.AppendLine(
            //        $"Principal Entity: {fk.PrincipalEntityType.ClrType.Name}");

            //    debug.AppendLine();
            //}

            //debug.AppendLine();
            //debug.AppendLine("========== NAVIGATION PROPERTIES ==========");

            //foreach (var navigation in entityType.GetNavigations())
            //{
            //    debug.AppendLine(
            //        $"Navigation: {navigation.Name}, " +
            //        $"Target: {navigation.TargetEntityType.ClrType.Name}");
            //}

            //debug.AppendLine();
            //debug.AppendLine("========== GENERATED SQL ==========");

            //debug.AppendLine(
            //    db.WfWorkflowDefinitions
            //        .AsNoTracking()
            //        .ToQueryString());

            //debug.AppendLine();
            //debug.AppendLine("========== FULL QUERY SQL ==========");

            //debug.AppendLine(
            //    db.WfWorkflowDefinitions
            //        .AsNoTracking()
            //        .AsSplitQuery()
            //        .Include(x => x.WorkflowJobTypes)
            //        .Include(x => x.Stages)
            //        .Where(x =>
            //            x.IsActive &&
            //            x.RequestTypeId == requestTypeId &&
            //            (
            //                x.OrganisationBusinessEntityId ==
            //                    organisationBusinessEntityId ||
            //                x.OrganisationBusinessEntityId == null
            //            ) &&
            //            (
            //                x.EffectiveFrom == null ||
            //                x.EffectiveFrom <= effectiveDate
            //            ) &&
            //            (
            //                x.EffectiveTo == null ||
            //                x.EffectiveTo >= effectiveDate
            //            ))
            //        .ToQueryString());

            //// TEMPORARY: Show diagnostics in exception
            //throw new InvalidOperationException(
            //    debug.ToString());
























            // 1. Validate JobTypeID when provided.
            if (jobTypeId.HasValue)
            {
                bool exists = await db.JobTypes
                    .AsNoTracking()
                    .AnyAsync(
                        x => x.JobTypeId == jobTypeId.Value,
                        cancellationToken);

                if (!exists)
                {
                    throw new InvalidOperationException(
                        $"JobTypeID {jobTypeId.Value} does not exist.");
                }
            }

            // 2. Load only applicable active definitions.
            var definitions = await db.WfWorkflowDefinitions
                .AsNoTracking()
                .AsSplitQuery()
                .Include(x => x.WorkflowJobTypes)
                .Include(x => x.Stages)
                .Where(x =>
                    x.IsActive &&
                    x.LeaveTypeId == null && x.LeaveRequestKind == null &&
                    x.RequestTypeId == requestTypeId &&
                    (
                        x.OrganisationBusinessEntityId ==
                            organisationBusinessEntityId ||
                        x.OrganisationBusinessEntityId == null
                    ) &&
                    (
                        x.EffectiveFrom == null ||
                        x.EffectiveFrom <= effectiveDate
                    ) &&
                    (
                        x.EffectiveTo == null ||
                        x.EffectiveTo >= effectiveDate
                    ))
                .ToListAsync(cancellationToken);


















            if (definitions.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No active workflow found for RequestTypeID " +
                    $"{requestTypeId}, OrganisationID " +
                    $"{organisationBusinessEntityId}.");
            }

            // 3. Determine applicability and precedence.
            var candidates = definitions
                .Select(definition =>
                {
                    var mappings = definition.WorkflowJobTypes
                        .ToList();

                    // Generic means NO mappings at all.
                    // Inactive mappings must not make a
                    // restricted workflow generic.
                    bool isGeneric = mappings.Count == 0;

                    bool matchesJobType =
                        jobTypeId.HasValue &&
                        mappings.Any(m =>
                            m.IsActive &&
                            m.JobTypeId == jobTypeId.Value);

                    bool matchesOrganisation =
                        definition.OrganisationBusinessEntityId ==
                        organisationBusinessEntityId;

                    int priority = matchesOrganisation
                        ? (matchesJobType ? 4 : 3)
                        : (matchesJobType ? 2 : 1);

                    return new
                    {
                        Definition = definition,
                        IsGeneric = isGeneric,
                        MatchesJobType = matchesJobType,
                        Priority = priority
                    };
                })
                .Where(x =>
                    x.IsGeneric || x.MatchesJobType)
                .OrderByDescending(x => x.Priority)
                .ThenByDescending(x =>
                    x.Definition.VersionNumber)
                .ToList();

            if (candidates.Count == 0)
            {
                throw new InvalidOperationException(
                    $"No matching workflow for RequestTypeID " +
                    $"{requestTypeId}, OrganisationID " +
                    $"{organisationBusinessEntityId}, JobTypeID " +
                    $"{jobTypeId?.ToString() ?? "NULL"}.");
            }

            // 4. Select highest-priority workflow.
            var selected = candidates[0];

            // 5. Reject ambiguous configurations.
            var conflicts = candidates
                .Where(x =>
                    x.Priority == selected.Priority &&
                    x.Definition.VersionNumber ==
                        selected.Definition.VersionNumber)
                .ToList();

            if (conflicts.Count > 1)
            {
                var ids = string.Join(
                    ", ",
                    conflicts.Select(x =>
                        x.Definition.WorkflowDefinitionId));

                throw new InvalidOperationException(
                    "Ambiguous workflow configuration. " +
                    $"Matching WorkflowDefinitionIDs: {ids}");
            }

            var workflow = selected.Definition;

            // 6. Validate active stages.
            var stages = workflow.Stages
                .Where(x => x.IsActive)
                .OrderBy(x => x.StageOrder)
                .ToList();

            if (stages.Count == 0)
            {
                throw new InvalidOperationException(
                    $"WorkflowDefinitionID " +
                    $"{workflow.WorkflowDefinitionId} " +
                    "has no active stages.");
            }

            var duplicateOrders = stages
                .GroupBy(x => x.StageOrder)
                .Where(x => x.Count() > 1)
                .Select(x => x.Key)
                .ToList();

            if (duplicateOrders.Count > 0)
            {
                throw new InvalidOperationException(
                    $"WorkflowDefinitionID " +
                    $"{workflow.WorkflowDefinitionId} " +
                    "has duplicate stage orders: " +
                    string.Join(", ", duplicateOrders));
            }

            return workflow;
        }
    }
}
