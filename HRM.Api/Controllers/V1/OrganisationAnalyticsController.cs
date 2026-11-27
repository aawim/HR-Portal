using HRM.Api.Infrastructure;
using HRM.Application.Contracts.Organisation;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRM.Api.Controllers.V1;

[Route("api/v1/organisation-analytics")]
public sealed class OrganisationAnalyticsController(
    IDbContextFactory<HrmTeContext> factory,
    IUserAccessService access) : HrmApiController
{
    [HttpGet("{organisationId:int}")]
    public async Task<ActionResult<OrganisationAnalyticsDto>> Get(
        int organisationId,
        [FromQuery] int? departmentId = null,
        [FromQuery] int? jobTypeId = null,
        [FromQuery] int months = 6,
        CancellationToken ct = default)
    {
        if (organisationId <= 0) return BadRequest();
        var actor = await access.RequireContextAsync();
        if (!actor.IsSuperAdministrator && !await access.HasOrganisationAccessAsync(organisationId))
            return Forbid();

        months = months is >= 3 and <= 12 ? months : 6;
        var nowUtc = DateTime.UtcNow;
        var today = nowUtc.AddHours(5).Date;
        var twelveMonthsAgo = today.AddMonths(-12);

        await using var db = await factory.CreateDbContextAsync(ct);

        var orgName = await db.Organisations.AsNoTracking()
            .Where(o => o.BusinessEntityID == organisationId)
            .Select(o => o.OrganisationName)
            .SingleOrDefaultAsync(ct);
        if (orgName is null) return NotFound();

        // Load all approved/active + recently terminated jobs for this organisation
        var allJobs = await (from j in db.Jobs.AsNoTracking()
                             join i in db.Individuals.AsNoTracking() on j.IndividualID equals i.BusinessEntityId
                             join s in db.OrganisationStructures.AsNoTracking() on j.OrganisationStructureId equals s.OrganisationStructureId into ss
                             from s in ss.DefaultIfEmpty()
                             join jt in db.JobTypes.AsNoTracking() on j.JobTypeId equals jt.JobTypeId into jts
                             from jt in jts.DefaultIfEmpty()
                             where j.OrganisationID == organisationId && j.JobStateId == 4
                             select new
                             {
                                 j.JobId,
                                 j.IndividualID,
                                 FullName = ((i.FirstNameEnglish ?? "") + " " + (i.MiddleNameEnglish ?? "") + " " + (i.LastNameEnglish ?? "")).Trim(),
                                 i.GenderTypeId,
                                 DepartmentId = j.OrganisationStructureId,
                                 DepartmentName = s != null ? s.Name : "Unassigned",
                                 JobTypeId = j.JobTypeId,
                                 JobTypeName = jt != null ? jt.TypeName : "Unspecified",
                                 j.JoinedDate,
                                 j.TerminatedDate,
                                 BasicSalary = j.BasicSalary ?? 0m
                             }).ToListAsync(ct);

        var activeAll = allJobs
            .Where(j => j.JoinedDate <= today && (!j.TerminatedDate.HasValue || j.TerminatedDate.Value.Date > today))
            .ToList();

        // Filter dropdown options (computed before applying department/jobType filter)
        var deptOptions = activeAll
            .Where(j => j.DepartmentId > 0)
            .GroupBy(j => new { j.DepartmentId, j.DepartmentName })
            .Select(g => new AnalyticsFilterOption(g.Key.DepartmentId, g.Key.DepartmentName, g.Count()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Name)
            .ToList();

        var jobTypeOptions = activeAll
            .Where(j => j.JobTypeId > 0)
            .GroupBy(j => new { j.JobTypeId, j.JobTypeName })
            .Select(g => new AnalyticsFilterOption(g.Key.JobTypeId, g.Key.JobTypeName, g.Count()))
            .OrderByDescending(x => x.Count)
            .ThenBy(x => x.Name)
            .ToList();

        // Apply optional filters
        var scopedAll = allJobs.Where(j =>
            (!departmentId.HasValue || departmentId.Value <= 0 || j.DepartmentId == departmentId.Value) &&
            (!jobTypeId.HasValue || jobTypeId.Value <= 0 || j.JobTypeId == jobTypeId.Value)).ToList();

        var active = scopedAll
            .Where(j => j.JoinedDate <= today && (!j.TerminatedDate.HasValue || j.TerminatedDate.Value.Date > today))
            .ToList();

        var totalHeadcount = active.Count;
        var permanentCount = active.Count(j => j.JobTypeName.Contains("Permanent", StringComparison.OrdinalIgnoreCase) || j.JobTypeName.Contains("Full", StringComparison.OrdinalIgnoreCase));
        var otherEmpCount = totalHeadcount - permanentCount;

        var hires12M = scopedAll.Count(j => j.JoinedDate >= twelveMonthsAgo && j.JoinedDate <= today);
        var exits12M = scopedAll.Count(j => j.TerminatedDate.HasValue && j.TerminatedDate.Value.Date >= twelveMonthsAgo && j.TerminatedDate.Value.Date <= today);

        var baseHeadcount = Math.Max(1, totalHeadcount + exits12M / 2);
        var turnoverRate = Math.Round((decimal)exits12M * 100m / baseHeadcount, 1);
        var retentionRate = Math.Max(0m, Math.Round(100m - turnoverRate, 1));

        var avgTenureYears = totalHeadcount == 0
            ? 0m
            : Math.Round((decimal)active.Average(j => Math.Max(0, (today - j.JoinedDate.Date).TotalDays) / 365.25), 1);

        var totalPayroll = active.Sum(j => j.BasicSalary);

        // Gender breakdown
        var femaleCount = active.Count(j => j.GenderTypeId == 2);
        var maleCount = active.Count(j => j.GenderTypeId == 1);
        var unspecifiedGender = totalHeadcount - femaleCount - maleCount;

        // Monthly Talent Flow (last N months)
        var startMonth = new DateTime(today.Year, today.Month, 1).AddMonths(-(months - 1));
        var flow = new List<TalentFlowPoint>(months);
        for (var i = 0; i < months; i++)
        {
            var mStart = startMonth.AddMonths(i);
            var mEnd = mStart.AddMonths(1);
            var h = scopedAll.Count(j => j.JoinedDate >= mStart && j.JoinedDate < mEnd);
            var e = scopedAll.Count(j => j.TerminatedDate.HasValue && j.TerminatedDate.Value.Date >= mStart && j.TerminatedDate.Value.Date < mEnd);
            flow.Add(new TalentFlowPoint(mStart.ToString("MMM yy"), h, e, h - e));
        }

        // Teams per department
        var teamCounts = await db.Teams.AsNoTracking()
            .Where(t => t.OrganisationId == organisationId && t.IsValid != false && t.OrganisationStructureId.HasValue)
            .GroupBy(t => t.OrganisationStructureId!.Value)
            .Select(g => new { DeptId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.DeptId, x => x.Count, ct);

        var departments = active
            .GroupBy(j => new { j.DepartmentId, j.DepartmentName })
            .Select(g =>
            {
                var count = g.Count();
                var share = totalHeadcount == 0 ? 0m : Math.Round((decimal)count * 100m / totalHeadcount, 1);
                var deptHires = scopedAll.Count(j => j.DepartmentId == g.Key.DepartmentId && j.JoinedDate >= twelveMonthsAgo && j.JoinedDate <= today);
                teamCounts.TryGetValue(g.Key.DepartmentId, out var tCount);
                return new DepartmentAnalyticsRow(
                    g.Key.DepartmentId,
                    g.Key.DepartmentName,
                    count,
                    share,
                    g.Sum(x => x.BasicSalary),
                    tCount,
                    deptHires);
            })
            .OrderByDescending(x => x.Headcount)
            .ThenBy(x => x.DepartmentName)
            .ToList();

        var jobTypeBreakdown = active
            .GroupBy(j => j.JobTypeName)
            .Select(g => new JobTypeBreakdownRow(
                g.Key,
                g.Count(),
                totalHeadcount == 0 ? 0m : Math.Round((decimal)g.Count() * 100m / totalHeadcount, 1)))
            .OrderByDescending(x => x.Count)
            .ToList();

        // Recruitment metrics
        var openVacancies = await db.RecruitmentVacancies.AsNoTracking()
            .Where(v => v.OrganisationId == organisationId && v.Status == "Published" && v.ClosingAtUtc > nowUtc)
            .Select(v => v.VacancySlots)
            .ToListAsync(ct);

        var totalApplications = await db.RecruitmentApplications.AsNoTracking()
            .CountAsync(a => a.OrganisationId == organisationId && a.Status != "Draft", ct);

        var offerStatuses = await db.RecruitmentOffers.AsNoTracking()
            .Where(o => o.OrganisationId == organisationId && o.Status != "Draft")
            .Select(o => o.Status)
            .ToListAsync(ct);

        var offersIssued = offerStatuses.Count;
        var offersAccepted = offerStatuses.Count(s => s is "Accepted" or "OnboardingSubmitted");
        var offerAcceptRate = offersIssued == 0 ? 0m : Math.Round((decimal)offersAccepted * 100m / offersIssued, 1);

        // Approved leaves YTD for scoped active jobs
        var activeJobIds = active.Select(j => j.JobId).ToArray();
        var yearStart = new DateTime(today.Year, 1, 1);
        var approvedLeavesYtd = activeJobIds.Length == 0
            ? 0
            : await db.Leaves.AsNoTracking()
                .Where(l => activeJobIds.Contains(l.JobId) && l.FromDate >= yearStart && l.LeaveStateId == 4)
                .SumAsync(l => (int?)l.Duration, ct) ?? 0;

        // Recent hires (top 8) with active position name
        var recentHireJobs = active
            .OrderByDescending(j => j.JoinedDate)
            .Take(8)
            .ToList();
        var recentHireJobIds = recentHireJobs.Select(j => j.JobId).ToArray();
        var posByJob = recentHireJobIds.Length == 0
            ? new Dictionary<int, string>()
            : (await db.JobPositions.AsNoTracking()
                .Where(jp => recentHireJobIds.Contains(jp.JobId) && (jp.ToDate == null || jp.ToDate > nowUtc))
                .Select(jp => new { jp.JobId, Name = jp.Position != null ? jp.Position.Name : "" })
                .ToListAsync(ct))
                .GroupBy(x => x.JobId)
                .ToDictionary(g => g.Key, g => g.First().Name);

        var recentHires = recentHireJobs.Select(j => new RecentHireRow(
            j.IndividualID,
            System.Text.RegularExpressions.Regex.Replace(j.FullName, @"\s+", " ").Trim(),
            posByJob.TryGetValue(j.JobId, out var pName) && !string.IsNullOrWhiteSpace(pName) ? pName : j.JobTypeName,
            j.DepartmentName,
            j.JobTypeName,
            j.JoinedDate,
            (today - j.JoinedDate.Date).TotalDays <= 90)).ToList();

        // Upcoming work anniversaries (next 60 days, >= 1 year)
        var anniversaries = new List<WorkAnniversaryRow>();
        foreach (var j in active)
        {
            if (j.JoinedDate.Year >= today.Year) continue;
            var month = j.JoinedDate.Month;
            var day = Math.Min(j.JoinedDate.Day, DateTime.DaysInMonth(today.Year, month));
            var nextAnniv = new DateTime(today.Year, month, day);
            if (nextAnniv < today)
            {
                var nextYear = today.Year + 1;
                day = Math.Min(j.JoinedDate.Day, DateTime.DaysInMonth(nextYear, month));
                nextAnniv = new DateTime(nextYear, month, day);
            }
            if ((nextAnniv - today).TotalDays <= 60)
            {
                anniversaries.Add(new WorkAnniversaryRow(
                    j.IndividualID,
                    System.Text.RegularExpressions.Regex.Replace(j.FullName, @"\s+", " ").Trim(),
                    j.DepartmentName,
                    nextAnniv.Year - j.JoinedDate.Year,
                    nextAnniv));
            }
        }
        var topAnniversaries = anniversaries
            .OrderBy(a => a.AnniversaryDate)
            .Take(8)
            .ToList();

        return Ok(new OrganisationAnalyticsDto(
            organisationId,
            orgName,
            nowUtc,
            totalHeadcount,
            permanentCount,
            otherEmpCount,
            turnoverRate,
            retentionRate,
            hires12M,
            exits12M,
            openVacancies.Count,
            openVacancies.Sum(),
            avgTenureYears,
            totalPayroll,
            approvedLeavesYtd,
            femaleCount,
            maleCount,
            unspecifiedGender,
            totalApplications,
            offersIssued,
            offersAccepted,
            offerAcceptRate,
            deptOptions,
            jobTypeOptions,
            flow,
            departments,
            jobTypeBreakdown,
            recentHires,
            topAnniversaries));
    }
}
