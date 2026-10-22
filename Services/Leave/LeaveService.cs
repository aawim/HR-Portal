using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Leave;
using HRM.DTOs.LeaveTypes;
using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Models.LeaveTypes;
using HRM.Helpers;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.Leave;
using HRM.Services.Interfaces.AppovalWF;
using HRM.Services.Stores;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Leave
{
    public class LeaveService : ILeaveService
    {
        //private readonly IUserContext _userContextService;
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IOperationLogService _operationLogService;
        private readonly IUserAccessService _access;
        private readonly IRequestService _requestService;
        private readonly IWorkflowEngine _workflowEngine;

        private readonly ToastService _toastService;
        private readonly ILeaveChangeService? _leaveChanges;

        public LeaveService(
            IDbContextFactory<HrmTeContext> dbFactory, 
            IOperationLogService operationLogService, 
            IUserContext userContextService, 
            ToastService toastService, 
            IUserAccessService access,
            IRequestService requestService,
            IWorkflowEngine workflowEngine,
            ILeaveChangeService? leaveChanges = null
            )
        {
            _dbFactory = dbFactory;
            _operationLogService = operationLogService;
            _toastService = toastService;
            //_userContextService = userContextService;
            _access = access;
            _requestService = requestService;
            _workflowEngine = workflowEngine;
            _leaveChanges = leaveChanges;
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
            if (context.IndividualId <= 0) return [];

            await using var db = await _dbFactory.CreateDbContextAsync();

            return await db.LeaveRequests
                .AsNoTracking()
                .Where(lr =>
                    lr.Leave.Job.IndividualID == context.IndividualId &&
                    lr.Leave.LeaveStateId != SharedConfig.LeaveStates.APPROVED &&
                    lr.Leave.LeaveStateId != SharedConfig.LeaveStates.REJECTED &&
                    lr.Leave.LeaveStateId != SharedConfig.LeaveStates.CANCELLED &&
                    lr.Request.RequestState.IsProcessingState &&
                    !lr.Request.RequestState.IsFinalState)
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


        public async Task<List<Leaf>> GetMyLeaveRequestsAsync(int? statusId = null, DateTime? fromDate = null, DateTime? toDate = null, int? limit = null)
        {
            var user = await _access.RequireContextAsync();
            if (user.IndividualId <= 0) return [];
            if (fromDate.HasValue && toDate.HasValue && fromDate.Value.Date > toDate.Value.Date)
                throw new ArgumentException("From date must be on or before To date.");
            if (limit is <= 0) throw new ArgumentOutOfRangeException(nameof(limit));
            await using var db = await _dbFactory.CreateDbContextAsync();
            var query = db.Leaves.AsNoTracking()
                .Include(x => x.LeaveType).Include(x => x.LeaveState)
                .Where(x => x.Job.IndividualID == user.IndividualId);
            if (statusId is > 0) query = query.Where(x => x.LeaveStateId == statusId);
            if (fromDate.HasValue) query = query.Where(x => x.ToDate >= fromDate.Value.Date);
            if (toDate.HasValue)
            {
                var endExclusive = toDate.Value.Date.AddDays(1);
                query = query.Where(x => x.FromDate < endExclusive);
            }
            // LeaveId reflects application creation order, independent of scheduled leave dates.
            query = query.OrderByDescending(x => x.LeaveId);
            if (limit.HasValue) query = query.Take(limit.Value);
            return await query.ToListAsync();
        }


        public async Task<List<JobLeaveType>> GetMyLeaveBalancesAsync(int? leaveTypeId = 0)
        {
            var context = await _access.RequireContextAsync();

                if (context.ActiveJob == null)
                return new List<JobLeaveType>();

            return await GetJobLeaveBalancesAsync(context.ActiveJob.JobId,leaveTypeId);
        }

        //public async Task<List<JobLeaveType>> GetJobLeaveBalancesAsync(int jobId, int? leaveTypeId = 0)
        //{
        //    using var db = await _dbFactory.CreateDbContextAsync();

        //    IQueryable<JobLeaveType> query = db.JobLeaveTypes
        //        .AsNoTracking()
        //        .Include(x => x.LeaveType)
        //        .Where(x =>
        //            x.JobId == jobId &&
        //            x.IsValid &&
        //            x.IsLeaveInfoUpdated == true);

        //    if (leaveTypeId > 0)
        //    {
        //        query = query.Where(x => x.LeaveTypeId == leaveTypeId);
        //    }

        //    return await query
        //        .OrderBy(x => x.LeaveType.Name)
        //        .ToListAsync();
        //}

        public async Task<List<JobLeaveType>> GetJobLeaveBalancesAsync(
          int jobId,
          int? leaveTypeId = null)
            {
                await using var db =
                    await _dbFactory.CreateDbContextAsync();

                IQueryable<JobLeaveType> query = db.JobLeaveTypes
                    .AsNoTracking()
                    .Include(x => x.LeaveType)
                    .Include(x => x.LeaveDefinition)
                    .Where(x =>
                        x.JobId == jobId &&
                        x.IsValid &&
                        x.IsLeaveInfoUpdated == true);

                if (leaveTypeId.HasValue && leaveTypeId.Value > 0)
                {
                    query = query.Where(x =>
                        x.LeaveTypeId == leaveTypeId.Value);
                }

                var balances = await query.ToListAsync();
                var organisationId = await db.Jobs.AsNoTracking()
                    .Where(x => x.JobId == jobId)
                    .Select(x => x.OrganisationID)
                    .FirstOrDefaultAsync();
                var mappings = await GetApplicationMappingsAsync(db, organisationId);

                return balances
                    .GroupBy(x =>
                    {
                        var definitionId = ResolveApplicationDefinitionId(x, x.LeaveType, mappings);
                        return definitionId.HasValue
                            ? $"definition:{definitionId.Value}"
                            : $"legacy:{x.LeaveTypeId}";
                    })
                    .Select(group => group
                        .OrderByDescending(x => x.RemainingDays > 0)
                        .ThenByDescending(x => x.LeaveDefinitionId.HasValue)
                        .ThenByDescending(x => mappings.Any(m => m.LegacyLeaveTypeId == x.LeaveTypeId))
                        .ThenByDescending(x => x.EffectiveFromDate)
                        .ThenByDescending(x => x.JobLeaveTypeId)
                        .First())
                    .OrderBy(x =>
                        x.LeaveDefinition?.Name ??
                        x.LeaveType?.Name ??
                        string.Empty)
                    .ToList();
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
                    x.JobId == context.ActiveJob.JobId &&
                    x.IsValid)
                .Include(x => x.LeaveType)
                .OrderBy(x => x.LeaveType.Name)
                .ToListAsync();
        }


        public async Task<List<Leaf>> GetLeaveRequestsAsync(int jobId)
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            return await context.Leaves
             .AsNoTracking()
             .Include(l => l.LeaveType)  // Fetches the Type name
             .Include(l => l.LeaveState) // Fetches the Status name
             .Where(l => l.JobId == jobId)
             .OrderByDescending(l => l.LeaveId)
             .ToListAsync();

        }



        public async Task<List<LeaveReasonDto>> GetReasonsByLeaveType(int? typeId)
        {
            if (!typeId.HasValue || typeId.Value <= 0)
                return new List<LeaveReasonDto>();

            using var db = await _dbFactory.CreateDbContextAsync();

            return await db.LeaveTypeReasonTypes
                .AsNoTracking()
                .Where(x => x.LeaveTypeId == typeId.Value)
                .OrderBy(x => x.ReasonTypeName)
                .Select(x => new LeaveReasonDto
                {
                    Id = x.LeaveTypeReasonTypeId,
                    Name = x.ReasonTypeName
                })
                .ToListAsync();
        }

        public async Task<LeaveApplicationTypeDto?> GetApplicationLeaveTypeAsync(int jobLeaveTypeId)
        {
            var context = await _access.RequireContextAsync();
            if (context.ActiveJob == null)
                return null;

            await using var db = await _dbFactory.CreateDbContextAsync();
            var balance = await db.JobLeaveTypes.AsNoTracking()
                .FirstOrDefaultAsync(x => x.JobLeaveTypeId == jobLeaveTypeId &&
                    x.JobId == context.ActiveJob.JobId && x.IsValid && x.IsLeaveInfoUpdated);
            if (balance == null)
                return null;

            var legacyType = await ResolveApplicationLegacyTypeAsync(
                db, balance, context.ActiveJob.OrganisationId);
            if (legacyType == null)
                return null;

            return new LeaveApplicationTypeDto
            {
                JobLeaveTypeId = balance.JobLeaveTypeId,
                LeaveTypeId = legacyType.LeaveTypeId,
                LeaveTypeName = legacyType.Name,
                RemainingDays = balance.RemainingDays ?? 0,
                IncludeHolidays = legacyType.IncludeHolidays,
                Reasons = await GetReasonsByLeaveType(legacyType.LeaveTypeId)
            };
        }

        internal static async Task<LeaveType?> ResolveApplicationLegacyTypeAsync(
            HrmTeContext db, JobLeaveType balance, int organisationId)
        {
            var legacyType = balance.LeaveTypeId.HasValue
                ? await db.LeaveTypes.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.LeaveTypeId == balance.LeaveTypeId.Value)
                : null;
            var mappings = await GetApplicationMappingsAsync(db, organisationId);
            var definitionId = ResolveApplicationDefinitionId(balance, legacyType, mappings);
            if (!definitionId.HasValue)
                return legacyType;

            var candidates = mappings.Where(x => x.LeaveDefinitionId == definitionId.Value).ToList();
            if (candidates.Count == 0)
                return legacyType;

            var candidateIds = candidates.Select(x => x.LegacyLeaveTypeId).ToArray();
            var typesWithReasons = await db.LeaveTypeReasonTypes.AsNoTracking()
                .Where(x => candidateIds.Contains(x.LeaveTypeId))
                .Select(x => x.LeaveTypeId).Distinct().ToListAsync();
            var source = candidates.FirstOrDefault(x => typesWithReasons.Contains(x.LegacyLeaveTypeId))
                ?? candidates[0];
            return await db.LeaveTypes.AsNoTracking()
                .FirstOrDefaultAsync(x => x.LeaveTypeId == source.LegacyLeaveTypeId);
        }

        private static Task<List<LeaveTypeMapping>> GetApplicationMappingsAsync(
            HrmTeContext db, int organisationId)
        {
            var today = DateTime.Today;
            return db.LeaveTypeMappings.AsNoTracking().Include(x => x.LeaveDefinition)
                .Where(x => x.IsActive &&
                    (x.OrganisationId == organisationId || x.OrganisationId == null) &&
                    (x.EffectiveFrom == null || x.EffectiveFrom <= today) &&
                    (x.EffectiveTo == null || x.EffectiveTo >= today))
                .OrderByDescending(x => x.OrganisationId.HasValue)
                .ThenByDescending(x => x.EffectiveFrom)
                .ThenByDescending(x => x.LeaveTypeMappingId)
                .ToListAsync();
        }

        private static int? ResolveApplicationDefinitionId(
            JobLeaveType balance, LeaveType? legacyType, List<LeaveTypeMapping> mappings)
        {
            if (balance.LeaveDefinitionId.HasValue)
                return balance.LeaveDefinitionId.Value;
            var direct = mappings.FirstOrDefault(x => x.LegacyLeaveTypeId == balance.LeaveTypeId);
            if (direct != null)
                return direct.LeaveDefinitionId;
            if (legacyType?.MigratedLeaveDefinitionID != null)
                return legacyType.MigratedLeaveDefinitionID;

            // Organisation copies can retain the definition's name without its ID.
            // Only accept a unique definition in the applicable mapping scope.
            var matchingIds = mappings.Where(x => string.Equals(
                    x.LeaveDefinition.Name.Trim(), legacyType?.Name.Trim(), StringComparison.OrdinalIgnoreCase))
                .Select(x => x.LeaveDefinitionId).Distinct().ToArray();
            return matchingIds.Length == 1 ? matchingIds[0] : null;
        }

        public Task<List<LeaveReasonDto>> GetReasonsByLeaveType()
        {
            throw new NotImplementedException();
        }

       
        public Task<ServiceResult> ShowDetailLeaveAsync(int? leaveId)
        {
            throw new NotImplementedException();
        }






      



        public async Task<int> GetJobIdByStaffId(int StaffId)
        {
            await _access.RequireContextAsync();
            var org = await _access.GetCurrentOrganisationIdAsync();
            if(org <= 0 || !await _access.HasOrganisationAccessAsync(org)) throw new UnauthorizedAccessException("Select an organisation you can access.");
            var today = DateTime.UtcNow.AddHours(5).Date;
            await using var db =
              await _dbFactory.CreateDbContextAsync();

            return await db.Jobs
            .AsNoTracking()
            .Where(x => x.IndividualID == StaffId && x.OrganisationID == org && x.JobStateId == SharedConfig.JobStates.APPROVED && x.JoinedDate <= today && x.TerminatedDate == null)
            .OrderByDescending(x => x.JoinedDate).ThenByDescending(x => x.JobId)
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
 

        internal static async Task<int?> ResolveHandoverThresholdAsync(HrmTeContext db, JobLeaveType balance, LeaveType legacyType, int organisationId, DateTime fromDate)
        {
            var definitionId = ResolveApplicationDefinitionId(balance, legacyType, await GetApplicationMappingsAsync(db, organisationId));
            if (!definitionId.HasValue) return null;
            var date = fromDate.Date;
            var policy = await db.LeavePolicies.AsNoTracking().Where(x => x.OrganisationId == organisationId &&
                x.LeaveDefinitionId == definitionId && x.IsActive && x.EffectiveFrom <= date && (x.EffectiveTo == null || x.EffectiveTo >= date))
                .OrderByDescending(x => x.EffectiveFrom).ThenByDescending(x => x.LeavePolicyId)
                .Select(x => new { x.RequireWorkHandover, x.WorkHandoverAfterDays }).FirstOrDefaultAsync();
            return policy?.RequireWorkHandover == true ? policy.WorkHandoverAfterDays : null;
        }
        public async Task<LeaveHandoverRulesDto> GetHandoverRulesAsync(int jobLeaveTypeId, DateTime fromDate)
        {
            var context = await _access.RequireContextAsync();
            var jobId = context.ActiveJob?.JobId ?? 0;
            await using var db = await _dbFactory.CreateDbContextAsync();
            var balance = await db.JobLeaveTypes.AsNoTracking().FirstOrDefaultAsync(x => x.JobLeaveTypeId == jobLeaveTypeId && x.JobId == jobId && x.IsValid && x.IsLeaveInfoUpdated)
                ?? throw new InvalidOperationException("Leave balance was not found.");
            var organisationId = await db.Jobs.Where(x => x.JobId == jobId).Select(x => x.OrganisationID).SingleAsync();
            var legacy = await ResolveApplicationLegacyTypeAsync(db, balance, organisationId)
                ?? throw new InvalidOperationException("Leave type mapping was not found.");
            var threshold = await ResolveHandoverThresholdAsync(db, balance, legacy, organisationId, fromDate);
            var recipients = threshold.HasValue ? await db.Jobs.AsNoTracking()
                .Where(x => x.OrganisationID == organisationId && x.IndividualID != context.BusinessEntityId && x.TerminatedDate == null && x.JobStateId == SharedConfig.JobStates.APPROVED && x.JoinedDate <= fromDate.Date && db.Users.Any(u => u.BusinessEntityID == x.IndividualID))
                .OrderBy(x => x.Individual.FirstNameEnglish)
                .Select(x => new LeaveHandoverRecipient(x.JobId, x.Individual.FirstNameEnglish + " " + (x.Individual.MiddleNameEnglish ?? "") + " " + (x.Individual.LastNameEnglish ?? "")))
                .ToListAsync() : new List<LeaveHandoverRecipient>();
            return new(threshold, recipients);
        }

        //// SUBMIT LEAVE (MAIN PART)

        public async Task<ServiceResult> SubmitLeave(LeaveApplicationDto model)
        {
            var context = await _access.RequireContextAsync();
            using var db = await _dbFactory.CreateDbContextAsync();

 

            await using var transaction =
                await db.Database.BeginTransactionAsync();

            try
            {
                int jobId = context.ActiveJob?.JobId ?? 0;

                if (jobId == 0)
                {
                    return new ServiceResult
                    {
                        Success = false,
                        Message = "Job not found for current user."
                    };
                }


                model.JobId = jobId;
                var organisationId = await db.Jobs.AsNoTracking()
                    .Where(x => x.JobId == jobId)
                    .Select(x => x.OrganisationID)
                    .FirstOrDefaultAsync();
                if (organisationId <= 0)
                    return ServiceResult.Fail("The employee's job has no organisation. Leave was not submitted.");

                var jobLeaveType = await db.JobLeaveTypes.FirstOrDefaultAsync(x =>
                    x.JobId == jobId && x.IsValid && x.IsLeaveInfoUpdated &&
                    (model.JobLeaveTypeId.HasValue
                        ? x.JobLeaveTypeId == model.JobLeaveTypeId.Value
                        : x.LeaveTypeId == model.LeaveTypeId));
                if (jobLeaveType == null)
                    return ServiceResult.Fail("Leave balance not found for selected leave type.");

                var legacyType = await ResolveApplicationLegacyTypeAsync(
                    db, jobLeaveType, organisationId);
                if (legacyType == null)
                    return ServiceResult.Fail("No active legacy leave type mapping was found for the selected leave balance.");

                // Keep the selected legacy balance's identity for existing leave
                // and cancellation paths; reasons can come from its mapped source.
                model.LeaveTypeId = jobLeaveType.LeaveTypeId ?? legacyType.LeaveTypeId;
                var reasonType = await db.LeaveTypeReasonTypes.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.LeaveTypeReasonTypeId == model.LeaveReasonId &&
                        x.LeaveTypeId == legacyType.LeaveTypeId);
                if (reasonType == null)
                    return ServiceResult.Fail("Select a valid reason for the selected leave type.");

                if (model.FromDate == default || model.ToDate < model.FromDate)
                    return ServiceResult.Fail("Select a valid start and end date.");
                var contractYearError = await LeaveContractYearBalance.ValidateSpendAsync(db, jobLeaveType, model.ToDate);
                if (contractYearError != null) return ServiceResult.Fail(contractYearError);
                model.Duration = LeaveApplicationDates.Count(model.FromDate, model.ToDate, legacyType.IncludeHolidays);
                if (model.Duration <= 0 || model.Duration > (jobLeaveType.RemainingDays ?? 0))
                    return ServiceResult.Fail("The requested days must be positive and within your available balance.");
                var handoverAfterDays = await ResolveHandoverThresholdAsync(db, jobLeaveType, legacyType, organisationId, model.FromDate);
                var needsHandover = handoverAfterDays.HasValue && model.Duration > handoverAfterDays.Value;
                var handoverTasks = model.HandoverTasks.ToList();
                if (handoverTasks.Count == 0 && model.HandoverJobId.HasValue)
                    handoverTasks.Add(new() { JobId = model.HandoverJobId, Tasks = model.HandoverNotes ?? "" });
                var handoverSupervisors = new List<int>();
                if (needsHandover)
                {
                    var handoverError = await LeaveHandoverService.ValidateTasksAsync(db, handoverTasks, organisationId, context.BusinessEntityId, model.FromDate);
                    if (handoverError != null) return ServiceResult.Fail(handoverError);
                    handoverSupervisors = await LeaveHandoverService.SupervisorsAsync(db, jobId);
                    if (handoverSupervisors.Count == 0) return ServiceResult.Fail("No active supervisor with a user account is configured for your team or department. Ask HR to assign one under Settings > Organisation > Teams and supervisors.");
                }
                var explanation = LeaveApplicationDetails.Build(model);
                if (explanation.Length > 250)
                    return ServiceResult.Fail("Explanation and additional details must total 250 characters or fewer.");
                if (!legacyType.RequestTypeId.HasValue || legacyType.RequestTypeId.Value <= 0)
                    return ServiceResult.Fail("No request type is configured for this leave type. Contact HR before applying.");

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
                        db,
                        new CreateRequestDto
                        {
                            RequestTypeId =
                                legacyType.RequestTypeId.Value,

                            OperationLogId =
                                operationLog.OperationLogId,

                            ApplicantBusinessEntityId =
                                context.BusinessEntityId,

                            OrganisationBusinessEntityId =
                                organisationId,

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
                if (needsHandover)
                {
                    db.LeaveHandoverReviews.Add(new() { LeaveId = leave.LeaveId, OperationLogId = operationLog.OperationLogId });
                    foreach (var supervisor in handoverSupervisors) db.LeaveHandoverSupervisors.Add(new() { LeaveId = leave.LeaveId, IndividualId = supervisor });
                    foreach (var task in handoverTasks) db.LeaveWorkHandOvers.Add(new HRM.Models.Archives.LeaveWorkHandOver
                        { LeaveId = leave.LeaveId, JobId = task.JobId!.Value, TaskDetails = task.Tasks.Trim(), IsValid = true });
                }
                if (reasonType != null)
                {
                    db.LeaveReasons.Add(new LeaveReason
                    {
                        LeaveId = leave.LeaveId,
                        LeaveTypeReasonTypeId = reasonType.LeaveTypeReasonTypeId,
                        Reason = string.IsNullOrWhiteSpace(explanation) ? reasonType.ReasonTypeName : explanation
                    });
                }
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

                try
                {
                    await _workflowEngine.StartAsync(db, request.RequestId, jobId);
                    // Startup chooses the first state from the employee's configured workflow.
                    // Read it back because the engine updates the request through SQL.
                    var initialState = await db.Requests.AsNoTracking()
                        .Where(x => x.RequestId == request.RequestId)
                        .Select(x => x.RequestStateId).SingleAsync();
                    if (needsHandover && initialState == SharedConfig.RequestStates.APPROVED)
                        throw new InvalidOperationException("Leave with a handover requires a pending approval stage before final HR approval.");
                    var isProcessing = await db.RequestStates.AnyAsync(x => x.RequestStateId == initialState && x.IsProcessingState && !x.IsFinalState);
                    leave.LeaveStateId = LeaveRequestStateMapper.ToLeaveState(initialState, isProcessing);
                    await db.SaveChangesAsync();
                }
                catch (InvalidOperationException ex)
                {
                    await transaction.RollbackAsync();
                    return ServiceResult.Fail(
                        $"Leave was not submitted because its approval workflow could not start. {ex.Message}");
                }

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
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            try
            {
                var leave = await db.Leaves
                    .Include(x => x.Job)
                    .FirstOrDefaultAsync(l => l.LeaveId == leaveId);

                if (leave == null)
                    return ServiceResult.Fail("Leave request not found.");
                var personId = context.IndividualId > 0 ? context.IndividualId : context.BusinessEntityId;
                if (personId <= 0 || leave.Job.IndividualID != personId)
                    return ServiceResult.Fail("You can only cancel your own leave.");
                if (leave.LeaveStateId is SharedConfig.LeaveStates.APPROVED or SharedConfig.LeaveStates.REJECTED)
                    return ServiceResult.Fail("This leave is final and cannot be cancelled as a pending request.");

                if (leave.LeaveStateId == SharedConfig.LeaveStates.CANCELLED)
                    return ServiceResult.Fail("Leave request is already cancelled.");

                // Find the Request linked to this Leave
                var request = await db.LeaveRequests
                    .Where(x => x.LeaveId == leave.LeaveId)
                    .Select(x => x.Request)
                    .FirstOrDefaultAsync();

                if (request == null)
                    return ServiceResult.Fail("Associated request not found.");
                await RequestTransitionPolicy.RequireAllowedAsync(db, request.RequestTypeId, request.RequestStateId, SharedConfig.RequestStates.CANCELLED);
                if (await db.WfRequestInstances.AnyAsync(x => x.RequestId == request.RequestId))
                {
                    await transaction.RollbackAsync();
                    if (_leaveChanges == null) return ServiceResult.Fail("Cancel this request through its approval workflow.");
                    return await _leaveChanges.ExecuteLeaveActionAsync(request.RequestId, new RequestActionSubmissionDto
                    { FromStateId = request.RequestStateId, ToStateId = SharedConfig.RequestStates.CANCELLED, Comments = "Cancelled by applicant" });
                }

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
            catch (InvalidOperationException ex)
            {
                await transaction.RollbackAsync();
                return ServiceResult.Fail(ex.Message);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

       
 
    }
}
