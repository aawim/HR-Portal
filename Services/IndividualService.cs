using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services
{
    public class IndividualService : IIndividual
    {
        private readonly AuthenticationStateProvider _authProvider;
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;

        public IndividualService(
            IDbContextFactory<HrmTeContext> dbFactory,
            AuthenticationStateProvider authProvider)
        {
            _dbFactory = dbFactory;
            _authProvider = authProvider;
        }
        //public async Task<int> GetIndividualID()
        //{
        //    await using var context = await _dbFactory.CreateDbContextAsync();

        //    var authState = await _authProvider.GetAuthenticationStateAsync();

        //    var username = authState.User.FindFirst("idnumber")?.Value;

        //    if (string.IsNullOrWhiteSpace(username))
        //        return 0;

        //    var user = await context.Users
        //        .FirstOrDefaultAsync(u => u.Username == username);

        //    if (user == null)
        //        return 0;

        //    return user.BusinessEntityID;
        //}

        public async Task<int> GetIndividualID()
        {
            await using var context = await _dbFactory.CreateDbContextAsync();

            var authState = await _authProvider.GetAuthenticationStateAsync();

            var username = authState.User.FindFirst("idnumber")?.Value;

            Console.WriteLine($"Claim username: {username}");

            if (string.IsNullOrWhiteSpace(username))
                return 0;

            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Username == username.ToString());

            if (user == null)
            {
                Console.WriteLine("User not found.");
                return 0;
            }

            Console.WriteLine($"BusinessEntityID: {user.BusinessEntityID}");

            return user.BusinessEntityID;
        }
    }
}
