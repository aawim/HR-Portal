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
        private readonly Microsoft.AspNetCore.DataProtection.IDataProtectionProvider? _certificateProtection;

        public LeaveService(
            IDbContextFactory<HrmTeContext> dbFactory, 
            IOperationLogService operationLogService, 
            IUserContext userContextService, 
            ToastService toastService, 
            IUserAccessService access,
            IRequestService requestService,
            IWorkflowEngine workflowEngine,
            ILeaveChangeService? leaveChanges = null,
            Microsoft.AspNetCore.DataProtection.IDataProtectionProvider? certificateProtection = null
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
            _certificateProtection = certificateProtection;
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
                    .Include(x => x.LeaveType)
                    .Where(x =>
                        x.JobId == jobId &&
                        x.IsValid &&
                        x.IsLeaveInfoUpdated == true);

                await StaffDataAccess.RequireJobAsync(db, _access, jobId);

                if (leaveTypeId.HasValue && leaveTypeId.Value > 0)
                {
                    query = query.Where(x =>
                        x.LeaveTypeId == leaveTypeId.Value);
                }

                var balances = await query.ToListAsync();
                return balances.OrderBy(x => x.LeaveType?.Name ?? "").ToList();
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
            await StaffDataAccess.RequireJobAsync(context, _access, jobId);

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

        public async Task<List<LeaveApplicantJob>> GetLeaveApplicantJobsAsync(int staffId)
        {
            var org = await LeaveApplicantScope.RequireHrOrganisationAsync(_access);
            var today = DateTime.UtcNow.AddHours(5).Date;
            await using var db = await _dbFactory.CreateDbContextAsync();
            return await db.Jobs.AsNoTracking().Where(j => j.IndividualID == staffId && j.OrganisationID == org &&
                j.JobStateId == 4 && j.JoinedDate <= today && (!j.TerminatedDate.HasValue || j.TerminatedDate > today))
                .OrderByDescending(j => j.JoinedDate).Select(j => new LeaveApplicantJob(j.JobId,
                    (j.Individual.FirstNameEnglish + " " + (j.Individual.MiddleNameEnglish ?? "") + " " + (j.Individual.LastNameEnglish ?? "")).Trim(),
                    j.OrganisationStructure.Name, j.JobType.TypeName)).ToListAsync();
        }

        public async Task<List<JobLeaveType>> GetApplicantLeaveBalancesAsync(int applicantJobId)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var job = await LeaveApplicantScope.ResolveAsync(db, _access, applicantJobId);
            return await GetJobLeaveBalancesAsync(job.JobId);
        }

        public async Task<LeaveApplicationTypeDto?> GetApplicationLeaveTypeAsync(int jobLeaveTypeId, int? applicantJobId = null, DateTime? date = null)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var applicant = await LeaveApplicantScope.ResolveAsync(db, _access, applicantJobId);
            var balance = await db.JobLeaveTypes.AsNoTracking()
                .FirstOrDefaultAsync(x => x.JobLeaveTypeId == jobLeaveTypeId &&
                    x.JobId == applicant.JobId && x.IsValid && x.IsLeaveInfoUpdated);
            if (balance == null)
                return null;

            var leaveType = await ResolveApplicationTypeAsync(
                db, balance, applicant.OrganisationID, date);
            if (leaveType == null)
                return null;

            return new LeaveApplicationTypeDto
            {
                JobLeaveTypeId = balance.JobLeaveTypeId,
                LeaveTypeId = leaveType.LeaveTypeId,
                LeaveTypeName = leaveType.Name,
                RemainingDays = balance.RemainingDays ?? 0,
                IncludeHolidays = leaveType.IncludeHolidays,
                Subtypes = await LeaveSubtypeRules.OptionsAsync(db,balance,applicant.OrganisationID,date??DateTime.UtcNow.AddHours(5).Date),
                Reasons = await GetReasonsByLeaveType(leaveType.LeaveTypeId)
            };
        }

        internal static Task<LeaveType?> ResolveApplicationTypeAsync(HrmTeContext db, JobLeaveType balance, int organisationId, DateTime? date = null)
            => LeavePolicyResolver.ResolveAsync(db, balance.LeaveTypeId, organisationId, date ?? DateTime.UtcNow.AddHours(5).Date);

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
 

        private static WorkflowConfigurationException HandoverIssue(Job applicant, LeaveType type, HandoverRuleConflictException ex) => new(new HRM.DTOs.ApprovalFlow.WorkflowRequestContextDto
        { RequestTypeId=type.RequestTypeId??0, OrganisationBusinessEntityId=applicant.OrganisationID,JobTypeId=applicant.JobTypeId,SubjectJobId=applicant.JobId,LeaveTypeId=type.LeaveTypeId,LeaveRequestKind=0 },ex.Detail)
        { ConfigurationUrl="/admin/settings/leaves/work-handover-rules",AlertKind="Handover configuration needs attention" };

        public async Task<LeaveHandoverRulesDto> GetHandoverRulesAsync(int jobLeaveTypeId, DateTime fromDate, int? applicantJobId = null)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            var applicant = await LeaveApplicantScope.ResolveAsync(db, _access, applicantJobId);
            var jobId = applicant.JobId;
            var balance = await db.JobLeaveTypes.AsNoTracking().FirstOrDefaultAsync(x => x.JobLeaveTypeId == jobLeaveTypeId && x.JobId == jobId && x.IsValid && x.IsLeaveInfoUpdated)
                ?? throw new InvalidOperationException("Leave balance was not found.");
            var organisationId = await db.Jobs.Where(x => x.JobId == jobId).Select(x => x.OrganisationID).SingleAsync();
            var type = await ResolveApplicationTypeAsync(db, balance, organisationId, fromDate)
                ?? throw new InvalidOperationException("Leave type was not found.");
            EffectiveHandoverRule decision;
            try { decision=await LeaveHandoverRuleResolver.ResolveAsync(db,jobId,type.LeaveTypeId,fromDate); }
            catch(HandoverRuleConflictException ex){await HandoverIssue(applicant,type,ex).NotifyAsync(_dbFactory);throw;}
            var threshold=decision.AfterDays;
            var recipients = threshold.HasValue ? await db.Jobs.AsNoTracking()
                .Where(x => x.OrganisationID == organisationId && x.IndividualID != applicant.IndividualID && x.TerminatedDate == null && x.JobStateId == SharedConfig.JobStates.APPROVED && x.JoinedDate <= fromDate.Date && db.Users.Any(u => u.BusinessEntityID == x.IndividualID))
                .OrderBy(x => x.Individual.FirstNameEnglish)
                .Select(x => new LeaveHandoverRecipient(x.JobId, x.Individual.FirstNameEnglish + " " + (x.Individual.MiddleNameEnglish ?? "") + " " + (x.Individual.LastNameEnglish ?? "")))
                .ToListAsync() : new List<LeaveHandoverRecipient>();
            return new(threshold, recipients,decision.Source,decision.RuleId,decision.Version);
        }

        //// SUBMIT LEAVE (MAIN PART)

        public async Task<ServiceResult> SubmitLeave(LeaveApplicationDto model, int? applicantJobId = null)
        {
            var context = await _access.RequireContextAsync();
            using var db = await _dbFactory.CreateDbContextAsync();

 

            await using var transaction =
                await db.Database.BeginTransactionAsync();

            try
            {
                var applicant = await LeaveApplicantScope.ResolveAsync(db, _access, applicantJobId);
                int jobId = applicant.JobId;

                if (jobId == 0)
                {
                    return new ServiceResult
                    {
                        Success = false,
                        Message = "Job not found for current user."
                    };
                }


                model.JobId = jobId;
                await LeaveSubtypeRules.LockAsync(db,jobId);
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

                var leaveType = await ResolveApplicationTypeAsync(
                    db, jobLeaveType, organisationId, model.FromDate);
                if (leaveType == null)
                    return ServiceResult.Fail("The selected leave type is unavailable.");

                var routingError = await LeaveRequestTypeLink.ValidateAsync(db, leaveType.RequestTypeId);
                if (routingError != null)
                {
                    var issue = new WorkflowConfigurationException(new HRM.DTOs.ApprovalFlow.WorkflowRequestContextDto
                    {
                        RequestTypeId = leaveType.RequestTypeId ?? 0, OrganisationBusinessEntityId = organisationId,
                        SubjectJobId = jobId, JobTypeId = applicant.JobTypeId, ApplicantBusinessEntityId = applicant.IndividualID,
                        LeaveTypeId = leaveType.LeaveTypeId, LeaveRequestKind = 0
                    }, $"Leave type {leaveType.LeaveTypeId} ({leaveType.Name}) has an invalid application request type. Correct its request type in Settings > Leaves.")
                    { ConfigurationUrl = "/admin/settings/leaves/leave-types", AlertKind = "Leave configuration needs attention" };
                    await transaction.RollbackAsync();
                    await issue.NotifyAsync(_dbFactory);
                    return ServiceResult.Fail(WorkflowConfigurationException.StaffMessage);
                }

                model.LeaveTypeId = jobLeaveType.LeaveTypeId ?? leaveType.LeaveTypeId;
                var reasonType = await db.LeaveTypeReasonTypes.AsNoTracking()
                    .FirstOrDefaultAsync(x => x.LeaveTypeReasonTypeId == model.LeaveReasonId &&
                        x.LeaveTypeId == leaveType.LeaveTypeId);
                if (reasonType == null)
                    return ServiceResult.Fail("Select a valid reason for the selected leave type.");

                if (model.FromDate == default || model.ToDate < model.FromDate)
                    return ServiceResult.Fail("Select a valid start and end date.");
                var contractYearError = await LeaveContractYearBalance.ValidateSpendAsync(db, jobLeaveType, model.ToDate);
                if (contractYearError != null) return ServiceResult.Fail(contractYearError);
                model.Duration = LeaveApplicationDates.Count(model.FromDate, model.ToDate, leaveType.IncludeHolidays);
                if (model.Duration <= 0 || model.Duration > (jobLeaveType.RemainingDays ?? 0))
                    return ServiceResult.Fail("The requested days must be positive and within your available balance.");
                var subtypeUsage=await LeaveSubtypeRules.ReserveAsync(db,jobLeaveType,organisationId,model,_certificateProtection);
                EffectiveHandoverRule handoverRule;
                try { handoverRule=await LeaveHandoverRuleResolver.ResolveAsync(db,jobId,leaveType.LeaveTypeId,model.FromDate); }
                catch(HandoverRuleConflictException ex){await transaction.RollbackAsync();await HandoverIssue(applicant,leaveType,ex).NotifyAsync(_dbFactory);return ServiceResult.Fail(WorkflowConfigurationException.StaffMessage);}
                var needsHandover = handoverRule.Requires(model.Duration);
                var handoverTasks = model.HandoverTasks.ToList();
                if (handoverTasks.Count == 0 && model.HandoverJobId.HasValue)
                    handoverTasks.Add(new() { JobId = model.HandoverJobId, Tasks = model.HandoverNotes ?? "" });
                var handoverSupervisors = new List<int>();
                if (needsHandover)
                {
                    var handoverError = await LeaveHandoverService.ValidateTasksAsync(db, handoverTasks, organisationId, applicant.IndividualID, model.FromDate);
                    if (handoverError != null) return ServiceResult.Fail(handoverError);
                    handoverSupervisors = await LeaveHandoverService.SupervisorsAsync(db, jobId);
                    if (handoverSupervisors.Count == 0) return ServiceResult.Fail("No active supervisor with a user account is configured for your team or department. Ask HR to assign one under Settings > Organisation > Teams and supervisors.");
                }
                var explanation = LeaveApplicationDetails.Build(model);
                if (explanation.Length > 250)
                    return ServiceResult.Fail("Explanation and additional details must total 250 characters or fewer.");
                if (!leaveType.RequestTypeId.HasValue || leaveType.RequestTypeId.Value <= 0)
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
                    remarks: applicantJobId.HasValue
                        ? $"Leave application submitted by User {context.UserId} on behalf of Individual {applicant.IndividualID}, Job {jobId}."
                        : "Leave application submitted");

                await db.SaveChangesAsync();

                // CREATE REQUEST
                var request =
                    await _requestService.CreateAsync(
                        db,
                        new CreateRequestDto
                        {
                            RequestTypeId =
                                leaveType.RequestTypeId.Value,

                            OperationLogId =
                                operationLog.OperationLogId,

                            ApplicantBusinessEntityId =
                                applicant.IndividualID,

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
                    HandoverRuleSnapshot = handoverRule.Snapshot(model.Duration),
                    OperationLogId = operationLog.OperationLogId,
                    LeaveStateId = SharedConfig.LeaveStates.PENDING_VERIFICATION
                };

                db.Leaves.Add(leave);
                if(subtypeUsage!=null){subtypeUsage.Leave=leave;db.LeaveSubtypeUsages.Add(subtypeUsage);}
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
                        Address = model.Address ?? string.Empty,
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
                    if (ex is WorkflowConfigurationException configuration)
                    {
                        await configuration.NotifyAsync(_dbFactory);
                        return ServiceResult.Fail(WorkflowConfigurationException.StaffMessage);
                    }
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
            catch(LeaveSubtypeValidationException ex)
            {
                await transaction.RollbackAsync();
                return ServiceResult.Fail(ex.Message);
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
                await LeaveSubtypeRules.LockAsync(db,leave.JobId);
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
