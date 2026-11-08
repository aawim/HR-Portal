using HRM.Application.Contracts.Pages;
using HRM.DTOs.UserContext;
using HRM.Services.Interfaces;
namespace HRM.Services.Queries;

public sealed class ApiReadScope(IUserAccessService access)
{
    public async Task<int> OrganisationAsync(int? selected = null)
    {
        var actor = await access.RequireContextAsync();
        if (actor.IsSuperAdministrator && selected.GetValueOrDefault() > 0) return selected!.Value;
        var current = await access.GetCurrentOrganisationIdAsync();
        var organisation = selected ?? current;
        if (organisation <= 0 || (!actor.IsSuperAdministrator && organisation != current) ||
            !actor.IsSuperAdministrator && !await access.HasOrganisationAccessAsync(organisation))
            throw new UnauthorizedAccessException("Use your current organisation.");
        return organisation;
    }
    public async Task<UserContextDto> RequireStaffReadAsync(string permission)
    {
        var actor = await access.RequireContextAsync();
        if (!actor.IsSuperAdministrator && !await access.HasPermissionAsync(permission) && !await access.HasPermissionAsync("Admin"))
            throw new UnauthorizedAccessException("Staff information requires the relevant permission.");
        return actor;
    }
    public static (DateTime From, DateTime Until) Dates(DateOnly from, DateOnly to)
    {
        if (from == default || to == DateOnly.MaxValue || to < from || to.DayNumber - from.DayNumber > 3660)
            throw new ArgumentException("Choose a valid date range of up to ten years.");
        return (from.ToDateTime(TimeOnly.MinValue), to.AddDays(1).ToDateTime(TimeOnly.MinValue));
    }
}
