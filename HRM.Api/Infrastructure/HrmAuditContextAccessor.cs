using HRM.AuditLogs.Abstractions;
using HRM.Helpers;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Api.Infrastructure;

public sealed class HrmAuditContextAccessor(
    IHttpContextAccessor httpContextAccessor,
    IServiceProvider serviceProvider) : IAuditContextAccessor
{
    public AuditActorContext? GetCurrentContext()
    {
        var http = httpContextAccessor.HttpContext;
        if (http is null)
            return AuditActorContext.System("HRM.Api");

        var correlationId = ResolveCorrelationId(http);
        var ipAddress = ResolveClientIpAddress(http);
        var url = ResolveRequestUrl(http);

        var access = http.RequestServices.GetService<IUserAccessService>();
        if (access is null)
        {
            return new AuditActorContext
            {
                ActorType = AuditActorTypes.System,
                Source = "HRM.Api",
                CorrelationId = correlationId,
                RequestId = http.TraceIdentifier,
                IpAddress = ipAddress,
                Url = url
            };
        }

        // Synchronous path (e.g. sync SaveChanges): use GetContextAsync if already cached, else system fallback.
        try
        {
            return BuildContextAsync(http, access, CancellationToken.None).GetAwaiter().GetResult();
        }
        catch
        {
            return new AuditActorContext
            {
                ActorType = AuditActorTypes.System,
                Source = "HRM.Api",
                CorrelationId = correlationId,
                RequestId = http.TraceIdentifier,
                IpAddress = ipAddress,
                Url = url
            };
        }
    }

    public async Task<AuditActorContext> GetCurrentContextAsync(CancellationToken cancellationToken = default)
    {
        var http = httpContextAccessor.HttpContext;
        if (http is null)
            return AuditActorContext.System("HRM.Api");

        var access = http.RequestServices.GetService<IUserAccessService>()
                     ?? serviceProvider.GetService<IUserAccessService>();
        if (access is null)
        {
            return new AuditActorContext
            {
                ActorType = AuditActorTypes.System,
                Source = "HRM.Api",
                CorrelationId = ResolveCorrelationId(http),
                RequestId = http.TraceIdentifier,
                IpAddress = ResolveClientIpAddress(http),
                Url = ResolveRequestUrl(http)
            };
        }

        return await BuildContextAsync(http, access, cancellationToken);
    }

    private const string AuditContextItemKey = "HRM.Audit.ActorContext";

    private async Task<AuditActorContext> BuildContextAsync(
        HttpContext http,
        IUserAccessService access,
        CancellationToken cancellationToken)
    {
        if (http.Items.TryGetValue(AuditContextItemKey, out var cached) && cached is AuditActorContext existing)
            return existing;

        var correlationId = ResolveCorrelationId(http);
        var ipAddress = ResolveClientIpAddress(http);
        var url = ResolveRequestUrl(http);
        var userCtx = http.RequestServices.GetService<IUserContextService>()?.Current
                      ?? await access.GetContextAsync();
        if (userCtx is null || userCtx.UserId <= 0)
        {
            return new AuditActorContext
            {
                ActorType = AuditActorTypes.System,
                Source = "HRM.Api",
                CorrelationId = correlationId,
                RequestId = http.TraceIdentifier,
                IpAddress = ipAddress,
                Url = url
            };
        }

        var currentOrg = userCtx.Organisation?.OrganisationBusinessEntityId ?? userCtx.ActiveJob?.OrganisationId ?? 0;
        var isAdmin = userCtx.IsSuperAdministrator || userCtx.IsOrganisationAdmin || userCtx.HasPermission("Admin");
        var isDeveloper = userCtx.UserGroups.Any(g => g.GroupName.Equals("Developer", StringComparison.OrdinalIgnoreCase))
                          || userCtx.HasPermission("Developer");
        var isHrLead = userCtx.IsHRHead || userCtx.IsHRManager;
        var canViewAudit = isAdmin || isDeveloper || isHrLead || userCtx.IsHRStaff ||
                           userCtx.HasPermission("Audit.View") ||
                           userCtx.HasPermission("AuditLogs.View");
        var canViewConfidential = userCtx.IsSuperAdministrator || userCtx.IsOrganisationAdmin || isHrLead ||
                                  userCtx.HasPermission("Audit.ViewConfidential") ||
                                  userCtx.HasPermission("Payroll.Manage");
        var canExport = userCtx.IsSuperAdministrator || userCtx.IsOrganisationAdmin || isHrLead ||
                        userCtx.HasPermission("Audit.Export");
        var canCrossOrg = userCtx.IsSuperAdministrator || userCtx.HasPermission("Audit.CrossOrganisation");

        var authorizedOrgs = new HashSet<int>();
        if (currentOrg > 0) authorizedOrgs.Add(currentOrg);
        if (userCtx.ActiveJob?.OrganisationId > 0) authorizedOrgs.Add(userCtx.ActiveJob.OrganisationId);

        var authorizedDivisions = new HashSet<int>();
        var dbFactory = http.RequestServices.GetService<IDbContextFactory<HrmTeContext>>();
        if (HttpMethods.IsGet(http.Request.Method) && dbFactory is not null && !userCtx.IsSuperAdministrator && currentOrg > 0 && !isAdmin && !isHrLead && !userCtx.IsHRStaff)
        {
            try
            {
                await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
                var scope = await OrganisationalAccessScopeResolver.ResolveCurrentUserAsync(db, access, currentOrg);
                if (scope.HasExplicitScopeGrants)
                {
                    foreach (var structureId in scope.AuthorisedStructureIds)
                        authorizedDivisions.Add(structureId);
                }
            }
            catch
            {
                // Keep audit context resolution resilient if scope lookup is unavailable.
            }
        }

        var isDelegated = userCtx.ActiveJob != null && currentOrg > 0 && userCtx.ActiveJob.OrganisationId != currentOrg;

        var result = new AuditActorContext
        {
            TenantId = currentOrg > 0 ? currentOrg : userCtx.ActiveJob?.OrganisationId,
            OrganisationId = currentOrg > 0 ? currentOrg : userCtx.ActiveJob?.OrganisationId,
            DivisionId = userCtx.ActiveJob?.OrganisationStructureId,
            UserId = userCtx.UserId,
            ActorType = isDelegated ? AuditActorTypes.Delegated : AuditActorTypes.User,
            Username = !string.IsNullOrWhiteSpace(userCtx.FullName) ? userCtx.FullName.Trim() : userCtx.Username?.Trim(),
            CorrelationId = correlationId,
            RequestId = http.TraceIdentifier,
            Source = "HRM.Api",
            IpAddress = ipAddress,
            Url = url,
            IsSuperAdministrator = userCtx.IsSuperAdministrator,
            CanViewAuditLogs = canViewAudit,
            CanViewConfidentialValues = canViewConfidential,
            CanExportAuditLogs = canExport,
            CanViewCrossOrganisationAudit = canCrossOrg,
            AuthorizedOrganisationIds = authorizedOrgs.ToArray(),
            AuthorizedDivisionIds = authorizedDivisions.ToArray()
        };
        http.Items[AuditContextItemKey] = result;
        return result;
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
}
