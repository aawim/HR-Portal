using HRM.DTOs;
using HRM.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Interfaces
{
    public class UserContext: IUserContext
    {
        private readonly AuthenticationStateProvider _auth;
        private readonly IDbContextFactory<HrmTeContext> _factory;

        public UserContext(AuthenticationStateProvider auth,
                           IDbContextFactory<HrmTeContext> factory)
        {
            _auth = auth;
            _factory = factory;
        }

        public async Task<int?> GetOrganisationIdAsync()
        {
            var state = await _auth.GetAuthenticationStateAsync();

            var claim = state.User.FindFirst("orgId")?.Value;

            return int.TryParse(claim, out var id) ? id : null;
        }

        public async Task<string> GetOrganisationNameAsync()
        {
            var state = await _auth.GetAuthenticationStateAsync();

            return state.User.FindFirst("orgName")?.Value ?? "";
        }

        public async Task<string?> GetUsernameAsync()
        {
            var state = await _auth.GetAuthenticationStateAsync();

            var userKeyClaim = state.User.FindFirst("idnumber");

            return userKeyClaim?.Value;

            //return state.User.Identity?.Name;
        }

        public async Task<int> GetIndividualIdAsync()
        {
            var username = await GetUsernameAsync();
            if (username == null) return 0;

            await using var db = await _factory.CreateDbContextAsync();

            return await db.Jobs
                .Where(j => j.User.Username == username)
                .Select(j => j.IndividualID)
                .FirstOrDefaultAsync();
        }

        public async Task<int> GetJobIdAsync()
        {
            var indId = await GetIndividualIdAsync();

            await using var db = await _factory.CreateDbContextAsync();

            return await db.Jobs
                .Where(j => j.IndividualID == indId)
                .Select(j => j.JobId)
                .FirstOrDefaultAsync();
        }

        public async Task<int> GetUserIdAsync()
        {
            var username = await GetUsernameAsync();

            await using var db = await _factory.CreateDbContextAsync();

            return await db.Users
                .Where(u => u.Username == username)
                .Select(u => u.UserId)
                .FirstOrDefaultAsync();
        }

        public async Task<UserSessionDto?> GetSessionAsync()
        {
            var username = await GetUsernameAsync();

            if (string.IsNullOrWhiteSpace(username))
                return null;

            await using var db = await _factory.CreateDbContextAsync();

            return await db.Users
                .Where(u => u.Username == username)
                .Select(u => new UserSessionDto
                {
                    UserId = u.UserId,

                    IndividualId = u.Jobs
                        .Select(j => j.IndividualID)
                        .FirstOrDefault(),

                    JobId = u.Jobs
                        .Select(j => j.JobId)
                        .FirstOrDefault(),

                    OrganisationId = u.UserOrganisationId,

                    OrganisationName = u.UserOrganisation.Organisation.OrganisationName,

                    CanAccessAdminPortal = true

                   
                })
                .FirstOrDefaultAsync();
        }
    }
}
