using System.Data;
using HRM.DTOs.Calendar;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Calendar;

/// <summary>Organisation Event Manager. Events are informational unless published with an explicit
/// CalendarImpactType, in which case an approved <see cref="OrganisationCalendarException"/> is created
/// (the only record the calendar resolver trusts). Drafts and cancelled events never affect schedules.</summary>
public sealed class OrganisationEventService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access)
    : global::HRM.Services.Calendar.Contracts.OrganisationEventServiceContracts.IOperations
{
    private static readonly string[] Categories = ["Meeting", "Ceremony", "Gathering", "Training", "Workshop", "Holiday", "SpecialWorkingDay", "NonWorkingDay", "WorkingHourChange", "StaffDevelopment", "MandatoryAttendance", "Official"];
    private static readonly string[] Attendance = ["InformationOnly", "Optional", "Required", "RequiredSelected"];
    private static readonly string[] Impacts = ["None", "MarkAsNonWorkingDay", "MarkAsWorkingDay", "OverrideWorkingHours", "ShortenedWorkingDay", "ExtendedWorkingDay"];
    private static readonly string[] AudienceTypes = ["Organisation", "Department", "Team", "Individual", "EmploymentCategory"];
    private static readonly string[] TimedImpacts = ["MarkAsWorkingDay", "OverrideWorkingHours", "ShortenedWorkingDay", "ExtendedWorkingDay"];

    private async Task<(int Org, int Actor, bool CanPublish)> RequireManagerAsync()
    {
        var ctx = await access.RequireContextAsync();
        var admin = ctx.IsOrganisationAdmin || await access.HasPermissionAsync("Admin");
        if (!admin && !await access.HasPermissionAsync("OrganisationEvent.Manage"))
            throw new UnauthorizedAccessException("You are not allowed to manage organisation events.");
        var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0) throw new InvalidOperationException("An organisation context is required.");
        return (org, ctx.IndividualId, admin || await access.HasPermissionAsync("OrganisationEvent.Publish"));
    }

    private static string Summarise(IEnumerable<OrganisationEventAudience> audiences)
    {
        var list = audiences.Where(a => a.IsIncluded).ToList();
        if (list.Count == 0 || list.Any(a => a.AudienceType == "Organisation")) return "Entire organisation";
        return string.Join(", ", list.GroupBy(a => a.AudienceType).Select(g => $"{g.Count()} {g.Key.ToLowerInvariant()}{(g.Count() == 1 ? "" : "s")}"));
    }

    private static OrganisationEventDto ToDto(OrganisationEvent e) => new()
    {
        OrganisationEventId = e.OrganisationEventId, EventCode = e.EventCode, Title = e.Title, Category = e.Category, Description = e.Description,
        StartDateTime = e.StartDateTime, EndDateTime = e.EndDateTime, TimeZoneId = e.TimeZoneId, IsAllDay = e.IsAllDay,
        VenueName = e.VenueName, VenueAddress = e.VenueAddress, MeetingUrl = e.MeetingUrl, OrganisingStructureId = e.OrganisingStructureId,
        ResponsibleIndividualId = e.ResponsibleIndividualId, ContactInformation = e.ContactInformation, DressCode = e.DressCode,
        AttendanceInstructions = e.AttendanceInstructions, RequiredItems = e.RequiredItems, AdditionalNotes = e.AdditionalNotes,
        AttendanceRequirement = e.AttendanceRequirement, CalendarImpactType = e.CalendarImpactType,
        EffectiveWorkStartTime = e.EffectiveWorkStartTime, EffectiveWorkEndTime = e.EffectiveWorkEndTime,
        AffectsAttendanceExpectation = e.AffectsAttendanceExpectation, RequiresLeaveRecalculation = e.RequiresLeaveRecalculation,
        Status = e.Status, Visibility = e.Visibility, NotifyStaff = e.NotifyStaff, NotifyBeforeMinutes = e.NotifyBeforeMinutes, PublishDate = e.PublishDate,
        Audiences = e.Audiences.Select(a => new OrganisationEventAudienceDto { AudienceType = a.AudienceType, TargetEntityId = a.TargetEntityId, IsIncluded = a.IsIncluded }).ToList(),
        Recurrence = e.Recurrence == null ? null : new OrganisationEventRecurrenceDto
        {
            RecurrenceType = e.Recurrence.RecurrenceType, Interval = e.Recurrence.Interval, WeekdayMask = e.Recurrence.WeekdayMask,
            EffectiveFrom = e.Recurrence.EffectiveFrom, EffectiveTo = e.Recurrence.EffectiveTo, MaxOccurrences = e.Recurrence.MaxOccurrences
        },
        AudienceSummary = Summarise(e.Audiences),
        RowVersion = Convert.ToBase64String(e.RowVersion)
    };

    private static void Audit(HrmTeContext db, OrganisationEvent e, int actor, string action, string? previous = null, string? now = null, string? reason = null) =>
        db.OrganisationEventAudits.Add(new OrganisationEventAudit
        {
            OrganisationBusinessEntityId = e.OrganisationBusinessEntityId, OrganisationEventId = e.OrganisationEventId,
            Action = action, ActorIndividualId = actor, PreviousValue = previous, NewValue = now, Reason = reason, CreatedAtUtc = DateTime.UtcNow
        });

    private static void Validate(OrganisationEventDto d)
    {
        if (string.IsNullOrWhiteSpace(d.Title) || d.Title.Trim().Length > 200) throw new InvalidOperationException("Enter an event title of up to 200 characters.");
        if (!Categories.Contains(d.Category)) throw new InvalidOperationException("Select a valid event category.");
        if (!Attendance.Contains(d.AttendanceRequirement)) throw new InvalidOperationException("Select a valid attendance requirement.");
        if (!Impacts.Contains(d.CalendarImpactType)) throw new InvalidOperationException("Select a valid calendar impact.");
        if (d.EndDateTime < d.StartDateTime) throw new InvalidOperationException("The end must not be before the start.");
        if ((d.EndDateTime - d.StartDateTime).TotalDays > 366) throw new InvalidOperationException("An event cannot span more than 366 days.");
        if (!string.IsNullOrWhiteSpace(d.MeetingUrl) && !(Uri.TryCreate(d.MeetingUrl, UriKind.Absolute, out var u) && u.Scheme is "http" or "https"))
            throw new InvalidOperationException("The online meeting link must be an http or https address.");
        if (d.Audiences.Any(a => !AudienceTypes.Contains(a.AudienceType) || (a.AudienceType != "Organisation" && a.TargetEntityId is null)))
            throw new InvalidOperationException("Each audience group needs a valid type and target.");
        if (TimedImpacts.Contains(d.CalendarImpactType) && (d.EffectiveWorkStartTime is null || d.EffectiveWorkEndTime is null || d.EffectiveWorkEndTime <= d.EffectiveWorkStartTime))
            throw new InvalidOperationException("Enter effective working hours where the end is after the start.");
        if (d.CalendarImpactType != "None" && d.Recurrence != null)
            throw new InvalidOperationException("Recurring events cannot change working days yet; create one event per affected date.");
        if (d.CalendarImpactType != "None" && d.Audiences.Any(a => a.AudienceType == "EmploymentCategory" && a.IsIncluded))
            throw new InvalidOperationException("Employment-category audiences cannot be used for working-day changes.");
        if (d.Recurrence is { } r && (r.Interval < 1 || r.MaxOccurrences is <= 0 || (r.EffectiveTo is { } to && to < r.EffectiveFrom)))
            throw new InvalidOperationException("The recurrence settings are not valid.");
    }

    public async Task<List<OrganisationEventDto>> ListAsync(DateTime? from, DateTime? to, string? category, string? status, string? impact, int? structureId, bool? upcomingOnly)
    {
        var (org, _, _) = await RequireManagerAsync();
        var start = (from ?? DateTime.Today.AddMonths(-1)).Date;
        var end = (to ?? start.AddMonths(13)).Date.AddDays(1);
        if ((end - start).TotalDays > 800) throw new InvalidOperationException("Choose a date range of at most about two years.");
        await using var db = await factory.CreateDbContextAsync();
        await OrganisationCalendarSchemaBootstrapper.EnsureSchemaAsync(db);
        var q = db.OrganisationEvents.AsNoTracking().Include(x => x.Audiences).Include(x => x.Recurrence)
            .Where(x => x.OrganisationBusinessEntityId == org && x.IsValid && x.StartDateTime < end && x.EndDateTime >= start);
        if (!string.IsNullOrWhiteSpace(category)) q = q.Where(x => x.Category == category);
        if (!string.IsNullOrWhiteSpace(status)) q = q.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(impact)) q = q.Where(x => x.CalendarImpactType == impact);
        if (structureId.HasValue) q = q.Where(x => x.OrganisingStructureId == structureId || x.Audiences.Any(a => a.AudienceType == "Department" && a.TargetEntityId == structureId));
        if (upcomingOnly == true) q = q.Where(x => x.EndDateTime >= DateTime.Now);
        return (await q.OrderBy(x => x.StartDateTime).Take(500).ToListAsync()).Select(ToDto).ToList();
    }

    private static async Task<OrganisationEvent> LoadAsync(HrmTeContext db, int org, int id, bool track) =>
        await (track ? db.OrganisationEvents : db.OrganisationEvents.AsNoTracking())
            .Include(x => x.Audiences).Include(x => x.Recurrence)
            .SingleOrDefaultAsync(x => x.OrganisationEventId == id && x.OrganisationBusinessEntityId == org && x.IsValid)
        ?? throw new InvalidOperationException("The event was not found.");

    public async Task<OrganisationEventDto> GetAsync(int id)
    {
        var (org, _, _) = await RequireManagerAsync();
        await using var db = await factory.CreateDbContextAsync();
        return ToDto(await LoadAsync(db, org, id, false));
    }

    public async Task<int> SaveAsync(OrganisationEventDto dto)
    {
        var (org, actor, _) = await RequireManagerAsync();
        Validate(dto);
        await using var db = await factory.CreateDbContextAsync();
        OrganisationEvent e;
        string? before = null;
        if (dto.OrganisationEventId == 0)
        {
            e = new OrganisationEvent { OrganisationBusinessEntityId = org, CreatedBy = actor, CreatedDate = DateTime.UtcNow, Status = "Draft" };
            db.OrganisationEvents.Add(e);
        }
        else
        {
            e = await LoadAsync(db, org, dto.OrganisationEventId, true);
            if (e.Status is "Cancelled" or "Completed") throw new InvalidOperationException("A cancelled or completed event cannot be edited.");
            if (e.Status == "Published" && e.CalendarImpactType != dto.CalendarImpactType)
                throw new InvalidOperationException("Cancel the published event and create a new one to change its calendar impact.");
            if (!string.IsNullOrEmpty(dto.RowVersion)) db.Entry(e).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(dto.RowVersion);
            before = $"{e.Title}|{e.StartDateTime:O}|{e.EndDateTime:O}|{e.VenueName}";
            e.UpdatedBy = actor; e.UpdatedDate = DateTime.UtcNow;
        }
        e.EventCode = dto.EventCode?.Trim(); e.Title = dto.Title.Trim(); e.Category = dto.Category; e.Description = dto.Description;
        e.StartDateTime = dto.StartDateTime; e.EndDateTime = dto.EndDateTime; e.TimeZoneId = string.IsNullOrWhiteSpace(dto.TimeZoneId) ? "Indian/Maldives" : dto.TimeZoneId;
        e.IsAllDay = dto.IsAllDay; e.VenueName = dto.VenueName; e.VenueAddress = dto.VenueAddress; e.MeetingUrl = dto.MeetingUrl;
        e.OrganisingStructureId = dto.OrganisingStructureId; e.ResponsibleIndividualId = dto.ResponsibleIndividualId; e.ContactInformation = dto.ContactInformation;
        e.DressCode = dto.DressCode; e.AttendanceInstructions = dto.AttendanceInstructions; e.RequiredItems = dto.RequiredItems; e.AdditionalNotes = dto.AdditionalNotes;
        e.AttendanceRequirement = dto.AttendanceRequirement; e.CalendarImpactType = dto.CalendarImpactType;
        e.EffectiveWorkStartTime = dto.EffectiveWorkStartTime; e.EffectiveWorkEndTime = dto.EffectiveWorkEndTime;
        e.AffectsAttendanceExpectation = dto.AffectsAttendanceExpectation; e.RequiresLeaveRecalculation = dto.RequiresLeaveRecalculation;
        e.Visibility = string.IsNullOrWhiteSpace(dto.Visibility) ? "Audience" : dto.Visibility; e.NotifyStaff = dto.NotifyStaff; e.NotifyBeforeMinutes = dto.NotifyBeforeMinutes;
        e.PublishDate = dto.PublishDate;

        db.OrganisationEventAudiences.RemoveRange(e.Audiences);
        e.Audiences = dto.Audiences.GroupBy(a => (a.AudienceType, a.TargetEntityId)).Select(g => g.First())
            .Select(a => new OrganisationEventAudience { AudienceType = a.AudienceType, TargetEntityId = a.AudienceType == "Organisation" ? null : a.TargetEntityId, IsIncluded = a.IsIncluded }).ToList();
        if (e.Audiences.Count == 0) e.Audiences.Add(new OrganisationEventAudience { AudienceType = "Organisation" });
        if (dto.Recurrence == null) { if (e.Recurrence != null) { db.OrganisationEventRecurrences.Remove(e.Recurrence); e.Recurrence = null; } }
        else
        {
            e.Recurrence ??= new OrganisationEventRecurrence();
            var r = dto.Recurrence;
            e.Recurrence.RecurrenceType = r.RecurrenceType; e.Recurrence.Interval = r.Interval; e.Recurrence.WeekdayMask = r.WeekdayMask;
            e.Recurrence.EffectiveFrom = r.EffectiveFrom; e.Recurrence.EffectiveTo = r.EffectiveTo; e.Recurrence.MaxOccurrences = r.MaxOccurrences;
        }
        await db.SaveChangesAsync();
        Audit(db, e, actor, dto.OrganisationEventId == 0 ? "Created" : "Updated", before, $"{e.Title}|{e.StartDateTime:O}|{e.EndDateTime:O}|{e.VenueName}");
        await db.SaveChangesAsync();
        return e.OrganisationEventId;
    }

    private static List<(string Scope, int? Target)> Scopes(OrganisationEvent e)
    {
        var included = e.Audiences.Where(a => a.IsIncluded).ToList();
        if (included.Count == 0 || included.Any(a => a.AudienceType == "Organisation")) return [("Organisation", null)];
        return included.Where(a => a.AudienceType != "EmploymentCategory").Select(a => (a.AudienceType, a.TargetEntityId)).ToList();
    }

    private static string Describe(OrganisationEvent e) => e.CalendarImpactType switch
    {
        "MarkAsNonWorkingDay" => "The covered dates become non-working days; normal schedules are suppressed and absence is not recorded for cancelled work periods.",
        "MarkAsWorkingDay" => $"The covered dates become working days, {e.EffectiveWorkStartTime:hh\\:mm}–{e.EffectiveWorkEndTime:hh\\:mm}, even if normally non-working.",
        "OverrideWorkingHours" => $"Working hours on the covered dates are replaced by {e.EffectiveWorkStartTime:hh\\:mm}–{e.EffectiveWorkEndTime:hh\\:mm}.",
        "ShortenedWorkingDay" => $"Working hours are shortened to {e.EffectiveWorkStartTime:hh\\:mm}–{e.EffectiveWorkEndTime:hh\\:mm}.",
        "ExtendedWorkingDay" => $"Working hours are extended to {e.EffectiveWorkStartTime:hh\\:mm}–{e.EffectiveWorkEndTime:hh\\:mm}.",
        _ => "Informational only: this event does not change schedules, attendance or leave calculations."
    };

    private static async Task<List<string>> FindConflictsAsync(HrmTeContext db, OrganisationEvent e)
    {
        var from = e.StartDateTime.Date; var to = e.EndDateTime.Date;
        var existing = await db.OrganisationCalendarExceptions.AsNoTracking().Where(x => x.OrganisationBusinessEntityId == e.OrganisationBusinessEntityId && x.IsActive && x.IsApproved &&
            x.OrganisationEventId != e.OrganisationEventId && x.EffectiveDate <= to && x.EffectiveEndDate >= from).ToListAsync();
        var scopes = Scopes(e);
        return existing.Where(x => scopes.Any(s => s.Scope == x.TargetScope && s.Target == x.TargetEntityId) && x.CalendarImpactType != e.CalendarImpactType)
            .Select(x => $"{x.TargetScope} exception ({x.CalendarImpactType}) already covers {x.EffectiveDate:dd MMM yyyy}–{x.EffectiveEndDate:dd MMM yyyy}: {x.Reason}").ToList();
    }

    public async Task<EventImpactPreviewDto> PreviewImpactAsync(int id)
    {
        var (org, _, _) = await RequireManagerAsync();
        await using var db = await factory.CreateDbContextAsync();
        var e = await LoadAsync(db, org, id, false);
        var p = new EventImpactPreviewDto { OrganisationEventId = id, ChangesWorkSchedule = e.CalendarImpactType != "None", EffectSummary = Describe(e) };
        if (!p.ChangesWorkSchedule) { p.CanPublish = e.Status is "Draft" or "PendingApproval"; return p; }
        for (var d = e.StartDateTime.Date; d <= e.EndDateTime.Date; d = d.AddDays(1)) p.AffectedDates.Add(d);
        p.Scopes = Scopes(e).Select(s => s.Target is null ? "Entire organisation" : $"{s.Scope} #{s.Target}").ToList();
        p.Conflicts = await FindConflictsAsync(db, e);
        if (e.StartDateTime.Date < DateTime.Today) p.Warnings.Add("Some affected dates are in the past; historical attendance and payroll-locked periods are not modified.");
        if (e.RequiresLeaveRecalculation) p.Warnings.Add("Approved leave on these dates will be flagged for review, not changed automatically.");
        p.CanPublish = p.Conflicts.Count == 0 && e.Status is "Draft" or "PendingApproval";
        return p;
    }

    public async Task PublishAsync(int id)
    {
        var (org, actor, canPublish) = await RequireManagerAsync();
        if (!canPublish) throw new UnauthorizedAccessException("You are not allowed to publish organisation events.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var e = await LoadAsync(db, org, id, true);
        if (e.Status is not ("Draft" or "PendingApproval")) throw new InvalidOperationException("Only draft or pending events can be published.");
        Validate(ToDto(e));
        if (e.CalendarImpactType != "None")
        {
            var conflicts = await FindConflictsAsync(db, e);
            if (conflicts.Count > 0) throw new InvalidOperationException("Conflicting calendar exceptions must be resolved first: " + conflicts[0]);
            foreach (var (scope, target) in Scopes(e))
            {
                var ex = new OrganisationCalendarException
                {
                    OrganisationBusinessEntityId = org, OrganisationEventId = e.OrganisationEventId, EffectiveDate = e.StartDateTime.Date, EffectiveEndDate = e.EndDateTime.Date,
                    TargetScope = scope, TargetEntityId = target, CalendarImpactType = e.CalendarImpactType,
                    Priority = scope switch { "Individual" => 40, "Team" => 30, "Department" => 20, _ => 10 },
                    Reason = e.Title, IsApproved = true, IsActive = true, ApprovedBy = actor, ApprovedDate = DateTime.UtcNow, CreatedBy = actor, CreatedDate = DateTime.UtcNow
                };
                if (TimedImpacts.Contains(e.CalendarImpactType))
                    ex.Segments.Add(new OrganisationCalendarExceptionSegment { StartTime = e.EffectiveWorkStartTime!.Value, EndTime = e.EffectiveWorkEndTime!.Value });
                db.OrganisationCalendarExceptions.Add(ex);
            }
        }
        e.Status = "Published"; e.PublishDate = DateTime.UtcNow; e.PublishedBy = actor; e.ApprovedBy = actor; e.ApprovedDate = DateTime.UtcNow;
        Audit(db, e, actor, "Published", "Draft", "Published", e.CalendarImpactType == "None" ? null : $"Calendar impact: {e.CalendarImpactType}");
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task CancelAsync(int id, string reason)
    {
        var (org, actor, canPublish) = await RequireManagerAsync();
        if (!canPublish) throw new UnauthorizedAccessException("You are not allowed to cancel organisation events.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Length > 400) throw new InvalidOperationException("Enter a cancellation reason of up to 400 characters.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var e = await LoadAsync(db, org, id, true);
        if (e.Status is "Cancelled" or "Completed") throw new InvalidOperationException("The event is already closed.");
        var previous = e.Status;
        // Deactivate (never delete) overrides so history and prior impact assessments stay traceable.
        foreach (var ex in await db.OrganisationCalendarExceptions.Where(x => x.OrganisationEventId == id && x.IsActive).ToListAsync()) ex.IsActive = false;
        e.Status = "Cancelled"; e.UpdatedBy = actor; e.UpdatedDate = DateTime.UtcNow;
        Audit(db, e, actor, "Cancelled", previous, "Cancelled", reason.Trim());
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task<int> DuplicateAsync(int id)
    {
        var (org, actor, _) = await RequireManagerAsync();
        await using var db = await factory.CreateDbContextAsync();
        var dto = ToDto(await LoadAsync(db, org, id, false));
        dto.OrganisationEventId = 0; dto.Status = "Draft"; dto.RowVersion = null; dto.EventCode = null; dto.PublishDate = null;
        dto.Title = dto.Title.Length > 190 ? dto.Title[..190] + " (copy)" : dto.Title + " (copy)";
        return await SaveAsync(dto);
    }

    public async Task<List<OrganisationEventAuditDto>> AuditAsync(int id)
    {
        var (org, _, _) = await RequireManagerAsync();
        await using var db = await factory.CreateDbContextAsync();
        await LoadAsync(db, org, id, false);
        return await db.OrganisationEventAudits.AsNoTracking().Where(x => x.OrganisationEventId == id && x.OrganisationBusinessEntityId == org)
            .OrderByDescending(x => x.CreatedAtUtc).Take(200)
            .Select(x => new OrganisationEventAuditDto { Action = x.Action, ActorIndividualId = x.ActorIndividualId, PreviousValue = x.PreviousValue, NewValue = x.NewValue, Reason = x.Reason, CreatedAtUtc = x.CreatedAtUtc }).ToListAsync();
    }
}
