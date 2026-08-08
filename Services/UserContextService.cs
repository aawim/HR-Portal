using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services
{
    public class UserContextService : IUserContextService
    {
        private readonly IUserContextBuilder _builder;
        private readonly IUserContextCache _cache;
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly AuthenticationStateProvider _authentication;

        public UserContextDto? Current { get; private set; }

        public UserContextService(
            IUserContextBuilder builder,
            IUserContextCache cache,
            IDbContextFactory<HrmTeContext> dbFactory,
            AuthenticationStateProvider authentication)
        {
            _builder = builder;
            _cache = cache;
            _dbFactory = dbFactory;
            _authentication = authentication;
        }
        public async Task<UserContextDto?> GetCurrentAsync()
        {

 

            if (Current != null)
                return Current;

            var authState =
                await _authentication.GetAuthenticationStateAsync();


            var user1 = authState.User;

            bool isAuthenticated = user1.Identity?.IsAuthenticated ?? false;
            string? name = user1.Identity?.Name;
            int claimCount = user1.Claims.Count();
       
            var username =
                authState.User.FindFirst("idnumber")?.Value;

            if (string.IsNullOrWhiteSpace(username))
                return null;

            await using var db =
                await _dbFactory.CreateDbContextAsync();

            var user =
                await db.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x =>
                        x.Username == username);

            if (user == null)
                return null;

            return await GetAsync(user.UserId);
        }


        public async Task<UserContextDto?> GetAsync(int userId)
        {
            var cached =
                await _cache.GetAsync(userId);

            if (cached != null)
            {
                Current = cached;
                return cached;
            }

            var context =
                await _builder.BuildAsync(userId);

            if (context == null)
                return null;

            await _cache.SetAsync(userId, context);

            Current = context;

            return context;
        }


        public async Task<UserContextDto?> RefreshAsync(int userId)
        {
            await _cache.RemoveAsync(userId);

            Current = null;

            return await GetAsync(userId);
        }


        public async Task InvalidateAsync(int userId)
        {
            await _cache.RemoveAsync(userId);

            if (Current?.UserId == userId)
                Current = null;
        }

    }
}
