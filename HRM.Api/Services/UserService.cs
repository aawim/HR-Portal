using Microsoft.EntityFrameworkCore;
using HRM.Models;
using HRM.Services.Interfaces;
using HRM.DTOs.UserContext;
using HRM.DTOs; // Adjust this if your models are in a different folder

namespace HRM.Services
{
    public class UserService :global::HRM.Services.Contracts.UserServiceContracts,global::HRM.Services.Contracts.UserServiceContracts.IOperations,IUserService
    {

        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IUserAccessService _access;
        private readonly UserSettingsService? _accounts;

        public UserService(
            IDbContextFactory<HrmTeContext> dbFactory, IUserAccessService access, UserSettingsService? accounts = null)
        {
            _dbFactory = dbFactory;
            _access = access;
            _accounts = accounts;
        }



        public async Task<UserProfileDto?> GetByIdAsync(int userId)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync();

            return await db.Users
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => new UserProfileDto
                {
                    UserId = x.UserId,

                    Username = x.Username,

                    IndividualID = x.BusinessEntityID
                })
                .FirstOrDefaultAsync();
        }

        public async Task<UserProfileDto?> GetByUsernameAsync(string username)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync();

            return await db.Users
                .AsNoTracking()
                .Where(x => x.Username == username)
                .Select(x => new UserProfileDto
                {
                    UserId = x.UserId,

                    Username = x.Username,

                    IndividualID = x.BusinessEntityID
                })
                .FirstOrDefaultAsync();
        }

        public async Task<UserProfileDto?> GetByBusinessEntityAsync(int businessEntityId)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync();

            return await db.Users
                .AsNoTracking()
                .Where(x => x.BusinessEntityID == businessEntityId)
                .Select(x => new UserProfileDto
                {
                    UserId = x.UserId,

                    Username = x.Username,

                    IndividualID = x.BusinessEntityID,

                    FullName =
                        x.BusinessEntity == null ? x.FullName ?? x.Username :
                        x.BusinessEntity.FirstNameEnglish + " " + x.BusinessEntity.LastNameEnglish
                })
                .FirstOrDefaultAsync();
        }


        public async Task<UserProfileDto?> GetMyProfileAsync()
        {
            var context = await _access.RequireContextAsync();

            if (context.UserId == 0)
                return null;

            await using var db = await _dbFactory.CreateDbContextAsync();

            return new UserProfileDto
            {
                UserId = context.UserId,

                IndividualID = context.IndividualId,

                Username = context.Username,

                FullName = context.FullName,
                JobHistory = await JobHistoryQuery.LoadAsync(db, context.IndividualId),

                OrganisationName =
                    context.Organisation?.OrganisationName,

                ActiveJob = context.ActiveJob == null
                    ? null
                    : new JobDto
                    {
                        JobID = context.ActiveJob.JobId,
                        IndividualID = context.IndividualId,
                        JobStateID = context.ActiveJob.JobStateId,
                        JobTypeID = context.ActiveJob.JobTypeId ?? 0,
                        JobTypeName = context.ActiveJob.JobTypeName,
                        JoinedDate = context.ActiveJob.JoinedDate,
                        TerminatedDate = context.ActiveJob.TerminatedDate,

                        OrganisationID = context.ActiveJob.OrganisationId,

                        OrganisationName =
                        context.ActiveJob.OrganisationName,

                                    OrganisationStructureName =
                        context.ActiveJob.OrganisationStructureName,

            

                                    JobStateName =
                        context.ActiveJob.JobStateName,

                                    BasicSalary =
                        context.ActiveJob.BasicSalary
                    }
            };
        }

        [Obsolete("Use UserSettingsService.SaveAccountAsync.")]
        public Task<bool> UpdateUserProfileAsync(UserProfileDto updatedUser) => UpdateUserAsync(updatedUser);

        public async Task<bool> UpdateUserAsync(UserProfileDto profile)
        {
            if (_accounts == null) throw new InvalidOperationException("Use Manage Account to update accounts.");
            return (await _accounts.SaveLegacyProfileAsync(profile)).Success;
        }
    }
}
 
