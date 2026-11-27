using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace HRM.Services
{
    public class OperationLogService : IOperationLogService
    {

        private readonly HrmTeContext _dbContext;
        private readonly PortalContext _portalContext;
        private readonly AuthenticationStateProvider _authStateProvider;
        private readonly IUserAccessService _access;
        private readonly IHttpContextAccessor _httpContextAccessor;
        public OperationLogService(HrmTeContext dbContext, AuthenticationStateProvider authStateProvider, PortalContext portalContext, IHttpContextAccessor httpContextAccessor ,IIndividual individual, 
            IUserAccessService access )
        {
            _dbContext = dbContext;
            _authStateProvider = authStateProvider;
            _portalContext = portalContext;
            _httpContextAccessor = httpContextAccessor;
            _access = access;
         
        }


        public async Task<OperationLog> CreateAsync(HrmTeContext dbContext, int actionId,string remarks)
        {
            var userContext = await _access.GetContextAsync();
            var ip =
                _httpContextAccessor.HttpContext?
                .Connection
                .RemoteIpAddress?
                .ToString();

            int? individualId = null;
            int? userId = null;
            int? userOrganisationId = null;

            if (userContext is { UserId: > 0 })
            {
                individualId = userContext.IndividualId > 0 ? userContext.IndividualId : null;
                userId = userContext.UserId;
                var organisationId = userContext.Organisation?.OrganisationBusinessEntityId ?? userContext.ActiveJob?.OrganisationId;
                userOrganisationId = organisationId.HasValue
                    ? await dbContext.UserOrganisations.Where(x => x.BusinessEntityID == organisationId.Value)
                        .Select(x => (int?)x.UserOrganisationID).FirstOrDefaultAsync()
                    : await dbContext.Users.Where(x => x.UserId == userContext.UserId).Select(x => x.UserOrganisationId).FirstOrDefaultAsync();
            }
            else
            {
                var principal = _httpContextAccessor.HttpContext?.User;
                var idNumber = principal?.Identity?.IsAuthenticated == true
                    ? principal.FindFirstValue("idnumber")?.Trim()
                    : null;
                if (string.IsNullOrWhiteSpace(idNumber))
                    throw new UnauthorizedAccessException("User is not authenticated.");

                individualId = await dbContext.Idcards.AsNoTracking()
                    .Where(c => c.IdcardNumber == idNumber)
                    .Select(c => (int?)c.BusinessEntityId)
                    .FirstOrDefaultAsync();
            }

            var log = new OperationLog
            {
                OperationLogActionId = actionId,

                CreatedByIndividualId = individualId,
                UpdatedByIndividualId = individualId,

                CreatedByUserId = userId,
                UpdatedByUserId = userId,

                CreatedByUserOrganisationId = userOrganisationId,
                UpdatedByUserOrganisationId = userOrganisationId,

                CreatedByContextId = 1,
                UpdatedContextId = 1,

                CreatedDate = DateTime.Now,
                UpdatedDate = DateTime.Now,

                CreatedByIp = ip ?? "",
                UpdatedByIp = ip ?? "",

                Remarks = remarks
            };

            dbContext.OperationLogs.Add(log);

            await dbContext.SaveChangesAsync();

            return log;
        }



        public async Task<int> CreateLogAsync(int actionTypeId, string? remarks = null)
        {
            // 1. Get the current OIDC Authenticated User
            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            int userId = 0;
            int? orgId = null;
            int? individualId = null;
            var ipAddress = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();

            if (user.Identity != null && user.Identity.IsAuthenticated)
            {
                // 2. Extract the OIDC Subject (sub) or NameIdentifier
                // Note: If your OIDC provider uses strings (Guids) for sub, you will need to map 
                // that string to your integer UserID in the database here!
                var subClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                            ?? user.FindFirst("sub")?.Value;


                // 2. Safely try to parse the string into a System.Guid
                if (!string.IsNullOrEmpty(subClaim) && Guid.TryParse(subClaim, out Guid userKeyGuid))
                {
                    // 3. Query the DB using the properly parsed Guid (userKeyGuid)
                    var userInfo = await _dbContext.Users.Where(u => u.SsouserKey == userKeyGuid).FirstOrDefaultAsync();

                    if (userInfo != null)
                    {
                     

                        if (int.TryParse(userInfo.Username, out var businessEntityId))
                        {
                            var individual = await _dbContext.Individuals
                                .FirstOrDefaultAsync(i => i.BusinessEntityId == businessEntityId);

                            individualId = individual?.BusinessEntityId;
                        }
                      
                    }

                    if (userInfo != null)
                    {
                        userId = userInfo.UserId;
                        orgId = userInfo.UserOrganisationId;
                        individualId = userInfo.BusinessEntityID;
                    }
                }
            }

            // 4. Create the log
            var log = new OperationLog
            {
                OperationLogActionId = actionTypeId,
                CreatedByIndividualId = individualId,
                CreatedByUserId = userId,
                CreatedByContextId = (int)_portalContext.CurrentContext,
                CreatedDate = DateTime.Now,
                UpdatedDate = null,
                UpdatedByIndividualId = null,
                UpdatedByUserId = null,
                CreatedByIp = ipAddress?.ToString() ?? string.Empty,
                UpdatedByIp = string.Empty,
                Remarks = remarks,
                UpdatedContextId = (int)_portalContext.CurrentContext,
                CreatedByUserOrganisationId = orgId,
                UpdatedByUserOrganisationId = null,
            };

            _dbContext.OperationLogs.Add(log);
            await _dbContext.SaveChangesAsync();

            return log.OperationLogId;
        }




    }
}
