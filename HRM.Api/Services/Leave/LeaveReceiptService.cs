using HRM.Components.Shared;
using HRM.DTOs.Leave;
using HRM.Models;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.Leave;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Leave;

public sealed class LeaveReceiptService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access) : ILeaveReceiptService
{
    public async Task<LeaveReceiptDto?> GetMyReceiptAsync(int leaveId)
    {
        var user = await access.RequireContextAsync();
        if (user.IndividualId <= 0) return null;
        await using var db = await factory.CreateDbContextAsync();
        // Scope ownership in SQL, including the employee's previous jobs.
        var leave = await db.Leaves.AsNoTracking()
            .Include(x => x.Job).ThenInclude(x => x.Individual)
            .Include(x => x.Job).ThenInclude(x => x.Organisation)
            .Include(x => x.LeaveType)
            .SingleOrDefaultAsync(x => x.LeaveId == leaveId && x.Job.IndividualID == user.IndividualId &&
                x.LeaveStateId == SharedConfig.LeaveStates.APPROVED);
        if (leave == null) return null;

        var request = await db.LeaveRequests.AsNoTracking().Where(x => x.LeaveId == leaveId)
            .Select(x => x.Request).FirstOrDefaultAsync();
        // Do not infer approval from a pending legacy request linked to an approved leave.
        var approvedRequest = request?.RequestStateId == SharedConfig.RequestStates.APPROVED;
        var approver = approvedRequest && request!.LastStateChangedByUserId.HasValue
            ? await db.Users.AsNoTracking().Where(x => x.UserId == request.LastStateChangedByUserId)
                .Select(x => x.FullName ?? x.Username).SingleOrDefaultAsync() : null;
        var balances = await db.JobLeaveTypes.AsNoTracking()
            .Where(x => x.JobId == leave.JobId && x.LeaveTypeId == leave.LeaveTypeId && x.IsValid)
            .OrderByDescending(x => x.EffectiveFromDate).ThenByDescending(x => x.JobLeaveTypeId)
            .Select(x => x.RemainingDays).Take(2).ToListAsync();
        // Multiple active balances need reconciliation; never guess a balance for the receipt.
        var balance = balances.Count == 1 ? balances[0] : null;
        var afterLeave = leave.ToDate.Date.AddDays(1);
        var through = afterLeave.AddDays(90);
        var reporting = await (
            from segment in db.WorkPlanSegments.AsNoTracking()
            join plan in db.WorkPlans.AsNoTracking() on segment.WorkPlanId equals plan.WorkPlanId
            where plan.JobId == leave.JobId && plan.IndividualId == user.IndividualId && plan.IsValid &&
                segment.IsValid && segment.RequiresAttendance && plan.WorkDate >= afterLeave && plan.WorkDate < through &&
                !db.Leaves.Any(l => l.Job.IndividualID == user.IndividualId && l.LeaveStateId == SharedConfig.LeaveStates.APPROVED &&
                    l.FromDate < plan.WorkDate.Date.AddDays(1) && l.ToDate >= plan.WorkDate.Date)
            orderby plan.WorkDate, segment.StartDateTime
            select (DateTime?)plan.WorkDate).FirstOrDefaultAsync();
        var person = leave.Job.Individual;
        return new LeaveReceiptDto
        {
            LeaveId = leave.LeaveId, Reference = request?.ReferenceNumber ?? leave.ChitNo ?? $"LEAVE-{leave.LeaveId}",
            Organisation = leave.Job.Organisation.OrganisationName,
            OrganisationDhivehi = leave.Job.Organisation.OrganisationNameDhivehi,
            RegistrationNumber = leave.Job.Organisation.RegistrationNumber,
            StaffName = string.Join(" ", new[] { person.FirstNameEnglish, person.MiddleNameEnglish, person.LastNameEnglish }
                .Where(x => !string.IsNullOrWhiteSpace(x))), StaffNumber = leave.Job.Sapnumber,
            LeaveType = leave.LeaveType.Name, FromDate = leave.FromDate, ToDate = leave.ToDate, Days = leave.Duration,
            CurrentBalance = balance, ReportingDate = reporting,
            ApprovedBy = approver, ApprovedAt = approvedRequest ? request!.LastStateChangedDate : null
        };
    }
}
