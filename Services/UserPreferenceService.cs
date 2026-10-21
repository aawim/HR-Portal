using System.Data;
using HRM.Components.Shared;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed record UserThemePreference(int? UserId, string ThemeMode);

public sealed class UserPreferenceService(IDbContextFactory<HrmTeContext> factory,
    AuthenticationStateProvider authentication, IOperationLogService logs)
{
    private async Task<User?> CurrentUserAsync(HrmTeContext db)
    {
        var principal = (await authentication.GetAuthenticationStateAsync()).User;
        var username = principal.FindFirst("idnumber")?.Value;
        if (principal.Identity?.IsAuthenticated != true || string.IsNullOrWhiteSpace(username)) return null;
        return await db.Users.SingleOrDefaultAsync(x => x.Username == username && x.IsActive && x.LocalUserStateId == 1);
    }

    public async Task<UserThemePreference> GetThemeAsync()
    {
        await using var db = await factory.CreateDbContextAsync();
        var user = await CurrentUserAsync(db);
        if (user == null) return new(null, "dark");
        var theme = await db.UserPreferences.AsNoTracking().Where(x => x.UserPreferenceId == user.UserPreferenceId)
            .Select(x => x.ThemeMode).SingleOrDefaultAsync();
        return new(user.UserId, theme == "light" ? "light" : "dark");
    }

    public async Task SaveThemeAsync(string theme, int expectedUserId)
    {
        if (theme is not ("dark" or "light")) throw new ArgumentException("Choose dark or light mode.", nameof(theme));
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var user = await CurrentUserAsync(db);
        if (user == null || user.UserId != expectedUserId)
            throw new UnauthorizedAccessException("Your session has changed. Reload the page before saving settings.");
        var preference = await db.UserPreferences.SingleOrDefaultAsync(x => x.UserPreferenceId == user.UserPreferenceId);
        var shared = preference != null && await db.Users.AnyAsync(x => x.UserId != user.UserId && x.UserPreferenceId == preference.UserPreferenceId);
        if (preference?.ThemeMode == theme && !shared) return;
        var log = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.USERS_UPDATE, $"User {user.UserId}: theme changed to {theme}.");
        // Legacy shared preferences must not change another user's settings.
        if (preference == null || shared)
        {
            preference = new UserPreference { SelectedContextId = preference?.SelectedContextId,
                SelectedLanguageId = preference?.SelectedLanguageId, OperationLogId = log.OperationLogId };
            db.UserPreferences.Add(preference);
            user.UserPreference = preference;
        }
        preference.ThemeMode = theme;
        preference.UpdatedAtUtc = DateTime.UtcNow;
        preference.OperationLogId = log.OperationLogId;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
