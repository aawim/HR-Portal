using HRM.DTOs.Leave;
using HRM.Models;
using HRM.Models.LeaveTypes;
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

                await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);

                var name = request.Name.Trim();

                var alreadyExists = await db.LeaveTypes 
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

        //public async Task<LeaveTypeSaveResult> UpdateAsync(
        //    LeaveTypeSaveRequest request,
        //    CancellationToken cancellationToken = default)
        //{


        //    var context =
        //    await _userAccessService.GetContextAsync();

        //    var isSuperAdmin =
        //        context?.IsSuperAdministrator == true;



        //    //if (!isSuperAdmin &&
        //    //    (request.IsSystemType == true || request.IsGlobal == true ))
        //    //{
        //    //    return LeaveTypeSaveResult.Failure(
        //    //        "You do not have permission to edit this leave type.");
        //    //}




        //    if (request.LeaveTypeId == 0|| request.LeaveTypeId <= 0)
        //    {
        //        return LeaveTypeSaveResult.Failure(
        //            "A valid leave type ID is required.");
        //    }

        //    var validationMessage =
        //        Validate(request);

        //    if (validationMessage is not null)
        //    {
        //        return LeaveTypeSaveResult.Failure(
        //            validationMessage);
        //    }

        //    try
        //    {
        //        var organisationId =
        //            await GetCurrentOrganisationIdAsync();

        //        await using var db =
        //            await _dbFactory.CreateDbContextAsync(
        //                cancellationToken);



        //        //var userOrg = await db.UserOrganisations
        //        //      .Where(x => x.BusinessEntityID == organisationId)
        //        //      .FirstOrDefaultAsync();

        //        //// 2. Safely extract the ID. Falls back to 0 (or null) if no record exists.
        //        //int userOrganisationId = userOrg?.UserOrganisationID ?? 0;


        //        var leaveType =
        //            await db.LeaveTypes
        //                .FirstOrDefaultAsync(
        //                    x =>
        //                        x.LeaveTypeId ==
        //                            request.LeaveTypeId &&
        //                        x.OrganisationId ==
        //                            organisationId,
        //                    cancellationToken);

        //        if (leaveType is null)
        //        {
        //            return LeaveTypeSaveResult.Failure(
        //                "The leave type could not be found.");
        //        }

        //        /*
        //         * Organisation users should not modify
        //         * system/global leave types.
        //         */
        //        if (leaveType.IsSystemType ||
        //            leaveType.IsGlobal)
        //        {
        //            return LeaveTypeSaveResult.Failure(
        //                "System or global leave types cannot be edited here.");
        //        }

        //        var name =
        //            request.Name.Trim();

        //        var duplicateName =
        //            await db.LeaveTypes
        //                .AsNoTracking()
        //                .AnyAsync(
        //                    x =>
        //                        x.LeaveTypeId !=
        //                            leaveType.LeaveTypeId &&
        //                        x.OrganisationId ==
        //                            organisationId &&
        //                        x.Name == name,
        //                    cancellationToken);

        //        if (duplicateName)
        //        {
        //            return LeaveTypeSaveResult.Failure(
        //                "Another leave type with this name already exists.");
        //        }

        //        leaveType.Name =
        //            name;

        //        leaveType.NameDhivehi =
        //            Clean(request.NameDhivehi);

        //        leaveType.Duration =
        //            request.Duration;

        //        leaveType.IncludeHolidays =
        //            request.IncludeHolidays;

        //        leaveType.IncludePay =
        //            request.IncludePay;

        //        leaveType.IsPublic =
        //            request.IsPublic;

        //        leaveType.IsLocationRequired =
        //            request.IsLocationRequired;

        //        leaveType.ServiceDurationMonths =
        //            request.ServiceDurationMonths;

        //        leaveType.RequestTypeId =
        //            request.RequestTypeId;

        //        leaveType.IsRenewed =
        //            request.IsRenewed;

        //        leaveType.IsStaffWideAvailable =
        //            request.IsStaffWideAvailable;

        //        leaveType.PayPercentage =
        //            request.IncludePay
        //                ? request.PayPercentage
        //                : 0;

        //        leaveType.StartInMonth =
        //            request.IsRenewed
        //                ? request.StartInMonth : 0;

        //        leaveType.RepeatedEveryInMonth =
        //            request.IsRenewed
        //                ? request.RepeatedEveryInMonth
        //                : 0;

        //        await db.SaveChangesAsync(
        //            cancellationToken);

        //        return LeaveTypeSaveResult.Successful(
        //            leaveType.LeaveTypeId,
        //            "Leave type updated successfully.");
        //    }
        //    catch (Exception exception)
        //    {
        //        _logger.LogError(
        //            exception,
        //            "Unable to update leave type {LeaveTypeId}.",
        //            request.LeaveTypeId);

        //        return LeaveTypeSaveResult.Failure(
        //            "The leave type could not be updated.");
        //    }
        //}

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


        public async Task<LeaveTypeSaveResult> UpdateAsync(
            LeaveTypeSaveRequest request,
            CancellationToken cancellationToken = default)
        {
            var context =
                await _userAccessService.GetContextAsync();

            var isSuperAdmin = context?.IsSuperAdministrator == false;

            if (request.LeaveTypeId <= 0)
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

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            await using var transaction =
                await db.Database.BeginTransactionAsync(
                    cancellationToken);

            try
            {
                var organisationId =
                    await GetCurrentOrganisationIdAsync(
                        cancellationToken);

                // =========================================================
                // 1. LOAD LEGACY LEAVE TYPE
                // =========================================================

                //var leaveType =
                //    await db.LeaveTypes
                //        .FirstOrDefaultAsync(
                //            x =>
                //                x.LeaveTypeId ==
                //                    request.LeaveTypeId &&
                //                x.OrganisationId ==
                //                    organisationId,
                //                        cancellationToken);

                //var isSuperAdmin = await _userAccessService.IsSuperAdministratorAsync();

                var leaveType = await db.LeaveTypes
                    .FirstOrDefaultAsync(
                        x => x.LeaveTypeId == request.LeaveTypeId &&
                             (isSuperAdmin || x.OrganisationId == organisationId),
                        cancellationToken);








                if (leaveType is null)
                {
                    await transaction.RollbackAsync(
                        cancellationToken);

                    return LeaveTypeSaveResult.Failure(
                        "The leave type could not be found.");
                }

                // =========================================================
                // 2. ACCESS CHECK
                // =========================================================

                if (!isSuperAdmin && ( leaveType.IsSystemType || leaveType.IsGlobal ))
                {
                    await transaction.RollbackAsync(
                        cancellationToken);

                    return LeaveTypeSaveResult.Failure(
                        "You do not have permission to edit system or global leave types.");
                }





                // =========================================================
                // 3. DUPLICATE NAME CHECK
                // =========================================================

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
                    await transaction.RollbackAsync(
                        cancellationToken);

                    return LeaveTypeSaveResult.Failure(
                        "Another leave type with this name already exists.");
                }

                // =========================================================
                // 4. UPDATE LEGACY LEAVE TYPE
                // =========================================================

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
                        ? request.StartInMonth
                        : 0;

                leaveType.RepeatedEveryInMonth =
                    request.IsRenewed
                        ? request.RepeatedEveryInMonth
                        : 0;

                // =========================================================
                // 5. HANDLE NEW FRAMEWORK MAPPING
                // =========================================================

                var existingMapping =
                    await db.LeaveTypeMappings
                        .Where(x =>
                            x.LegacyLeaveTypeId ==
                                leaveType.LeaveTypeId &&
                            x.OrganisationId ==
                                organisationId &&
                            x.IsActive)
                        .OrderByDescending(x =>
                            x.EffectiveFrom)
                        .ThenByDescending(x =>
                            x.LeaveTypeMappingId)
                        .FirstOrDefaultAsync(
                            cancellationToken);

                /*
                 * User selected a LeaveDefinition.
                 */
                if (request.LeaveDefinitionId.HasValue &&
                    request.LeaveDefinitionId.Value > 0)
                {
                    var definitionId =
                        request.LeaveDefinitionId.Value;

                    // Make sure selected definition is valid
                    // for the current organisation.
                    var definitionExists =
                        await db.LeaveDefinitions
                            .AsNoTracking()
                            .AnyAsync(
                                x =>
                                    x.LeaveDefinitionId ==
                                        definitionId &&
                                    x.IsActive &&
                                    (
                                        x.IsGlobal ||
                                        x.OwnerOrganisationId ==
                                            organisationId
                                    ),
                                cancellationToken);

                    if (!definitionExists)
                    {
                        await transaction.RollbackAsync(
                            cancellationToken);

                        return LeaveTypeSaveResult.Failure(
                            "The selected leave definition is not available for this organisation.");
                    }

                    var effectiveFrom =
                        request.MappingEffectiveFrom?.Date ??
                        DateTime.Today;

                    /*
                     * No mapping exists yet.
                     */
                    if (existingMapping is null)
                    {
                        var newMapping =
                            new LeaveTypeMapping
                            {
                                LegacyLeaveTypeId =
                                    leaveType.LeaveTypeId,

                                LeaveDefinitionId =
                                    definitionId,

                                OrganisationId =
                                    organisationId,

                                EffectiveFrom =
                                    effectiveFrom,

                                EffectiveTo =
                                    null,

                                IsActive =
                                    true
                            };

                        db.LeaveTypeMappings.Add(
                            newMapping);
                    }

                    /*
                     * Same definition is already mapped.
                     *
                     * Just update effective date if required.
                     */
                    else if (
                        existingMapping.LeaveDefinitionId ==
                            definitionId)
                    {
                        existingMapping.EffectiveFrom =
                            effectiveFrom;

                        existingMapping.EffectiveTo =
                            null;

                        existingMapping.IsActive =
                            true;
                    }

                    /*
                     * Mapping changed to another definition.
                     *
                     * Keep the old mapping for history and
                     * create a new mapping.
                     */
                    else
                    {
                        existingMapping.IsActive =
                            false;

                        existingMapping.EffectiveTo =
                            effectiveFrom.AddDays(-1);

                        var newMapping =
                            new LeaveTypeMapping
                            {
                                LegacyLeaveTypeId =
                                    leaveType.LeaveTypeId,

                                LeaveDefinitionId =
                                    definitionId,

                                OrganisationId =
                                    organisationId,

                                EffectiveFrom =
                                    effectiveFrom,

                                EffectiveTo =
                                    null,

                                IsActive =
                                    true
                            };

                        db.LeaveTypeMappings.Add(
                            newMapping);
                    }
                }
                else
                {
                    /*
                     * "Not mapped" selected.
                     *
                     * Do not delete the old mapping.
                     * Close it so migration history is preserved.
                     */
                    if (existingMapping is not null)
                    {
                        existingMapping.IsActive =
                            false;

                        existingMapping.EffectiveTo =
                            DateTime.Today;
                    }
                }

                // =========================================================
                // 6. SAVE EVERYTHING
                // =========================================================

                await db.SaveChangesAsync(
                    cancellationToken);

                // =========================================================
                // 7. COMMIT TRANSACTION
                // =========================================================

                await transaction.CommitAsync(
                    cancellationToken);

                return LeaveTypeSaveResult.Successful(
                    leaveType.LeaveTypeId,
                    "Leave type and migration mapping updated successfully.");
            }
            catch (Exception exception)
            {
                // =========================================================
                // 8. ROLLBACK
                // =========================================================

                await transaction.RollbackAsync(
                    cancellationToken);

                _logger.LogError(
                    exception,
                    "Unable to update leave type {LeaveTypeId}.",
                    request.LeaveTypeId);

                return LeaveTypeSaveResult.Failure(
                    "The leave type could not be updated.");
            }
        }



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
