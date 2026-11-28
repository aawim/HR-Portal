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

            var person = await db.Users.Where(x => x.UserId == userId).Select(x => (int?)x.BusinessEntityID).SingleOrDefaultAsync();
            if (!person.HasValue) return null;
            var actor = await _access.RequireContextAsync();
            if (person != actor.IndividualId && !actor.IsSuperAdministrator && !await _access.HasPermissionAsync("Employee.View") && !await _access.HasPermissionAsync("User.Manage") && !await _access.HasPermissionAsync("Admin"))
                throw new UnauthorizedAccessException("Staff user information requires permission.");
            await HRM.Helpers.StaffDataAccess.RequireIndividualAsync(db, _access, person.Value);

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

            var person = await db.Users.Where(x => x.Username == username).Select(x => (int?)x.BusinessEntityID).SingleOrDefaultAsync();
            if (!person.HasValue) return null;
            var actor = await _access.RequireContextAsync();
            if (person != actor.IndividualId && !actor.IsSuperAdministrator && !await _access.HasPermissionAsync("Employee.View") && !await _access.HasPermissionAsync("User.Manage") && !await _access.HasPermissionAsync("Admin"))
                throw new UnauthorizedAccessException("Staff user information requires permission.");
            await HRM.Helpers.StaffDataAccess.RequireIndividualAsync(db, _access, person.Value);

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

            var person = await db.Users.Where(x => x.BusinessEntityID == businessEntityId).Select(x => (int?)x.BusinessEntityID).SingleOrDefaultAsync();
            if (!person.HasValue) return null;
            var actor = await _access.RequireContextAsync();
            if (person != actor.IndividualId && !actor.IsSuperAdministrator && !await _access.HasPermissionAsync("Employee.View") && !await _access.HasPermissionAsync("User.Manage") && !await _access.HasPermissionAsync("Admin"))
                throw new UnauthorizedAccessException("Staff user information requires permission.");
            await HRM.Helpers.StaffDataAccess.RequireIndividualAsync(db, _access, person.Value);

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

            var userRow = await db.Users.AsNoTracking()
                .Where(u => u.UserId == context.UserId)
                .Select(u => new { u.Email, u.Mobile })
                .FirstOrDefaultAsync();

            string? fullNameDv = null, gender = null, nationality = null, nationalId = null, passportNo = null, currentPosition = null, sapNumber = null;
            DateTime? dob = null;
            var contacts = new List<UserProfileContactDto>();
            var recentLeaves = new List<UserProfileLeaveDto>();
            int activeDocsCount = 0, totalLeavesYtd = 0;

            if (context.IndividualId > 0)
            {
                var ind = await db.Individuals.AsNoTracking()
                    .Where(i => i.BusinessEntityId == context.IndividualId)
                    .Select(i => new
                    {
                        i.FirstNameDhivehi,
                        i.MiddleNameDhivehi,
                        i.LastNameDhivehi,
                        i.DateOfBirth,
                        i.GenderTypeId,
                        Nationality = i.Country != null ? (i.Country.NationalityEnglish ?? i.Country.NameEnglish) : null
                    })
                    .FirstOrDefaultAsync();

                if (ind is not null)
                {
                    var dvParts = new[] { ind.FirstNameDhivehi, ind.MiddleNameDhivehi, ind.LastNameDhivehi }
                        .Where(p => !string.IsNullOrWhiteSpace(p));
                    fullNameDv = string.Join(" ", dvParts);
                    if (string.IsNullOrWhiteSpace(fullNameDv)) fullNameDv = null;
                    dob = ind.DateOfBirth;
                    gender = ind.GenderTypeId == 1 ? "Male" : ind.GenderTypeId == 2 ? "Female" : null;
                    nationality = ind.Nationality;
                }

                nationalId = await db.Idcards.AsNoTracking()
                    .Where(c => c.BusinessEntityId == context.IndividualId)
                    .OrderByDescending(c => c.IdcardId)
                    .Select(c => c.IdcardNumber)
                    .FirstOrDefaultAsync();

                passportNo = await db.Passports.AsNoTracking()
                    .Where(p => p.BusinessEntityId == context.IndividualId)
                    .OrderByDescending(p => p.PassportId)
                    .Select(p => p.PassportNumber)
                    .FirstOrDefaultAsync();

                contacts = await db.BussinessEntityContactInformations.AsNoTracking()
                    .Where(c => c.BussinessEntityId == context.IndividualId && c.IsValid && !string.IsNullOrWhiteSpace(c.Value))
                    .Select(c => new UserProfileContactDto(c.ContactInformationType != null ? c.ContactInformationType.TypeName : "Contact", c.Value))
                    .Take(10)
                    .ToListAsync();

                activeDocsCount = await db.BusinessEntitiesDocuments.AsNoTracking()
                    .CountAsync(d => d.BusinessEntityId == context.IndividualId);
            }

            if (context.ActiveJob is not null)
            {
                var jobId = context.ActiveJob.JobId;
                sapNumber = await db.Jobs.AsNoTracking()
                    .Where(j => j.JobId == jobId)
                    .Select(j => j.Sapnumber)
                    .FirstOrDefaultAsync();

                currentPosition = await db.JobPositions.AsNoTracking()
                    .Where(jp => jp.JobId == jobId && (jp.ToDate == null || jp.ToDate > DateTime.UtcNow))
                    .OrderByDescending(jp => jp.FromDate)
                    .Select(jp => jp.Position != null ? jp.Position.Name : null)
                    .FirstOrDefaultAsync();

                var yearStart = new DateTime(DateTime.UtcNow.Year, 1, 1);
                totalLeavesYtd = await db.Leaves.AsNoTracking()
                    .Where(l => l.JobId == jobId && l.FromDate >= yearStart && l.LeaveStateId == 4)
                    .SumAsync(l => (int?)l.Duration) ?? 0;

                recentLeaves = await db.Leaves.AsNoTracking()
                    .Where(l => l.JobId == jobId)
                    .OrderByDescending(l => l.FromDate)
                    .Take(10)
                    .Select(l => new UserProfileLeaveDto(
                        l.LeaveId,
                        l.LeaveType != null ? l.LeaveType.Name : "Leave",
                        l.FromDate,
                        l.ToDate,
                        l.Duration,
                        l.LeaveState != null ? l.LeaveState.StateName : "Recorded"))
                    .ToListAsync();
            }

            var roles = await db.UserAssignedUserGroups.AsNoTracking()
                .Where(g => g.UserId == context.UserId && g.UserGroup != null)
                .Select(g => new UserRoleDto
                {
                    RoleId = g.UserGroupId,
                    RoleName = g.UserGroup.GroupName,
                    UserOrganisationID = g.UserGroup.UserOrganisation != null ? g.UserGroup.UserOrganisation.BusinessEntityID : (int?)null,
                    IsActive = true
                })
                .ToListAsync();

            return new UserProfileDto
            {
                UserId = context.UserId,

                IndividualID = context.IndividualId,

                Username = context.Username,

                FullName = context.FullName,
                Email = userRow?.Email ?? "",
                Mobile = userRow?.Mobile ?? "",
                FullNameDhivehi = fullNameDv,
                DateOfBirth = dob,
                Gender = gender,
                Nationality = nationality,
                NationalId = nationalId ?? context.Username,
                PassportNumber = passportNo,
                CurrentPositionName = currentPosition,
                Roles = roles,
                ActiveDocumentsCount = activeDocsCount,
                TotalLeavesTakenDays = totalLeavesYtd,
                Contacts = contacts,
                RecentLeaves = recentLeaves,
                ImageUrl = !string.IsNullOrWhiteSpace(context.ImageUrl) ? context.ImageUrl :
                    context.IndividualId > 0 ? IndividualImage.DataUrl(
                        db.Individuals.AsNoTracking().Where(i => i.BusinessEntityId == context.IndividualId).Select(i => i.ImageData).FirstOrDefault(),
                        db.Individuals.AsNoTracking().Where(i => i.BusinessEntityId == context.IndividualId).Select(i => i.ImageContentType).FirstOrDefault()) : null,
                JobHistory = await JobHistoryQuery.LoadAsync(db, context.IndividualId),

                OrganisationName =
                    context.Organisation?.OrganisationName,

                ActiveJob = context.ActiveJob == null
                    ? null
                    : new JobDto
                    {
                        JobID = context.ActiveJob.JobId,
                        SAPNumber = sapNumber,
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
 
