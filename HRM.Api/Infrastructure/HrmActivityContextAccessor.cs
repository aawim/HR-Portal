using System.Security.Claims;
using HRM.ActivityLogs.Abstractions;
using HRM.ActivityLogs.Security;
using HRM.Helpers;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Api.Infrastructure;

public sealed class HrmActivityContextAccessor(
    IHttpContextAccessor httpContextAccessor,
    IServiceProvider serviceProvider) : IActivityContextAccessor
{
    public const string ExplicitActivityRecordedItemKey = "HRM.ActivityLogs.ExplicitRecorded";

    public void MarkActivityRecorded()
    {
        var http = httpContextAccessor.HttpContext;
        if (http is not null)
            http.Items[ExplicitActivityRecordedItemKey] = true;
    }
    public async Task<ActivityActorContext> GetCurrentContextAsync(CancellationToken cancellationToken = default)
    {
        var http = httpContextAccessor.HttpContext;
        if (http is null)
            return ActivityActorContext.System("HRM.Api");

        var correlationId = ResolveCorrelationId(http);
        var opaqueSessionId = ResolveOpaqueSessionId(http);
        var ipAddress = ResolveClientIpAddress(http);
        var url = ResolveRequestUrl(http);

        var access = http.RequestServices.GetService<IUserAccessService>()
                     ?? serviceProvider.GetService<IUserAccessService>();
        if (access is null)
        {
            return new ActivityActorContext
            {
                ActorType = ActivityActorTypes.System,
                Source = "HRM.Api",
                CorrelationId = correlationId,
                OpaqueSessionId = opaqueSessionId,
                IpAddress = ipAddress,
                Url = url
            };
        }

        var userCtx = await access.GetContextAsync();
        if (userCtx is null || userCtx.UserId <= 0)
        {
            return new ActivityActorContext
            {
                ActorType = ActivityActorTypes.System,
                Source = "HRM.Api",
                CorrelationId = correlationId,
                OpaqueSessionId = opaqueSessionId,
                IpAddress = ipAddress,
                Url = url
            };
        }

        var currentOrg = await access.GetCurrentOrganisationIdAsync();
        var effectiveOrg = currentOrg > 0 ? currentOrg : userCtx.ActiveJob?.OrganisationId;

        var isAdmin = userCtx.IsSuperAdministrator || userCtx.IsOrganisationAdmin || await access.HasPermissionAsync("Admin");
        var isHrLead = userCtx.IsHRHead || userCtx.IsHRManager;
        var canViewOrg = isAdmin || isHrLead || userCtx.IsHRStaff ||
                         await access.HasPermissionAsync("Activity.View") ||
                         await access.HasPermissionAsync("ActivityLogs.View");
        var canViewTeam = canViewOrg || userCtx.IsSupervisor ||
                          await access.HasPermissionAsync("Activity.ViewTeam");
        var canCrossOrg = userCtx.IsSuperAdministrator || await access.HasPermissionAsync("Activity.CrossOrganisation");
        var canExport = userCtx.IsSuperAdministrator || userCtx.IsOrganisationAdmin || isHrLead ||
                        await access.HasPermissionAsync("Activity.Export");

        var authorizedOrgs = new HashSet<int>();
        if (currentOrg > 0) authorizedOrgs.Add(currentOrg);
        if (userCtx.ActiveJob?.OrganisationId > 0) authorizedOrgs.Add(userCtx.ActiveJob.OrganisationId);

        var authorizedDivisions = new HashSet<int>();
        var authorizedTeams = new HashSet<int>();

        var dbFactory = http.RequestServices.GetService<IDbContextFactory<HrmTeContext>>();
        if (dbFactory is not null && !userCtx.IsSuperAdministrator && effectiveOrg is > 0)
        {
            try
            {
                await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);

                var scope = await OrganisationalAccessScopeResolver.ResolveCurrentUserAsync(db, access, effectiveOrg.Value);
                if (scope.HasExplicitScopeGrants && !canViewOrg)
                {
                    foreach (var structureId in scope.AuthorisedStructureIds)
                        authorizedDivisions.Add(structureId);
                }

                if (userCtx.IndividualId > 0)
                {
                    var now = DateTime.UtcNow;
                    var teamIds = await db.TeamStaffs
                        .AsNoTracking()
                        .Where(ts => ts.IsValid &&
                                     (ts.EndDate == null || ts.EndDate >= now) &&
                                     ts.Staff.IndividualId == userCtx.IndividualId)
                        .Select(ts => ts.TeamId)
                        .Distinct()
                        .ToListAsync(cancellationToken);

                    foreach (var tid in teamIds)
                        authorizedTeams.Add(tid);
                }
            }
            catch
            {
                // Keep activity context resolution resilient if scope lookup is temporarily unavailable.
            }
        }

        var isDelegated = userCtx.ActiveJob != null && currentOrg > 0 && userCtx.ActiveJob.OrganisationId != currentOrg;

        return new ActivityActorContext
        {
            TenantId = effectiveOrg,
            OrganisationId = effectiveOrg,
            DivisionId = userCtx.ActiveJob?.OrganisationStructureId,
            TeamId = authorizedTeams.Count == 1 ? authorizedTeams.First() : null,
            UserId = userCtx.UserId,
            UserName = !string.IsNullOrWhiteSpace(userCtx.FullName) ? userCtx.FullName.Trim() : userCtx.Username?.Trim(),
            ActorType = isDelegated ? ActivityActorTypes.Delegated : ActivityActorTypes.User,
            IpAddress = ipAddress,
            Url = url,
            CorrelationId = correlationId,
            OpaqueSessionId = opaqueSessionId,
            Source = "HRM.Api",
            IsSuperAdministrator = userCtx.IsSuperAdministrator,
            CanViewOrganisationActivity = canViewOrg,
            CanViewTeamActivity = canViewTeam,
            CanViewCrossOrganisationActivity = canCrossOrg,
            CanExportActivity = canExport,
            AuthorizedOrganisationIds = authorizedOrgs.ToArray(),
            AuthorizedDivisionIds = authorizedDivisions.ToArray(),
            AuthorizedTeamIds = authorizedTeams.ToArray()
        };
    }

    private static string ResolveCorrelationId(HttpContext http)
    {
        if (http.Request.Headers.TryGetValue("X-Correlation-ID", out var header) && !string.IsNullOrWhiteSpace(header))
            return header.ToString().Trim();
        return http.TraceIdentifier;
    }

    private static string? ResolveClientIpAddress(HttpContext http)
    {
        if (http.Request.Headers.TryGetValue("X-Forwarded-For", out var forwarded) && !string.IsNullOrWhiteSpace(forwarded))
        {
            var first = forwarded.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first))
                return first;
        }

        if (http.Request.Headers.TryGetValue("X-Real-IP", out var realIp) && !string.IsNullOrWhiteSpace(realIp))
            return realIp.ToString().Trim();

        return http.Connection.RemoteIpAddress?.ToString();
    }

    private static string? ResolveRequestUrl(HttpContext http)
    {
        if (http.Request.Headers.TryGetValue("X-Client-Url", out var clientUrl) && !string.IsNullOrWhiteSpace(clientUrl))
        {
            var raw = clientUrl.ToString().Trim();
            var qIdx = raw.IndexOf('?');
            return qIdx >= 0 ? raw[..qIdx] : raw;
        }

        var path = $"{http.Request.PathBase}{http.Request.Path}";
        if (string.IsNullOrWhiteSpace(path)) return null;
        return http.Request.Host.HasValue
            ? $"{http.Request.Scheme}://{http.Request.Host}{path}"
            : path;
    }

    private static string? ResolveOpaqueSessionId(HttpContext http)
    {
        // Derive an opaque one-way identifier — never store raw session tokens or cookies.
        var sid = http.User?.FindFirstValue("sid")
                  ?? http.User?.FindFirstValue(ClaimTypes.Sid)
                  ?? http.User?.FindFirstValue("jti");
        if (!string.IsNullOrWhiteSpace(sid))
            return ActivitySanitizer.ToOpaqueSessionId(sid);

        if (http.Request.Headers.TryGetValue("X-Session-Key", out var sessionHeader) && !string.IsNullOrWhiteSpace(sessionHeader))
            return ActivitySanitizer.ToOpaqueSessionId(sessionHeader.ToString());

        var userId = http.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
            return ActivitySanitizer.ToOpaqueSessionId($"user:{userId}:{http.Connection.Id}");

        return null;
    }
}
