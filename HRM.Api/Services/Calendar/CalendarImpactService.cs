using System.Data;
using HRM.DTOs.Calendar;
using HRM.Models;
using HRM.Services.Calendar.Contracts;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Calendar;

public sealed class CalendarImpactService(
    IDbContextFactory<HrmTeContext> factory,
    IUserAccessService access,
    IOrganisationCalendarResolver resolver)
    : global::HRM.Services.Calendar.Contracts.CalendarImpactServiceContracts.IOperations
{
    private static string FormatName(Individual? ind, int id)
    {
        if (ind == null) return $"Staff #{id}";
        var name = (ind.FirstNameEnglish + " " + (ind.MiddleNameEnglish ?? "") + " " + (ind.LastNameEnglish ?? "")).Trim();
        return string.IsNullOrWhiteSpace(name) ? $"Staff #{id}" : name;
    }

    public async Task<List<LeaveImpactAssessmentDto>> GetAssessmentsForExceptionAsync(int exceptionId)
    {
        var org = await access.GetCurrentOrganisationIdAsync();
        await using var db = await factory.CreateDbContextAsync();

        var query = from a in db.CalendarImpactAssessments.AsNoTracking()
                    join e in db.OrganisationCalendarExceptions.AsNoTracking() on a.OrganisationCalendarExceptionId equals e.OrganisationCalendarExceptionId
                    join ind in db.Individuals.AsNoTracking() on a.IndividualId equals ind.BusinessEntityId into indGroup
                    from ind in indGroup.DefaultIfEmpty()
                    where a.OrganisationCalendarExceptionId == exceptionId && e.OrganisationBusinessEntityId == org
                    select new
                    {
                        a.CalendarImpactAssessmentId,
                        a.OrganisationCalendarExceptionId,
                        a.SourceRecordType,
                        a.SourceRecordId,
                        a.IndividualId,
                        Individual = ind,
                        a.AssessmentType,
                        a.PreviousValue,
                        a.ProposedValue,
                        a.ImpactDetails,
                        a.Status,
                        a.IsLockedPeriod,
                        a.AssessedDate
                    };

        var items = await query.ToListAsync();

        return items.Select(x => new LeaveImpactAssessmentDto
        {
            CalendarImpactAssessmentId = x.CalendarImpactAssessmentId,
            OrganisationCalendarExceptionId = x.OrganisationCalendarExceptionId,
            SourceRecordType = x.SourceRecordType,
            SourceRecordId = x.SourceRecordId,
            IndividualId = x.IndividualId,
            StaffName = FormatName(x.Individual, x.IndividualId),
            AssessmentType = x.AssessmentType,
            PreviousValue = x.PreviousValue,
            ProposedValue = x.ProposedValue,
            ImpactDetails = x.ImpactDetails,
            Status = x.Status,
            IsLockedPeriod = x.IsLockedPeriod,
            AssessedDate = x.AssessedDate
        }).ToList();
    }

    public async Task AssessImpactForExceptionAsync(int exceptionId)
    {
        var org = await access.GetCurrentOrganisationIdAsync();
        await using var db = await factory.CreateDbContextAsync();

        var ex = await db.OrganisationCalendarExceptions.SingleOrDefaultAsync(x => x.OrganisationCalendarExceptionId == exceptionId && x.OrganisationBusinessEntityId == org)
            ?? throw new InvalidOperationException("Exception not found.");

        var from = ex.EffectiveDate;
        var to = ex.EffectiveEndDate;

        // Find leaves overlapping this exception
        var leaves = await db.Leaves.AsNoTracking()
            .Include(l => l.Job)
            .Include(l => l.LeaveType)
            .Where(l => l.Job.OrganisationID == org &&
                        (l.LeaveStateId == 3 || l.LeaveStateId == 4) &&
                        l.FromDate <= to && l.ToDate >= from)
            .ToListAsync();

        foreach (var l in leaves)
        {
            var personId = l.Job.IndividualID;
            // Check if already assessed
            var existing = await db.CalendarImpactAssessments.SingleOrDefaultAsync(a =>
                a.OrganisationCalendarExceptionId == exceptionId &&
                a.SourceRecordType == "Leave" &&
                a.SourceRecordId == l.LeaveId &&
                a.AssessmentType == "LeaveDuration");

            var detail = $"Leave #{l.LeaveId} ({l.LeaveType?.Name}) from {l.FromDate:yyyy-MM-dd} to {l.ToDate:yyyy-MM-dd} overlaps calendar exception '{ex.Reason}' ({ex.CalendarImpactType}).";

            if (existing == null)
            {
                var assessment = new CalendarImpactAssessment
                {
                    OrganisationCalendarExceptionId = exceptionId,
                    SourceRecordType = "Leave",
                    SourceRecordId = l.LeaveId,
                    IndividualId = personId,
                    AssessmentType = "LeaveDuration",
                    PreviousValue = $"{l.FromDate:yyyy-MM-dd} to {l.ToDate:yyyy-MM-dd}",
                    ProposedValue = ex.CalendarImpactType == "MarkAsNonWorkingDay" ? "Review: Potential exclusion of non-working day from leave deduction" : "Review: Special working day overlaps leave window",
                    ImpactDetails = detail,
                    Status = "Pending",
                    IsLockedPeriod = l.ToDate < DateTime.Today.AddMonths(-1),
                    AssessedDate = DateTime.UtcNow
                };
                db.CalendarImpactAssessments.Add(assessment);
            }
            else
            {
                existing.ImpactDetails = detail;
                existing.AssessedDate = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();
    }

    public async Task ActOnAssessmentAsync(int assessmentId, string action)
    {
        var org = await access.GetCurrentOrganisationIdAsync();
        var actor = await access.RequireContextAsync();

        await using var db = await factory.CreateDbContextAsync();
        var assessment = await db.CalendarImpactAssessments
            .SingleOrDefaultAsync(a => a.CalendarImpactAssessmentId == assessmentId)
            ?? throw new InvalidOperationException("Assessment not found.");

        var hasOrgAccess = await db.OrganisationCalendarExceptions.AnyAsync(x => x.OrganisationCalendarExceptionId == assessment.OrganisationCalendarExceptionId && x.OrganisationBusinessEntityId == org);
        if (!hasOrgAccess) throw new UnauthorizedAccessException("Not authorized.");

        assessment.Status = action switch
        {
            "Reviewed" => "Reviewed",
            "Dismissed" => "Dismissed",
            "Adjusted" => "Adjusted",
            _ => "Reviewed"
        };
        assessment.ReviewedBy = actor.IndividualId;
        assessment.ReviewedDate = DateTime.UtcNow;

        await db.SaveChangesAsync();
    }
}
