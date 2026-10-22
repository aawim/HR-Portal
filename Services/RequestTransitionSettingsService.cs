using System.Data;
using HRM.DTOs;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public sealed class RequestTransitionSettingsService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access)
{
    private async Task RequireDeveloperAsync()
    {
        await access.RequireContextAsync();
        if (!await access.HasUserGroupAsync("Developer"))
            throw new UnauthorizedAccessException("Only the Developer user group can access request-state transition configuration.");
    }
    public async Task<RequestTransitionSettingsDto> LoadAsync()
    {
        await RequireDeveloperAsync();
        await using var db = await factory.CreateDbContextAsync();
        return new()
        {
            RequestTypes = await db.RequestTypes.AsNoTracking().OrderBy(x => x.TypeName).Select(x => new RequestTransitionOption(x.RequestTypeId, x.TypeName)).ToListAsync(),
            States = await db.RequestStates.AsNoTracking().OrderBy(x => x.RequestStateId).Select(x => new RequestTransitionOption(x.RequestStateId, x.StateName.Trim())).ToListAsync()
        };
    }
    public async Task<List<AllowedRequestTransitionDto>> GetAsync(int requestType)
    {
        await RequireDeveloperAsync();
        await using var db = await factory.CreateDbContextAsync();
        return await db.RequestTypesAllowedRequestStateTransitions.AsNoTracking().Where(x => x.RequestTypeId == requestType)
            .OrderBy(x => x.FromRequestStateId).ThenBy(x => x.ToRequestStateId)
            .Select(x => new AllowedRequestTransitionDto(x.FromRequestStateId, x.FromRequestState.StateName.Trim(), x.ToRequestStateId, x.ToRequestState.StateName.Trim())).ToListAsync();
    }
    public async Task<ServiceResult> SetAsync(int requestType, int from, int to, bool allowed)
    {
        await RequireDeveloperAsync();
        if (from == to) return ServiceResult.Fail("From State and To State must be different.");
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
        if (!await db.RequestTypes.AnyAsync(x => x.RequestTypeId == requestType) ||
            await db.RequestStates.CountAsync(x => x.RequestStateId == from || x.RequestStateId == to) != 2)
            return ServiceResult.Fail("Select a valid Request Type, From State and To State.");
        if (allowed && await db.RequestStates.AnyAsync(x => x.RequestStateId == from && x.IsFinalState))
            return ServiceResult.Fail("A final state cannot have outgoing transitions. Select a processing state as From State. Existing final-state rules can be removed.");
        var row = await db.RequestTypesAllowedRequestStateTransitions.SingleOrDefaultAsync(x => x.RequestTypeId == requestType && x.FromRequestStateId == from && x.ToRequestStateId == to);
        if (allowed && row == null) db.RequestTypesAllowedRequestStateTransitions.Add(new RequestTypesAllowedRequestStateTransition
            { RequestTypeId = requestType, FromRequestStateId = from, ToRequestStateId = to });
        if (!allowed && row != null) db.RequestTypesAllowedRequestStateTransitions.Remove(row);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
        return ServiceResult.Ok(allowed ? "Transition allowed." : "Transition removed. Future actions using this transition will be blocked.");
    }
}
