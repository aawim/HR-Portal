using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HRM.Application.Features.Payroll;
using HRM.Models;
using HRM.Models.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRM.Services.Payroll;

public sealed class PayrollProcessor : IPayrollProcessor
{
    private readonly IDbContextFactory<HrmTeContext> _factory;
    private readonly IPayrollCalculationEngine _engine;
    private readonly IPayrollSnapshotService _snapshotService;
    private readonly ILogger<PayrollProcessor> _logger;

    public PayrollProcessor(
        IDbContextFactory<HrmTeContext> factory,
        IPayrollCalculationEngine engine,
        IPayrollSnapshotService snapshotService,
        ILogger<PayrollProcessor> logger)
    {
        _factory = factory;
        _engine = engine;
        _snapshotService = snapshotService;
        _logger = logger;
    }

    public async Task ProcessRunBatchesAsync(int organisationId, int payrollRunId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var run = await db.PayrollRuns
            .Include(r => r.PayrollPeriod)
            .SingleOrDefaultAsync(r => r.PayrollRunId == payrollRunId && r.OrganisationId == organisationId, cancellationToken);

        if (run == null)
        {
            _logger.LogError("Payroll run {RunId} for organisation {OrgId} not found", payrollRunId, organisationId);
            return;
        }

        if (run.PayrollPeriod == null)
        {
            _logger.LogError("Payroll period for run {RunId} not found", payrollRunId);
            return;
        }

        run.Status = PayrollRunStatus.Calculating;
        await db.SaveChangesAsync(cancellationToken);

        try
        {
            // 1. Get or create snapshot items
            var snapshotItems = await _snapshotService.CreateOrGetRunSnapshotsAsync(
                organisationId, payrollRunId, run.PayrollPeriod.PeriodStartDate, run.PayrollPeriod.PeriodEndDate);

            if (snapshotItems.Count == 0)
            {
                run.Status = PayrollRunStatus.ValidationFailed;
                run.Description = "No eligible employees found for this payroll run.";
                await db.SaveChangesAsync(cancellationToken);
                return;
            }

            // 2. Identify batches (100 per batch)
            const int batchSize = 100;
            var batches = snapshotItems
                .Select((item, index) => new { item, index })
                .GroupBy(x => x.index / batchSize)
                .Select(g => g.Select(x => x.item).ToList())
                .ToList();

            run.TotalEmployees = snapshotItems.Count;
            await db.SaveChangesAsync(cancellationToken);

            // Synchronize batch records
            var existingBatches = await db.PayrollRunBatches
                .Where(b => b.PayrollRunId == payrollRunId)
                .ToListAsync(cancellationToken);

            var extraBatches = existingBatches.Where(b => b.BatchNumber > batches.Count).ToList();
            if (extraBatches.Count > 0)
            {
                db.PayrollRunBatches.RemoveRange(extraBatches);
                existingBatches.RemoveAll(b => b.BatchNumber > batches.Count);
            }

            var batchDict = existingBatches.ToDictionary(b => b.BatchNumber);
            for (int i = 0; i < batches.Count; i++)
            {
                int batchNum = i + 1;
                if (!batchDict.TryGetValue(batchNum, out var batch))
                {
                    var newBatch = new PayrollRunBatch
                    {
                        PayrollRunId = payrollRunId,
                        BatchNumber = batchNum,
                        TotalEmployees = batches[i].Count,
                        Status = PayrollBatchStatus.Pending
                    };
                    db.PayrollRunBatches.Add(newBatch);
                }
                else
                {
                    batch.TotalEmployees = batches[i].Count;
                }
            }
            await db.SaveChangesAsync(cancellationToken);

            // 3. Process each batch
            var updatedBatches = await db.PayrollRunBatches
                .Where(b => b.PayrollRunId == payrollRunId)
                .OrderBy(b => b.BatchNumber)
                .ToListAsync(cancellationToken);

            foreach (var batch in updatedBatches)
            {
                if (batch.Status == PayrollBatchStatus.Completed)
                {
                    continue; // idempotent resume
                }

                int batchIndex = batch.BatchNumber - 1;
                if (batchIndex >= batches.Count) continue;
                var batchEmployees = batches[batchIndex];

                await ProcessBatchInternalAsync(db, run, batch, batchEmployees, cancellationToken);
            }

            // 4. Update overall Run Totals
            var calculatedEmployees = await db.PayrollEmployees
                .Where(e => e.PayrollRunId == payrollRunId)
                .ToListAsync(cancellationToken);

            run.ProcessedEmployees = calculatedEmployees.Count;
            run.TotalGrossEarnings = calculatedEmployees.Sum(e => e.GrossEarnings);
            run.TotalDeductions = calculatedEmployees.Sum(e => e.TotalDeductions);
            run.TotalEmployerContributions = calculatedEmployees.Sum(e => e.EmployerContributions);
            run.TotalNetPayable = calculatedEmployees.Sum(e => e.NetSalary);

            var attendanceAdjustments = await db.AttendanceAdjustments
                .Where(a => a.PayrollRunId == payrollRunId && a.OrganisationId == organisationId && a.PayrollProcessingStatus == "Ingested")
                .ToListAsync(cancellationToken);
            foreach (var adjustment in attendanceAdjustments)
            {
                adjustment.PayrollProcessingStatus = "Calculated";
                adjustment.UpdatedAtUtc = DateTime.UtcNow;
            }

            bool hasErrors = await db.PayrollExceptions.AnyAsync(e => e.PayrollRunId == payrollRunId && e.Severity == PayrollIssueSeverity.Blocking, cancellationToken);
            run.Status = hasErrors ? PayrollRunStatus.ValidationFailed : PayrollRunStatus.Calculated;

            await db.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Completed payroll calculation for Run {RunId}. Status: {Status}, Processed: {Count}/{Total}",
                payrollRunId, run.Status, run.ProcessedEmployees, run.TotalEmployees);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to calculate payroll run {RunId}", payrollRunId);
            run.Status = PayrollRunStatus.Failed;
            run.Description = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task ProcessBatchAsync(int organisationId, int payrollRunId, int batchId, CancellationToken cancellationToken = default)
    {
        await using var db = await _factory.CreateDbContextAsync(cancellationToken);
        var run = await db.PayrollRuns.Include(r => r.PayrollPeriod).SingleOrDefaultAsync(r => r.PayrollRunId == payrollRunId && r.OrganisationId == organisationId, cancellationToken);
        var batch = await db.PayrollRunBatches.SingleOrDefaultAsync(b => b.PayrollRunBatchId == batchId, cancellationToken);

        if (run == null || batch == null || run.PayrollPeriod == null) return;

        var snapshots = await _snapshotService.CreateOrGetRunSnapshotsAsync(organisationId, payrollRunId, run.PayrollPeriod.PeriodStartDate, run.PayrollPeriod.PeriodEndDate);
        const int batchSize = 100;
        var batchItems = snapshots.Skip((batch.BatchNumber - 1) * batchSize).Take(batchSize).ToList();

        await ProcessBatchInternalAsync(db, run, batch, batchItems, cancellationToken);
    }

    private async Task ProcessBatchInternalAsync(
        HrmTeContext db,
        PayrollRun run,
        PayrollRunBatch batch,
        List<EmployeePayrollInputItem> employees,
        CancellationToken cancellationToken)
    {
        batch.Status = PayrollBatchStatus.Processing;
        batch.StartedAtUtc = DateTime.UtcNow;
        batch.ErrorMessage = null;
        await db.SaveChangesAsync(cancellationToken);

        // Load existing processed employee individual IDs for idempotency
        var existingProcessedIndividualIds = (await db.PayrollEmployees
            .Where(e => e.PayrollRunId == run.PayrollRunId)
            .Select(e => e.IndividualId)
            .ToListAsync(cancellationToken))
            .ToHashSet();

        var newEmployees = new List<PayrollEmployee>();
        var newExceptions = new List<PayrollException>();

        // Ensure distinct individuals in the batch to avoid duplicates
        var distinctEmployees = employees
            .GroupBy(e => e.IndividualId)
            .Select(g => g.First())
            .ToList();

        // Load active organization calculation rules (Pension %, Overtime Multiplier, etc.)
        var rules = await db.CalculationRules
            .AsNoTracking()
            .Where(r => r.OrganisationId == run.OrganisationId && r.IsActive)
            .ToListAsync(cancellationToken);

        var eePensionRule = rules.FirstOrDefault(r => r.RuleCode == "PENSION_EE");
        var erPensionRule = rules.FirstOrDefault(r => r.RuleCode == "PENSION_ER");
        decimal eePensionRate = eePensionRule != null && eePensionRule.PercentageValue >= 0 ? eePensionRule.PercentageValue / 100m : 0.07m;
        decimal erPensionRate = erPensionRule != null && erPensionRule.PercentageValue >= 0 ? erPensionRule.PercentageValue / 100m : 0.07m;

        try
        {
            foreach (var empInput in distinctEmployees)
            {
                if (existingProcessedIndividualIds.Contains(empInput.IndividualId))
                {
                    continue; // Skip already calculated
                }
                existingProcessedIndividualIds.Add(empInput.IndividualId);

                var outcome = _engine.CalculateEmployee(
                    empInput,
                    run.PayrollPeriod!.PeriodStartDate,
                    run.PayrollPeriod.PeriodEndDate,
                    employeePensionRate: eePensionRate,
                    employerPensionRate: erPensionRate
                );

                var empEntity = new PayrollEmployee
                {
                    PayrollRunId = run.PayrollRunId,
                    PayrollRunBatchId = batch.PayrollRunBatchId,
                    JobId = empInput.JobId,
                    IndividualId = empInput.IndividualId,
                    EmployeeNumber = empInput.EmployeeNumber,
                    EmployeeName = empInput.EmployeeName,
                    DepartmentName = empInput.DepartmentName,
                    PositionTitle = empInput.PositionTitle,
                    GradeCode = empInput.GradeCode,
                    StepNumber = empInput.StepNumber,
                    BaseSalary = outcome.BaseSalary,
                    GrossEarnings = outcome.GrossEarnings,
                    TotalDeductions = outcome.TotalDeductions,
                    EmployerContributions = outcome.EmployerContributions,
                    NetSalary = outcome.NetSalary,
                    CalculationStatus = outcome.Errors.Count > 0 ? "Error" : (outcome.Warnings.Count > 0 ? "Warning" : "Calculated"),
                    ErrorMessage = outcome.Errors.FirstOrDefault(),
                    CreatedAtUtc = DateTime.UtcNow
                };

                foreach (var line in outcome.Lines)
                {
                    empEntity.Lines.Add(new PayrollLine
                    {
                        PayrollRunId = run.PayrollRunId,
                        ComponentCode = line.ComponentCode,
                        ComponentName = line.ComponentName,
                        ComponentType = line.ComponentType,
                        Rate = line.Rate,
                        Units = line.Units,
                        CalculatedAmount = line.CalculatedAmount,
                        IsTaxable = line.IsTaxable,
                        IsPensionable = line.IsPensionable,
                        SourceReference = line.SourceReference,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }

                newEmployees.Add(empEntity);

                // Register exceptions
                foreach (var err in outcome.Errors)
                {
                    newExceptions.Add(new PayrollException
                    {
                        PayrollRunId = run.PayrollRunId,
                        JobId = empInput.JobId,
                        IndividualId = empInput.IndividualId,
                        Severity = PayrollIssueSeverity.Blocking,
                        Code = "CALC_ERROR",
                        Message = err,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }

                foreach (var warn in outcome.Warnings)
                {
                    newExceptions.Add(new PayrollException
                    {
                        PayrollRunId = run.PayrollRunId,
                        JobId = empInput.JobId,
                        IndividualId = empInput.IndividualId,
                        Severity = PayrollIssueSeverity.Warning,
                        Code = "CALC_WARNING",
                        Message = warn,
                        CreatedAtUtc = DateTime.UtcNow
                    });
                }
            }

            db.PayrollEmployees.AddRange(newEmployees);
            db.PayrollExceptions.AddRange(newExceptions);

            batch.Status = PayrollBatchStatus.Completed;
            batch.CompletedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            batch.Status = PayrollBatchStatus.Failed;
            batch.ErrorMessage = ex.Message.Length > 1000 ? ex.Message[..1000] : ex.Message;
            batch.RetryCount += 1;
            await db.SaveChangesAsync(CancellationToken.None);
            throw;
        }
    }
}
