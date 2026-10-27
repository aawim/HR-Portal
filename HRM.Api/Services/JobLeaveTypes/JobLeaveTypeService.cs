using Azure.Core;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Leave;
using HRM.DTOs.LeaveTypes;
using HRM.DTOs.Profile;
using HRM.Models;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.JobLeaveTypes;
using HRM.Services.Interfaces.Profile;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace HRM.Services.JobLeaveTypes
{
    public class JobLeaveTypeService : IJobLeaveTypeService
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IOperationLogService _operationLogService;
        private readonly IUserAccessService _userAccessService;



        public JobLeaveTypeService(
                IDbContextFactory<HrmTeContext> dbFactory
                ,IOperationLogService operationLogService
                ,IUserAccessService userAccessService
            )
        {
            _dbFactory = dbFactory;
            _operationLogService = operationLogService;
            _userAccessService = userAccessService;


        }
        public async Task<ServiceResult> AssignAsync(AssignLeaveTypeCatalogueDto dto, CancellationToken cancellationToken = default)
        {
            var actor = await _userAccessService.RequireContextAsync();
            var org = await _userAccessService.GetCurrentOrganisationIdAsync();
            if (org <= 0 || !await _userAccessService.HasOrganisationAccessAsync(org)) throw new UnauthorizedAccessException("Select an organisation you can access.");
            if (!actor.IsSuperAdministrator && !actor.IsHRStaff && !actor.IsHRHead && !actor.IsHRManager && !await _userAccessService.HasPermissionAsync("Admin") && !await _userAccessService.HasPermissionAsync("Leave.Configuration.Manage"))
                throw new UnauthorizedAccessException("HR permission is required to assign leave types.");
            if (dto.LeaveTypeId is not > 0 || dto.EffectiveToDate?.Date < dto.EffectiveFromDate.Date) return ServiceResult.Fail("Select a leave type and a valid effective period.");
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            if (!await db.Jobs.AnyAsync(x => x.JobId == dto.JobId && x.OrganisationID == org && x.JobStateId == 4 && x.TerminatedDate == null, cancellationToken))
                return ServiceResult.Fail("The selected active job was not found in this organisation.");
            var type = await db.LeaveTypes.AsNoTracking().SingleOrDefaultAsync(x => x.LeaveTypeId == dto.LeaveTypeId && x.IsActive && (x.IsGlobal || x.OrganisationId == org), cancellationToken);
            if (type == null) return ServiceResult.Fail("An active leave type was not found.");
            if (await db.JobLeaveTypes.AnyAsync(x => x.JobId == dto.JobId && x.LeaveTypeId == dto.LeaveTypeId && x.IsValid, cancellationToken)) return ServiceResult.Fail("This leave type is already assigned.");
            var audit = await _operationLogService.CreateAsync(db, SharedConfig.OperationLogActionTypes.JOB_LEAVE_TYPE_CREATE, $"Assigned leave type {type.LeaveTypeId}: {type.Name} to job {dto.JobId}.");
            var day = dto.EffectiveFromDate.Date;
            var policy = await db.LeavePolicies.AsNoTracking().Where(x => x.LeaveTypeId == type.LeaveTypeId && x.OrganisationId == org && x.IsActive && x.EffectiveFrom <= day && (x.EffectiveTo == null || x.EffectiveTo >= day)).OrderByDescending(x => x.EffectiveFrom).ThenByDescending(x => x.LeavePolicyId).FirstOrDefaultAsync(cancellationToken);
            var jobType = await db.Jobs.Where(x => x.JobId == dto.JobId).Select(x => x.JobTypeId).SingleAsync(cancellationToken);
            var accrues = await db.JobTypeLeaveAccrualRules.AnyAsync(x => x.OrganisationId == org && x.JobTypeId == jobType && x.LeaveTypeId == type.LeaveTypeId && x.IsEnabled, cancellationToken);
            db.JobLeaveTypes.Add(new JobLeaveType { JobId = dto.JobId, LeaveTypeId = type.LeaveTypeId, RemainingDays = accrues ? 0 : policy?.DefaultEntitlementDays ?? type.Duration ?? 0,
                IsValid = true, IsLeaveInfoUpdated = true, EffectiveFromDate = day, EffectiveToDate = dto.EffectiveToDate, RenewedDate = day, OperationLogId = audit.OperationLogId });
            await db.SaveChangesAsync(cancellationToken); await tx.CommitAsync(cancellationToken);
            return ServiceResult.Ok("Leave type assigned successfully.");
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

        public async Task<List<JobLeaveTypeDto>> GetJobLeaveType()
        {
            var jobId = await _userAccessService.GetCurrentJobIdAsync();

            return await GetJobLeaveType(jobId);
        }



        //public async Task<List<JobLeaveTypeDto>> GetJobLeaveType(int jobId)
        //{
        //    await using var db =
        //        await _dbFactory.CreateDbContextAsync();

        //    var balances = await db.JobLeaveTypes
        //        .AsNoTracking()
        //        .Include(x => x.LeaveType)
        //        .Include(x => x.LeaveType)
        //        .Where(x =>
        //            x.JobId == jobId &&
        //            x.IsValid &&
        //            x.IsLeaveInfoUpdated == true)
        //        .ToListAsync();


        //    return balances
        //        .OrderBy(x =>
        //            x.LeaveType?.Name ??
        //            x.LeaveType?.Name ??
        //            string.Empty)
        //        .ToList();



        //}



        public Task<List<JobLeaveTypeDto>> GetJobLeaveType(int JobId) => ReadEntitlementsAsync(JobId, false);

        public Task<List<JobLeaveTypeDto>> GetEntitlementsAsync(int jobId) => ReadEntitlementsAsync(jobId, true);

        private async Task<List<JobLeaveTypeDto>> ReadEntitlementsAsync(int JobId, bool includeUnconfirmed)
        {
            var actor = await _userAccessService.RequireContextAsync();
            var org = await _userAccessService.GetCurrentOrganisationIdAsync();
            if (org <= 0 || !await _userAccessService.HasOrganisationAccessAsync(org))
                throw new UnauthorizedAccessException("Select an organisation you can access.");
            using var db = await _dbFactory.CreateDbContextAsync();
            var job = await db.Jobs.AsNoTracking().SingleOrDefaultAsync(x => x.JobId == JobId && x.OrganisationID == org)
                ?? throw new UnauthorizedAccessException("This job is not in the current organisation.");
            if (job.IndividualID != actor.IndividualId && !actor.IsHRStaff && !actor.IsHRHead && !actor.IsHRManager &&
                !actor.IsSuperAdministrator && !await _userAccessService.HasPermissionAsync("Super.Admin.Manage") && !await _userAccessService.HasPermissionAsync("Admin") &&
                !(await HRM.Services.Leave.LeaveHandoverService.SupervisorsAsync(db, job.JobId)).Contains(actor.IndividualId))
                throw new UnauthorizedAccessException("You cannot view this staff member's leave balances.");

            return await db.JobLeaveTypes
                 .Include(x => x.LeaveType)
                 .Where(x => x.JobId == JobId )
                 .Where(x => x.IsValid && (includeUnconfirmed || x.IsLeaveInfoUpdated))
                 .Select(x => new JobLeaveTypeDto
                 {
                     JobLeaveTypeId = x.JobLeaveTypeId,

                     JobId = x.JobId,

                     LeaveTypeId = x.LeaveTypeId,
                     LeaveTypeName = x.LeaveType != null ? x.LeaveType.Name : "",
                     LeaveTypeNameDv = x.LeaveType != null ? x.LeaveType.NameDhivehi ?? "" : "",

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

    }
}
