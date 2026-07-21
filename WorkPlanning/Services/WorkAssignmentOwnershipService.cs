using HRM.DTOs.WorkPlanning;
using HRM.Enum;
using HRM.Models.WorkPlanning;
using HRM.Models;
using HRM.WorkPlanning.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HRM.WorkPlanning.Services
{
    public sealed class WorkAssignmentOwnershipService : IWorkAssignmentOwnershipService
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly ILogger<WorkAssignmentOwnershipService> _logger;

        public WorkAssignmentOwnershipService(
            IDbContextFactory<HrmTeContext> dbFactory,
            ILogger<WorkAssignmentOwnershipService> logger)
        {
            _dbFactory = dbFactory;
            _logger = logger;
        }

        //public async Task<WorkAssignmentOwnershipResult> AssignAsync(
        //    AssignWorkAssignmentRequest request,
        //    CancellationToken cancellationToken = default)
        //{
        //    if (request is null)
        //    {
        //        return WorkAssignmentOwnershipResult.Failed(
        //            "The work-assignment ownership request is required.");
        //    }

        //    var validationMessage = ValidateAssignRequest(request);

        //    if (validationMessage is not null)
        //    {
        //        return WorkAssignmentOwnershipResult.Failed(
        //            validationMessage);
        //    }

        //    await using var db =
        //        await _dbFactory.CreateDbContextAsync(
        //            cancellationToken);

        //    await using var transaction =
        //        await db.Database.BeginTransactionAsync(
        //            cancellationToken);

        //    try
        //    {
        //        var assignment = await db.WorkAssignments
        //            .SingleOrDefaultAsync(
        //                x =>
        //                    x.WorkAssignmentId ==
        //                    request.WorkAssignmentId &&
        //                    x.IsValid &&
        //                    !x.CancelledDate.HasValue,
        //                cancellationToken);

        //        if (assignment is null)
        //        {
        //            return WorkAssignmentOwnershipResult.Failed(
        //                $"Active work assignment ID " +
        //                $"{request.WorkAssignmentId} was not found.");
        //        }

        //        var individualExists = await db.Individuals
        //            .AsNoTracking()
        //            .AnyAsync(
        //                x =>
        //                    x.BusinessEntityId ==
        //                    request.IndividualId,
        //                cancellationToken);

        //        if (!individualExists)
        //        {
        //            return WorkAssignmentOwnershipResult.Failed(
        //                $"Individual ID {request.IndividualId} " +
        //                $"was not found.");
        //        }

        //        var job = await db.Jobs
        //            .AsNoTracking()
        //            .Where(x =>
        //                x.JobId == request.JobId &&
        //                x.IndividualID == request.IndividualId)
        //            .Select(x => new
        //            {
        //                x.JobId,
        //                x.IndividualID,
        //                x.OrganisationID,
        //                x.JobStateId,
        //                x.TerminatedDate
        //            })
        //            .SingleOrDefaultAsync(cancellationToken);

        //        if (job is null)
        //        {
        //            return WorkAssignmentOwnershipResult.Failed(
        //                "The selected job does not belong to the " +
        //                "selected individual.");
        //        }

        //        if (job.TerminatedDate.HasValue)
        //        {
        //            return WorkAssignmentOwnershipResult.Failed(
        //                "The selected job has already been terminated.");
        //        }

        //        /*
        //         * Ownership should not begin after the assignment ends.
        //         */
        //        if (request.EffectiveFrom >
        //            assignment.EndDateTime)
        //        {
        //            return WorkAssignmentOwnershipResult.Failed(
        //                $"Ownership cannot begin after the assignment " +
        //                $"ends on " +
        //                $"{assignment.EndDateTime:dd MMM yyyy HH:mm}.");
        //        }

        //        /*
        //         * If ownership has an explicit end, it should not begin
        //         * before the assignment starts or extend beyond the
        //         * assignment unless your business rules intentionally
        //         * allow that.
        //         */
        //        if (request.EffectiveTo.HasValue &&
        //            request.EffectiveTo.Value <
        //            assignment.StartDateTime)
        //        {
        //            return WorkAssignmentOwnershipResult.Failed(
        //                "The ownership period ends before the work " +
        //                "assignment begins.");
        //        }

        //        /*
        //         * Prevent another current owner from being created.
        //         *
        //         * A normal Assign operation is only for an assignment
        //         * that currently has no active owner. TransferAsync will
        //         * close the existing owner and create the replacement.
        //         */
        //        var currentOwner = await db.WorkAssignmentOwners
        //            .AsNoTracking()
        //            .Where(x =>
        //                x.WorkAssignmentId ==
        //                    request.WorkAssignmentId &&
        //                x.IsValid &&
        //                x.IsCurrentOwner &&
        //                !x.RelievedDate.HasValue)
        //            .Select(x => new
        //            {
        //                x.WorkAssignmentOwnerId,
        //                x.IndividualId,
        //                x.JobId,
        //                x.EffectiveFrom,
        //                x.EffectiveTo
        //            })
        //            .FirstOrDefaultAsync(cancellationToken);

        //        if (currentOwner is not null)
        //        {
        //            return WorkAssignmentOwnershipResult.Failed(
        //                $"Work assignment ID " +
        //                $"{request.WorkAssignmentId} already has a " +
        //                $"current owner, individual ID " +
        //                $"{currentOwner.IndividualId}. Use the transfer " +
        //                $"operation to change ownership.");
        //        }

        //        /*
        //         * Prevent duplicate or overlapping ownership records
        //         * for the same individual and assignment.
        //         */


        //        var duplicateOwnershipExists =
        //        await db.WorkAssignmentOwners
        //            .AsNoTracking()
        //            .AnyAsync(
        //                x =>
        //                    x.WorkAssignmentId ==
        //                        request.WorkAssignmentId &&
        //                    x.IndividualId ==
        //                        request.IndividualId &&
        //                    x.JobId ==
        //                        request.JobId &&
        //                    x.IsValid &&
        //                    x.EffectiveFrom < request.EffectiveTo &&
        //                    request.EffectiveFrom <
        //                        (x.EffectiveTo ?? DateTime.MaxValue),
        //                cancellationToken);



        //        if (duplicateOwnershipExists)
        //        {
        //            return WorkAssignmentOwnershipResult.Failed(
        //                "An overlapping ownership record already exists " +
        //                "for this employee, job and work assignment.");
        //        }

        //        var owner = new WorkAssignmentOwner
        //        {
        //            WorkAssignmentId =
        //                assignment.WorkAssignmentId,

        //            IndividualId =
        //                request.IndividualId,

        //            JobId =
        //                request.JobId,

        //            EffectiveFrom =
        //                request.EffectiveFrom,

        //            EffectiveTo =
        //                request.EffectiveTo,

        //            RelievedDate =
        //                null,

        //            OwnershipType =
        //                WorkOwnershipType.Original,

        //            IsCurrentOwner =
        //                true,

        //            IsValid =
        //                true,

        //            OperationLogId =
        //                request.OperationLogId
        //        };

        //        db.WorkAssignmentOwners.Add(owner);

        //        await db.SaveChangesAsync(cancellationToken);

        //        await transaction.CommitAsync(cancellationToken);

        //        return new WorkAssignmentOwnershipResult
        //        {
        //            Success = true,

        //            Message =
        //                $"Work assignment '{assignment.Name}' was " +
        //                $"assigned successfully to individual " +
        //                $"{request.IndividualId}.",

        //            WorkAssignmentId =
        //                assignment.WorkAssignmentId,

        //            WorkAssignmentOwnerId =
        //                owner.WorkAssignmentOwnerId,

        //            IndividualId =
        //                owner.IndividualId,

        //            JobId =
        //                owner.JobId,

        //            EffectiveFrom =
        //                owner.EffectiveFrom,

        //            EffectiveTo =
        //                owner.EffectiveTo
        //        };
        //    }
        //    catch (OperationCanceledException)
        //    {
        //        await transaction.RollbackAsync(
        //            CancellationToken.None);

        //        throw;
        //    }
        //    catch (Exception exception)
        //    {
        //        try
        //        {
        //            await transaction.RollbackAsync(
        //                CancellationToken.None);
        //        }
        //        catch (Exception rollbackException)
        //        {
        //            _logger.LogError(
        //                rollbackException,
        //                "Failed to roll back work-assignment ownership transaction.");
        //        }

        //        _logger.LogError(
        //            exception,
        //            """
        //        Failed to assign work-assignment ownership.
        //        WorkAssignmentId: {WorkAssignmentId}
        //        IndividualId: {IndividualId}
        //        JobId: {JobId}
        //        EffectiveFrom: {EffectiveFrom}
        //        """,
        //            request.WorkAssignmentId,
        //            request.IndividualId,
        //            request.JobId,
        //            request.EffectiveFrom);

        //        var baseException =
        //            exception.GetBaseException();

        //        return WorkAssignmentOwnershipResult.Failed(
        //            $"{baseException.GetType().Name}: " +
        //            $"{baseException.Message}");
        //    }
        //}

        //private static string? ValidateAssignRequest(
        //    AssignWorkAssignmentRequest request)
        //{
        //    if (request.WorkAssignmentId <= 0)
        //    {
        //        return "A valid work assignment is required.";
        //    }

        //    if (request.IndividualId <= 0)
        //    {
        //        return "A valid individual is required.";
        //    }

        //    if (request.JobId <= 0)
        //    {
        //        return "A valid job is required.";
        //    }

        //    if (request.OperationLogId <= 0)
        //    {
        //        return "A valid operation log is required.";
        //    }

        //    if (request.EffectiveFrom == default)
        //    {
        //        return "A valid ownership effective date is required.";
        //    }

        //    if (request.EffectiveTo.HasValue &&
        //        request.EffectiveTo.Value <=
        //        request.EffectiveFrom)
        //    {
        //        return "The ownership effective-to date must be later " +
        //               "than the effective-from date.";
        //    }

        //    return null;
        //}

        //private static bool PeriodsOverlap(
        //    DateTime existingFrom,
        //    DateTime? existingTo,
        //    DateTime requestedFrom,
        //    DateTime? requestedTo)
        //{
        //    var existingEnd =
        //        existingTo ?? DateTime.MaxValue;

        //    var requestedEnd =
        //        requestedTo ?? DateTime.MaxValue;

        //    return existingFrom < requestedEnd &&
        //           requestedFrom < existingEnd;
        //}


        public async Task<WorkAssignmentOwnershipResult> AssignAsync(
       AssignWorkAssignmentRequest request,
       CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                return WorkAssignmentOwnershipResult.Failed(
                    "The assignment ownership request is required.");
            }

            var validationMessage =
                ValidateAssignRequest(request);

            if (validationMessage is not null)
            {
                return WorkAssignmentOwnershipResult.Failed(
                    validationMessage,
                    request.WorkAssignmentId);
            }

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                var assignment =
                    await LoadActiveAssignmentAsync(
                        db,
                        request.WorkAssignmentId,
                        cancellationToken);

                if (assignment is null)
                {
                    return WorkAssignmentOwnershipResult.Failed(
                        $"Active work assignment ID " +
                        $"{request.WorkAssignmentId} was not found.",
                        request.WorkAssignmentId);
                }

                var jobValidation =
                    await ValidateJobAsync(
                        db,
                        request.IndividualId,
                        request.JobId,
                        cancellationToken);

                if (jobValidation is not null)
                {
                    return WorkAssignmentOwnershipResult.Failed(
                        jobValidation,
                        request.WorkAssignmentId);
                }

                var periodValidation =
                    ValidateOwnershipPeriod(
                        assignment,
                        request.EffectiveFrom,
                        request.EffectiveTo);

                if (periodValidation is not null)
                {
                    return WorkAssignmentOwnershipResult.Failed(
                        periodValidation,
                        request.WorkAssignmentId);
                }

                var currentOwner =
                    await GetCurrentOwnerAsync(
                        db,
                        request.WorkAssignmentId,
                        cancellationToken);

                if (currentOwner is not null)
                {
                    return WorkAssignmentOwnershipResult.Failed(
                        $"Work assignment '{assignment.Name}' already " +
                        $"has a current owner, individual ID " +
                        $"{currentOwner.IndividualId}. Use the transfer " +
                        $"operation to change ownership.",
                        assignment.WorkAssignmentId);
                }

                var overlapExists =
                    await OwnershipOverlapExistsAsync(
                        db,
                        request.WorkAssignmentId,
                        request.IndividualId,
                        request.JobId,
                        request.EffectiveFrom,
                        request.EffectiveTo,
                        excludedOwnerId: null,
                        cancellationToken);

                if (overlapExists)
                {
                    return WorkAssignmentOwnershipResult.Failed(
                        "An overlapping ownership record already exists " +
                        "for the selected employee and job.",
                        assignment.WorkAssignmentId);
                }

                var owner = new WorkAssignmentOwner
                {
                    WorkAssignmentId =
                        assignment.WorkAssignmentId,

                    IndividualId =
                        request.IndividualId,

                    JobId =
                        request.JobId,

                    EffectiveFrom =
                        request.EffectiveFrom,

                    EffectiveTo =
                        request.EffectiveTo,

                    RelievedDate =
                        null,

                    OwnershipType =
                        WorkOwnershipType.Original,

                    IsCurrentOwner =
                        true,

                    IsValid =
                        true,

                    OperationLogId =
                        request.OperationLogId
                };

                db.WorkAssignmentOwners.Add(owner);

                await db.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return new WorkAssignmentOwnershipResult
                {
                    Success = true,

                    Message =
                        $"Work assignment '{assignment.Name}' was " +
                        $"assigned successfully to individual " +
                        $"{owner.IndividualId}.",

                    WorkAssignmentId =
                        assignment.WorkAssignmentId,

                    WorkAssignmentOwnerId =
                        owner.WorkAssignmentOwnerId,

                    IndividualId =
                        owner.IndividualId,

                    JobId =
                        owner.JobId,

                    EffectiveFrom =
                        owner.EffectiveFrom,

                    EffectiveTo =
                        owner.EffectiveTo
                };
            }
            catch (OperationCanceledException)
            {
                await RollbackSafelyAsync(transaction);

                throw;
            }
            catch (Exception exception)
            {
                await RollbackSafelyAsync(transaction);

                LogOwnershipError(
                    exception,
                    "assign",
                    request.WorkAssignmentId,
                    request.IndividualId,
                    request.JobId);

                return FailureFromException(
                    exception,
                    request.WorkAssignmentId);
            }
        }

        public async Task<WorkAssignmentOwnershipResult> TransferAsync(
            TransferWorkAssignmentRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                return WorkAssignmentOwnershipResult.Failed(
                    "The transfer request is required.");
            }

            var validationMessage =
                ValidateTransferRequest(request);

            if (validationMessage is not null)
            {
                return WorkAssignmentOwnershipResult.Failed(
                    validationMessage,
                    request.WorkAssignmentId);
            }

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                var assignment =
                    await LoadActiveAssignmentAsync(
                        db,
                        request.WorkAssignmentId,
                        cancellationToken);

                if (assignment is null)
                {
                    return WorkAssignmentOwnershipResult.Failed(
                        $"Active work assignment ID " +
                        $"{request.WorkAssignmentId} was not found.",
                        request.WorkAssignmentId);
                }

                var currentOwner =
                    await db.WorkAssignmentOwners
                        .SingleOrDefaultAsync(
                            x =>
                                x.WorkAssignmentId ==
                                    request.WorkAssignmentId &&
                                x.IsValid &&
                                x.IsCurrentOwner &&
                                !x.RelievedDate.HasValue,
                            cancellationToken);


                if (currentOwner is null)
                {
                    return WorkAssignmentOwnershipResult.Failed(
                        "This work assignment does not currently have " +
                        "an owner to transfer.",
                        assignment.WorkAssignmentId);
                }

                if (currentOwner.IndividualId !=
                        request.FromIndividualId ||
                    currentOwner.JobId !=
                        request.FromJobId)
                {
                    return WorkAssignmentOwnershipResult.Failed(
                        $"The current owner is individual ID " +
                        $"{currentOwner.IndividualId}, job ID " +
                        $"{currentOwner.JobId}. The supplied source " +
                        $"owner does not match.",
                        assignment.WorkAssignmentId);
                }

                if (request.TransferDateTime <
                    currentOwner.EffectiveFrom)
                {
                    return WorkAssignmentOwnershipResult.Failed(
                        "The transfer date cannot be before the current " +
                        "ownership start date.",
                        assignment.WorkAssignmentId);
                }

                if (request.TransferDateTime <
                        assignment.StartDateTime ||
                    request.TransferDateTime >
                        assignment.EndDateTime)
                {
                    return WorkAssignmentOwnershipResult.Failed(
                        $"The transfer date must be inside the assignment " +
                        $"period: {assignment.StartDateTime:dd MMM yyyy HH:mm} " +
                        $"to {assignment.EndDateTime:dd MMM yyyy HH:mm}.",
                        assignment.WorkAssignmentId);
                }

                var destinationJobValidation =
                    await ValidateJobAsync(
                        db,
                        request.ToIndividualId,
                        request.ToJobId,
                        cancellationToken);

                if (destinationJobValidation is not null)
                {
                    return WorkAssignmentOwnershipResult.Failed(
                        destinationJobValidation,
                        assignment.WorkAssignmentId);
                }

                var overlapExists =
                    await OwnershipOverlapExistsAsync(
                        db,
                        request.WorkAssignmentId,
                        request.ToIndividualId,
                        request.ToJobId,
                        request.TransferDateTime,
                        null,
                        currentOwner.WorkAssignmentOwnerId,
                        cancellationToken);

                if (overlapExists)
                {
                    return WorkAssignmentOwnershipResult.Failed(
                        "The destination employee already has an " +
                        "overlapping ownership record for this assignment.",
                        assignment.WorkAssignmentId);
                }

                currentOwner.IsCurrentOwner = false;

                currentOwner.EffectiveTo = request.TransferDateTime;

                currentOwner.RelievedDate = request.TransferDateTime;

                var newOwner = new WorkAssignmentOwner
                {
                    WorkAssignmentId =
                        assignment.WorkAssignmentId,

                    IndividualId =
                        request.ToIndividualId,

                    JobId =
                        request.ToJobId,

                    EffectiveFrom =
                        request.TransferDateTime,

                    EffectiveTo =
                        null,

                    RelievedDate =
                        null,

                    OwnershipType =
                        WorkOwnershipType.Transfer,

                    IsCurrentOwner =
                        true,

                    IsValid =
                        true,

                    OperationLogId =
                        request.OperationLogId
                };

                db.WorkAssignmentOwners.Add(newOwner);
                await db.SaveChangesAsync(cancellationToken);

               

                var transfer = new WorkAssignmentTransfer
                {
                    WorkAssignmentId = assignment.WorkAssignmentId,

                    FromWorkAssignmentOwnerId =
                        currentOwner.WorkAssignmentOwnerId,

                                    ToWorkAssignmentOwnerId =
                        newOwner.WorkAssignmentOwnerId,

                                    FromIndividualId =
                        currentOwner.IndividualId,

                                    FromJobId =
                        currentOwner.JobId,

                                    ToIndividualId =
                        newOwner.IndividualId,

                                    ToJobId =
                        newOwner.JobId,

                    EffectiveFrom = request.TransferDateTime,

                                    Reason =
                        request.Reason,

                    ApprovedByUserId =
                        request.TransferredByUserId,

                                    OperationLogId =
                        request.OperationLogId,

                                    IsValid = true,

                      WorkAssignmentTransferStateId = 2,
                };

                db.WorkAssignmentTransfers.Add(transfer);

                await db.SaveChangesAsync(cancellationToken);

   
                await transaction.CommitAsync(cancellationToken);

                return new WorkAssignmentOwnershipResult
                {
                    Success = true,
                    Message = $"Work assignment '{assignment.Name}' was transferred successfully.",

                    WorkAssignmentId = assignment.WorkAssignmentId,
                    WorkAssignmentTransferId =
                    transfer.WorkAssignmentTransferId,

                                PreviousWorkAssignmentOwnerId =
                    currentOwner.WorkAssignmentOwnerId,

                                WorkAssignmentOwnerId =
                    newOwner.WorkAssignmentOwnerId,

                                IndividualId =
                    newOwner.IndividualId,

                                JobId =
                    newOwner.JobId,

                                EffectiveFrom =
                    newOwner.EffectiveFrom
                            };

                //return new WorkAssignmentOwnershipResult
                //{
                //    Success = true,

                //    Message =
                //        $"Work assignment '{assignment.Name}' was " +
                //        $"transferred from individual " +
                //        $"{currentOwner.IndividualId} to individual " +
                //        $"{newOwner.IndividualId} effective " +
                //        $"{request.TransferDateTime:dd MMM yyyy HH:mm}.",

                //    WorkAssignmentId =
                //        assignment.WorkAssignmentId,

                //    PreviousWorkAssignmentOwnerId = currentOwner.WorkAssignmentOwnerId,

                //    WorkAssignmentOwnerId =
                //        newOwner.WorkAssignmentOwnerId,

                //    IndividualId =
                //        newOwner.IndividualId,

                //    JobId =
                //        newOwner.JobId,

                //    EffectiveFrom =
                //        newOwner.EffectiveFrom,

                //    EffectiveTo =
                //        newOwner.EffectiveTo
                //};
            }
            catch (OperationCanceledException)
            {
                await RollbackSafelyAsync(transaction);

                throw;
            }
            catch (Exception exception)
            {
                await RollbackSafelyAsync(transaction);

                LogOwnershipError(
                    exception,
                    "transfer",
                    request.WorkAssignmentId,
                    request.ToIndividualId,
                    request.ToJobId);

                return FailureFromException(
                    exception,
                    request.WorkAssignmentId);
            }
        }

        public async Task<WorkAssignmentOwnershipResult> RelieveAsync(
            RelieveWorkAssignmentRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                return WorkAssignmentOwnershipResult.Failed(
                    "The relief request is required.");
            }

            var validationMessage =
                ValidateRelieveRequest(request);

            if (validationMessage is not null)
            {
                return WorkAssignmentOwnershipResult.Failed(
                    validationMessage,
                    request.WorkAssignmentId);
            }

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                var assignment =
                    await LoadActiveAssignmentAsync(
                        db,
                        request.WorkAssignmentId,
                        cancellationToken);

                if (assignment is null)
                {
                    return WorkAssignmentOwnershipResult.Failed(
                        $"Active work assignment ID " +
                        $"{request.WorkAssignmentId} was not found.",
                        request.WorkAssignmentId);
                }

                var currentOwner =
                    await db.WorkAssignmentOwners
                        .SingleOrDefaultAsync(
                            x =>
                                x.WorkAssignmentId ==
                                    request.WorkAssignmentId &&
                                x.IsValid &&
                                x.IsCurrentOwner &&
                                !x.RelievedDate.HasValue,
                            cancellationToken);

                if (currentOwner is null)
                {
                    return WorkAssignmentOwnershipResult.Failed(
                        "This work assignment does not currently have " +
                        "an active owner.",
                        assignment.WorkAssignmentId);
                }

                if (currentOwner.IndividualId !=
                        request.IndividualId ||
                    currentOwner.JobId !=
                        request.JobId)
                {
                    return WorkAssignmentOwnershipResult.Failed(
                        $"The current owner is individual ID " +
                        $"{currentOwner.IndividualId}, job ID " +
                        $"{currentOwner.JobId}.",
                        assignment.WorkAssignmentId);
                }

                if (request.RelievedDateTime <
                    currentOwner.EffectiveFrom)
                {
                    return WorkAssignmentOwnershipResult.Failed(
                        "The relieved date cannot be before the ownership " +
                        "effective-from date.",
                        assignment.WorkAssignmentId);
                }

                if (request.RelievedDateTime >
                    assignment.EndDateTime)
                {
                    return WorkAssignmentOwnershipResult.Failed(
                        "The relieved date cannot be after the work " +
                        "assignment has ended.",
                        assignment.WorkAssignmentId);
                }

                currentOwner.IsCurrentOwner = false;

                currentOwner.EffectiveTo =
                    request.RelievedDateTime;

                currentOwner.RelievedDate =
                    request.RelievedDateTime;

                await db.SaveChangesAsync(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return new WorkAssignmentOwnershipResult
                {
                    Success = true,

                    Message =
                        $"Individual {currentOwner.IndividualId} was " +
                        $"relieved from work assignment " +
                        $"'{assignment.Name}' effective " +
                        $"{request.RelievedDateTime:dd MMM yyyy HH:mm}.",

                    WorkAssignmentId =
                        assignment.WorkAssignmentId,

                    WorkAssignmentOwnerId =
                        currentOwner.WorkAssignmentOwnerId,

                    IndividualId =
                        currentOwner.IndividualId,

                    JobId =
                        currentOwner.JobId,

                    EffectiveFrom =
                        currentOwner.EffectiveFrom,

                    EffectiveTo =
                        currentOwner.EffectiveTo
                };
            }
            catch (OperationCanceledException)
            {
                await RollbackSafelyAsync(transaction);

                throw;
            }
            catch (Exception exception)
            {
                await RollbackSafelyAsync(transaction);

                LogOwnershipError(
                    exception,
                    "relieve",
                    request.WorkAssignmentId,
                    request.IndividualId,
                    request.JobId);

                return FailureFromException(
                    exception,
                    request.WorkAssignmentId);
            }
        }

        public async Task<WorkAssignmentOwnershipResult> TakeOverAsync(
            TakeOverWorkAssignmentRequest request,
            CancellationToken cancellationToken = default)
        {
            if (request is null)
            {
                return WorkAssignmentOwnershipResult.Failed(
                    "The takeover request is required.");
            }

            if (request.WorkAssignmentId <= 0)
            {
                return WorkAssignmentOwnershipResult.Failed(
                    "A valid work assignment is required.");
            }

            if (request.ToIndividualId <= 0)
            {
                return WorkAssignmentOwnershipResult.Failed(
                    "A valid destination individual is required.",
                    request.WorkAssignmentId);
            }

            if (request.ToJobId <= 0)
            {
                return WorkAssignmentOwnershipResult.Failed(
                    "A valid destination job is required.",
                    request.WorkAssignmentId);
            }

            if (request.OperationLogId <= 0)
            {
                return WorkAssignmentOwnershipResult.Failed(
                    "A valid operation log is required.",
                    request.WorkAssignmentId);
            }

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            var currentOwner =
                await db.WorkAssignmentOwners
                    .AsNoTracking()
                    .Where(x =>
                        x.WorkAssignmentId ==
                            request.WorkAssignmentId &&
                        x.IsValid &&
                        x.IsCurrentOwner &&
                        !x.RelievedDate.HasValue)
                    .Select(x => new
                    {
                        x.IndividualId,
                        x.JobId
                    })
                    .SingleOrDefaultAsync(cancellationToken);

            if (currentOwner is null)
            {
                return WorkAssignmentOwnershipResult.Failed(
                    "The work assignment does not have a current owner. " +
                    "Use AssignAsync instead.",
                    request.WorkAssignmentId);
            }

            /*
             * Use local application time here because the rest of your
             * work-planning records currently use DateTime.Now.
             *
             * If you later standardize the database to UTC, change this
             * to DateTime.UtcNow consistently across the module.
             */
            var transferDateTime = DateTime.Now;

            var transferRequest =
                new TransferWorkAssignmentRequest
                {
                    WorkAssignmentId =
                        request.WorkAssignmentId,

                    FromIndividualId =
                        currentOwner.IndividualId,

                    FromJobId =
                        currentOwner.JobId,

                    ToIndividualId =
                        request.ToIndividualId,

                    ToJobId =
                        request.ToJobId,

                    TransferDateTime =
                        transferDateTime,

                    OperationLogId =
                        request.OperationLogId,

                    TransferredByUserId =
                        request.TakenOverByUserId,

                    Reason =
                        request.Reason
                };

            return await TransferAsync(
                transferRequest,
                cancellationToken);
        }

        private static async Task<WorkAssignment?>
            LoadActiveAssignmentAsync(
                HrmTeContext db,
                long workAssignmentId,
                CancellationToken cancellationToken)
        {
            return await db.WorkAssignments
                .SingleOrDefaultAsync(
                    x =>
                        x.WorkAssignmentId ==
                            workAssignmentId &&
                        x.IsValid &&
                        !x.CancelledDate.HasValue,
                    cancellationToken);
        }

        private static async Task<WorkAssignmentOwner?>
            GetCurrentOwnerAsync(
                HrmTeContext db,
                long workAssignmentId,
                CancellationToken cancellationToken)
        {
            return await db.WorkAssignmentOwners
                .SingleOrDefaultAsync(
                    x =>
                        x.WorkAssignmentId ==
                            workAssignmentId &&
                        x.IsValid &&
                        x.IsCurrentOwner &&
                        !x.RelievedDate.HasValue,
                    cancellationToken);
        }

        private static async Task<string?> ValidateJobAsync(
            HrmTeContext db,
            int individualId,
            int jobId,
            CancellationToken cancellationToken)
        {
            var individualExists =
                await db.Individuals
                    .AsNoTracking()
                    .AnyAsync(
                        x =>
                            x.BusinessEntityId ==
                            individualId,
                        cancellationToken);

            if (!individualExists)
            {
                return $"Individual ID {individualId} was not found.";
            }

            var job = await db.Jobs
                .AsNoTracking()
                .Where(x =>
                    x.JobId == jobId &&
                    x.IndividualID == individualId)
                .Select(x => new
                {
                    x.JobId,
                    x.TerminatedDate,
                    x.JobStateId
                })
                .SingleOrDefaultAsync(cancellationToken);

            if (job is null)
            {
                return $"Job ID {jobId} does not belong to " +
                       $"individual ID {individualId}.";
            }

            if (job.TerminatedDate.HasValue)
            {
                return $"Job ID {jobId} has already been terminated.";
            }

            /*
             * Add your approved-job-state rule here when required:
             *
             * if (job.JobStateId != SharedConfig.JobStates.APPROVED)
             * {
             *     return "The selected job is not active.";
             * }
             */

            return null;
        }

        private static string? ValidateOwnershipPeriod(
            WorkAssignment assignment,
            DateTime effectiveFrom,
            DateTime? effectiveTo)
        {
            if (effectiveFrom <
                assignment.StartDateTime)
            {
                return
                    $"Ownership cannot begin before the assignment " +
                    $"starts on " +
                    $"{assignment.StartDateTime:dd MMM yyyy HH:mm}.";
            }

            if (effectiveFrom >
                assignment.EndDateTime)
            {
                return
                    $"Ownership cannot begin after the assignment ends " +
                    $"on {assignment.EndDateTime:dd MMM yyyy HH:mm}.";
            }

            if (effectiveTo.HasValue &&
                effectiveTo.Value <= effectiveFrom)
            {
                return
                    "The ownership effective-to date must be later " +
                    "than its effective-from date.";
            }

            if (effectiveTo.HasValue &&
                effectiveTo.Value >
                assignment.EndDateTime)
            {
                return
                    $"Ownership cannot continue after the assignment " +
                    $"ends on " +
                    $"{assignment.EndDateTime:dd MMM yyyy HH:mm}.";
            }

            return null;
        }

        private static async Task<bool>
            OwnershipOverlapExistsAsync(
                HrmTeContext db,
                long workAssignmentId,
                int individualId,
                int jobId,
                DateTime effectiveFrom,
                DateTime? effectiveTo,
                long? excludedOwnerId,
                CancellationToken cancellationToken)
        {
            var requestedEnd =
                effectiveTo ?? DateTime.MaxValue;

            return await db.WorkAssignmentOwners
                .AsNoTracking()
                .AnyAsync(
                    x =>
                        x.WorkAssignmentId ==
                            workAssignmentId &&
                        x.IndividualId ==
                            individualId &&
                        x.JobId ==
                            jobId &&
                        x.IsValid &&
                        (!excludedOwnerId.HasValue ||
                         x.WorkAssignmentOwnerId !=
                            excludedOwnerId.Value) &&
                        x.EffectiveFrom < requestedEnd &&
                        effectiveFrom <
                            (x.EffectiveTo ?? DateTime.MaxValue),
                    cancellationToken);
        }

        private static string? ValidateAssignRequest(
            AssignWorkAssignmentRequest request)
        {
            if (request.WorkAssignmentId <= 0)
            {
                return "A valid work assignment is required.";
            }

            if (request.IndividualId <= 0)
            {
                return "A valid individual is required.";
            }

            if (request.JobId <= 0)
            {
                return "A valid job is required.";
            }

            if (request.OperationLogId <= 0)
            {
                return "A valid operation log is required.";
            }

            if (request.EffectiveFrom == default)
            {
                return "A valid effective-from date is required.";
            }

            if (request.EffectiveTo.HasValue &&
                request.EffectiveTo.Value <=
                request.EffectiveFrom)
            {
                return "Effective-to must be later than effective-from.";
            }

            return null;
        }

        private static string? ValidateTransferRequest(
            TransferWorkAssignmentRequest request)
        {
            if (request.WorkAssignmentId <= 0)
            {
                return "A valid work assignment is required.";
            }

            if (request.FromIndividualId <= 0 ||
                request.FromJobId <= 0)
            {
                return "A valid current owner and job are required.";
            }

            if (request.ToIndividualId <= 0 ||
                request.ToJobId <= 0)
            {
                return "A valid destination owner and job are required.";
            }

            if (request.FromIndividualId ==
                    request.ToIndividualId &&
                request.FromJobId ==
                    request.ToJobId)
            {
                return "The destination owner must be different from " +
                       "the current owner.";
            }

            if (request.TransferDateTime == default)
            {
                return "A valid transfer date and time are required.";
            }

            if (request.OperationLogId <= 0)
            {
                return "A valid operation log is required.";
            }

            return null;
        }

        private static string? ValidateRelieveRequest(
            RelieveWorkAssignmentRequest request)
        {
            if (request.WorkAssignmentId <= 0)
            {
                return "A valid work assignment is required.";
            }

            if (request.IndividualId <= 0)
            {
                return "A valid individual is required.";
            }

            if (request.JobId <= 0)
            {
                return "A valid job is required.";
            }

            if (request.RelievedDateTime == default)
            {
                return "A valid relieved date and time are required.";
            }

            if (request.OperationLogId <= 0)
            {
                return "A valid operation log is required.";
            }

            return null;
        }

        private async Task RollbackSafelyAsync(
            Microsoft.EntityFrameworkCore.Storage
                .IDbContextTransaction transaction)
        {
            try
            {
                if (transaction.GetDbTransaction().Connection is not null)
                {
                    await transaction.RollbackAsync(
                        CancellationToken.None);
                }
            }
            catch (Exception rollbackException)
            {
                _logger.LogError(
                    rollbackException,
                    "An error occurred while rolling back a work-assignment ownership transaction.");
            }
        }

        private void LogOwnershipError(
            Exception exception,
            string operation,
            long workAssignmentId,
            int individualId,
            int jobId)
        {
            _logger.LogError(
                exception,
                """
            Work-assignment ownership operation failed.
            Operation: {Operation}
            WorkAssignmentId: {WorkAssignmentId}
            IndividualId: {IndividualId}
            JobId: {JobId}
            """,
                operation,
                workAssignmentId,
                individualId,
                jobId);
        }

        private static WorkAssignmentOwnershipResult
            FailureFromException(
                Exception exception,
                long workAssignmentId)
        {
            var baseException =
                exception.GetBaseException();

            return WorkAssignmentOwnershipResult.Failed(
                $"{baseException.GetType().Name}: " +
                $"{baseException.Message}",
                workAssignmentId);
        }


        public async Task<WorkAssignmentTransferStateDto> GetTransferStateAsync(
        long workAssignmentId,
        CancellationToken cancellationToken = default)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync(cancellationToken);

            var transfer = await db.WorkAssignmentTransfers
                .AsNoTracking()
                .Where(x =>
                    x.WorkAssignmentId == workAssignmentId &&
                    x.IsValid)
                .OrderByDescending(x => x.WorkAssignmentTransferId)
                .Select(x => new
                {
                    x.WorkAssignmentTransferId,
                    x.WorkAssignmentTransferStateId,
                    StateName = x.WorkAssignmentTransferState.Name
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (transfer is null)
            {
                return new WorkAssignmentTransferStateDto
                {
                    WorkAssignmentId = workAssignmentId,
                    HasTransfer = false,
                    StateName = "No Transfer"
                };
            }

            return new WorkAssignmentTransferStateDto
            {
                WorkAssignmentId = workAssignmentId,
                WorkAssignmentTransferId =
                    transfer.WorkAssignmentTransferId,

                TransferStateId =
                    transfer.WorkAssignmentTransferStateId,

                StateName =
                    transfer.StateName,

                HasTransfer = true
            };
        }





    }

}
 
