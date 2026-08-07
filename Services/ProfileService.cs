using HRM.Components.Shared;
using HRM.DTOs.JobPosition;
using HRM.DTOs.Profile;
using HRM.DTOs.Profile.ProfileAccess;
using HRM.DTOs.Team;
using HRM.DTOs.UserContext;
using HRM.Enum;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.JobPosition;
using HRM.Services.Interfaces.Profile;
using HRM.Services.JobPosition;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services
{
    public class ProfileService : IProfileService
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IUserAccessService _userAccessService;
        private readonly IJobService _jobService;
        private readonly ILogger<ProfileService> _logger;
        private readonly IJobPosition _jobPositionService;

        public ProfileService(
        IDbContextFactory<HrmTeContext> dbFactory,
        IUserAccessService userAccessService, IJobService jobService, ILogger<ProfileService> logger, IJobPosition jobPositionService)
        {
            _dbFactory = dbFactory;
            _userAccessService = userAccessService;
            _jobService = jobService;
            _logger = logger;
            _jobPositionService = jobPositionService;
        }

        public async Task<ProfileOverviewDto?> GetProfileAsync(int individualId,CancellationToken cancellationToken = default)
        {
            if (individualId <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(individualId),
                    "Individual ID must be greater than zero.");
            }

            await _userAccessService.RequireContextAsync();
        
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

            try
            {
                var individual =
                    await LoadIndividualAsync(
                        db,
                        individualId,
                        cancellationToken);

                if (individual is null)
                {
                    return null;
                }

                var identityCardNumber =
                    await LoadIdentityCardNumberAsync(
                        db,
                        individualId,
                        cancellationToken);

                /*
                 * JobService resolves the correct approved,
                 * non-terminated job for this individual.
                 */
                var activeJob =
                    await _jobService.GetActiveJobAsync(
                        individualId,
                        cancellationToken);

                ProfileAccessDto access = new();

                if (activeJob is not null &&
                    activeJob.IsActive)
                {
                    access =
                        await LoadProfileAccessAsync(
                            db,
                            individualId,
                            activeJob.OrganisationId,
                            cancellationToken);
                }



                List<ProfilePositionDto> activePositions = [];

                List<JobPositionHistoryDto> positionHistory = [];



                if (activeJob is not null &&
                    activeJob.IsActive)
                {
                    activePositions =
                        await LoadActivePositionsAsync(
                            db,
                            activeJob.JobId,
                            cancellationToken);

                    ApplyPrimaryPosition(
                        activeJob,
                        activePositions);

                    positionHistory =
                      await _jobPositionService
                          .GetPositionHistoryByJobAsync(activeJob.JobId,cancellationToken);

                    activeJob.Teams =
                        await LoadActiveTeamsAsync(
                            db,
                            individualId,
                            activeJob.OrganisationId,
                            cancellationToken);

                    SetDepartmentName(
                        activeJob);

                    SetServiceDuration(
                        activeJob);
                }


                var supervisingTeams =
                    await
                    (
                        from teamStaff in db.TeamStaffs.AsNoTracking()

                        join team in db.Teams.AsNoTracking()
                            on teamStaff.TeamId equals team.TeamId

                        where
                            teamStaff.StaffId == individualId &&
                            teamStaff.IsSuperVisor &&
                            teamStaff.IsValid == true &&
                            team.IsValid == true

                        orderby team.Name

                        select new ProfileSupervisingGroupDto
                        {
                            TeamId = team.TeamId,

                            TeamName =
                                team.Name ?? string.Empty,

                            OrganisationId =
                                team.OrganisationId,

                            StartDate =
                                teamStaff.StartDate,

                            EndDate =
                                teamStaff.EndDate,

                            IsActive =
                                teamStaff.EndDate == null ||
                                teamStaff.EndDate >= DateTime.Today
                        }
                    )
                    .ToListAsync(cancellationToken);



                var contacts =
                    await LoadContactsAsync(
                        db,
                        individualId,
                        cancellationToken);

                var hasEmploymentHistory =
                    await HasEmploymentHistoryAsync(
                        db,
                        individualId,
                        cancellationToken);

                var profileType =
                    ResolveProfileType(
                        activeJob,
                        hasEmploymentHistory);


                var StaffNo = await LoadStaffNumberAsync(db,
                        individualId,
                        cancellationToken);

                return new ProfileOverviewDto
                {
                    //DateOfBirth = individual.DateOfBirth.Value.ToString("dd MMM yyyy") ?? "Not Available",

                    DateOfBirth = individual.DateOfBirth ?? DateTime.MinValue,

                    GenderId = individual.GenderId,

                    IndividualId =
                        individual.IndividualId,

                    BusinessEntityId =
                        individual.IndividualId,

                    FullName =
                        BuildFullName(
                            individual.FirstNameEnglish,
                            individual.MiddleNameEnglish,
                            individual.LastNameEnglish),

                    FullNameDhivehi =
                        BuildFullName(
                            individual.FirstNameDhivehi,
                            individual.MiddleNameDhivehi,
                            individual.LastNameDhivehi),

                    IdentityCardNumber =
                        identityCardNumber,

                    ProfileType =
                        profileType,

                    ActiveJob =
                        activeJob?.IsActive == true
                            ? activeJob
                            : null,

                    ActivePositions =
                        activePositions,

                    Contacts =
                        contacts,
    
                    StaffNo = StaffNo?.StaffNo ?? "N/A",


                    JobHistory =[],
                    /*
                     * Populate these as their loaders are implemented.
                     */
                    Addresses = [],

                    LeaveTypes = [],
                    PositionHistory = positionHistory,

                    Education = [],

                    Documents = [],

                    SupervisingTeams = supervisingTeams,

                    Access = access
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    """
                Unable to load profile.
                IndividualId: {IndividualId}
                """,
                    individualId);

                throw;
            }
        }
        private static async Task<ProfileAccessDto>
     LoadProfileAccessAsync(
         HrmTeContext db,
         int individualId,
         int organisationId,
         CancellationToken cancellationToken)
        {
            var today =
                DateTime.Today;

            var teams =
                await
                (
                    from teamStaff in db.TeamStaffs.AsNoTracking()

                    join team in db.Teams.AsNoTracking()
                        on teamStaff.TeamId equals team.TeamId

                    where
                        teamStaff.StaffId == individualId &&
                        teamStaff.IsValid == true &&
                        team.IsValid == true &&
                        team.OrganisationId == organisationId &&
                        (
                            teamStaff.StartDate == null ||
                            teamStaff.StartDate <= today
                        ) &&
                        (
                            teamStaff.EndDate == null ||
                            teamStaff.EndDate >= today
                        )

                    orderby
                        teamStaff.IsSuperVisor descending,
                        team.Name

                    select new ProfileTeamDto
                    {
                        TeamId =
                            team.TeamId,

                        TeamName =
                            team.Name ?? string.Empty,

                        OrganisationId =
                            team.OrganisationId,

                        IsSupervisor =
                            teamStaff.IsSuperVisor,

                        StartDate =
                            teamStaff.StartDate,

                        EndDate =
                            teamStaff.EndDate,

                        IsActive =
                            true
                    }
                )
                .ToListAsync(cancellationToken);

            var supervisingGroups =
                teams
                    .Where(team =>
                        team.IsSupervisor)
                    .Select(team =>
                        new ProfileSupervisingGroupDto
                        {
                            TeamId =
                                team.TeamId,

                            TeamName =
                                team.TeamName,

                            OrganisationId =
                                team.OrganisationId,

                            StartDate =
                                team.StartDate,

                            EndDate =
                                team.EndDate,

                            IsActive =
                                team.IsActive
                        })
                    .ToList();

            var organisationRoles =
                await LoadOrganisationRolesAsync(
                    db,
                    individualId,
                    organisationId,
                    cancellationToken);

            return new ProfileAccessDto
            {
                Teams =
                    teams,

                SupervisingGroups =
                    supervisingGroups,

                Roles =
                    organisationRoles,

                Groups =
                    [],

                Permissions =
                    []
            };
        }

        private static async Task<List<ProfileRoleDto>>
    LoadOrganisationRolesAsync(
        HrmTeContext db,
        int individualId,
        int organisationId,
        CancellationToken cancellationToken)
        {
            if (individualId <= 0 ||
                organisationId <= 0)
            {
                return [];
            }

            return await
            (
                from user in db.Users.AsNoTracking()

                join userRole in db.UserRoles.AsNoTracking()
                    on user.UserId equals userRole.UserId

                join role in db.Roles.AsNoTracking()
                    on userRole.RoleId equals role.RoleID

                join userOrganisation in db.UserOrganisations.AsNoTracking()
                    on userRole.UserOrganisationId equals
                       userOrganisation.UserOrganisationID

                where
                    user.BusinessEntityID == individualId &&

                    userRole.IsActive == true



                   //&& userRole.UserOrganisationId != null 

                   && userOrganisation.BusinessEntityID == organisationId

                orderby
                    role.Name

                select new ProfileRoleDto
                {
                    RoleId =
                        role.RoleID,

                    RoleKey =
                        role.RoleKey ?? string.Empty,

                    RoleName =
                        role.Name ?? string.Empty,

                    OrganisationId =
                        userOrganisation.UserOrganisationID,

                    IsSystemRole = (bool)role.IsSystemRole,

                    IsActive =
                        userRole.IsActive,

                    Source =
                        "Organisation"
                }
            )
            .Distinct()
            .ToListAsync(cancellationToken);
        }





        private static async Task<IndividualProfileModel?>LoadIndividualAsync(
              HrmTeContext db,
              int individualId,
              CancellationToken cancellationToken)
        {
            return await db.Individuals
                .AsNoTracking()
                .Where(individual =>
                    individual.BusinessEntityId ==
                    individualId)
                .Select(individual =>
                    new IndividualProfileModel
                    {
                        IndividualId =
                            individual.BusinessEntityId,

                        FirstNameEnglish =
                            individual.FirstNameEnglish,

                        MiddleNameEnglish =
                            individual.MiddleNameEnglish,

                        LastNameEnglish =
                            individual.LastNameEnglish,

                        FirstNameDhivehi =
                            individual.FirstNameDhivehi,

                        MiddleNameDhivehi =
                            individual.MiddleNameDhivehi,

                        LastNameDhivehi =
                            individual.LastNameDhivehi,

                        DateOfBirth = individual.DateOfBirth,

                        GenderId = individual.GenderTypeId,
                        
                    })
                .SingleOrDefaultAsync(
                    cancellationToken);
        }

        private static async Task<StaffNumber?> LoadStaffNumberAsync(
            HrmTeContext db,
            int individualId,
            CancellationToken cancellationToken)
        {
            return await db.Staffs
                .AsNoTracking()
                .Where(staff =>
                    staff.IndividualId ==
                    individualId)
                .Select(staff =>
                    new StaffNumber
                    {
                        IndividualId = staff.IndividualId,
                        StaffNo = staff.EmployeeNumber

                    })
                .SingleOrDefaultAsync(
                    cancellationToken);
        }

        private static async Task<List<ProfilePositionDto>>
    LoadActivePositionsAsync(
        HrmTeContext db,
        int jobId,
        CancellationToken cancellationToken)
        {
            if (jobId <= 0)
            {
                return [];
            }

            var today = DateTime.Today;
            var approvedPositionStateId = SharedConfig.JobPositionStates.APPROVED;

            var positions =
                await
                (
                    from jobPosition in db.JobPositions.AsNoTracking()

                    join position in db.Positions.AsNoTracking()
                        on jobPosition.PositionId equals
                        position.PositionId

                    join job in db.Jobs.AsNoTracking()
                        on jobPosition.JobId equals
                        job.JobId

                    join structure in db.OrganisationStructures.AsNoTracking()
                        on job.OrganisationStructureId equals
                        structure.OrganisationStructureId
                        into structureGroup

                    from structure in structureGroup.DefaultIfEmpty()

                    where
                        job.JobId == jobId &&

                        job.JobStateId ==
                            SharedConfig.JobStates.APPROVED &&

                        job.TerminatedDate == null &&

                        jobPosition.JobPositionStateId ==
                            approvedPositionStateId &&

                        jobPosition.FromDate <= today &&

                        (
                            jobPosition.ToDate == null ||
                            jobPosition.ToDate >= today
                        )

                    orderby
                        jobPosition.FromDate descending,
                        jobPosition.JobPositionId descending

                    select new ProfilePositionDto
                    {
                        JobPositionId =
                            jobPosition.JobPositionId,

                        JobId =
                            jobPosition.JobId,

                        PositionId =
                            jobPosition.PositionId,

                        PositionName =
                            position.Name ?? string.Empty,

                        JobPositionStateId =
                            jobPosition.JobPositionStateId,

                        JobPositionStateName =
                            jobPosition.JobPositionState != null
                                ? jobPosition.JobPositionState.StateName
                                    ?? string.Empty
                                : string.Empty,

                        FromDate =
                            jobPosition.FromDate,

                        ToDate =
                            jobPosition.ToDate,

                        IsCurrent =
                            true,

                        IsActive =
                            true,

                        OrganisationStructureId =
                            job.OrganisationStructureId,

                        OrganisationStructureName =
                            structure != null
                                ? structure.Name ?? string.Empty
                                : string.Empty
                    }
                )
                .ToListAsync(cancellationToken);

            foreach (var position in positions)
            {
                position.EffectivePeriodText =
                    position.ToDate.HasValue
                        ? $"{position.FromDate:dd MMM yyyy} – " +
                          $"{position.ToDate.Value:dd MMM yyyy}"
                        : $"{position.FromDate:dd MMM yyyy} – Present";
            }

            return positions;
        }
        private static void ApplyPrimaryPosition(
         ActiveJobDto activeJob,
         IReadOnlyList<ProfilePositionDto> positions)
        {
            var currentPosition =
                positions
                    .OrderByDescending(position =>
                        position.FromDate)
                    .ThenByDescending(position =>
                        position.JobPositionId)
                    .FirstOrDefault();

            if (currentPosition is null)
            {
                activeJob.PositionId = null;
                activeJob.PositionName = string.Empty;
                return;
            }

            activeJob.PositionId =
                currentPosition.PositionId;

            activeJob.PositionName =
                currentPosition.PositionName;
        }



        private static async Task<List<ActiveJobTeamDto>>LoadActiveTeamsAsync(
          HrmTeContext db,
          int individualId,
          int organisationId,
          CancellationToken cancellationToken)
        {
            if (individualId <= 0 ||
                organisationId <= 0)
            {
                return [];
            }

            var today =
                DateTime.Today;

            return await
            (
                from teamStaff in
                    db.TeamStaffs.AsNoTracking()

                join team in
                    db.Teams.AsNoTracking()
                    on teamStaff.TeamId equals
                    team.TeamId

                where
                    teamStaff.StaffId ==
                        individualId &&

                    teamStaff.IsValid == true &&

                    (
                        teamStaff.StartDate == null ||
                        teamStaff.StartDate <= today
                    ) &&

                    (
                        teamStaff.EndDate == null ||
                        teamStaff.EndDate >= today
                    ) &&

                    team.IsValid == true &&

                    (
                        team.StartDate == null ||
                        team.StartDate <= today
                    ) &&

                    (
                        team.EndDate == null ||
                        team.EndDate >= today
                    ) &&

                    team.OrganisationId ==
                        organisationId

                orderby
                    teamStaff.IsSuperVisor descending,
                    teamStaff.StartDate descending,
                    team.Name

                select new ActiveJobTeamDto
                {
                    TeamId =
                        team.TeamId,

                    Name =
                        team.Name ??
                        string.Empty,

                    NameDhivehi =
                        team.NameDhivehi,

                    IsSupervisor =
                        teamStaff.IsSuperVisor,

                    StartDate =
                        teamStaff.StartDate,

                    EndDate =
                        teamStaff.EndDate
                }
            )
            .ToListAsync(
                cancellationToken);
        }

        private static void SetDepartmentName(ActiveJobDto activeJob)
        {
            /*
             * Prefer the team where the employee is a supervisor.
             * Otherwise use the most recently joined active team.
             *
             * If no team exists, use the formal organisation structure.
             */
            activeJob.DepartmentName =
                activeJob.Teams
                    .OrderByDescending(team =>
                        team.IsSupervisor)
                    .ThenByDescending(team =>
                        team.StartDate)
                    .Select(team =>
                        team.Name)
                    .FirstOrDefault()
                ?? activeJob.OrganisationStructureName
                ?? string.Empty;
        }

        private static async Task<List<ProfileContactDto>>
            LoadContactsAsync(
                HrmTeContext db,
                int individualId,
                CancellationToken cancellationToken)
        {
            return await
            (
                from contact in
                    db.BussinessEntityContactInformations
                        .AsNoTracking()

                join contactType in
                    db.ContactInformationTypes
                        .AsNoTracking()
                    on contact.ContactInformationTypeId equals
                    contactType.ContactInformationTypeId

                where
                    contact.BussinessEntityId ==
                        individualId &&

                    contact.IsValid

                orderby
                    contact.ContactInformationTypeId,
                    contact.BussinessEntityContactInformationId

                select new ProfileContactDto
                {
                    ContactId =
                        contact
                            .BussinessEntityContactInformationId,

                    BusinessEntityId =
                        contact.BussinessEntityId,

                    ContactInformationTypeId =
                        contact.ContactInformationTypeId,

                    ContactTypeName =
                        contactType.TypeName ??
                        string.Empty,

                    Value =
                        contact.Value ??
                        string.Empty,

                    IsValid =
                        contact.IsValid
                }
            )
            .ToListAsync(
                cancellationToken);
        }

        private static async Task<bool>HasEmploymentHistoryAsync(
                HrmTeContext db,
                int individualId,
                CancellationToken cancellationToken)
        {
            return await db.Jobs
                .AsNoTracking()
                .AnyAsync(
                    job =>
                        job.IndividualID ==
                        individualId,
                    cancellationToken);
        }



        private static ProfileType ResolveProfileType(ActiveJobDto? activeJob,bool hasEmploymentHistory)
        {
            if (activeJob?.IsActive == true)
            {
                return ProfileType.Staff;
            }

            return hasEmploymentHistory
                ? ProfileType.FormerStaff
                : ProfileType.Individual;
        }

        private static void SetServiceDuration(ActiveJobDto job)
        {
            var start =
                job.JoinedDate.Date;

            var end =
                (job.TerminatedDate ??
                 DateTime.Today).Date;

            if (start > end)
            {
                job.ServiceYears = 0;
                job.ServiceMonths = 0;
                job.ServiceDurationText =
                    "Not available";

                return;
            }

            var years =
                end.Year - start.Year;

            var months =
                end.Month - start.Month;

            if (end.Day < start.Day)
            {
                months--;
            }

            if (months < 0)
            {
                years--;
                months += 12;
            }

            years =
                Math.Max(0, years);

            months =
                Math.Max(0, months);

            job.ServiceYears =
                years;

            job.ServiceMonths =
                months;

            if (years == 0 &&
                months == 0)
            {
                job.ServiceDurationText =
                    "Less than one month";

                return;
            }

            var parts =
                new List<string>();

            if (years > 0)
            {
                parts.Add(
                    years == 1
                        ? "1 year"
                        : $"{years} years");
            }

            if (months > 0)
            {
                parts.Add(
                    months == 1
                        ? "1 month"
                        : $"{months} months");
            }

            job.ServiceDurationText =
                string.Join(
                    " ",
                    parts);
        }

        private static string BuildFullName(
            string? firstName,
            string? middleName,
            string? lastName)
        {
            return string.Join(
                " ",
                new[]
                {
                firstName,
                middleName,
                lastName
                }
                .Where(value =>
                    !string.IsNullOrWhiteSpace(value))
                .Select(value =>
                    value!.Trim()));
        }


        private static async Task<string?> LoadIdentityCardNumberAsync(
            HrmTeContext db,
            int individualId,
            CancellationToken cancellationToken)
                {
                    if (individualId <= 0)
                    {
                        return null;
                    }

                    return await db.Idcards
                        .AsNoTracking()
                        .Where(card =>
                            card.BusinessEntityId == individualId )
                        .OrderByDescending(card =>
                            card.IdcardId)
                        .Select(card =>
                            card.IdcardNumber)
                        .FirstOrDefaultAsync(cancellationToken);
                }


        private sealed class IndividualProfileModel
        {
            public int IndividualId { get; init; }

            public string? FirstNameEnglish { get; init; }

            public string? MiddleNameEnglish { get; init; }

            public string? LastNameEnglish { get; init; }

            public string? FirstNameDhivehi { get; init; }

            public string? MiddleNameDhivehi { get; init; }

            public string? LastNameDhivehi { get; init; }

            public DateTime? DateOfBirth { get; init; }

            public int GenderId { get; set; }

        }




        private sealed class StaffNumber
        {
            public int IndividualId { get; init; }
            public string? StaffNo { get; init; }

        }

        //private static void SetServiceDuration(ActiveJobDto job)
        //{
        //    var start =
        //        job.JoinedDate.Date;

        //    var end =
        //        (job.TerminatedDate ?? DateTime.Today).Date;

        //    if (start > end)
        //    {
        //        job.ServiceYears = 0;
        //        job.ServiceMonths = 0;
        //        job.ServiceDurationText =
        //            "Not available";

        //        return;
        //    }

        //    var years =
        //        end.Year - start.Year;

        //    var months =
        //        end.Month - start.Month;

        //    if (end.Day < start.Day)
        //    {
        //        months--;
        //    }

        //    if (months < 0)
        //    {
        //        years--;
        //        months += 12;
        //    }

        //    years =
        //        Math.Max(0, years);

        //    months =
        //        Math.Max(0, months);

        //    job.ServiceYears =
        //        years;

        //    job.ServiceMonths =
        //        months;

        //    if (years == 0 &&
        //        months == 0)
        //    {
        //        job.ServiceDurationText =
        //            "Less than one month";

        //        return;
        //    }

        //    var parts =
        //        new List<string>();

        //    if (years > 0)
        //    {
        //        parts.Add(
        //            years == 1
        //                ? "1 year"
        //                : $"{years} years");
        //    }

        //    if (months > 0)
        //    {
        //        parts.Add(
        //            months == 1
        //                ? "1 month"
        //                : $"{months} months");
        //    }

        //    job.ServiceDurationText =
        //        string.Join(" ", parts);
        //}
        //private static string BuildFullName(
        //    string? firstName,
        //    string? middleName,
        //    string? lastName)
        //{
        //    return string.Join(
        //        " ",
        //        new[]
        //        {
        //        firstName,
        //        middleName,
        //        lastName
        //        }
        //        .Where(value => !string.IsNullOrWhiteSpace(value))
        //        .Select(value => value!.Trim()));
        //}


    }
}
