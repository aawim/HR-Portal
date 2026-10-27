using System.Data;
using HRM.Components.Shared;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;



public sealed class UserPreferenceService(IDbContextFactory<HrmTeContext> factory,
    AuthenticationStateProvider authentication, IOperationLogService logs)
:global::HRM.Services.Contracts.UserPreferenceServiceContracts,global::HRM.Services.Contracts.UserPreferenceServiceContracts.IOperations{
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
            .Select(x => new { x.ThemeMode, x.FontSizePercent }).SingleOrDefaultAsync();
        return new(user.UserId, theme?.ThemeMode == "light" ? "light" : "dark", AllowedFontSizes.Contains(theme?.FontSizePercent ?? 100) ? theme?.FontSizePercent ?? 100 : 100);
    }

    

    public Task SaveThemeAsync(string theme, int expectedUserId)
    {
        if (theme is not ("dark" or "light")) throw new ArgumentException("Choose dark or light mode.", nameof(theme));
        return SaveAppearanceAsync(expectedUserId, theme, null);
    }
    public Task SaveFontSizeAsync(int percent, int expectedUserId)
    {
        if (!AllowedFontSizes.Contains(percent)) throw new ArgumentException("Choose an available font size.", nameof(percent));
        return SaveAppearanceAsync(expectedUserId, null, percent);
    }
    private async Task SaveAppearanceAsync(int expectedUserId, string? theme, int? fontSize)
    {
        await using var db = await factory.CreateDbContextAsync();
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        var user = await CurrentUserAsync(db);
        if (user == null || user.UserId != expectedUserId)
            throw new UnauthorizedAccessException("Your session has changed. Reload the page before saving settings.");
        var preference = await db.UserPreferences.SingleOrDefaultAsync(x => x.UserPreferenceId == user.UserPreferenceId);
        var shared = preference != null && await db.Users.AnyAsync(x => x.UserId != user.UserId && x.UserPreferenceId == preference.UserPreferenceId);
        if (preference != null && (theme == null || preference.ThemeMode == theme) &&
            (!fontSize.HasValue || preference.FontSizePercent == fontSize.Value) && !shared) return;
        var log = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.USERS_UPDATE, $"User {user.UserId}: appearance setting updated; theme {theme ?? "unchanged"}, font size {fontSize?.ToString() ?? "unchanged"}%.");
        // Legacy shared preferences must not change another user's settings.
        if (preference == null || shared)
        {
            preference = new UserPreference { SelectedContextId = preference?.SelectedContextId,
                SelectedLanguageId = preference?.SelectedLanguageId,
                ThemeMode = preference?.ThemeMode ?? "dark", FontSizePercent = preference?.FontSizePercent ?? 100,
                RecentWorkspaceSearchesJson = preference?.RecentWorkspaceSearchesJson, OperationLogId = log.OperationLogId };
            db.UserPreferences.Add(preference);
            user.UserPreference = preference;
        }
        if (theme != null) preference.ThemeMode = theme;
        if (fontSize.HasValue) preference.FontSizePercent = fontSize.Value;
        preference.UpdatedAtUtc = DateTime.UtcNow;
        preference.OperationLogId = log.OperationLogId;
        await db.SaveChangesAsync();
        await transaction.CommitAsync();
    }
}
