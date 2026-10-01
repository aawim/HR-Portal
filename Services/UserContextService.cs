using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Data;

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

            var authState = await _authentication.GetAuthenticationStateAsync();
            var principal = authState.User;

            if (principal.Identity?.IsAuthenticated != true)
                return null;

            var username = principal.FindFirst("idnumber")?.Value;

            if (string.IsNullOrWhiteSpace(username))
                return null;

            var fullName =
                principal.FindFirst("full_name")?.Value ??
                principal.Identity?.Name ??
                username;

            var firstName =
                principal.FindFirst("first_name")?.Value ??
                principal.FindFirst("given_name")?.Value ??
                fullName;

            var middleName =
                principal.FindFirst("middle_name")?.Value;

            var lastName =
                principal.FindFirst("last_name")?.Value ??
                principal.FindFirst("family_name")?.Value ??
                string.Empty;

            var email = principal.FindFirst("email")?.Value;
            var mobile = principal.FindFirst("mobile")?.Value;

            await using var db =
                await _dbFactory.CreateDbContextAsync();

            // 1. Existing user: allow access only when active and approved.
            var existingUser = await db.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Username == username);

            if (existingUser != null)
            {
                var hasAccess =
                    existingUser.IsActive &&
                    //existingUser.IsApproved == true &&
                    existingUser.LocalUserStateId == 1; // Replace with your Active state ID

                if (hasAccess)
                {
                    return await GetAsync(existingUser.UserId);
                }

                // Existing user, but not active/approved: do not create anything.
                return null;
            }

            // 2. The person may already have been created during an earlier denied login.
            var existingIdCard = await db.Idcards
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.IdcardNumber == username);

            if (existingIdCard != null)
            {
                // Individual exists but HR has not created an active User yet.
                return null;
            }

            // 3. Create a pending Individual + ID card.
            // Serializable prevents duplicate records when two login attempts occur together.
            await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            try
            {
                // Use an existing Individual BusinessEntity configuration as the template.
                var businessEntityTemplate = await (
                    from individuals in db.Individuals.AsNoTracking()
                    join BusinessEntity in db.BusinessEntities.AsNoTracking()
                        on individuals.BusinessEntityId equals BusinessEntity.BusinessEntityID
                    select new
                    {
                        BusinessEntity.BusinessEntityTypeId,
                        BusinessEntity.VerifiedStateId,
                        BusinessEntity.BusinessEntityStateId
                    }
                ).FirstOrDefaultAsync();

                if (businessEntityTemplate == null)
                {
                    throw new InvalidOperationException(
                        "No existing Individual business entity is available as a template.");
                }

                var businessEntity = new BusinessEntity
                {
                    BusinessEntityTypeId =
                        businessEntityTemplate.BusinessEntityTypeId,

                    VerifiedStateId =
                        businessEntityTemplate.VerifiedStateId,

                    BusinessEntityStateId =
                        businessEntityTemplate.BusinessEntityStateId,

                    // OperationLogID is nullable in BusinessEntities.
                    OperationLogId = null
                };

                db.BusinessEntities.Add(businessEntity);
                await db.SaveChangesAsync();

                var individual = new Individual
                {
                    BusinessEntityId = businessEntity.BusinessEntityID,

                    FirstNameEnglish = firstName,
                    MiddleNameEnglish = middleName,
                    LastNameEnglish = lastName,

                    FirstNameDhivehi = null,
                    MiddleNameDhivehi = null,
                    LastNameDhivehi = null,

                    // Replace with eFaas values when available.
                    // Do not use these defaults if your HR policy requires verified data.
                    DateOfBirth = DateTime.Today,
                    GenderTypeId = 1,
                    CountryId = 1
                };

                db.Individuals.Add(individual);
                await db.SaveChangesAsync();

                var idCard = new Idcard
                {
                    BusinessEntityId = businessEntity.BusinessEntityID,

                    // eFaas idnumber becomes the HR ID card number.
                    IdcardNumber = username,

                    ExpiryDate = null,

                    // As you specified.
                    IdcardStateId = 1,

                    // Nullable.
                    OperationLogId = null
                };

                db.Idcards.Add(idCard);
                await db.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }

            // Individual was created, but no User exists.
            // The caller should redirect to Access Denied.
            return null;

            //if (Current != null)
            //    return Current;

            //var authState =
            //    await _authentication.GetAuthenticationStateAsync();


            //var user1 = authState.User;

            //bool isAuthenticated = user1.Identity?.IsAuthenticated ?? false;
            //string? name = user1.Identity?.Name;
            //int claimCount = user1.Claims.Count();

            //var username =
            //    authState.User.FindFirst("idnumber")?.Value;

            //if (string.IsNullOrWhiteSpace(username))
            //    return null;

            //await using var db =
            //    await _dbFactory.CreateDbContextAsync();

            //var user =
            //    await db.Users
            //        .AsNoTracking()
            //        .FirstOrDefaultAsync(x =>
            //            x.Username == username);

            //if (user == null)
            //    return null;

            //return await GetAsync(user.UserId);
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
