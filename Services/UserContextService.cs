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


            //var auth = await _authentication.GetAuthenticationStateAsync(); 

            //Console.WriteLine(auth.User.Identity?.IsAuthenticated);

            //foreach (var claim in auth.User.Claims)
            //{
            //    Console.WriteLine($"{claim.Type} = {claim.Value}");
            //}



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














        //private readonly IUserService _userService;
        //private readonly IUserContextBuilder _builder;
        //private readonly IUserContextCache _cache;
        //private readonly AuthenticationStateProvider _auth;


        //public UserContextService(
        //IUserService userService,
        //IUserContextBuilder builder,
        //IUserContextCache cache,
        //AuthenticationStateProvider auth)
        //{
        //    _userService = userService;
        //    _builder = builder;
        //    _cache = cache;
        //    _auth = auth;
        //}


        //public UserContextDto? Current { get; private set; }

        //public async Task<UserContextDto?> GetCurrentAsync()
        //{
        //    if (Current != null)
        //        return Current;


        //    var authState =
        //        await _auth.GetAuthenticationStateAsync();


        //    var username =
        //        authState.User
        //        .FindFirst("idnumber")
        //        ?.Value;


        //    if (string.IsNullOrEmpty(username))
        //        return null;


        //    var user =
        //        await _userService.GetByUsernameAsync(username);


        //    if (user == null)
        //        return null;


        //    var cached =
        //        await _cache.GetAsync(user.UserID);


        //    if (cached != null)
        //    {
        //        Current = cached;
        //        return cached;
        //    }



        //    var context =
        //        await _builder
        //        .BuildAsync(user.UserID);


        //    await _cache.SetAsync(
        //        user.UserID,
        //        context);


        //    Current = context;


        //    return context;
        //}



        //public async Task RefreshAsync()
        //{
        //    Current = null;


        //    var authState =
        //        await _auth.GetAuthenticationStateAsync();


        //    var username =
        //        authState.User
        //        .FindFirst("idnumber")
        //        ?.Value;


        //    if (username == null)
        //        return;


        //    var user =
        //        await _userService.GetByUsernameAsync(username);


        //    if (user == null)
        //        return;


        //    await _cache.RemoveAsync(
        //        user.UserID);
        //}


    }
}
