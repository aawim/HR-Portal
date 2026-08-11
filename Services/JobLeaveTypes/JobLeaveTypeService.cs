using Azure.Core;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Leave;
using HRM.DTOs.LeaveTypes;
using HRM.Models;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.JobLeaveTypes;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.JobLeaveTypes
{
    public class JobLeaveTypeService : IJobLeaveTypeService
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IOperationLogService _operationLogService;
        private readonly IUserAccessService _userAccessService;

        public JobLeaveTypeService(IDbContextFactory<HrmTeContext> dbFactory,IOperationLogService operationLogService, IUserAccessService userAccessService )
        {
            _dbFactory = dbFactory;
            _operationLogService = operationLogService;
            _userAccessService = userAccessService;
        }
        //public async Task<ServiceResult> AssignAsync(AssignLeaveTypeDto dto)
        //{
        //    await using var db = await _dbFactory.CreateDbContextAsync();

        //    await using var transaction =
        //        await db.Database.BeginTransactionAsync();

        //    try
        //    {
        //        var exists = await db.JobLeaveTypes
        //            .AnyAsync(x =>
        //                x.JobId == dto.JobId &&
        //                x.LeaveTypeId == dto.LeaveTypeId &&
        //                x.IsValid);

        //        if (exists)
        //        {
        //            return ServiceResult.Fail(
        //                "This leave type is already assigned.");
        //        }


        //        var leaveType = await db.LeaveTypes
        //            .FirstOrDefaultAsync(x =>
        //                x.LeaveTypeId == dto.LeaveTypeId);


        //        if (leaveType == null)
        //        {
        //            return ServiceResult.Fail(
        //                "Leave type not found.");
        //        }


        //        // Create Audit Log
        //        var operationLog =
        //            await _operationLogService.CreateAsync(
        //                db,
        //                actionId: SharedConfig.OperationLogActionTypes.JOB_LEAVE_TYPE_CREATE,
        //                remarks: $"Assigned leave type: {leaveType.Name}");


        //        var jobLeaveType = new JobLeaveType
        //        {
        //            JobId = dto.JobId,
        //            LeaveTypeId = dto.LeaveTypeId,

        //            // Initial balance from Leave Type policy
        //            RemainingDays = leaveType.Duration ?? 0,
        //            IsValid = true,
        //            IsLeaveInfoUpdated = true,
        //            EffectiveFromDate = DateTime.Today,
        //            EffectiveToDate = DateTime.Today.AddYears(1),
        //            RenewedDate = DateTime.Today,
        //            OperationLogId = operationLog.OperationLogId
        //        };

        //        db.JobLeaveTypes.Add(jobLeaveType);

        //        await db.SaveChangesAsync();

        //        await transaction.CommitAsync();

        //        return ServiceResult.Ok("Leave type assigned successfully.");
        //    }
        //    catch
        //    {
        //        await transaction.RollbackAsync();
        //        throw;
        //    }
        //}


        public async Task<ServiceResult> AssignLegacyAsync(
           AssignLegacyLeaveTypeDto dto,
           CancellationToken cancellationToken = default)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync(cancellationToken);

            await using var transaction =
                await db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var userContext =
                    await _userAccessService.GetContextAsync();

                var currentOrganisationId = userContext?.ActiveJob?.OrganisationId;

                if (!currentOrganisationId.HasValue)
                {
                    return ServiceResult.Fail(
                        "The current user does not have an active organisation.");
                }

                var jobExists = await db.Jobs
                    .AnyAsync(
                        x => x.JobId == dto.JobId &&
                             x.OrganisationID ==
                                 currentOrganisationId.Value,
                        cancellationToken);

                if (!jobExists)
                {
                    return ServiceResult.Fail(
                        "The selected job does not belong to your organisation.");
                }

                var leaveType = await db.LeaveTypes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x => x.LeaveTypeId == dto.LeaveTypeId &&
                             x.OrganisationId ==
                                 currentOrganisationId.Value,
                        cancellationToken);

                if (leaveType is null)
                {
                    return ServiceResult.Fail(
                        "Leave type not found.");
                }

                // Check the same legacy type.
                var alreadyAssigned = await db.JobLeaveTypes
                    .AnyAsync(
                        x => x.JobId == dto.JobId &&
                             x.LeaveTypeId == dto.LeaveTypeId &&
                             x.IsValid,
                        cancellationToken);

                if (alreadyAssigned)
                {
                    return ServiceResult.Fail(
                        "This leave type is already assigned.");
                }

                /*
                 * If the legacy type has been migrated, prevent assigning
                 * it when the corresponding definition is already assigned.
                 */
                if (leaveType.MigratedLeaveDefinitionID.HasValue)
                {
                    var migratedDefinitionAssigned =
                        await db.JobLeaveTypes.AnyAsync(
                            x => x.JobId == dto.JobId &&
                                 x.LeaveDefinitionId ==
                                     leaveType.MigratedLeaveDefinitionID.Value &&
                                 x.IsValid,
                            cancellationToken);

                    if (migratedDefinitionAssigned)
                    {
                        return ServiceResult.Fail(
                            "The new definition for this legacy leave type " +
                            "is already assigned.");
                    }
                }

                var operationLog =
                    await _operationLogService.CreateAsync(
                        db,
                        actionId: SharedConfig.OperationLogActionTypes
                            .JOB_LEAVE_TYPE_CREATE,
                        remarks:
                            $"Assigned legacy leave type: {leaveType.Name}");

                var effectiveFrom =
                    dto.EffectiveFromDate.Date;

                var jobLeaveType = new JobLeaveType
                {
                    JobId = dto.JobId,

                    // Legacy source
                    LeaveTypeId = dto.LeaveTypeId,

                    LeaveDefinitionId = null,

                    RemainingDays = leaveType.Duration ?? 0,
                    IsValid = true,
                    IsLeaveInfoUpdated = true,

                    EffectiveFromDate = effectiveFrom,

                    EffectiveToDate =
                        dto.EffectiveToDate ??
                        effectiveFrom.AddYears(1),

                    RenewedDate = effectiveFrom,

                    OperationLogId =
                        operationLog.OperationLogId
                };

                db.JobLeaveTypes.Add(jobLeaveType);

                await db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return ServiceResult.Ok(
                    "Legacy leave type assigned successfully.");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }

        public async Task<ServiceResult> AssignDefinitionAsync(AssignLeaveDefinitionDto dto,
            CancellationToken cancellationToken = default)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync(cancellationToken);

            await using var transaction =
                await db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var userContext =
                    await _userAccessService.GetContextAsync();

                var currentOrganisationId = userContext?.ActiveJob?.OrganisationId;

                if (!currentOrganisationId.HasValue)
                {
                    return ServiceResult.Fail(
                        "The current user does not have an active organisation.");
                }

                var jobExists = await db.Jobs
                    .AnyAsync(
                        x => x.JobId == dto.JobId &&
                             x.OrganisationID ==
                                 currentOrganisationId.Value,
                        cancellationToken);

                if (!jobExists)
                {
                    return ServiceResult.Fail(
                        "The selected job does not belong to your organisation.");
                }

                var definition = await db.LeaveDefinitions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        x =>
                            x.LeaveDefinitionId ==
                                dto.LeaveDefinitionId &&
                            x.OwnerOrganisationId ==
                                currentOrganisationId.Value &&
                            x.IsActive,
                        cancellationToken);

                if (definition is null)
                {
                    return ServiceResult.Fail(
                        "An active leave definition was not found.");
                }

                // Check the same definition.
                var alreadyAssigned = await db.JobLeaveTypes
                    .AnyAsync(
                        x => x.JobId == dto.JobId &&
                             x.LeaveDefinitionId ==
                                 dto.LeaveDefinitionId &&
                             x.IsValid,
                        cancellationToken);

                if (alreadyAssigned)
                {
                    return ServiceResult.Fail(
                        "This leave definition is already assigned.");
                }

                /*
                 * Find legacy types mapped to the selected definition.
                 * This prevents assigning the definition when the staff
                 * already has its legacy equivalent.
                 */
                var mappedLegacyTypeIds = db.LeaveTypes
                    .Where(x =>
                        x.MigratedLeaveDefinitionID == dto.LeaveDefinitionId)
                    .Select(x => x.LeaveTypeId);

                var mappedLegacyTypeAssigned =
                    await db.JobLeaveTypes.AnyAsync(
                        x => x.JobId == dto.JobId &&
                             x.IsValid  &&
                             x.LeaveTypeId > 0 &&
                             mappedLegacyTypeIds.Contains(
                                 x.LeaveTypeId),
                        cancellationToken);

                if (mappedLegacyTypeAssigned)
                {
                    return ServiceResult.Fail(
                        "The legacy version of this leave definition " +
                        "is already assigned.");
                }

                var operationLog =
                    await _operationLogService.CreateAsync(
                        db,
                        actionId: SharedConfig.OperationLogActionTypes
                            .JOB_LEAVE_TYPE_CREATE,
                        remarks:
                            $"Assigned leave definition: {definition.Name}");

                var effectiveFrom =
                    dto.EffectiveFromDate.Date;

                var jobLeaveType = new JobLeaveType
                {
                    JobId = dto.JobId,

                    // New source
                    LeaveTypeId = 0,
                    LeaveDefinitionId =
                        dto.LeaveDefinitionId,

                    /*
                     * The policy/accrual engine will calculate and post
                     * the entitlement after assignment.
                     */
                    RemainingDays = 0,

                    IsValid = true,
                    IsLeaveInfoUpdated = true,

                    EffectiveFromDate = effectiveFrom,
                    EffectiveToDate = dto.EffectiveToDate,
                    RenewedDate = effectiveFrom,

                    OperationLogId =
                        operationLog.OperationLogId
                };

                db.JobLeaveTypes.Add(jobLeaveType);

                await db.SaveChangesAsync(cancellationToken);

                /*
                 * Call the new entitlement/accrual service here after the
                 * JobLeaveType has received its primary key.
                 *
                 * Example:
                 *
                 * await _leaveAccrualService
                 *     .CalculateInitialEntitlementAsync(
                 *         db,
                 *         jobLeaveType.JobLeaveTypeId,
                 *         effectiveFrom,
                 *         cancellationToken);
                 */

                await transaction.CommitAsync(cancellationToken);

                return ServiceResult.Ok(
                    "Leave definition assigned successfully.");
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }


        public async Task<ServiceResult> UpdateAsync(JobLeaveTypeEditDto dto)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            await using var transaction =
                await db.Database.BeginTransactionAsync();

            try
            {
                var jobLeaveType = await db.JobLeaveTypes
                    .FirstOrDefaultAsync(x =>
                        x.JobLeaveTypeId == dto.JobLeaveTypeId);


                if (jobLeaveType == null)
                {
                    return ServiceResult.Fail(
                        "Leave assignment not found.");
                }


                if (dto.RemainingDays < 0)
                {
                    return ServiceResult.Fail(
                        "Remaining days cannot be negative.");
                }


                if (dto.EffectiveFromDate.HasValue &&
                    dto.EffectiveToDate.HasValue &&
                    dto.EffectiveFromDate > dto.EffectiveToDate)
                {
                    return ServiceResult.Fail(
                        "Effective From date cannot be after Effective To date.");
                }


                var operationLog =
                    await _operationLogService.CreateAsync(
                        db,
                        actionId: SharedConfig.OperationLogActionTypes.JOB_LEAVE_TYPE_UDPATE,
                        remarks:
                            $"Updated leave assignment {jobLeaveType.JobLeaveTypeId}");


                jobLeaveType.RemainingDays =
                    dto.RemainingDays;


                jobLeaveType.EffectiveFromDate =
                    dto.EffectiveFromDate;


                jobLeaveType.EffectiveToDate =
                    dto.EffectiveToDate;


                jobLeaveType.IsValid =
                    dto.IsValid;


                jobLeaveType.IsLeaveInfoUpdated = true;


                jobLeaveType.OperationLogId =
                    operationLog.OperationLogId;


                await db.SaveChangesAsync();


                await transaction.CommitAsync();


                return ServiceResult.Ok(
                    "Leave assignment updated successfully.");
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


        public async Task<List<JobLeaveTypeDto>> GetJobLeaveTypeByJobId(int StaffId)
        {

            using var db = await _dbFactory.CreateDbContextAsync();

            var jobId = await GetJobIdByStaffId(StaffId);


            return await db.JobLeaveTypes
                 .Include(x => x.LeaveType)
                 .Where(x => x.JobId == jobId && x.IsValid)
                 .Where(x => x.IsLeaveInfoUpdated == true)
                 .Select(x => new JobLeaveTypeDto
                 {
                     JobLeaveTypeId = x.JobLeaveTypeId,

                     JobId = x.JobId,

                     LeaveTypeId = x.LeaveTypeId,

                     LeaveTypeName = x.LeaveType.Name,

                     RemainingDays = x.RemainingDays ?? 0,

                     LastLeaveTakenDate = x.LastLeaveTakenDate,

                     RenewedDate = x.RenewedDate,

                     EffectiveFromDate = x.EffectiveFromDate,

                     EffectiveToDate = x.EffectiveToDate,

                     IsValid = x.IsValid,

                     IsLeaveInfoUpdated = x.IsLeaveInfoUpdated
                 })
                  .OrderBy(x => x.LeaveTypeName)
                 .ToListAsync();
        }

        private async Task<int> GetJobIdByStaffId(int StaffId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.Jobs
            .AsNoTracking()
            .Where(x => x.IndividualID == StaffId && x.TerminatedDate == null)
            .Select(x => x.JobId)
            .FirstOrDefaultAsync();
        }


    }
}
