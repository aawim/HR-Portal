using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Attendance;
using HRM.DTOs.Leave;
using HRM.Helpers;
using HRM.Enum;
using HRM.Models;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
using HRM.Services.Interfaces.Leave;
using HRM.Services.Leave;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public class AttendanceRequestService(
    IDbContextFactory<HrmTeContext> factory,
    IUserAccessService access,
    IAttendanceService attendance,
    ILeaveChangeService leaveChanges,
    IRequestService requests,
    IOperationLogService logs,
    IWorkflowEngine workflow) : IAttendanceRequestService
{
    public async Task<AttendanceRequestDateInfoDto> GetDateInfoAsync(DateOnly date, int? originalLogId = null)
    {
        var user = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync();
        if (date == default || date == DateOnly.MaxValue)
            return new() { CanSubmit = false, Message = "Select a valid attendance date." };
        if (originalLogId.HasValue && !await db.AttendanceLogs.AnyAsync(x =>
            x.AttendanceLogId == originalLogId && x.IndividualId == user.IndividualId &&
            x.AttendanceLogStateId == SharedConfig.AttendanceLogStates.Valid &&
            x.Date >= date.ToDateTime(TimeOnly.MinValue) && x.Date < date.ToDateTime(TimeOnly.MinValue).AddDays(1)))
            return new() { CanSubmit = false, Message = "Select one of your own uneven clock events on its original date." };
        return await DateInfoAsync(db, user.IndividualId, date, originalLogId.HasValue);
    }

    private static async Task<AttendanceRequestDateInfoDto> DateInfoAsync(HrmTeContext db, int individualId, DateOnly date, bool isCorrection = false)
    {
        if (date == default || date == DateOnly.MaxValue) return new() { CanSubmit = false, Message = "Select a valid attendance date." };
        var day = date.ToDateTime(TimeOnly.MinValue);
        var leaves = await db.Leaves.AsNoTracking().Include(x => x.Job).Include(x => x.LeaveType)
            .Where(x => x.Job.IndividualID == individualId && x.LeaveStateId == SharedConfig.LeaveStates.APPROVED &&
                x.FromDate < day.AddDays(1) && x.ToDate >= day).ToListAsync();
        if (leaves.Count == 0) return new();
        if (leaves.Count > 1) return new() { CanSubmit = false, Message = "Approved leave overlaps on this date. Contact HR to reconcile it before requesting attendance." };
        var leave = leaves[0];
        var info = new AttendanceRequestDateInfoDto { LeaveId = leave.LeaveId };
        var type = leave.LeaveType?.Name ?? "approved leave";
        if (day <= DateTime.Today)
        {
            if (isCorrection && day < DateTime.Today && await HasCheckInAsync(db, individualId, day))
            {
                info.Message = $"This past {type} date has a check-in event. An uneven-log correction can be requested.";
                return info;
            }
            info.CanSubmit = false;
            info.Message = isCorrection && day < DateTime.Today
                ? $"A correction on a past {type} date requires a check-in event for that same date."
                : $"Attendance cannot be requested for {type} on or before today.";
            return info;
        }
        if (await db.LeaveChangeRequests.AnyAsync(x => x.LeaveId == leave.LeaveId &&
            (x.Request.RequestState.IsProcessingState || x.Request.RequestStateId == SharedConfig.RequestStates.INCOMPLETE)))
        {
            info.CanSubmit = false;
            info.Message = "This leave already has an open change request. Complete or cancel it first.";
            return info;
        }
        var source = await LeaveService.ResolveApplicationLegacyTypeAsync(db,
            new JobLeaveType { LeaveTypeId = leave.LeaveTypeId }, leave.Job.OrganisationID);
        (DateTime EndDate, int Duration, int BalanceAdjustment) adjustment;
        try
        {
            adjustment = LeaveChangeRules.Calculate(leave.FromDate, leave.ToDate, leave.Duration,
                LeaveChangeKind.EarlyReturn, day, source?.IncludeHolidays ?? leave.LeaveType?.IncludeHolidays ?? false);
        }
        catch (InvalidOperationException ex)
        {
            info.CanSubmit = false;
            info.Message = ex.Message;
            return info;
        }
        info.IsEarlyReturn = true;
        info.BalanceAdjustment = adjustment.BalanceAdjustment;
        info.Message = $"This date is within {type}. Submit an early-return request for {day:dd MMM yyyy}; " +
            $"{adjustment.BalanceAdjustment} unused leave days will return to your leave balance after approval. No clock event will be created.";
        return info;
    }

    private static Task<bool> HasCheckInAsync(HrmTeContext db, int individualId, DateTime day) =>
        db.AttendanceLogs.AsNoTracking().Where(x =>
            x.IndividualId == individualId && x.AttendanceLogStateId == SharedConfig.AttendanceLogStates.Valid &&
            x.Date >= day && x.Date < day.AddDays(1))
        .AnyAsync(x =>
            x.AttendanceLogResolutions.Where(r => r.IsValid).OrderByDescending(r => r.AttendanceLogResolutionId)
                .Select(r => r.AttendanceClockTypeId).FirstOrDefault() == (int)AttendanceClockType.CheckIn ||
            (!x.AttendanceLogResolutions.Any(r => r.IsValid) && x.InOutModeId == 1));

    public async Task<ServiceResult> SubmitAsync(AttendanceRequestApplicationDto model)
    {
        var user = await access.RequireContextAsync();
        if (string.IsNullOrWhiteSpace(model.Comments) || model.Comments.Trim().Length > 250)
            return ServiceResult.Fail("Enter a reason of 250 characters or fewer.");
        if (model.Date == default) return ServiceResult.Fail("Select a valid attendance date.");
        await using var db = await factory.CreateDbContextAsync();
        AttendanceLog? original = null;
        if (model.OriginalLogId.HasValue)
        {
            original = await db.AttendanceLogs.AsNoTracking().SingleOrDefaultAsync(x =>
                x.AttendanceLogId == model.OriginalLogId && x.IndividualId == user.IndividualId &&
                x.AttendanceLogStateId == SharedConfig.AttendanceLogStates.Valid);
            if (original == null) return ServiceResult.Fail("Select one of your own uneven clock events.");
            if (model.Date != DateOnly.FromDateTime(original.Date))
                return ServiceResult.Fail("An uneven-log correction must use the original clock event's date.");
            if (!(await attendance.GetMyUnevenLogsAsync()).Any(x => x.AttendanceLogID == original.AttendanceLogId))
                return ServiceResult.Fail("This clock event is no longer listed as uneven. Refresh and select an uneven event.");
        }
        var info = await DateInfoAsync(db, user.IndividualId, model.Date, original != null);
        if (!info.CanSubmit) return ServiceResult.Fail(info.Message);
        if (info.IsEarlyReturn)
        {
            if (original != null) return ServiceResult.Fail("Use a new request to submit an early return; an existing clock event cannot be moved onto approved leave.");
            return await leaveChanges.SubmitAsync(new()
            {
                LeaveId = info.LeaveId!.Value, Kind = LeaveChangeKind.EarlyReturn,
                Date = model.Date.ToDateTime(TimeOnly.MinValue), Reason = model.Comments.Trim()
            });
        }
        if (model.InOutModeId is < 1 or > 4) return ServiceResult.Fail("Select an attendance mode.");
        var jobQuery = db.Jobs.AsNoTracking().Where(x => x.IndividualID == user.IndividualId);
        var activeJobId = user.ActiveJob?.JobId;
        var job = original != null
            ? await jobQuery.Where(x => x.OrganisationID == original.OrganisationId).OrderByDescending(x => x.JobId).FirstOrDefaultAsync()
            : await jobQuery.SingleOrDefaultAsync(x => x.JobId == activeJobId);
        if (job == null) return ServiceResult.Fail("An employment profile is required to submit this request.");
        var timestamp = model.Date.ToDateTime(model.Time);
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        try
        {
            // Recheck inside the transaction so leave or pending corrections cannot race submission.
            var currentInfo = await DateInfoAsync(db, user.IndividualId, model.Date, original != null);
            if (!currentInfo.CanSubmit || (currentInfo.LeaveId.HasValue && original == null))
                throw new InvalidOperationException("Leave on this date has changed. Refresh the request before submitting.");
            if (original != null && await db.AttendanceLogChangeRequests.AnyAsync(x =>
                x.AttendanceLog.RelatedAttendanceLogId == original.AttendanceLogId &&
                (x.Request.RequestState.IsProcessingState || x.Request.RequestStateId == SharedConfig.RequestStates.INCOMPLETE)))
                throw new InvalidOperationException("This clock event already has an open correction request.");
            if (await db.AttendanceLogRequests.AnyAsync(x => x.AttendanceLog.IndividualId == user.IndividualId &&
                x.AttendanceLog.Date == timestamp && x.AttendanceLog.InOutModeId == model.InOutModeId &&
                (x.Request.RequestState.IsProcessingState || x.Request.RequestStateId == SharedConfig.RequestStates.INCOMPLETE)))
                throw new InvalidOperationException("An attendance request already exists for this date, time and mode.");
            var operation = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.ATTENDANCE_LOG_CREATE,
                original == null ? "Attendance requested" : $"Correction requested for clock event {original.AttendanceLogId}");
            var request = await requests.CreateAsync(db, new()
            {
                RequestTypeId = original == null ? SharedConfig.RequestTypes.ATTENDANCE_LOG_REQUEST : SharedConfig.RequestTypes.ATTENDANCE_LOG_CHANGE_REQUEST,
                ApplicantBusinessEntityId = user.BusinessEntityId, OrganisationBusinessEntityId = job.OrganisationID,
                OperationLogId = operation.OperationLogId, EffectiveDate = timestamp
            });
            var proposed = new AttendanceLog
            {
                IndividualId = user.IndividualId, OrganisationId = job.OrganisationID,
                OrganisationStructureId = job.OrganisationStructureId, Date = timestamp,
                Year = timestamp.Year, Month = timestamp.Month, Day = timestamp.Day,
                Hour = timestamp.Hour, Minute = timestamp.Minute, Second = timestamp.Second,
                InOutModeId = model.InOutModeId, AttendanceLogModeId = 2,
                AttendanceLogStateId = SharedConfig.AttendanceLogStates.Invalid,
                RelatedAttendanceLogId = original?.AttendanceLogId,
                Uidstamp = Guid.NewGuid().ToString("N"), OperationLogId = operation.OperationLogId
            };
            db.AttendanceLogs.Add(proposed);
            await db.SaveChangesAsync();
            if (original == null)
                db.AttendanceLogRequests.Add(new() { AttendanceLogId = proposed.AttendanceLogId, RequestId = request.RequestId, Comments = model.Comments.Trim() });
            else
                db.AttendanceLogChangeRequests.Add(new() { AttendanceLogId = proposed.AttendanceLogId, RequestId = request.RequestId, Comments = model.Comments.Trim() });
            await db.SaveChangesAsync();
            await workflow.StartAsync(db, request.RequestId, job.JobId);
            await transaction.CommitAsync();
            return ServiceResult.Ok($"{(original == null ? "Attendance" : "Uneven-log correction")} request {request.ReferenceNumber} submitted for approval.");
        }
        catch (Exception ex) when (ex is InvalidOperationException or UnauthorizedAccessException)
        {
            await transaction.RollbackAsync();
            if (ex is WorkflowConfigurationException configuration) await configuration.NotifyAsync(factory);
            return ServiceResult.Fail(ex.Message);
        }
    }
}
