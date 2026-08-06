using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Leave;
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

        public JobLeaveTypeService(IDbContextFactory<HrmTeContext> dbFactory,IOperationLogService operationLogService)
        {
            _dbFactory = dbFactory;
            _operationLogService = operationLogService;
        }
        public async Task<ServiceResult> AssignAsync(AssignLeaveTypeDto dto)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            await using var transaction =
                await db.Database.BeginTransactionAsync();

            try
            {
                var exists = await db.JobLeaveTypes
                    .AnyAsync(x =>
                        x.JobId == dto.JobId &&
                        x.LeaveTypeId == dto.LeaveTypeId &&
                        x.IsValid);

                if (exists)
                {
                    return ServiceResult.Fail(
                        "This leave type is already assigned.");
                }


                var leaveType = await db.LeaveTypes
                    .FirstOrDefaultAsync(x =>
                        x.LeaveTypeId == dto.LeaveTypeId);


                if (leaveType == null)
                {
                    return ServiceResult.Fail(
                        "Leave type not found.");
                }


                // Create Audit Log
                var operationLog =
                    await _operationLogService.CreateAsync(
                        db,
                        actionId: SharedConfig.OperationLogActionTypes.JOB_LEAVE_TYPE_CREATE,
                        remarks: $"Assigned leave type: {leaveType.Name}");


                var jobLeaveType = new JobLeaveType
                {
                    JobId = dto.JobId,

                    LeaveTypeId = dto.LeaveTypeId,


                    // Initial balance from Leave Type policy
                    RemainingDays = leaveType.Duration ?? 0,


                    IsValid = true,

                    IsLeaveInfoUpdated = true,


                    EffectiveFromDate = DateTime.Today,

                    EffectiveToDate = DateTime.Today.AddYears(1),


                    RenewedDate = DateTime.Today,


                    OperationLogId = operationLog.OperationLogId
                };


                db.JobLeaveTypes.Add(jobLeaveType);


                await db.SaveChangesAsync();


                await transaction.CommitAsync();


                return ServiceResult.Ok(
                    "Leave type assigned successfully.");
            }
            catch
            {
                await transaction.RollbackAsync();
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
