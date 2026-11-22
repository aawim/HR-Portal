using HRM.Models;
using HRM.Models.WorkPlanning;
using HRM.WorkPlanning.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HRM.WorkPlanning.Services;

public sealed class WorkforceRosterService(IDbContextFactory<HrmTeContext> factory, IDutyAvailabilityService availability, IWorkforceConstraintService constraints) : IWorkforceRosterService
{
    private static readonly HRM.WorkPlanning.WorkPlanGenerationGate MutationGate = new();
    private static Task<IDisposable> EnterMutationAsync(int organisationId, CancellationToken ct) => MutationGate.EnterAsync(organisationId, 0, DateOnly.MinValue, ct);
    private static async Task LockMutationAsync(HrmTeContext db, int organisationId, CancellationToken ct)
    {
        if (!db.Database.IsSqlServer()) return;
        var resource = $"HRM.WorkforceRoster:{organisationId}";
        await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource={resource}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=5000; IF @r < 0 THROW 51000, 'Roster is busy. Refresh and try again.', 1;", ct);
    }
    public async Task<WorkforceRosterDraft> CreateDraftAsync(CreateWorkforceRosterRequest request, CancellationToken cancellationToken = default)
    {
        if (request.EndsOn < request.StartsOn) throw new InvalidOperationException("The roster end date cannot precede its start date.");
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200) throw new InvalidOperationException("Enter a roster name of up to 200 characters.");
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var roster = new WorkforceRoster { OrganisationId = request.OrganisationId, Name = request.Name.Trim(), StartsOn = request.StartsOn.ToDateTime(TimeOnly.MinValue), EndsOn = request.EndsOn.ToDateTime(TimeOnly.MinValue), OperationLogId = request.OperationLogId };
        var version = new WorkforceRosterVersion { WorkforceRoster = roster, VersionNumber = 1, CreatedAtUtc = DateTime.UtcNow, CreatedByUserId = request.ActorUserId, OperationLogId = request.OperationLogId };
        db.WorkforceRosterVersions.Add(version); await db.SaveChangesAsync(cancellationToken);
        return new WorkforceRosterDraft(roster.Id, version.Id, version.VersionNumber);
    }

    public async Task<WorkforceRosterValidation> ValidateAsync(int rosterVersionId, int? organisationId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        return await ValidateCoreAsync(db, rosterVersionId, organisationId, cancellationToken);
    }

    private async Task<WorkforceRosterValidation> ValidateCoreAsync(HrmTeContext db, int rosterVersionId, int? organisationId, CancellationToken cancellationToken)
    {
        var version = await db.WorkforceRosterVersions.AsNoTracking().Include(x => x.WorkforceRoster).SingleOrDefaultAsync(x => x.Id == rosterVersionId, cancellationToken) ?? throw new InvalidOperationException("Roster version not found.");
        if (organisationId.HasValue && version.WorkforceRoster.OrganisationId != organisationId.Value) throw new UnauthorizedAccessException("The roster belongs to another organisation.");
        var occurrences = await db.DutyOccurrences.AsNoTracking().Where(x => x.WorkforceRosterVersionId == rosterVersionId).ToListAsync(cancellationToken);
        var errors = new List<string>();
        if (occurrences.Count == 0) errors.Add("Add at least one duty occurrence before publication.");
        if (occurrences.Any(x => x.EndAt <= x.StartAt)) errors.Add("Every duty occurrence must end after it starts.");
        if (occurrences.Any(x => x.StartAt.Date < version.WorkforceRoster.StartsOn.Date || x.EndAt.AddTicks(-1).Date > version.WorkforceRoster.EndsOn.Date)) errors.Add("Duty occurrences must fall within the roster period.");
        if (occurrences.Any(x => x.RequiredStaffCount < 1)) errors.Add("Every duty occurrence must require at least one staff member.");
        var assignedCounts = await db.DutyAssignments.AsNoTracking()
            .Where(x => occurrences.Select(o => o.Id).Contains(x.DutyOccurrenceId) && x.Status == "Assigned")
            .GroupBy(x => x.DutyOccurrenceId).Select(x => new { OccurrenceId = x.Key, Count = x.Count() })
            .ToDictionaryAsync(x => x.OccurrenceId, x => x.Count, cancellationToken);
        foreach (var occurrence in occurrences)
        {
            var assigned = assignedCounts.GetValueOrDefault(occurrence.Id);
            if (assigned < occurrence.RequiredStaffCount)
                errors.Add($"Duty occurrence {occurrence.Id} requires {occurrence.RequiredStaffCount} staff but has {assigned} assigned.");
        }
        var assignedDuties = await db.DutyAssignments.AsNoTracking()
            .Where(x => occurrences.Select(o => o.Id).Contains(x.DutyOccurrenceId) && x.Status == "Assigned")
            .Select(x => new { x.JobId, x.IndividualId, x.DutyOccurrenceId, x.DutyOccurrence.StartAt, x.DutyOccurrence.EndAt })
            .ToListAsync(cancellationToken);
        var staffIds = assignedDuties.Select(x => x.IndividualId).Distinct().ToList();
        var publishedDuties = await db.DutyAssignments.AsNoTracking().Where(x => x.Status == "Assigned" && staffIds.Contains(x.IndividualId) &&
            x.DutyOccurrence.WorkforceRosterVersionId != rosterVersionId && x.DutyOccurrence.Status == "Planned" &&
            x.DutyOccurrence.WorkforceRosterVersion.Status == "Published" && x.DutyOccurrence.WorkforceRosterVersion.WorkforceRoster.Status == "Published" &&
            x.DutyOccurrence.WorkforceRosterVersion.WorkforceRoster.OrganisationId == version.WorkforceRoster.OrganisationId)
            .Select(x => new { x.JobId, x.IndividualId, x.DutyOccurrenceId, x.DutyOccurrence.StartAt, x.DutyOccurrence.EndAt }).ToListAsync(cancellationToken);
        foreach (var duty in assignedDuties)
            foreach (var other in publishedDuties.Where(x => x.IndividualId == duty.IndividualId && x.StartAt < duty.EndAt && x.EndAt > duty.StartAt))
                errors.Add($"Staff member {duty.IndividualId} has overlapping duty occurrences {duty.DutyOccurrenceId} and published duty {other.DutyOccurrenceId}.");
        foreach (var staffDuties in assignedDuties.GroupBy(x => x.IndividualId))
        {
            var ordered = staffDuties.OrderBy(x => x.StartAt).ToList();
            if (ordered.Count < 2) continue;
            var prior = ordered[0];
            for (var index = 1; index < ordered.Count; index++)
            {
                if (ordered[index].StartAt < prior.EndAt)
                    errors.Add($"Staff member {staffDuties.Key} has overlapping duty occurrences {prior.DutyOccurrenceId} and {ordered[index].DutyOccurrenceId}.");
                if (ordered[index].EndAt > prior.EndAt) prior = ordered[index];
            }
        }
        foreach (var duty in assignedDuties)
        {
            var assessment = await availability.AssessAsync(version.WorkforceRoster.OrganisationId, duty.IndividualId, duty.JobId, duty.StartAt, duty.EndAt, cancellationToken);
            if (!assessment.IsAvailable) errors.Add($"Duty occurrence {duty.DutyOccurrenceId}: {assessment.Reason}");
        }
        var warnings = new List<string>();
        foreach (var violation in await constraints.ValidateAsync(rosterVersionId, cancellationToken))
        {
            var text = $"{violation.Code}: {violation.Message}";
            if (string.Equals(violation.Severity, "HARD_BLOCK", StringComparison.OrdinalIgnoreCase)) errors.Add(text);
            else warnings.Add($"{violation.Severity}: {text}");
        }
        var coverage = errors.Where(x => x.Contains("requires", StringComparison.OrdinalIgnoreCase) && x.Contains("assigned", StringComparison.OrdinalIgnoreCase)).ToList();
        var overlaps = errors.Where(x => x.Contains("overlapping duty", StringComparison.OrdinalIgnoreCase)).ToList();
        var leave = errors.Where(x => x.Contains("Approved", StringComparison.OrdinalIgnoreCase) && x.Contains("leave", StringComparison.OrdinalIgnoreCase)).ToList();
        return new WorkforceRosterValidation(errors.Count == 0, errors, coverage, overlaps, leave, warnings);
    }

    public async Task<long> AddOccurrenceAsync(AddDutyOccurrenceRequest request, CancellationToken cancellationToken = default)
    {
        if (request.EndAt <= request.StartAt || request.RequiredStaffCount < 1 || string.IsNullOrWhiteSpace(request.TimeZoneId)) throw new InvalidOperationException("Provide valid duty times, staffing and time zone.");
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        using var lease = await EnterMutationAsync(request.OrganisationId, cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        await LockMutationAsync(db, request.OrganisationId, cancellationToken);
        var version = await db.WorkforceRosterVersions.Include(x => x.WorkforceRoster).SingleOrDefaultAsync(x => x.Id == request.RosterVersionId, cancellationToken) ?? throw new InvalidOperationException("Roster version not found.");
        if (version.WorkforceRoster.OrganisationId != request.OrganisationId) throw new UnauthorizedAccessException("The roster belongs to another organisation.");
        if (version.Status != "Draft") throw new InvalidOperationException("Only a draft roster can be changed.");
        if (request.StartAt.Date < version.WorkforceRoster.StartsOn.Date || request.EndAt.AddTicks(-1).Date > version.WorkforceRoster.EndsOn.Date) throw new InvalidOperationException("Duty must fall within the roster period.");
        if (!await db.DutyTypes.AnyAsync(x => x.Id == request.DutyTypeId && x.OrganisationId == version.WorkforceRoster.OrganisationId && x.IsActive, cancellationToken)) throw new InvalidOperationException("Select an active duty type in this organisation.");
        var occurrence = new DutyOccurrence { WorkforceRosterVersionId = request.RosterVersionId, DutyTypeId = request.DutyTypeId, StartAt = request.StartAt, EndAt = request.EndAt, RequiredStaffCount = request.RequiredStaffCount, TimeZoneId = request.TimeZoneId.Trim(), LocationName = request.LocationName?.Trim(), SourceKey = request.SourceKey?.Trim(), OperationLogId = request.OperationLogId };
        db.DutyOccurrences.Add(occurrence); await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken); return occurrence.Id;
    }

    public async Task<long> AssignAsync(AssignDutyRequest request, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        using var lease = await EnterMutationAsync(request.OrganisationId, cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        await LockMutationAsync(db, request.OrganisationId, cancellationToken);
        var occurrence = await db.DutyOccurrences.Include(x => x.WorkforceRosterVersion).ThenInclude(x => x.WorkforceRoster).SingleOrDefaultAsync(x => x.Id == request.DutyOccurrenceId, cancellationToken) ?? throw new InvalidOperationException("Duty occurrence not found.");
        if (occurrence.WorkforceRosterVersion.WorkforceRoster.OrganisationId != request.OrganisationId) throw new UnauthorizedAccessException("The duty belongs to another organisation.");
        if (occurrence.WorkforceRosterVersion.Status != "Draft") throw new InvalidOperationException("Only a draft roster can be changed.");
        if (!await db.Jobs.AnyAsync(x => x.JobId == request.JobId && x.IndividualID == request.IndividualId && x.OrganisationID == occurrence.WorkforceRosterVersion.WorkforceRoster.OrganisationId, cancellationToken)) throw new InvalidOperationException("The selected job does not belong to the staff member in this organisation.");
        if (await db.DutyAssignments.AnyAsync(x => x.DutyOccurrenceId == request.DutyOccurrenceId && x.JobId == request.JobId, cancellationToken)) throw new InvalidOperationException("This staff member is already assigned to the duty.");
        var assignment = new DutyAssignment { DutyOccurrenceId = request.DutyOccurrenceId, JobId = request.JobId, IndividualId = request.IndividualId, AssignedByUserId = request.ActorUserId, AssignedAtUtc = DateTime.UtcNow, AssignmentReason = request.Reason?.Trim(), OperationLogId = request.OperationLogId };
        db.DutyAssignments.Add(assignment); await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken); return assignment.Id;
    }

    public async Task PublishAsync(int rosterVersionId, int actorUserId, int operationLogId, CancellationToken cancellationToken = default)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken);
        var organisationId = await db.WorkforceRosterVersions.Where(x => x.Id == rosterVersionId).Select(x => x.WorkforceRoster.OrganisationId).SingleAsync(cancellationToken);
        using var lease = await EnterMutationAsync(organisationId, cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        await LockMutationAsync(db, organisationId, cancellationToken);
        var validation = await ValidateCoreAsync(db, rosterVersionId, organisationId, cancellationToken);
        if (!validation.IsValid) throw new InvalidOperationException(string.Join(" ", validation.Errors));
        var version = await db.WorkforceRosterVersions.Include(x => x.WorkforceRoster).SingleAsync(x => x.Id == rosterVersionId, cancellationToken);
        if (version.Status != "Draft") throw new InvalidOperationException("Only a draft roster version can be published.");
        version.Status = "Published"; version.PublishedAtUtc = DateTime.UtcNow; version.PublishedByUserId = actorUserId; version.OperationLogId = operationLogId;
        version.WorkforceRoster.Status = "Published"; version.WorkforceRoster.OperationLogId = operationLogId; version.WorkforceRoster.Version++;
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
