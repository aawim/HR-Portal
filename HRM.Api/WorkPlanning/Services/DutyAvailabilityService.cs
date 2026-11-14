using HRM.Components.Shared;
using HRM.Models;
using HRM.WorkPlanning.Abstractions;
using Microsoft.EntityFrameworkCore;
namespace HRM.WorkPlanning.Services;
public sealed class DutyAvailabilityService(IDbContextFactory<HrmTeContext> factory):IDutyAvailabilityService
{public async Task<DutyAvailabilityAssessment> AssessAsync(int org,int individual,int job,DateTime start,DateTime end,CancellationToken ct=default){if(end<=start)throw new InvalidOperationException("Duty interval is invalid.");await using var db=await factory.CreateDbContextAsync(ct);var leave=await db.Leaves.AsNoTracking().Where(x=>x.JobId==job&&x.Job.OrganisationID==org&&x.Job.IndividualID==individual&&x.LeaveStateId==SharedConfig.LeaveStates.APPROVED&&x.FromDate<end&&x.ToDate>start).Select(x=>new{x.FromDate,x.ToDate,Name=x.LeaveType.Name}).FirstOrDefaultAsync(ct);return leave==null?new(true,null):new(false,$"Approved {leave.Name ?? "leave"} overlaps {leave.FromDate:dd MMM yyyy HH:mm}–{leave.ToDate:dd MMM yyyy HH:mm}.");}}
