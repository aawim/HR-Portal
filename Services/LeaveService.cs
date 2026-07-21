
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Leave;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services.Interfaces;
using HRM.Services.Stores;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services
{
    public class LeaveService : ILeaveService
    {
        private readonly IUserContext _userContextService;
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IOperationLogService _operationLogService;
        private readonly IUserAccessService _access;
        private readonly IRequestService _requestService;

        private readonly ToastService _toastService;

        public LeaveService(
            IDbContextFactory<HrmTeContext> dbFactory, 
            IOperationLogService operationLogService, 
            IUserContext userContextService, 
            ToastService toastService, 
            IUserAccessService access,
            IRequestService requestService
            )
        {
            _dbFactory = dbFactory;
            _operationLogService = operationLogService;
            _toastService = toastService;
            _userContextService = userContextService;
            _access = access;
            _requestService = requestService;
        }

        public async Task<LeaveResource?> BuildAsync(LeaveResource leaveRequestId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            var leave = await db.Leaves
                .AsNoTracking()
                .Include(x => x.Job)
                .FirstOrDefaultAsync(x =>
                    x.LeaveId == leaveRequestId.LeaveRequestID);

            if (leave == null)
                return null;

            return new LeaveResource
            {
                LeaveRequestID = leave.LeaveId,

                IndividualID = leave.Job.IndividualID,

                OrganisationID = leave.Job.OrganisationID,

                LeaveTypeID = leave.LeaveTypeId,

                LeaveStateID = leave.LeaveStateId,

                IsApproved = false,      // we'll improve this
                IsCancelled = false,     // we'll improve this
 
            };
        }
        public async Task<List<ProcessingLeaveDto>> GetMyProcessingLeavesAsync()
        {
            var context = await _access.RequireContextAsync();

            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.LeaveRequests
                .AsNoTracking()
                .Where(lr =>
                    lr.Leave.JobId == context.ActiveJob!.JobID &&
                    lr.Request.RequestState.IsProcessingState)
                .Select(lr => new ProcessingLeaveDto
                {
                    LeaveTypeId = lr.Leave.LeaveTypeId,

                    FromDate = lr.Leave.FromDate,

                    ToDate = lr.Leave.ToDate,

                    Duration = lr.Leave.Duration,

                    StateName = lr.Request.RequestState.StateName
                })
                .ToListAsync();
        }


        public async Task<List<Leaf>> GetMyLeaveRequestsAsync()
        {
            var jobId = await _access.GetCurrentJobIdAsync();

            return await GetLeaveRequestsAsync(jobId);
        }


        public async Task<List<JobLeaveType>> GetMyLeaveBalancesAsync(int leaveTypeId = 0)
        {
            var context = await _access.RequireContextAsync();

                if (context.ActiveJob == null)
                return new List<JobLeaveType>();

            return await GetJobLeaveBalancesAsync(context.ActiveJob.JobID,leaveTypeId);
        }

        public async Task<List<JobLeaveType>> GetJobLeaveBalancesAsync(int jobId, int leaveTypeId = 0)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            IQueryable<JobLeaveType> query = db.JobLeaveTypes
                .AsNoTracking()
                .Include(x => x.LeaveType)
                .Where(x =>
                    x.JobId == jobId &&
                    x.IsValid &&
                    x.IsLeaveInfoUpdated == true);

            if (leaveTypeId > 0)
            {
                query = query.Where(x => x.LeaveTypeId == leaveTypeId);
            }

            return await query
                .OrderBy(x => x.LeaveType.Name)
                .ToListAsync();
        }

        public async Task<List<JobLeaveTypeDto>> GetJobLeaveTypeByJobId(int StaffId)
        {

            using var db = await _dbFactory.CreateDbContextAsync();

            var jobId = await GetJobIdByStaffId(StaffId);


           return await db.JobLeaveTypes
                .Include(x => x.LeaveType)
                .Where(x => x.JobId == jobId && x.IsValid)
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




        public async Task<List<JobLeaveType>> GetJobLeaveTypesAsync()
        {
            var context = await _access.RequireContextAsync();

            if (context.ActiveJob == null)
            {
                return new List<JobLeaveType>();
            }

            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.JobLeaveTypes
                .AsNoTracking()
                .Where(x =>
                    x.JobId == context.ActiveJob.JobID &&
                    x.IsValid)
                .Include(x => x.LeaveType)
                .OrderBy(x => x.LeaveType.Name)
                .ToListAsync();
        }


        public async Task<List<Leaf>> GetLeaveRequestsAsync(int jobId)
        {
            var context = await _dbFactory.CreateDbContextAsync();

            return await context.Leaves
             .Include(l => l.LeaveType)  // Fetches the Type name
             .Include(l => l.LeaveState) // Fetches the Status name
             .Where(l => l.JobId == jobId)
             .Where(s => s.LeaveStateId != 4)
             .OrderByDescending(l => l.FromDate)
             .ToListAsync();

        }



        public async Task<List<LeaveReasonDto>> GetReasonsByLeaveType(int typeId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            return await db.LeaveReasons
                .Where(x => x.LeaveTypeReasonTypeId == typeId)
                .Select(x => new LeaveReasonDto
                {
                    Id = x.LeaveId,
                    Name = x.Reason
                })
                .ToListAsync();
        }

        public Task<List<LeaveReasonDto>> GetReasonsByLeaveType()
        {
            throw new NotImplementedException();
        }

       
        public Task<ServiceResult> ShowDetailLeaveAsync(int leaveId)
        {
            throw new NotImplementedException();
        }






        public async Task<List<LeaveTypeDto>> GetAvailableLeaveTypesAsync(int jobId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();

            var assignedLeaveTypeIds = await db.JobLeaveTypes
                .Where(x => x.JobId == jobId && x.IsValid)
                .Select(x => x.LeaveTypeId)
                .ToListAsync();

            return await db.LeaveTypes
                .AsNoTracking()
                .Where(x => !assignedLeaveTypeIds.Contains(x.LeaveTypeId))
                .OrderBy(x => x.Name)
                .Select(x => new LeaveTypeDto
                {
                    LeaveTypeID = x.LeaveTypeId,
                    Name = x.Name,
                    NameDhivehi = x.NameDhivehi,
                    Duration = x.Duration,
                    IncludeHolidays = x.IncludeHolidays,
                    IncludePay = x.IncludePay,
                    IsPublic = x.IsPublic,
                    IsGlobal = x.IsGlobal,
                    IsLocationRequired = x.IsLocationRequired,
                    ServiceDurationMonths = x.ServiceDurationMonths,
                    IsRenewed = x.IsRenewed,
                    IsStaffWideAvailable = x.IsStaffWideAvailable,
                    PayPercentage = x.PayPercentage,
                    StartInMonth = x.StartInMonth,
                    RepeatedEveryInMonth = x.RepeatedEveryInMonth
                })
                .ToListAsync();
        }






        public async Task<int> GetJobIdByStaffId(int StaffId)
        {
            await using var db =
              await _dbFactory.CreateDbContextAsync();

            return await db.Jobs
            .AsNoTracking()
            .Where(x => x.IndividualID == StaffId && x.TerminatedDate == null)
            .Select(x => x.JobId)
            .FirstOrDefaultAsync();
        }


        public async Task<UserProfileDto?> GetByIdAsync(int userId)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync();

            return await db.Users
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => new UserProfileDto
                {
                    UserId = x.UserId,

                    Username = x.Username,

                    IndividualID = x.BusinessEntityID
                })
                .FirstOrDefaultAsync();
        }


        //public async Task<List<JobLeaveType>> GetJobLeaveTypesAsync()
        //{
        //    using var db = await _dbFactory.CreateDbContextAsync();
        //    int jobId = await _userService.GetJobID();
        //    return await db.JobLeaveTypes
        //        .Where(x => x.JobId == jobId && x.IsValid == true && x.IsLeaveInfoUpdated == 1)
        //        .Include(x => x.LeaveType)
        //        .OrderBy(x => x.LeaveType.Name)
        //        .ToListAsync();
        //}


        //public async Task<List<JobLeaveTypeDto>> GetJobLeaveTypeByJobId(int jobId)
        //{

        //    using var context = _context.CreateDbContext();
        //    return await context.JobLeaveTypes
        //        .Include(x => x.LeaveType)
        //        .Where(x => x.JobId == jobId && x.IsValid)
        //        .Select(x => new JobLeaveTypeDto
        //        {
        //            JobId = x.JobId,
        //            LeaveTypeID = x.LeaveTypeId,
        //            LeaveTypeName = x.LeaveType.Name,
        //            RemainingDays = x.RemainingDays ?? 0
        //        })
        //        .ToListAsync();
        //}

        //public async Task<List<Leaf>> GetLeaveRequestsAsync(int jobId)
        //{
        //    using var context = _context.CreateDbContext();
        //      return await context.Leaves
        //        .Include(l => l.LeaveType)
        //        .Include(ls => ls.LeaveState)
        //        .Where(x => x.JobId == jobId)
        //        .OrderByDescending(l => l.LeaveId)
        //        .ToListAsync();
        //}

        //// Reasons
        //public async Task<List<LeaveReasonDto>> GetReasonsByLeaveType(int typeId)
        //{
        //    using var context = _context.CreateDbContext();

        //    return await context.LeaveReasons
        //        .Where(x => x.LeaveTypeReasonTypeId == typeId)
        //        .Select(x => new LeaveReasonDto
        //        {
        //            Id = x.LeaveId,
        //            Name = x.Reason
        //        })
        //        .ToListAsync();
        //}

        //public async Task<List<LeaveReasonDto>> GetReasonsByLeaveType()
        //{
        //    using var context = _context.CreateDbContext();

        //    return await context.LeaveReasons
        //          .Select(x => new LeaveReasonDto
        //        {
        //            Id = x.LeaveId,
        //            Name = x.Reason
        //        })
        //        .ToListAsync();
        //}

        //public async Task<List<JobLeaveType>> GetUserLeaveBalancesAsync(int jobId, int typeId)
        //{
        //    using var context = _context.CreateDbContext();
        //    return await context.JobLeaveTypes
        //        .Include(x => x.LeaveType)
        //        .Where(x => x.JobId == jobId && x.JobLeaveTypeId == typeId)
        //        .ToListAsync();
        //}

        //// SUBMIT LEAVE (MAIN PART)

        public async Task<ServiceResult> SubmitLeave(LeaveApplicationDto model)
        {
            var context = await _access.RequireContextAsync();
            using var db = await _dbFactory.CreateDbContextAsync();

 

            await using var transaction =
                await db.Database.BeginTransactionAsync();

            try
            {
                int jobId = context.ActiveJob.JobID;

                if (jobId == 0)
                {
                    return new ServiceResult
                    {
                        Success = false,
                        Message = "Job not found for current user."
                    };
                }


                model.JobId = jobId;

                // CHECK OVERLAP LEAVES (ANY STATE)
                bool hasOverlap = await db.Leaves
                    .AnyAsync(l =>
                        l.JobId == jobId &&
                        l.FromDate <= model.ToDate &&
                        l.ToDate >= model.FromDate &&
                        l.LeaveState.LeaveStateId != SharedConfig.LeaveStates.REJECTED &&
                        l.LeaveState.LeaveStateId != SharedConfig.LeaveStates.CANCELLED

                        );

                if (hasOverlap)
                {

                    return new ServiceResult
                    {
                        Success = false,
                        Message = "You already have a leave request in this date range."
                    };

                    //_toastService.Show("You already have a leave request in this date range.", "info");
                }

                // OPERATION LOG
                var operationLog = await _operationLogService.CreateAsync(
                    db,
                    actionId: SharedConfig.OperationLogActionTypes.LEAVE_CREATE,
                    remarks: "Leave application submitted");

                await db.SaveChangesAsync();

                // CREATE REQUEST
                var request =
                    await _requestService.CreateAsync(
                        new CreateRequestDto
                        {
                            RequestTypeId =
                                SharedConfig.RequestTypes.ANNUAL_LEAVE_REQUEST,

                            OperationLogId =
                                operationLog.OperationLogId,

                            ApplicantBusinessEntityId =
                                context.BusinessEntityId,

                            OrganisationBusinessEntityId =
                                context.Organisation?
                                       .OrganisationBusinessEntityId,

                            EffectiveDate =
                                model.FromDate
                        });

                // CREATE LEAVE
                var leave = new Leaf
                {
                    JobId = model.JobId,
                    LeaveTypeId = model.LeaveTypeId,
                    FromDate = model.FromDate,
                    ToDate = model.ToDate,
                    Duration = model.Duration,
                    OperationLogId = operationLog.OperationLogId,
                    LeaveStateId = SharedConfig.LeaveStates.PENDING_VERIFICATION
                };

                db.Leaves.Add(leave);
                await db.SaveChangesAsync();


                // CREATE A LEAVEREQUEST 
                var leaveRequest = new LeaveRequest
                {
                    LeaveId = leave.LeaveId,
                    RequestId = request.RequestId,
                };

                db.LeaveRequests.Add(leaveRequest);
                await db.SaveChangesAsync();

                // LOCATION (OPTIONAL)
                if (model.CountryId > 0)
                {
                    var location = new LeaveSpendingLocation
                    {
                        LeaveId = leave.LeaveId,
                        CountryId = model.CountryId,
                        IslandId = model.IslandId,
                        Address = model.Address,
                        OperationLogId = operationLog.OperationLogId
                    };

                    db.LeaveSpendingLocations.Add(location);
                    await db.SaveChangesAsync();
                }

                // UPDATE JOB LEAVE TYPE
                var jobLeaveType = await db.JobLeaveTypes
                    .FirstOrDefaultAsync(x =>
                    x.JobId == jobId &&
                    x.IsValid &&
                    x.LeaveTypeId == model.LeaveTypeId);

                if (jobLeaveType == null)
                {
                    return new ServiceResult
                    {
                        Success = false,
                        Message = "Leave balance not found for selected leave type."
                    };
                }

                if (jobLeaveType.RemainingDays < model.Duration)
                {
                    return new ServiceResult
                    {
                        Success = false,
                        Message = "Insufficient leave balance."
                    };
                }

                jobLeaveType.RemainingDays -= model.Duration;
                jobLeaveType.LastLeaveTakenDate = model.ToDate;
                jobLeaveType.OperationLogId = operationLog.OperationLogId;

                await db.SaveChangesAsync();

                await transaction.CommitAsync();

                return new ServiceResult
                {
                    Success = true,
                    Message = "Leave submitted successfully.",

                };

            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<ServiceResult> CancelLeaveAsync(int leaveId)
        {
            var context = await _access.RequireContextAsync();

            await using var db = await _dbFactory.CreateDbContextAsync();
            await using var transaction = await db.Database.BeginTransactionAsync();

            try
            {
                var leave = await db.Leaves
                    .FirstOrDefaultAsync(l => l.LeaveId == leaveId);

                if (leave == null)
                    return ServiceResult.Fail("Leave request not found.");

                if (leave.LeaveStateId == SharedConfig.LeaveStates.CANCELLED)
                    return ServiceResult.Fail("Leave request is already cancelled.");

                // Find the Request linked to this Leave
                var request = await db.LeaveRequests
                    .Where(x => x.LeaveId == leave.LeaveId)
                    .Select(x => x.Request)
                    .FirstOrDefaultAsync();

                if (request == null)
                    return ServiceResult.Fail("Associated request not found.");

                // Create Operation Log
                var operationLog = await _operationLogService.CreateAsync(
                    db,
                    actionId: SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE,
                    remarks: $"Leave request {request.ReferenceNumber} cancelled.");

                //
                // Update Request
                //
                request.RequestStateId = SharedConfig.RequestStates.CANCELLED;
                request.LastStateChangedByUserId = context.UserId;
                request.LastStateChangedDate = DateTime.Now;
                request.StateChangeRemarks = "Cancelled by applicant";
                request.OperationLogId = operationLog.OperationLogId;

                //
                // Update Leave
                //
                leave.LeaveStateId = SharedConfig.LeaveStates.CANCELLED;
                leave.OperationLogId = operationLog.OperationLogId;

                //
                // Restore Leave Balance
                //
                var jobLeaveType = await db.JobLeaveTypes
                    .FirstOrDefaultAsync(j =>
                        j.JobId == leave.JobId &&
                        j.LeaveTypeId == leave.LeaveTypeId &&
                        j.IsValid);

                if (jobLeaveType != null)
                {
                    jobLeaveType.RemainingDays += leave.Duration;
                    jobLeaveType.OperationLogId = operationLog.OperationLogId;

                    // Optional
                    // If your business rules require recalculation,
                    // don't simply set this to null.
                    jobLeaveType.LastLeaveTakenDate = null;
                }

                await db.SaveChangesAsync();
                await transaction.CommitAsync();

                return ServiceResult.Ok("Leave request cancelled successfully.");
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }


      

         













    }
}
