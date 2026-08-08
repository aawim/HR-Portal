using HRM.DTOs.Leave;
using HRM.Models;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.LeaveType;
using Microsoft.EntityFrameworkCore;
using static HRM.Components.Shared.SharedConfig;

namespace HRM.Services
{
    public class LeaveTypeService : ILeaveTypeService
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IUserAccessService _userAccessService;
        private readonly ILogger<LeaveTypeService> _logger;
        private readonly UserContext _userContext;

        public LeaveTypeService(
            IDbContextFactory<HrmTeContext> dbFactory,
            IUserAccessService userAccessService,
            ILogger<LeaveTypeService> logger,
            UserContext userContext)
        {
            _dbFactory = dbFactory;
            _userAccessService = userAccessService;
            _logger = logger;
            _userContext = userContext;
        }

        public async Task<List<LeaveTypeDto>>
       GetOrganisationLeaveTypesAsync(
           CancellationToken cancellationToken = default)
        {
            var organisationId =
                await GetCurrentOrganisationIdAsync();

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            return await db.LeaveTypes
                .AsNoTracking()
                .Where(x =>
                    x.OrganisationId == organisationId ||
                    x.IsGlobal)
                .OrderByDescending(x =>
                    x.IsSystemType)
                .ThenBy(x =>
                    x.Name)
                .Select(x =>
                    new LeaveTypeDto
                    {
                        LeaveTypeId =
                            x.LeaveTypeId,

                        Name =
                            x.Name ?? string.Empty,

                        NameDhivehi =
                            x.NameDhivehi,

                        Duration =
                            x.Duration,

                        IncludeHolidays =
                            x.IncludeHolidays,

                        IncludePay =
                            x.IncludePay,

                        IsPublic =
                            x.IsPublic,

                        IsGlobal =
                            x.IsGlobal,

                        IsLocationRequired =
                            x.IsLocationRequired,

                        ServiceDurationMonths =
                            x.ServiceDurationMonths,

                        OrganisationId =
                            x.OrganisationId,

                        RequestTypeId =
                            x.RequestTypeId,

                        IsSystemType =
                            x.IsSystemType,

                        IsRenewed =
                            x.IsRenewed,

                        IsStaffWideAvailable =
                            x.IsStaffWideAvailable,

                        PayPercentage =
                            x.PayPercentage,

                        StartInMonth =
                            x.StartInMonth,

                        RepeatedEveryInMonth =
                            x.RepeatedEveryInMonth
                    })
                .ToListAsync(
                    cancellationToken);
        }

        public async Task<LeaveTypeDto?> GetByIdAsync(
            int leaveTypeId,
            CancellationToken cancellationToken = default)
        {
            if (leaveTypeId <= 0)
            {
                return null;
            }

            var organisationId =
                await GetCurrentOrganisationIdAsync();

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            return await db.LeaveTypes
                .AsNoTracking()
                .Where(x =>
                    x.LeaveTypeId == leaveTypeId &&
                    (
                        x.OrganisationId == organisationId ||
                        x.IsGlobal
                    ))
                .Select(x =>
                    new LeaveTypeDto
                    {
                        LeaveTypeId =
                            x.LeaveTypeId,

                        Name =
                            x.Name ?? string.Empty,

                        NameDhivehi =
                            x.NameDhivehi,

                        Duration =
                            x.Duration,

                        IncludeHolidays =
                            x.IncludeHolidays,

                        IncludePay =
                            x.IncludePay,

                        IsPublic =
                            x.IsPublic,

                        IsGlobal =
                            x.IsGlobal,

                        IsLocationRequired =
                            x.IsLocationRequired,

                        ServiceDurationMonths =
                            x.ServiceDurationMonths,

                        OrganisationId =
                            x.OrganisationId,

                        RequestTypeId =
                            x.RequestTypeId,

                        IsSystemType =
                            x.IsSystemType,

                        IsRenewed =
                            x.IsRenewed,

                        IsStaffWideAvailable =
                            x.IsStaffWideAvailable,

                        PayPercentage =
                            x.PayPercentage,

                        StartInMonth =
                            x.StartInMonth,

                        RepeatedEveryInMonth =
                            x.RepeatedEveryInMonth
                    })
                .FirstOrDefaultAsync(
                    cancellationToken);
        }

        public async Task<LeaveTypeSaveResult> CreateAsync(
            LeaveTypeSaveRequest request,
            CancellationToken cancellationToken = default)
        {
            var validationMessage =
                Validate(request);

            if (validationMessage is not null)
            {
                return LeaveTypeSaveResult.Failure(
                    validationMessage);
            }

            try
            {
                var organisationId =
                    await GetCurrentOrganisationIdAsync();

                await using var db =
                    await _dbFactory.CreateDbContextAsync(
                        cancellationToken);

                var name =
                    request.Name.Trim();

                var alreadyExists =
                    await db.LeaveTypes
                        .AsNoTracking()
                        .AnyAsync(
                            x =>
                                x.OrganisationId ==
                                    organisationId &&
                                x.Name == name,
                            cancellationToken);

                if (alreadyExists)
                {
                    return LeaveTypeSaveResult.Failure(
                        "A leave type with this name already exists.");
                }

                var leaveType =
                    new LeaveType
                    {
                        Name =
                            name,

                        NameDhivehi =
                            Clean(request.NameDhivehi),

                        Duration =
                            request.Duration,

                        IncludeHolidays =
                            request.IncludeHolidays,

                        IncludePay = request.IncludePay,

                        IsPublic =
                            request.IsPublic,

                        IsGlobal =
                            false,

                        IsLocationRequired =
                            request.IsLocationRequired,

                        ServiceDurationMonths = (int)
                            request.ServiceDurationMonths,

                        OrganisationId =
                            organisationId,

                        RequestTypeId = request.RequestTypeId,

                        IsSystemType =
                            false,

                        IsRenewed =
                            request.IsRenewed,

                        IsStaffWideAvailable =
                            request.IsStaffWideAvailable,

                        PayPercentage =
                            request.IncludePay
                                ? request.PayPercentage
                                : null,

                        StartInMonth = request.IsRenewed ? request.StartInMonth : 0,
                               

                        RepeatedEveryInMonth = request.IsRenewed ? request.RepeatedEveryInMonth : 0,

                        /*
                         * Set your OperationLogId here when the
                         * operation-log implementation is connected.
                         */
                    };

                db.LeaveTypes.Add(
                    leaveType);

                await db.SaveChangesAsync(
                    cancellationToken);

                return LeaveTypeSaveResult.Successful(
                    leaveType.LeaveTypeId,
                    "Leave type created successfully.");
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unable to create leave type.");

                return LeaveTypeSaveResult.Failure(
                    "The leave type could not be created.");
            }
        }

        public async Task<LeaveTypeSaveResult> UpdateAsync(
            LeaveTypeSaveRequest request,
            CancellationToken cancellationToken = default)
        {


            var context =
    await _userAccessService.GetContextAsync();

            var isSuperAdmin =
                context?.IsSuperAdministrator == true;



            //if (!isSuperAdmin &&
            //    (request.IsSystemType == true || request.IsGlobal == true ))
            //{
            //    return LeaveTypeSaveResult.Failure(
            //        "You do not have permission to edit this leave type.");
            //}




            if (request.LeaveTypeId == 0|| request.LeaveTypeId <= 0)
            {
                return LeaveTypeSaveResult.Failure(
                    "A valid leave type ID is required.");
            }

            var validationMessage =
                Validate(request);

            if (validationMessage is not null)
            {
                return LeaveTypeSaveResult.Failure(
                    validationMessage);
            }

            try
            {
                var organisationId =
                    await GetCurrentOrganisationIdAsync();

                await using var db =
                    await _dbFactory.CreateDbContextAsync(
                        cancellationToken);



                //var userOrg = await db.UserOrganisations
                //      .Where(x => x.BusinessEntityID == organisationId)
                //      .FirstOrDefaultAsync();

                //// 2. Safely extract the ID. Falls back to 0 (or null) if no record exists.
                //int userOrganisationId = userOrg?.UserOrganisationID ?? 0;


                var leaveType =
                    await db.LeaveTypes
                        .FirstOrDefaultAsync(
                            x =>
                                x.LeaveTypeId ==
                                    request.LeaveTypeId &&
                                x.OrganisationId ==
                                    organisationId,
                            cancellationToken);

                if (leaveType is null)
                {
                    return LeaveTypeSaveResult.Failure(
                        "The leave type could not be found.");
                }

                /*
                 * Organisation users should not modify
                 * system/global leave types.
                 */
                if (leaveType.IsSystemType ||
                    leaveType.IsGlobal)
                {
                    return LeaveTypeSaveResult.Failure(
                        "System or global leave types cannot be edited here.");
                }

                var name =
                    request.Name.Trim();

                var duplicateName =
                    await db.LeaveTypes
                        .AsNoTracking()
                        .AnyAsync(
                            x =>
                                x.LeaveTypeId !=
                                    leaveType.LeaveTypeId &&
                                x.OrganisationId ==
                                    organisationId &&
                                x.Name == name,
                            cancellationToken);

                if (duplicateName)
                {
                    return LeaveTypeSaveResult.Failure(
                        "Another leave type with this name already exists.");
                }

                leaveType.Name =
                    name;

                leaveType.NameDhivehi =
                    Clean(request.NameDhivehi);

                leaveType.Duration =
                    request.Duration;

                leaveType.IncludeHolidays =
                    request.IncludeHolidays;

                leaveType.IncludePay =
                    request.IncludePay;

                leaveType.IsPublic =
                    request.IsPublic;

                leaveType.IsLocationRequired =
                    request.IsLocationRequired;

                leaveType.ServiceDurationMonths =
                    request.ServiceDurationMonths;

                leaveType.RequestTypeId =
                    request.RequestTypeId;

                leaveType.IsRenewed =
                    request.IsRenewed;

                leaveType.IsStaffWideAvailable =
                    request.IsStaffWideAvailable;

                leaveType.PayPercentage =
                    request.IncludePay
                        ? request.PayPercentage
                        : 0;

                leaveType.StartInMonth =
                    request.IsRenewed
                        ? request.StartInMonth : 0;

                leaveType.RepeatedEveryInMonth =
                    request.IsRenewed
                        ? request.RepeatedEveryInMonth
                        : 0;

                await db.SaveChangesAsync(
                    cancellationToken);

                return LeaveTypeSaveResult.Successful(
                    leaveType.LeaveTypeId,
                    "Leave type updated successfully.");
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "Unable to update leave type {LeaveTypeId}.",
                    request.LeaveTypeId);

                return LeaveTypeSaveResult.Failure(
                    "The leave type could not be updated.");
            }
        }

        //private async Task<int> GetCurrentOrganisationIdAsync()
        //{
        //    var context =
        //        await _userAccessService.GetContextAsync();

        //    if (context?.ActiveJob is null ||
        //        context.ActiveJob.OrganisationId <= 0)
        //    {
        //        throw new InvalidOperationException(
        //            "The current organisation could not be resolved.");
        //    }

        //    return context.ActiveJob.OrganisationId;
        //}

        private async Task<int> GetCurrentOrganisationIdAsync(
           CancellationToken cancellationToken = default)
        {
            var context =
                await _userAccessService.GetContextAsync();

            var session =
                await _userContext.GetSessionAsync();

            if (context is null)
            {
                throw new InvalidOperationException(
                    "The current user context could not be resolved.");
            }

            if (session is null ||
                session.OrganisationId <= 0)
            {
                throw new InvalidOperationException(
                    "The current portal organisation could not be resolved.");
            }

            /*
             * session.OrganisationId is actually UserOrganisationId.
             */
            var selectedUserOrganisationId =
                session.OrganisationId;

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            /*
             * Convert UserOrganisationId
             *      1
             *
             * into the real organisation BusinessEntityId
             *      3
             */
            var selectedOrganisationBusinessEntityId =
                await db.UserOrganisations
                    .AsNoTracking()
                    .Where(x =>
                        x.UserOrganisationID ==
                        selectedUserOrganisationId)
                    .Select(x =>
                        x.BusinessEntityID)
                    .FirstOrDefaultAsync(
                        cancellationToken);

            if (selectedOrganisationBusinessEntityId <= 0)
            {
                throw new InvalidOperationException(
                    "The selected organisation could not be resolved.");
            }

            /*
             * Super Administrator may work with the organisation
             * selected through the portal switcher.
             */
            if (context.IsSuperAdministrator)
            {
                return selectedOrganisationBusinessEntityId;
            }

            if (context.ActiveJob is null ||
                context.ActiveJob.OrganisationId <= 0)
            {
                throw new UnauthorizedAccessException(
                    "The current user does not have an active organisation.");
            }

            /*
             * Compare organisation BusinessEntityId to
             * organisation BusinessEntityId.
             *
             * Example:
             *
             * ActiveJob.OrganisationId = 3
             * selectedOrganisationBusinessEntityId = 3
             */
            if (context.ActiveJob.OrganisationId !=
                selectedOrganisationBusinessEntityId)
            {
                throw new UnauthorizedAccessException(
                    "You do not have access to the selected organisation.");
            }

            return selectedOrganisationBusinessEntityId;
        }

        private static string? Validate(
            LeaveTypeSaveRequest request)
        {
            if (string.IsNullOrWhiteSpace(
                request.Name))
            {
                return "Leave type name is required.";
            }

            if (request.Name.Trim().Length > 250)
            {
                return "Leave type name is too long.";
            }

            if (request.Duration < 0 )
            {
                return "Duration cannot be negative.";
            }

            if (request.ServiceDurationMonths < 0)
            {
                return "Service duration cannot be negative.";
            }

            if (request.IncludePay  &&
                request.PayPercentage < 0 &&
                (
                    request.PayPercentage < 0 ||
                    request.PayPercentage > 100
                ))
            {
                return "Pay percentage must be between 0 and 100.";
            }

            if (request.IsRenewed &&
                request.RepeatedEveryInMonth < 0)
            {
                return "Renewal interval must be greater than zero.";
            }

            return null;
        }

        private static string? Clean(
            string? value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? null
                : value.Trim();
        }

    }
}
