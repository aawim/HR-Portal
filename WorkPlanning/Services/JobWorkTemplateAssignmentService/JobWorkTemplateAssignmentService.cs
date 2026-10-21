using HRM.Components.Shared;
using HRM.DTOs.Job.JobWorkTemplateAssignment;
using HRM.DTOs.StaffSchedule;
using HRM.DTOs;
using HRM.Models;
using HRM.WorkPlanning.Abstractions.JobWorkTemplateAssignment;
using Microsoft.EntityFrameworkCore;
using HRM.Models.WorkPlanning;
using HRM.DTOs.WorkPlanning;
using HRM.Services.Interfaces;

namespace HRM.WorkPlanning.Services.JobWorkTemplateAssignmentService
{
    public class JobWorkTemplateAssignmentService : IJobWorkTemplateAssignmentService
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly ILogger<JobWorkTemplateAssignmentService> _logger;
        private readonly IUserAccessService _access;
        private readonly IOperationLogService _logs;



        public JobWorkTemplateAssignmentService(
            IDbContextFactory<HrmTeContext> dbFactory,
            ILogger<JobWorkTemplateAssignmentService> logger, IUserAccessService access, IOperationLogService logs)
        {
            _dbFactory = dbFactory;
            _logger = logger;
            _access = access;
            _logs = logs;
        }


        public async Task<CurrentWorkTemplateAssignmentDto?> GetCurrentAsync(
       int jobId,
       DateTime effectiveDate,
       CancellationToken cancellationToken = default)
        {
            if (jobId <= 0)
            {
                return null;
            }

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            var date =
                effectiveDate.Date;
            await RequireJobAccessAsync(db, jobId, cancellationToken);

            return await db.JobWorkTemplateAssignments
                .AsNoTracking()
                .Where(x =>
                    x.JobID == jobId &&
                    x.SchedulePurpose == "Official" &&
                    x.IsActive &&
                    x.EffectiveFrom <= date &&
                    (
                        x.EffectiveTo == null ||
                        x.EffectiveTo >= date
                    ) &&
                    x.WorkTemplate.IsActive)
                .OrderByDescending(x =>
                    x.EffectiveFrom)
                .ThenByDescending(x =>
                    x.JobWorkTemplateAssignmentID)
                .Select(x =>
                    new CurrentWorkTemplateAssignmentDto
                    {
                        SchedulePurpose = x.SchedulePurpose,
                        ScheduleMode = x.ScheduleMode, WorkingDaysMask = x.WorkingDaysMask,
                        RotationAnchorDate = x.RotationAnchorDate, RotationTemplateIds = x.RotationTemplateIds,
                        JobWorkTemplateAssignmentId =
                            x.JobWorkTemplateAssignmentID,

                        JobId =
                            x.JobID,

                        WorkTemplateId =
                            x.WorkTemplateID,

                        TemplateName =
                            x.WorkTemplate.Name,

                        TemplateCode =
                            x.WorkTemplate.Code,

                        TemplateTypeName =
                            x.WorkTemplate.WorkTemplateType.Name,

                        EffectiveFrom =
                            x.EffectiveFrom,

                        EffectiveTo =
                            x.EffectiveTo,

                        IsActive =
                            x.IsActive,
  
                       DefaultStartTime = x.WorkTemplate.DefaultStartTime.HasValue
                                        ? DateTime.MinValue.Add(x.WorkTemplate.DefaultStartTime.Value.ToTimeSpan())
                                        : null,

                        DefaultEndTime = x.WorkTemplate.DefaultEndTime.HasValue
                                        ? DateTime.MinValue.Add(x.WorkTemplate.DefaultEndTime.Value.ToTimeSpan())
                                        : null


                    })
                .FirstOrDefaultAsync(
                    cancellationToken);
        }

        public async Task<List<JobWorkTemplateAssignmentHistoryDto>>
            GetHistoryAsync(
                int jobId,
                CancellationToken cancellationToken = default)
        {
            if (jobId <= 0)
            {
                return [];
            }

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            var today =
                DateTime.UtcNow.AddHours(5).Date;
            await RequireJobAccessAsync(db, jobId, cancellationToken);

            return await db.JobWorkTemplateAssignments
                .AsNoTracking()
                .Where(x =>
                    x.JobID == jobId)
                .OrderByDescending(x =>
                    x.EffectiveFrom)
                .ThenByDescending(x =>
                    x.JobWorkTemplateAssignmentID)
                .Select(x =>
                    new JobWorkTemplateAssignmentHistoryDto
                    {
                        SchedulePurpose = x.SchedulePurpose,
                        JobWorkTemplateAssignmentId =
                            x.JobWorkTemplateAssignmentID,

                        WorkTemplateId =
                            x.WorkTemplateID,

                        TemplateName =
                            x.WorkTemplate.Name,

                        EffectiveFrom =
                            x.EffectiveFrom,

                        EffectiveTo =
                            x.EffectiveTo,

                        IsActive =
                            x.IsActive,

                        IsCurrent =
                            x.IsActive &&
                            x.EffectiveFrom <= today &&
                            (
                                x.EffectiveTo == null ||
                                x.EffectiveTo >= today
                            )
                    })
                .ToListAsync(
                    cancellationToken);
        }

        public async Task<List<WorkTemplateLookupDto>>
            SearchTemplatesAsync(
                int organisationBusinessEntityId,
                string? searchText,
                CancellationToken cancellationToken = default)
        {
            if (organisationBusinessEntityId <= 0)
            {
                return [];
            }

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            var query =
                db.WorkTemplates
                    .AsNoTracking()
                    .Where(x =>
                        x.IsActive &&
                        (
                            x.IsGlobal ||
                            x.OrganisationBusinessEntityId == 
                                organisationBusinessEntityId
                        ));
            await _access.RequireContextAsync();
            if (organisationBusinessEntityId != await _access.GetCurrentOrganisationIdAsync() ||
                !await _access.HasOrganisationAccessAsync(organisationBusinessEntityId))
                throw new UnauthorizedAccessException("Work templates are restricted to the current organisation.");

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var term =
                    searchText.Trim();

                query = query.Where(x =>
                    x.Name.Contains(term) ||
                    (
                        x.Code != null &&
                        x.Code.Contains(term)
                    ) ||
                    (
                        x.Description != null &&
                        x.Description.Contains(term)
                    ));
            }

            return await query
                .OrderBy(x =>
                    x.Name)
                .Select(x =>
                    new WorkTemplateLookupDto
                    {
                        WorkTemplateId = x.WorkTemplateId,

                        Name = x.Name,

                        Code = x.Code,

                        TemplateTypeName = x.WorkTemplateType.Name,

                        DefaultStartTime = x.DefaultStartTime,

                        DefaultEndTime = x.DefaultEndTime,

                        EndsNextDay = x.EndsNextDay
                    })
                .Take(50)
                .ToListAsync(
                    cancellationToken);
        }

        public Task<ServiceResult> AssignAsync(AssignJobWorkTemplateDto dto, CancellationToken cancellationToken = default) =>
            AssignCoreAsync(dto, null, cancellationToken);

        public Task<ServiceResult> AssignDutyAsync(AssignDutyScheduleDto duty, CancellationToken cancellationToken = default)
        {
            if (duty == null || string.IsNullOrWhiteSpace(duty.Name) || duty.Name.Trim().Length > 100 || duty.StartTime == duty.EndTime ||
                duty.StartTime.Ticks % TimeSpan.TicksPerMinute != 0 || duty.EndTime.Ticks % TimeSpan.TicksPerMinute != 0 ||
                duty.Period is not ("OneDay" or "DateRange" or "Ongoing") || duty.FromDate == default ||
                (duty.Period == "DateRange" && (!duty.ToDate.HasValue || duty.ToDate.Value.Date < duty.FromDate.Date)))
                return Task.FromResult(ServiceResult.Failed("Enter a duty name, different start and end times, and valid dates."));
            var request = new AssignJobWorkTemplateDto { JobId = duty.JobId, IndividualId = duty.IndividualId,
                OrganisationBusinessEntityId = duty.OrganisationBusinessEntityId, SchedulePurpose = "Duty", EffectiveFrom = duty.FromDate.Date,
                EffectiveTo = duty.Period == "OneDay" ? duty.FromDate.Date : duty.Period == "DateRange" ? duty.ToDate!.Value.Date : null };
            return AssignCoreAsync(request, duty, cancellationToken);
        }

        private async Task<ServiceResult> AssignCoreAsync(AssignJobWorkTemplateDto dto, AssignDutyScheduleDto? duty, CancellationToken ct)
        {
            if (dto == null) return ServiceResult.Failed("Select a staff member and schedule.");
            var validation = ValidateAssignmentRequest(dto, duty == null);
            if (validation != null) return ServiceResult.Failed(validation);
            if (dto.SchedulePurpose is not ("Official" or "Duty")) return ServiceResult.Failed("Choose official hours or duty.");
            if (dto.ScheduleMode is not ("Standard" or "Weekly" or "Rotation") || dto.WorkingDaysMask < 1 || dto.WorkingDaysMask > 127)
                return ServiceResult.Failed("Choose a valid schedule pattern and at least one working day.");
            if (dto.ScheduleMode == "Rotation" && (dto.SchedulePurpose != "Official" || !dto.RotationAnchorDate.HasValue ||
                dto.RotationTemplateIds.Count is < 1 or > 90 || dto.RotationTemplateIds.Any(x => x < 0) || !dto.RotationTemplateIds.Any(x => x > 0)))
                return ServiceResult.Failed("Add 1–90 cycle days with at least one shift and choose the date for cycle day 1. Use 0 for rest days.");
            await _access.RequireContextAsync();
            if (!await _access.HasPermissionAsync("Schedule.Attach") || dto.OrganisationBusinessEntityId != await _access.GetCurrentOrganisationIdAsync() ||
                !await _access.HasOrganisationAccessAsync(dto.OrganisationBusinessEntityId))
                return ServiceResult.Failed("You do not have permission to change schedules for this organisation. Ask your administrator for scheduling access.");
            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            if (!await db.Jobs.AnyAsync(x => x.JobId == dto.JobId && x.IndividualID == dto.IndividualId &&
                x.OrganisationID == dto.OrganisationBusinessEntityId && x.JobStateId == SharedConfig.JobStates.APPROVED && x.TerminatedDate == null, ct))
                return ServiceResult.Failed("This staff member does not have an active job in your organisation.");
            await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
            try
            {
                var from = dto.EffectiveFrom.Date;
                var to = dto.EffectiveTo?.Date;
                if (dto.ScheduleMode == "Rotation")
                {
                    var ids = dto.RotationTemplateIds.Where(x => x > 0).Distinct().ToList();
                    var available = await db.WorkTemplates.Where(x => ids.Contains(x.WorkTemplateId) && x.IsActive &&
                        (x.IsGlobal || x.OrganisationBusinessEntityId == dto.OrganisationBusinessEntityId) &&
                        (!x.EffectiveFrom.HasValue || x.EffectiveFrom <= from) &&
                        (!x.EffectiveTo.HasValue || (to.HasValue && x.EffectiveTo >= to.Value)))
                        .Select(x => x.WorkTemplateId).ToListAsync(ct);
                    if (available.Count != ids.Count) return ServiceResult.Failed("Every cycle shift must be available in your organisation for the selected dates.");
                }
                if (duty != null)
                {
                    var type = await db.WorkTemplateTypes.Where(x => x.Code == "DUTY" && x.IsActive).Select(x => (int?)x.WorkTemplateTypeId).SingleOrDefaultAsync(ct);
                    var clockTypes = await db.WorkSegmentTypes.Where(x => x.IsActive && (x.Code == "CHECK_IN" || x.Code == "CHECK_OUT")).ToListAsync(ct);
                    if (!type.HasValue || clockTypes.Count != 2) return ServiceResult.Failed("Duty schedules are not configured yet. Ask an administrator to enable Duty, Check in and Check out.");
                    var duration = (int)(duty.EndTime.ToTimeSpan() - duty.StartTime.ToTimeSpan()).TotalMinutes;
                    if (duration <= 0) duration += 1440;
                    var log = await _logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.WORK_ASSIGNMENT_CREATE,
                        $"Duty assignment: tenant {dto.OrganisationBusinessEntityId}, job {dto.JobId}, {duty.Name.Trim()}, {duty.StartTime:HH:mm}–{duty.EndTime:HH:mm}, {from:yyyy-MM-dd} through {to:yyyy-MM-dd}; check-in required {duty.RequireCheckIn}, check-out required {duty.RequireCheckOut}.");
                    var created = new WorkTemplate { Name = duty.Name.Trim(), Code = "DUTY-" + Guid.NewGuid().ToString("N"),
                        WorkTemplateTypeId = type.Value, OrganisationBusinessEntityId = dto.OrganisationBusinessEntityId,
                        DefaultStartTime = duty.StartTime, DefaultEndTime = duty.EndTime, EndsNextDay = duty.EndTime < duty.StartTime,
                        IsGlobal = false, IsActive = true, IsRepeatable = true, RequiresAttendance = duty.RequireCheckIn || duty.RequireCheckOut,
                        RequiresCheckOut = duty.RequireCheckOut, OperationLogId = log.OperationLogId };
                    created.WorkTemplateSegments.Add(new WorkTemplateSegment { WorkSegmentTypeId = clockTypes.Single(x => x.Code == "CHECK_IN").WorkSegmentTypeId,
                        Name = "Check in", SequenceNumber = 1, OffsetMinutes = 0, DurationMinutes = 30, IsMandatory = duty.RequireCheckIn,
                        RequiresAttendance = duty.RequireCheckIn, IsActive = true, IsPaid = true, OperationLogId = log.OperationLogId, CreatedAt = DateTime.UtcNow });
                    created.WorkTemplateSegments.Add(new WorkTemplateSegment { WorkSegmentTypeId = clockTypes.Single(x => x.Code == "CHECK_OUT").WorkSegmentTypeId,
                        Name = "Check out", SequenceNumber = 2, OffsetMinutes = duration, DurationMinutes = 30, IsMandatory = duty.RequireCheckOut,
                        RequiresAttendance = duty.RequireCheckOut, IsActive = true, IsPaid = true, OperationLogId = log.OperationLogId, CreatedAt = DateTime.UtcNow });
                    db.WorkTemplates.Add(created); await db.SaveChangesAsync(ct);
                    dto.WorkTemplateId = created.WorkTemplateId;
                }
                var template = await db.WorkTemplates.AsNoTracking().SingleOrDefaultAsync(x => x.WorkTemplateId == dto.WorkTemplateId && x.IsActive &&
                    (x.IsGlobal || x.OrganisationBusinessEntityId == dto.OrganisationBusinessEntityId), ct);
                if (template == null) return ServiceResult.Failed("This schedule is no longer available in your organisation.");
                if (template.EffectiveFrom.HasValue && from < template.EffectiveFrom.Value.Date)
                    return ServiceResult.Failed($"These working hours are available from {template.EffectiveFrom.Value:dd MMM yyyy}. Choose that date or later.");
                if (template.EffectiveTo.HasValue && (from > template.EffectiveTo.Value.Date || !to.HasValue || to.Value > template.EffectiveTo.Value.Date))
                    return ServiceResult.Failed($"These working hours are available until {template.EffectiveTo.Value:dd MMM yyyy}. Choose an end date on or before that day.");
                var overlaps = await db.JobWorkTemplateAssignments.Where(x => x.JobID == dto.JobId && x.SchedulePurpose == dto.SchedulePurpose && x.IsActive &&
                    (!to.HasValue || x.EffectiveFrom <= to.Value) && (x.EffectiveTo == null || x.EffectiveTo >= from)).ToListAsync(ct);
                if (duty == null)
                    await _logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.WORK_ASSIGNMENT_CREATE,
                        $"Schedule assignment: tenant {dto.OrganisationBusinessEntityId}, job {dto.JobId}, {dto.SchedulePurpose}, mode {dto.ScheduleMode}, template {dto.WorkTemplateId}, from {from:yyyy-MM-dd} to {to:yyyy-MM-dd}; weekdays {dto.WorkingDaysMask}, cycle anchor {dto.RotationAnchorDate:yyyy-MM-dd}, cycle {System.Text.Json.JsonSerializer.Serialize(dto.RotationTemplateIds)}.");
                foreach (var old in overlaps)
                {
                    var previousEnd = old.EffectiveTo;
                    if (to.HasValue && (!previousEnd.HasValue || previousEnd.Value > to.Value))
                        db.JobWorkTemplateAssignments.Add(new JobWorkTemplateAssignment { JobID = old.JobID, WorkTemplateID = old.WorkTemplateID,
                            SchedulePurpose = old.SchedulePurpose, ScheduledStartTime = old.ScheduledStartTime, EffectiveFrom = to.Value.AddDays(1),
                            ScheduleMode = old.ScheduleMode, WorkingDaysMask = old.WorkingDaysMask,
                            RotationAnchorDate = old.RotationAnchorDate, RotationTemplateIds = old.RotationTemplateIds,
                            EffectiveTo = previousEnd, IsActive = true, CreatedDate = DateTime.UtcNow });
                    if (old.EffectiveFrom < from) old.EffectiveTo = from.AddDays(-1);
                    else old.IsActive = false;
                }
                db.JobWorkTemplateAssignments.Add(new JobWorkTemplateAssignment { JobID = dto.JobId, WorkTemplateID = dto.WorkTemplateId,
                    ScheduleMode = dto.ScheduleMode, WorkingDaysMask = dto.ScheduleMode == "Standard" ? 31 : dto.WorkingDaysMask,
                    RotationAnchorDate = dto.ScheduleMode == "Rotation" ? dto.RotationAnchorDate?.Date : null,
                    RotationTemplateIds = dto.ScheduleMode == "Rotation" ? System.Text.Json.JsonSerializer.Serialize(dto.RotationTemplateIds) : null,
                    SchedulePurpose = dto.SchedulePurpose, EffectiveFrom = from, EffectiveTo = to, IsActive = true, CreatedDate = DateTime.UtcNow });
                await InvalidateUnusedPlansAsync(db, dto.JobId, from, to, ct);
                await db.SaveChangesAsync(ct); await tx.CommitAsync(ct);
                return ServiceResult.Ok($"{dto.SchedulePurpose} schedule '{template.Name}' saved.");
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Saving {Purpose} schedule for job {JobId}", dto.SchedulePurpose, dto.JobId);
                return ServiceResult.Failed("The schedule could not be saved. Please try again.");
            }
        }

        public async Task<ServiceResult> RemoveAsync(
            RemoveJobWorkTemplateDto dto,
            CancellationToken cancellationToken = default)
        {
            if (dto is null ||
                dto.JobWorkTemplateAssignmentId <= 0 ||
                dto.JobId <= 0)
            {
                return ServiceResult.Failed(
                    "A valid template assignment is required.");
            }

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            var assignment =
                await db.JobWorkTemplateAssignments
                    .SingleOrDefaultAsync(
                        x =>
                            x.JobWorkTemplateAssignmentID ==
                                dto.JobWorkTemplateAssignmentId &&
                            x.JobID == dto.JobId &&
                            x.IsActive,
                        cancellationToken);

            await _access.RequireContextAsync();
            var org = await _access.GetCurrentOrganisationIdAsync();
            if (!await _access.HasPermissionAsync("Schedule.Attach") || !await _access.HasOrganisationAccessAsync(org) ||
                !await db.Jobs.AnyAsync(x => x.JobId == dto.JobId && x.OrganisationID == org, cancellationToken))
                return ServiceResult.Failed("Schedule.Attach permission and access to this organisation are required.");

            if (assignment is null)
            {
                return ServiceResult.Failed(
                    "The active work-template assignment was not found.");
            }

            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
            var removalDate =
                dto.EffectiveDate.Date;
            if (assignment.EffectiveTo.HasValue && removalDate > assignment.EffectiveTo.Value.Date.AddDays(1))
                return ServiceResult.Failed("This schedule already ends earlier. Use Change to extend it.");

            if (removalDate <
                assignment.EffectiveFrom.Date)
            {
                return ServiceResult.Failed(
                    "The removal date cannot be before the assignment's effective date.");
            }

            if (removalDate ==
                assignment.EffectiveFrom.Date)
            {
                /*
                 * The assignment never becomes effective.
                 */
                assignment.IsActive =
                    false;

                assignment.EffectiveTo =
                    removalDate.AddDays(-1);
            }
            else
            {
                /*
                 * Preserve the assignment as valid history up to
                 * the day before removal.
                 */
                assignment.EffectiveTo =
                    removalDate.AddDays(-1);
            }

            await InvalidateUnusedPlansAsync(db, dto.JobId, removalDate, null, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ServiceResult.Ok("The work-template assignment was removed successfully.");
        }

        private static async Task InvalidateUnusedPlansAsync(HrmTeContext db, int jobId, DateTime from, DateTime? to, CancellationToken ct)
        {
            // Rebuild only unused generated plans. Attendance and finalized/manual plans retain their history.
            var plans = await db.WorkPlans.Include(x => x.WorkPlanSegments)
                .Include(x => x.WorkAssignments).ThenInclude(x => x.WorkAssignmentSegments)
                .Include(x => x.WorkAssignments).ThenInclude(x => x.WorkAssignmentOwners)
                .Where(x => x.JobId == jobId && x.IsValid && x.IsGenerated && !x.IsManual && !x.IsFinalized &&
                    x.WorkDate >= from && (!to.HasValue || x.WorkDate <= to.Value) &&
                    !db.AttendanceLogResolutions.Any(a => a.WorkPlanId == x.WorkPlanId) &&
                    !db.AttendanceLogs.Any(a => a.IndividualId == x.IndividualId && a.OrganisationId == x.OrganisationBusinessEntityId && a.Date.Date == x.WorkDate.Date) &&
                    !x.WorkPlanSegments.Any(s => s.AttendanceId.HasValue || s.IsCompleted))
                .ToListAsync(ct);
            foreach (var plan in plans)
            {
                plan.IsValid = false;
                foreach (var segment in plan.WorkPlanSegments) segment.IsValid = false;
                foreach (var assignment in plan.WorkAssignments)
                {
                    assignment.IsValid = false;
                    foreach (var segment in assignment.WorkAssignmentSegments) segment.IsValid = false;
                    foreach (var owner in assignment.WorkAssignmentOwners) owner.IsValid = false;
                }
            }
        }

        private async Task RequireJobAccessAsync(HrmTeContext db, int jobId, CancellationToken ct)
        {
            await _access.RequireContextAsync();
            var org = await _access.GetCurrentOrganisationIdAsync();
            if (!await _access.HasOrganisationAccessAsync(org) ||
                !await db.Jobs.AnyAsync(x => x.JobId == jobId && x.OrganisationID == org, ct))
                throw new UnauthorizedAccessException("Staff schedules are restricted to the current organisation.");
        }

        private static string? ValidateAssignmentRequest(
            AssignJobWorkTemplateDto dto, bool requireTemplate = true)
        {
            if (dto.JobId <= 0)
            {
                return "A valid job is required.";
            }

            if (dto.IndividualId <= 0)
            {
                return "A valid staff member is required.";
            }

            if (dto.OrganisationBusinessEntityId <= 0)
            {
                return "A valid organisation is required.";
            }

            if (requireTemplate && dto.WorkTemplateId <= 0)
            {
                return "A valid work template is required.";
            }

            if (dto.EffectiveFrom == default)
            {
                return "An effective-from date is required.";
            }

            if (dto.EffectiveTo.HasValue &&
                dto.EffectiveTo.Value.Date <
                dto.EffectiveFrom.Date)
            {
                return "The effective-to date cannot be before the effective-from date.";
            }

            return null;
        }




    }
}
