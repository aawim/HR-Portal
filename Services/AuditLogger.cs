using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using HRM.Services.Interfaces;
using HRM.Audit.Models;


namespace HRM.Services
{
    public class AuditLogger : IAuditLogger
    {
        private readonly AuditDbContext _auditDb;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly NavigationManager _navigationManager;
        private readonly AuthenticationStateProvider _authProvider;

        public AuditLogger(
            AuditDbContext auditDb,
          IHttpContextAccessor httpContextAccessor,
        NavigationManager navigationManager,
        AuthenticationStateProvider authProvider)
        {
            _auditDb = auditDb;
            _httpContextAccessor = httpContextAccessor;
            _navigationManager = navigationManager;
            _authProvider = authProvider;
        }
        public async Task CreateLogAsync(
        int dataTypeId,
        int actionTypeId,
        int dataItemId,
        object data)
        {
            var authState = await _authProvider.GetAuthenticationStateAsync();

            var username =
                authState.User.Identity?.Name ?? "Anonymous";

            var ipAddress =
                _httpContextAccessor.HttpContext?
                    .Connection
                    .RemoteIpAddress?
                    .ToString() ?? "";

            var auditLog = new AuditLog
            {
                AuditLogDataTypeID = dataTypeId,
                AuditLogActionTypeID = actionTypeId,
                DataItemID = dataItemId,
                Data = JsonSerializer.Serialize(data),

                URL = _navigationManager.Uri,

                CreatedByUserID = 0, // lookup actual user id
                CreatedByIPAddress = ipAddress,
                CreatedDate = DateTime.UtcNow
            };

            _auditDb.AuditLogs.Add(auditLog);
            await _auditDb.SaveChangesAsync();
        }

        public async Task<int> CreateNewLogAsync(
            int dataTypeId,
            int actionTypeId,
            int dataItemId,
            object data)
        {
            var authState = await _authProvider.GetAuthenticationStateAsync();

            var ipAddress =
                _httpContextAccessor.HttpContext?
                    .Connection
                    .RemoteIpAddress?
                    .ToString() ?? "";

            var auditLog = new AuditLog
            {
                AuditLogDataTypeID = dataTypeId,
                AuditLogActionTypeID = actionTypeId,
                DataItemID = dataItemId,
                Data = JsonSerializer.Serialize(data),
                URL = _navigationManager.Uri,
                CreatedByUserID = 0,
                CreatedByIPAddress = ipAddress,
                CreatedDate = DateTime.UtcNow
            };

            _auditDb.AuditLogs.Add(auditLog);
            await _auditDb.SaveChangesAsync();

            return auditLog.AuditLogID;
        }

    }
}
