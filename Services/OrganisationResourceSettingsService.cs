using System.Data;
using HRM.Components.Shared;
using HRM.DTOs;
using HRM.DTOs.Organisation;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed class OrganisationResourceSettingsService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access, IOperationLogService logs)
{
    private async Task<(int Org, bool Super, bool Edit)> Scope(int? selected)
    {
        var actor = await access.RequireContextAsync();
        var super = actor.IsSuperAdministrator || await access.HasPermissionAsync("Super.Admin.Manage");
        var current = await access.GetCurrentOrganisationIdAsync();
        var org = super && selected is > 0 ? selected.Value : current;
        if (org <= 0 || !super && (selected.HasValue && selected != current || !await access.HasOrganisationAccessAsync(org)))
            throw new UnauthorizedAccessException("Use your current organisation.");
        await using var db = await factory.CreateDbContextAsync();
        if (!await db.Organisations.AnyAsync(o => o.BusinessEntityID == org)) throw new InvalidOperationException("Organisation not found.");
        var edit = super || await access.HasPermissionAsync("Admin") || await access.HasPermissionAsync("Organisation.Edit") || await access.HasPermissionAsync("UserOrganisations.Edit");
        if (!edit && !await access.HasPermissionAsync("UserOrganisations.View") && !await access.HasPermissionAsync("UserOrganisations.Admin.View"))
            throw new UnauthorizedAccessException("You cannot view organisation settings.");
        return (org, super, edit);
    }
    public static bool IsCatalogue(OrganisationResourceKind kind) => kind is OrganisationResourceKind.StructureTypes or OrganisationResourceKind.LocationTypes or OrganisationResourceKind.OrganisationTypes or OrganisationResourceKind.StructureRequestTypes;
    private static async Task<List<OrganisationResourceRow>> Catalogue(HrmTeContext db, OrganisationResourceKind kind) => kind switch
    {
        OrganisationResourceKind.StructureTypes => await db.OrganisationStructureTypes.OrderBy(t => t.TypeName).Select(t => new OrganisationResourceRow(t.OrganisationStructureTypeId, t.TypeName, "", "", null)).ToListAsync(),
        OrganisationResourceKind.LocationTypes => await db.LocationTypes.OrderBy(t => t.TypeName).Select(t => new OrganisationResourceRow(t.LocationTypeId, t.TypeName, "", "", null)).ToListAsync(),
        OrganisationResourceKind.OrganisationTypes => await db.OrganisationTypes.OrderBy(t => t.TypeName).Select(t => new OrganisationResourceRow(t.OrganisationTypeId, t.TypeName, "", "", null)).ToListAsync(),
        OrganisationResourceKind.StructureRequestTypes => await db.OrganisationStructureRequestTypes.OrderBy(t => t.TypeName).Select(t => new OrganisationResourceRow(t.OrganisationStructureRequestTypeId, t.TypeName, "", "", null)).ToListAsync(),
        _ => throw new InvalidOperationException("Select a valid reference type.")
    };
    public async Task<OrganisationResourceData> LoadAsync(OrganisationResourceKind kind, int? organisationId = null)
    {
        var (org, super, edit) = await Scope(organisationId);
        await using var db = await factory.CreateDbContextAsync();
        var result = new OrganisationResourceData { OrganisationId = org, IsSuperAdmin = super, CanEdit = IsCatalogue(kind) ? super : edit,
            OrganisationName = await db.Organisations.Where(o => o.BusinessEntityID == org).Select(o => o.OrganisationName).SingleAsync(),
            Organisations = await db.Organisations.Where(o => super || o.BusinessEntityID == org).OrderBy(o => o.OrganisationName)
                .Select(o => new OrganisationSettingsOption(o.BusinessEntityID, o.OrganisationName)).ToListAsync() };
        if (IsCatalogue(kind)) result.Rows = await Catalogue(db, kind);
        else if (kind == OrganisationResourceKind.Calendar)
        {
            var rows = await db.BusinessEntityCalendars.Where(c => c.BusinessEntityId == org && c.IsLinked)
                .OrderBy(c => c.Calender.Name).Select(c => new { c.BusinessEntityCalenderId, c.Calender.Name, c.StartDate, c.EndDate }).ToListAsync();
            result.Rows = rows.Select(c => new OrganisationResourceRow(c.BusinessEntityCalenderId, c.Name,
                $"{c.StartDate:dd MMM yyyy} – {(c.EndDate.HasValue ? c.EndDate.Value.ToString("dd MMM yyyy") : "No end date")}")).ToList();
            result.Calendars = await db.Calendars.Where(c => c.IsGlobal || c.IsPublic || c.BusinessEntityCalendars.Any(b => b.BusinessEntityId == org && b.IsLinked))
                .OrderBy(c => c.Name).Select(c => new OrganisationSettingsOption(c.CalenderId, c.Name)).ToListAsync();
        }
        else if (kind == OrganisationResourceKind.Locations)
        {
            result.Rows = await db.BusinessEntityLocations.Where(l => l.BusinessEntityId == org && l.IsValid).OrderBy(l => l.LocationId)
                .Select(l => new OrganisationResourceRow(l.BusinessEntityLocationId, l.BusinessEntityLocationType.TypeName,
                    (l.Location.Address == null || l.Location.Address.AddressInstance == null ? "No postal address" :
                        (l.Location.Address.AddressInstance.AddressBase.AddressLine1 ?? "") + " " + (l.Location.Address.AddressInstance.AddressBase.StreetNameEnglish ?? "")) + " · " + l.Location.Country.NameEnglish,
                    l.Location.LocationType.TypeName, $"/admin/settings/org/manage/{org}/address")).ToListAsync();
        }
        else if (kind == OrganisationResourceKind.Requests)
        {
            var requests = await db.Requests.Where(r => r.OrganisationBusinessEntityID == org).OrderByDescending(r => r.ApplicationDate).ThenByDescending(r => r.RequestId).Take(100)
                .Select(r => new { r.RequestId, r.ReferenceNumber, r.RequestType.TypeName, r.RequestType.ViewUrl, r.RequestState.StateName, r.RequestState.IsFinalState, r.RequestState.IsProcessingState }).ToListAsync();
            result.Rows = requests.Select(r => new OrganisationResourceRow(r.RequestId, r.ReferenceNumber ?? $"Request {r.RequestId}", r.TypeName,
                (!r.IsFinalState && r.IsProcessingState ? "Processing · " : "") + r.StateName.Trim(), RequestUrl(r.ViewUrl, r.RequestId))).ToList();
        }
        return result;
    }
    private static string RequestUrl(string? url, int id)
    {
        if (string.IsNullOrWhiteSpace(url) || !url.StartsWith('/') || url.StartsWith("//") || url.Contains('\\')) return $"/admin/my/workspace?requestId={id}";
        if (url.Contains('{')) return $"/admin/my/workspace?requestId={id}";
        return url + (url.Contains('?') ? "&" : "?") + $"requestId={id}";
    }
    public async Task<ServiceResult> SaveTypeAsync(OrganisationResourceKind kind, int id, string name, string? expectedName)
    {
        var (_, super, _) = await Scope(null);
        if (!super) throw new UnauthorizedAccessException("Only Super Admin can edit these shared reference types.");
        if (!IsCatalogue(kind) || string.IsNullOrWhiteSpace(name) || name.Trim().Length > 250) return ServiceResult.Fail("Enter a type name of up to 250 characters.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var rows = await Catalogue(db, kind); name = name.Trim();
        if (rows.Any(r => r.Id != id && r.Name.Trim().Equals(name, StringComparison.OrdinalIgnoreCase))) return ServiceResult.Fail("This type name already exists.");
        if (id > 0 && rows.SingleOrDefault(r => r.Id == id)?.Name != expectedName) return ServiceResult.Fail("This type changed. Refresh before saving.");
        if (id > 0 && !rows.Any(r => r.Id == id)) return ServiceResult.Fail("This type no longer exists.");
        await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.BUSINESS_ENTITY_UPDATE, $"{(id == 0 ? "Created" : "Renamed")} shared {kind} type {id}: {expectedName} → {name}");
        switch (kind)
        {
            case OrganisationResourceKind.StructureTypes:
                if (id == 0) db.OrganisationStructureTypes.Add(new() { TypeName = name }); else (await db.OrganisationStructureTypes.SingleAsync(t => t.OrganisationStructureTypeId == id)).TypeName = name;
                break;
            case OrganisationResourceKind.LocationTypes:
                if (id == 0) db.LocationTypes.Add(new() { TypeName = name }); else (await db.LocationTypes.SingleAsync(t => t.LocationTypeId == id)).TypeName = name;
                break;
            case OrganisationResourceKind.OrganisationTypes:
                if (id == 0) db.OrganisationTypes.Add(new() { TypeName = name }); else (await db.OrganisationTypes.SingleAsync(t => t.OrganisationTypeId == id)).TypeName = name;
                break;
            case OrganisationResourceKind.StructureRequestTypes:
                if (id == 0) db.OrganisationStructureRequestTypes.Add(new() { TypeName = name }); else (await db.OrganisationStructureRequestTypes.SingleAsync(t => t.OrganisationStructureRequestTypeId == id)).TypeName = name;
                break;
        }
        await db.SaveChangesAsync(); await tx.CommitAsync(); return ServiceResult.Ok("Type saved.");
    }
    public async Task<ServiceResult> LinkCalendarAsync(int organisationId, int calendarId, DateTime from, DateTime? to)
    {
        from = from.Date; to = to?.Date;
        var (org, _, edit) = await Scope(organisationId);
        if (!edit) throw new UnauthorizedAccessException("You cannot assign organisation calendars.");
        if (from.Year < 1900 || from.Year > 2200 || to.HasValue && (to < from || to.Value.Year > 2200)) return ServiceResult.Fail("Enter a valid effective period.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await db.Calendars.AnyAsync(c => c.CalenderId == calendarId && (c.IsGlobal || c.IsPublic || c.BusinessEntityCalendars.Any(b => b.BusinessEntityId == org && b.IsLinked))))
            return ServiceResult.Fail("Select a calendar available to this organisation.");
        if (await db.BusinessEntityCalendars.AnyAsync(c => c.BusinessEntityId == org && c.CalenderId == calendarId && c.IsLinked &&
            (!c.EndDate.HasValue || c.EndDate >= from) && (!to.HasValue || c.StartDate <= to))) return ServiceResult.Fail("This calendar is already assigned during the selected period.");
        var log = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.BUSINESSENTITY_CALENDAR_CREATE, $"Assigned calendar {calendarId} to organisation {org}");
        db.BusinessEntityCalendars.Add(new BusinessEntityCalendar { BusinessEntityId = org, CalenderId = calendarId, StartDate = from.Date, EndDate = to?.Date, IsLinked = true, OperationLogId = log.OperationLogId });
        await db.SaveChangesAsync(); await tx.CommitAsync(); return ServiceResult.Ok("Calendar assigned.");
    }
}
