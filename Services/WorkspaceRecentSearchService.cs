using System.Data;
using System.Text.Json;
using HRM.DTOs;
using HRM.Enum;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed class WorkspaceRecentSearchService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access, IOperationLogService logs)
{
    public sealed record Entry(int OrganisationId, WorkspaceEntityType Type, int Id, DateTime AtUtc);
    static List<Entry> Read(string? value) { try { return string.IsNullOrWhiteSpace(value) ? [] : JsonSerializer.Deserialize<List<Entry>>(value) ?? []; } catch(JsonException) { return []; } }

    public async Task<WorkspaceSearchResult?> ResolveAsync(WorkspaceEntityType type, int id)
    {
        var actor = await access.RequireContextAsync(); var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org) || id <= 0) return null;
        await using var db = await factory.CreateDbContextAsync();
        return await ResolveAsync(db, actor, org, type, id);
    }

    private static async Task<WorkspaceSearchResult?> ResolveAsync(HrmTeContext db, HRM.DTOs.UserContext.UserContextDto actor, int org, WorkspaceEntityType type, int id)
    {
        if (id <= 0) return null;
        if (type == WorkspaceEntityType.Organisation)
            return await db.Organisations.AsNoTracking().Where(x => x.BusinessEntityID == id && (actor.IsSuperAdministrator || x.BusinessEntityID == org))
                .Select(x => new WorkspaceSearchResult { Id = x.BusinessEntityID, Name = x.OrganisationName, EntityType = WorkspaceEntityType.Organisation }).SingleOrDefaultAsync();
        if (type is not (WorkspaceEntityType.Staff or WorkspaceEntityType.Individual)) return null;
        var person = await db.Individuals.AsNoTracking().SingleOrDefaultAsync(x => x.BusinessEntityId == id);
        if (person == null) return null;
        var job = await db.Jobs.AsNoTracking().Where(x => x.IndividualID == id && (actor.IsSuperAdministrator || x.OrganisationID == org))
            .OrderByDescending(x => x.JobStateId == 4 && x.TerminatedDate == null).ThenByDescending(x => x.JoinedDate).Select(x => (int?)x.JobId).FirstOrDefaultAsync();
        if (!actor.IsSuperAdministrator && !job.HasValue) return null;
        return new WorkspaceSearchResult { Id = id, IndividualId = id, JobId = job, EntityType = job.HasValue ? WorkspaceEntityType.Staff : WorkspaceEntityType.Individual,
            Name = string.Join(" ", new[] { person.FirstNameEnglish, person.MiddleNameEnglish, person.LastNameEnglish }.Where(x => !string.IsNullOrWhiteSpace(x))) };
    }

    public async Task<List<WorkspaceSearchResult>> GetAsync()
    {
        var actor = await access.RequireContextAsync(); var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org)) return [];
        await using var db = await factory.CreateDbContextAsync();
        var preferenceId = await db.Users.Where(x => x.UserId == actor.UserId && x.BusinessEntityID == actor.IndividualId && x.IsActive && x.LocalUserStateId == 1)
            .Select(x => x.UserPreferenceId).SingleOrDefaultAsync();
        if (await db.Users.CountAsync(x => x.UserPreferenceId == preferenceId) != 1) return [];
        var json = await db.UserPreferences.Where(x => x.UserPreferenceId == preferenceId).Select(x => x.RecentWorkspaceSearchesJson).SingleOrDefaultAsync();
        var result = new List<WorkspaceSearchResult>();
        foreach (var entry in Read(json).Where(x => x.OrganisationId == org).OrderByDescending(x => x.AtUtc).Take(6))
        { var item = await ResolveAsync(db, actor, org, entry.Type, entry.Id); if(item != null && !result.Any(x => x.Id == item.Id && x.EntityType == item.EntityType)) result.Add(item); }
        return result;
    }

    public async Task<WorkspaceSearchResult?> RememberAsync(WorkspaceSearchResult selected)
    {
        var actor = await access.RequireContextAsync(); var org = await access.GetCurrentOrganisationIdAsync();
        if (org <= 0 || !await access.HasOrganisationAccessAsync(org)) return null;
        await using var db = await factory.CreateDbContextAsync();
        var item = await ResolveAsync(db, actor, org, selected.EntityType, selected.Id); if(item == null) return null;
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var user = await db.Users.SingleAsync(x => x.UserId == actor.UserId && x.BusinessEntityID == actor.IndividualId && x.IsActive && x.LocalUserStateId == 1);
        var preference = await db.UserPreferences.SingleOrDefaultAsync(x => x.UserPreferenceId == user.UserPreferenceId);
        var shared = preference != null && await db.Users.AnyAsync(x => x.UserId != user.UserId && x.UserPreferenceId == preference.UserPreferenceId);
        var entries = shared ? [] : Read(preference?.RecentWorkspaceSearchesJson);
        entries.RemoveAll(x => x.OrganisationId == org && x.Id == item.Id && x.Type == item.EntityType);
        entries.Insert(0, new(org, item.EntityType, item.Id, DateTime.UtcNow));
        var log = await logs.CreateAsync(db, HRM.Components.Shared.SharedConfig.OperationLogActionTypes.USERS_UPDATE, "Updated personal workspace recent searches.");
        if (preference == null || shared)
        {
            preference = new UserPreference { SelectedContextId = preference?.SelectedContextId, SelectedLanguageId = preference?.SelectedLanguageId,
                ThemeMode = preference?.ThemeMode ?? "dark", OperationLogId = log.OperationLogId };
            db.UserPreferences.Add(preference); user.UserPreference = preference;
        }
        preference.RecentWorkspaceSearchesJson = JsonSerializer.Serialize(entries.Take(30));
        preference.UpdatedAtUtc = DateTime.UtcNow; preference.OperationLogId = log.OperationLogId;
        await db.SaveChangesAsync(); await tx.CommitAsync(); return item;
    }
}
