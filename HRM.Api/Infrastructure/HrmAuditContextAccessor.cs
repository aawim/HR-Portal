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
        var ipAddress = http.Connection.RemoteIpAddress?.ToString();

        var access = http.RequestServices.GetService<IUserAccessService>();
        if (access is null)
        {
            return new AuditActorContext
            {
                ActorType = AuditActorTypes.System,
                Source = "HRM.Api",
                CorrelationId = correlationId,
                RequestId = http.TraceIdentifier,
                IpAddress = ipAddress
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
                IpAddress = ipAddress
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
                IpAddress = http.Connection.RemoteIpAddress?.ToString()
            };
        }

        return await BuildContextAsync(http, access, cancellationToken);
    }

    private async Task<AuditActorContext> BuildContextAsync(
        HttpContext http,
        IUserAccessService access,
        CancellationToken cancellationToken)
    {
        var correlationId = ResolveCorrelationId(http);
        var ipAddress = http.Connection.RemoteIpAddress?.ToString();
        var userCtx = await access.GetContextAsync();
        if (userCtx is null || userCtx.UserId <= 0)
        {
            return new AuditActorContext
            {
                ActorType = AuditActorTypes.System,
                Source = "HRM.Api",
                CorrelationId = correlationId,
                RequestId = http.TraceIdentifier,
                IpAddress = ipAddress
            };
        }

        var currentOrg = await access.GetCurrentOrganisationIdAsync();
        var isAdmin = userCtx.IsSuperAdministrator || userCtx.IsOrganisationAdmin || await access.HasPermissionAsync("Admin");
        var isHrLead = userCtx.IsHRHead || userCtx.IsHRManager;
        var canViewAudit = isAdmin || isHrLead || userCtx.IsHRStaff ||
                           await access.HasPermissionAsync("Audit.View") ||
                           await access.HasPermissionAsync("AuditLogs.View");
        var canViewConfidential = userCtx.IsSuperAdministrator || userCtx.IsOrganisationAdmin || isHrLead ||
                                  await access.HasPermissionAsync("Audit.ViewConfidential") ||
                                  await access.HasPermissionAsync("Payroll.Manage");
        var canExport = userCtx.IsSuperAdministrator || userCtx.IsOrganisationAdmin || isHrLead ||
                        await access.HasPermissionAsync("Audit.Export");
        var canCrossOrg = userCtx.IsSuperAdministrator || await access.HasPermissionAsync("Audit.CrossOrganisation");

        var authorizedOrgs = new HashSet<int>();
        if (currentOrg > 0) authorizedOrgs.Add(currentOrg);
        if (userCtx.ActiveJob?.OrganisationId > 0) authorizedOrgs.Add(userCtx.ActiveJob.OrganisationId);

        var authorizedDivisions = new HashSet<int>();
        var dbFactory = http.RequestServices.GetService<IDbContextFactory<HrmTeContext>>();
        if (dbFactory is not null && !userCtx.IsSuperAdministrator && currentOrg > 0)
        {
            try
            {
                await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
                var scope = await OrganisationalAccessScopeResolver.ResolveCurrentUserAsync(db, access, currentOrg);
                if (scope.HasExplicitScopeGrants && !isAdmin && !isHrLead && !userCtx.IsHRStaff)
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

        return new AuditActorContext
        {
            TenantId = currentOrg > 0 ? currentOrg : userCtx.ActiveJob?.OrganisationId,
            OrganisationId = currentOrg > 0 ? currentOrg : userCtx.ActiveJob?.OrganisationId,
            DivisionId = userCtx.ActiveJob?.OrganisationStructureId,
            UserId = userCtx.UserId,
            ActorType = isDelegated ? AuditActorTypes.Delegated : AuditActorTypes.User,
            Username = userCtx.Username,
            CorrelationId = correlationId,
            RequestId = http.TraceIdentifier,
            Source = "HRM.Api",
            IpAddress = ipAddress,
            IsSuperAdministrator = userCtx.IsSuperAdministrator,
            CanViewAuditLogs = canViewAudit,
            CanViewConfidentialValues = canViewConfidential,
            CanExportAuditLogs = canExport,
            CanViewCrossOrganisationAudit = canCrossOrg,
            AuthorizedOrganisationIds = authorizedOrgs.ToArray(),
            AuthorizedDivisionIds = authorizedDivisions.ToArray()
        };
    }

    private static string ResolveCorrelationId(HttpContext http)
    {
        if (http.Request.Headers.TryGetValue("X-Correlation-ID", out var header) && !string.IsNullOrWhiteSpace(header))
            return header.ToString().Trim();
        return http.TraceIdentifier;
    }
}
