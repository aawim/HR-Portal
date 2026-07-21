
using HRM.Models;
using HRM.WorkPlanning.Abstractions;
using HRM.WorkPlanning.Commands;
using HRM.WorkPlanning.Results;
using Microsoft.EntityFrameworkCore;


namespace HRM.WorkPlanning.Services
{
    public class WorkPlanningService : IWorkPlanningService
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IWorkPlanBuilder _workPlanBuilder;
        private readonly IWorkPlanValidator _validator;

        public WorkPlanningService(
            IDbContextFactory<HrmTeContext> dbFactory,
            IWorkPlanBuilder workPlanBuilder,
            IWorkPlanValidator validator)
        {
            _dbFactory = dbFactory;
            _workPlanBuilder = workPlanBuilder;
            _validator = validator;
        }
        public async Task<WorkPlanBuildResult> BuildAndSaveAsync(
       BuildWorkPlanCommand command,
       CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);

            var commandValidation =
                await _validator.ValidateCommandAsync(
                    command,
                    cancellationToken);

            if (!commandValidation.IsValid)
            {
                return WorkPlanBuildResult.Failure(
                    commandValidation.Errors);
            }

            var buildResult =
                await _workPlanBuilder.BuildAsync(
                    command,
                    cancellationToken);

            if (!buildResult.IsSuccessful ||
                buildResult.WorkPlan is null)
            {
                return buildResult;
            }

            var planValidation =
                _validator.ValidateBuiltPlan(
                    buildResult.WorkPlan);

            if (!planValidation.IsValid)
            {
                return WorkPlanBuildResult.Failure(
                    planValidation.Errors);
            }

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                db.WorkPlans.Add(buildResult.WorkPlan);

                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return WorkPlanBuildResult.Success(
                    buildResult.WorkPlan);
            }
            catch (DbUpdateException)
            {
                await transaction.RollbackAsync(cancellationToken);

                return WorkPlanBuildResult.Failure(
                    "The work plan could not be saved. " +
                    "A plan may already exist for this job and date.");
            }
        }

    }
}
