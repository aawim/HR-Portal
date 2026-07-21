using Microsoft.EntityFrameworkCore;
using HRM.Models;
using HRM.Services.Interfaces;
using HRM.DTOs.UserContext;
using HRM.DTOs; // Adjust this if your models are in a different folder

namespace HRM.Services
{
    public class UserService : IUserService
    {

        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IUserAccessService _access;

        public UserService(
            IDbContextFactory<HrmTeContext> dbFactory, IUserAccessService access)
        {
            _dbFactory = dbFactory;
            _access = access;
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
                        x.BusinessEntity.FirstNameEnglish + " " +
                        x.BusinessEntity.LastNameEnglish
                })
                .FirstOrDefaultAsync();
        }


        public async Task<UserProfileDto?> GetMyProfileAsync()
        {
            var context = await _access.RequireContextAsync();

            if (context.UserId == 0)
                return null;

            return new UserProfileDto
            {
                UserId = context.UserId,

                IndividualID = context.IndividualId,

                Username = context.Username,

                FullName = context.FullName,

                OrganisationName =
                    context.Organisation?.OrganisationName,

                ActiveJob = context.ActiveJob == null
                    ? null
                    : new JobDto
                    {
                        JobID = context.ActiveJob.JobID,

                        OrganisationID = context.ActiveJob.OrganisationID,

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

        public async Task<bool> UpdateUserProfileAsync(UserProfileDto updatedUser)
        {
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync();

                var user = await db.Users
                    .FirstOrDefaultAsync(x =>
                        x.UserId == updatedUser.UserId);

                if (user == null)
                    return false;


                user.Username = updatedUser.Username;
                user.BusinessEntityID = updatedUser.IndividualID;


                var changes = await db.SaveChangesAsync();

                return changes > 0;
            }
            catch (Exception)
            {
                return false;
            }
        }


        public async Task<bool> UpdateUserAsync(UserProfileDto profile)
        {
            try
            {
                await using var db = await _dbFactory.CreateDbContextAsync();

                var user = await db.Users
                    .FirstOrDefaultAsync(x =>
                        x.UserId == profile.UserId);

                if (user == null)
                    return false;


                // Update only editable fields
                user.Username = profile.Username;


                var changes = await db.SaveChangesAsync();

                return changes > 0;
            }
            catch (Exception ex)
            {
                // TODO: log exception
                return false;
            }
        }

       
    }
}
 