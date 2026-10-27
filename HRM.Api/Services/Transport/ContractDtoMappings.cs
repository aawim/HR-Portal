// Explicit transport projections. Database entities never cross the HTTP boundary.
namespace HRM.Services.Transport;
public static class ContractDtoMappings
{
    public static global::HRM.DTOs.Contracts.AttendanceLogDto ToDto(global::HRM.Models.AttendanceLog source) => new()
    {
        AttendanceLogId = source.AttendanceLogId,
        AttendanceDeviceId = source.AttendanceDeviceId,
        IndividualId = source.IndividualId,
        OrganisationId = source.OrganisationId,
        OrganisationStructureId = source.OrganisationStructureId,
        InOutModeId = source.InOutModeId,
        Year = source.Year,
        Month = source.Month,
        Day = source.Day,
        Hour = source.Hour,
        Minute = source.Minute,
        Second = source.Second,
        Date = source.Date,
        Uidstamp = source.Uidstamp,
        AttendanceLogModeId = source.AttendanceLogModeId,
        AttendanceLogStateId = source.AttendanceLogStateId,
        OperationLogId = source.OperationLogId,
        RelatedAttendanceLogId = source.RelatedAttendanceLogId,
        ActualInOutMode = source.ActualInOutMode,
        AttendanceLogResolutions = source.AttendanceLogResolutions.Select(item => ContractDtoMappings.ToDto(item)).ToList(),
    };

    public static global::HRM.DTOs.Contracts.AttendanceLogResolutionDto ToDto(global::HRM.Models.AttendanceLogResolution source) => new()
    {
        AttendanceLogResolutionId = source.AttendanceLogResolutionId,
        AttendanceLogId = source.AttendanceLogId,
        WorkPlanId = source.WorkPlanId,
        WorkAssignmentId = source.WorkAssignmentId,
        WorkAssignmentSegmentId = source.WorkAssignmentSegmentId,
        JobId = source.JobId,
        AttendanceResolutionStatusId = source.AttendanceResolutionStatusId,
        ResolutionDate = source.ResolutionDate,
        ResolutionMessage = source.ResolutionMessage,
        Latitude = source.Latitude,
        Longitude = source.Longitude,
        DeviceIdentifier = source.DeviceIdentifier,
        IsLocationValidated = source.IsLocationValidated,
        IsDeviceValidated = source.IsDeviceValidated,
        AttendanceClockTypeId = source.AttendanceClockTypeId,
        WorkPlanSegmentId = source.WorkPlanSegmentId,
        OperationLogId = source.OperationLogId,
        IsValid = source.IsValid,
        CreatedDate = source.CreatedDate,
    };


    public static global::HRM.DTOs.Leave.JobLeaveTypeDto ToDto(global::HRM.Models.JobLeaveType source) => new()
    {
        JobLeaveTypeId = source.JobLeaveTypeId,
        JobId = source.JobId,
        LeaveTypeId = source.LeaveTypeId,
        LeaveTypeName = source.LeaveType?.Name ?? "",
        LeaveTypeNameDv = source.LeaveType?.NameDhivehi ?? "",
        RemainingDays = source.RemainingDays ?? 0,
        IsValid = source.IsValid,
        IsLeaveInfoUpdated = source.IsLeaveInfoUpdated,
        LastLeaveTakenDate = source.LastLeaveTakenDate,
        RenewedDate = source.RenewedDate,
        EffectiveFromDate = source.EffectiveFromDate,
        EffectiveToDate = source.EffectiveToDate
    };


    public static global::HRM.DTOs.Contracts.JobTypeDto ToDto(global::HRM.Models.JobType source) => new()
    {
        JobTypeId = source.JobTypeId,
        TypeName = source.TypeName,
        TypeNameDhivehi = source.TypeNameDhivehi,
        CscjobTypePrimaryKey = source.CscjobTypePrimaryKey,
        IsActive = source.IsActive,
        OperationLogId = source.OperationLogId,
    };


    public static global::HRM.DTOs.Contracts.JobTypeLeaveAccrualRuleDto ToDto(global::HRM.Models.LeaveTypes.JobTypeLeaveAccrualRule source) => new()
    {
        Id = source.Id,
        OrganisationId = source.OrganisationId,
        JobTypeId = source.JobTypeId,
        LeaveTypeId = source.LeaveTypeId,
        GrantDays = source.GrantDays,
        FirstGrantAfterMonths = source.FirstGrantAfterMonths,
        RepeatEveryMonths = source.RepeatEveryMonths,
        CarryForwardMode = source.CarryForwardMode,
        CarryForwardLimit = source.CarryForwardLimit,
        MaximumBalance = source.MaximumBalance,
        EffectiveFrom = source.EffectiveFrom,
        IsEnabled = source.IsEnabled,
        Version = source.Version,
        OperationLogId = source.OperationLogId,
    };

    public static global::HRM.Models.LeaveTypes.JobTypeLeaveAccrualRule ToDomain(global::HRM.DTOs.Contracts.JobTypeLeaveAccrualRuleDto source) => new()
    {
        Id = source.Id,
        OrganisationId = source.OrganisationId,
        JobTypeId = source.JobTypeId,
        LeaveTypeId = source.LeaveTypeId,
        GrantDays = source.GrantDays,
        FirstGrantAfterMonths = source.FirstGrantAfterMonths,
        RepeatEveryMonths = source.RepeatEveryMonths,
        CarryForwardMode = source.CarryForwardMode,
        CarryForwardLimit = source.CarryForwardLimit,
        MaximumBalance = source.MaximumBalance,
        EffectiveFrom = source.EffectiveFrom,
        IsEnabled = source.IsEnabled,
        Version = source.Version,
        OperationLogId = source.OperationLogId,
    };

    public static global::HRM.DTOs.Contracts.JobTypeOvertimeRuleDto ToDto(global::HRM.Models.JobTypeOvertimeRule source) => new()
    {
        Id = source.Id,
        OrganisationId = source.OrganisationId,
        JobTypeId = source.JobTypeId,
        IsAllowed = source.IsAllowed,
        MaximumWeeklyHours = source.MaximumWeeklyHours,
        MaximumDailyHours = source.MaximumDailyHours,
        Version = source.Version,
        OperationLogId = source.OperationLogId,
    };

    public static global::HRM.Models.JobTypeOvertimeRule ToDomain(global::HRM.DTOs.Contracts.JobTypeOvertimeRuleDto source) => new()
    {
        Id = source.Id,
        OrganisationId = source.OrganisationId,
        JobTypeId = source.JobTypeId,
        IsAllowed = source.IsAllowed,
        MaximumWeeklyHours = source.MaximumWeeklyHours,
        MaximumDailyHours = source.MaximumDailyHours,
        Version = source.Version,
        OperationLogId = source.OperationLogId,
    };

    public static global::HRM.DTOs.Contracts.JobTypeWorkingHourRuleDto ToDto(global::HRM.Models.JobTypeWorkingHourRule source) => new()
    {
        Id = source.Id,
        OrganisationId = source.OrganisationId,
        JobTypeId = source.JobTypeId,
        RequiredWeeklyMinutes = source.RequiredWeeklyMinutes,
        WorkingDaysMask = source.WorkingDaysMask,
        WeekStartsOn = source.WeekStartsOn,
        CountDutyHours = source.CountDutyHours,
        EffectiveFrom = source.EffectiveFrom,
        EffectiveTo = source.EffectiveTo,
        Version = source.Version,
        OperationLogId = source.OperationLogId,
    };

    public static global::HRM.Models.JobTypeWorkingHourRule ToDomain(global::HRM.DTOs.Contracts.JobTypeWorkingHourRuleDto source) => new()
    {
        Id = source.Id,
        OrganisationId = source.OrganisationId,
        JobTypeId = source.JobTypeId,
        RequiredWeeklyMinutes = source.RequiredWeeklyMinutes,
        WorkingDaysMask = source.WorkingDaysMask,
        WeekStartsOn = source.WeekStartsOn,
        CountDutyHours = source.CountDutyHours,
        EffectiveFrom = source.EffectiveFrom,
        EffectiveTo = source.EffectiveTo,
        Version = source.Version,
        OperationLogId = source.OperationLogId,
    };

    public static global::HRM.DTOs.Contracts.LeafDto ToDto(global::HRM.Models.Leaf source) => new()
    {
        LeaveId = source.LeaveId,
        JobId = source.JobId,
        FromDate = source.FromDate,
        ToDate = source.ToDate,
        Duration = source.Duration,
        HandoverRuleSnapshot = source.HandoverRuleSnapshot,
        ChitNo = source.ChitNo,
        LeaveTypeId = source.LeaveTypeId,
        OperationLogId = source.OperationLogId,
        LeaveStateId = source.LeaveStateId,
        LeaveState = source.LeaveState is null ? null : ContractDtoMappings.ToDto(source.LeaveState),
        LeaveType = source.LeaveType is null ? null : ContractDtoMappings.ToDto(source.LeaveType),
    };

    public static global::HRM.DTOs.Contracts.LeaveAccrualSettingsDataResponse ToDto(global::HRM.Services.Leave.LeaveAccrualSettingsData source) => new()
    {
        OrganisationId = source.OrganisationId,
        Organisation = source.Organisation,
        JobTypes = source.JobTypes,
        LeaveTypes = source.LeaveTypes,
        Rules = source.Rules.Select(item => ContractDtoMappings.ToDto(item)).ToList(),
    };

    public static global::HRM.DTOs.Contracts.LeaveCarryForwardDto ToDto(global::HRM.Models.LeaveTypes.LeaveCarryForward source) => new()
    {
        Id = source.Id,
        RuleId = source.RuleId,
        RuleVersion = source.RuleVersion,
        JobId = source.JobId,
        JobLeaveTypeId = source.JobLeaveTypeId,
        RenewalDate = source.RenewalDate,
        PreviousBalance = source.PreviousBalance,
        CarriedDays = source.CarriedDays,
        PeriodStart = source.PeriodStart,
        PeriodEnd = source.PeriodEnd,
        ClosingGrant = source.ClosingGrant,
        RenewalGrant = source.RenewalGrant,
        NewBalance = source.NewBalance,
        ProcessedByUserId = source.ProcessedByUserId,
        ProcessedAtUtc = source.ProcessedAtUtc,
        Reason = source.Reason,
        OperationLogId = source.OperationLogId,
    };


    public static global::HRM.DTOs.Contracts.LeaveConfigurableValueDto ToDto(global::HRM.Models.Archives.LeaveConfigurableValue source) => new()
    {
        LeaveConfigurableValueId = source.LeaveConfigurableValueId,
        LeaveConfigurableValueTypeId = source.LeaveConfigurableValueTypeId,
        OrganisationId = source.OrganisationId,
        Value = source.Value,
        IsValid = source.IsValid,
        JobTypeId = source.JobTypeId,
    };

    public static global::HRM.Models.Archives.LeaveConfigurableValue ToDomain(global::HRM.DTOs.Contracts.LeaveConfigurableValueDto source) => new()
    {
        LeaveConfigurableValueId = source.LeaveConfigurableValueId,
        LeaveConfigurableValueTypeId = source.LeaveConfigurableValueTypeId,
        OrganisationId = source.OrganisationId,
        Value = source.Value,
        IsValid = source.IsValid,
        JobTypeId = source.JobTypeId,
    };

    public static global::HRM.DTOs.Contracts.LeaveConfigurableValueTypeDto ToDto(global::HRM.Models.Archives.LeaveConfigurableValueType source) => new()
    {
        LeaveConfigurableValueTypeId = source.LeaveConfigurableValueTypeId,
        Name = source.Name,
        Description = source.Description,
        Type = source.Type,
        Comment = source.Comment,
        IsJobTypeDependant = source.IsJobTypeDependant,
        Unit = source.Unit,
    };


    public static global::HRM.DTOs.Contracts.LeaveConfigurationDataResponse ToDto(global::HRM.Services.LeaveConfigurationData source) => new()
    {
        Organisations = source.Organisations.Select(item => ContractDtoMappings.ToDto(item)).ToList(),
        Types = source.Types.Select(item => ContractDtoMappings.ToDto(item)).ToList(),
        JobTypes = source.JobTypes.Select(item => ContractDtoMappings.ToDto(item)).ToList(),
        Values = source.Values.Select(item => ContractDtoMappings.ToDto(item)).ToList(),
    };

    public static global::HRM.DTOs.Contracts.LeaveSetConfigurationDataResponse ToDto(global::HRM.Services.LeaveSetConfigurationData source) => new()
    {
        Organisations = source.Organisations.Select(item => ContractDtoMappings.ToDto(item)).ToList(),
        Sets = source.Sets.Select(item => ContractDtoMappings.ToDto(item)).ToList(),
        IsSuperAdmin = source.IsSuperAdmin,
    };

    public static global::HRM.DTOs.Contracts.LeaveSetDto ToDto(global::HRM.Models.Archives.LeaveSet source) => new()
    {
        LeaveSetId = source.LeaveSetId,
        Name = source.Name,
        OrganisationId = source.OrganisationId,
        OperationLogId = source.OperationLogId,
        IsPublic = source.IsPublic,
        IsGlobal = source.IsGlobal,
    };


    public static global::HRM.DTOs.Contracts.LeaveStateDto ToDto(global::HRM.Models.LeaveState source) => new()
    {
        LeaveStateId = source.LeaveStateId,
        StateName = source.StateName,
    };


    public static global::HRM.DTOs.Contracts.LeaveTypeDto ToDto(global::HRM.Models.LeaveType source) => new()
    {
        LeaveTypeId = source.LeaveTypeId,
        Name = source.Name,
        NameDhivehi = source.NameDhivehi,
        Duration = source.Duration,
        IncludeHolidays = source.IncludeHolidays,
        IncludePay = source.IncludePay,
        IsPublic = source.IsPublic,
        IsGlobal = source.IsGlobal,
        IsLocationRequired = source.IsLocationRequired,
        ServiceDurationMonths = source.ServiceDurationMonths,
        OrganisationId = source.OrganisationId,
        RequestTypeId = source.RequestTypeId,
        IsSystemType = source.IsSystemType,
        IsRenewed = source.IsRenewed,
        OperationLogId = source.OperationLogId,
        IsStaffWideAvailable = source.IsStaffWideAvailable,
        PayPercentage = source.PayPercentage,
        StartInMonth = source.StartInMonth,
        RepeatedEveryInMonth = source.RepeatedEveryInMonth,
        Code = source.Code,
        Description = source.Description,
        IsActive = source.IsActive,
        CreatedDate = source.CreatedDate,
        UpdatedDate = source.UpdatedDate,
    };


    public static global::HRM.DTOs.Contracts.OrganisationDto ToDto(global::HRM.Models.Organisation source) => new()
    {
        BusinessEntityID = source.BusinessEntityID,
        OrganisationTypeId = source.OrganisationTypeId,
        RegistrationNumber = source.RegistrationNumber,
        OrganisationName = source.OrganisationName,
        OrganisationNameDhivehi = source.OrganisationNameDhivehi,
        RegistrationDate = source.RegistrationDate,
        CountryId = source.CountryId,
        ParentOrganisationBusinessEntityId = source.ParentOrganisationBusinessEntityId,
        CscofficePimaryKey = source.CscofficePimaryKey,
    };


    public static global::HRM.DTOs.Contracts.OrganisationScheduleAmendmentDto ToDto(global::HRM.Models.OrganisationScheduleAmendment source) => new()
    {
        Id = source.Id,
        OrganisationId = source.OrganisationId,
        WorkDate = source.WorkDate,
        OriginalStart = source.OriginalStart,
        RevisedStart = source.RevisedStart,
        Reason = source.Reason,
        IsActive = source.IsActive,
        Version = source.Version,
        OperationLogId = source.OperationLogId,
    };

    public static global::HRM.Models.OrganisationScheduleAmendment ToDomain(global::HRM.DTOs.Contracts.OrganisationScheduleAmendmentDto source) => new()
    {
        Id = source.Id,
        OrganisationId = source.OrganisationId,
        WorkDate = source.WorkDate,
        OriginalStart = source.OriginalStart,
        RevisedStart = source.RevisedStart,
        Reason = source.Reason,
        IsActive = source.IsActive,
        Version = source.Version,
        OperationLogId = source.OperationLogId,
    };

    public static global::HRM.DTOs.Contracts.OrganisationWorkingHourRequirementDto ToDto(global::HRM.Models.OrganisationWorkingHourRequirement source) => new()
    {
        Id = source.Id,
        OrganisationId = source.OrganisationId,
        JobTypeId = source.JobTypeId,
        FromDate = source.FromDate,
        ToDate = source.ToDate,
        RequiredDailyMinutes = source.RequiredDailyMinutes,
        Reason = source.Reason,
        IsActive = source.IsActive,
        Version = source.Version,
        OperationLogId = source.OperationLogId,
    };

    public static global::HRM.Models.OrganisationWorkingHourRequirement ToDomain(global::HRM.DTOs.Contracts.OrganisationWorkingHourRequirementDto source) => new()
    {
        Id = source.Id,
        OrganisationId = source.OrganisationId,
        JobTypeId = source.JobTypeId,
        FromDate = source.FromDate,
        ToDate = source.ToDate,
        RequiredDailyMinutes = source.RequiredDailyMinutes,
        Reason = source.Reason,
        IsActive = source.IsActive,
        Version = source.Version,
        OperationLogId = source.OperationLogId,
    };

    public static global::HRM.DTOs.Contracts.ScopeScheduleAssignmentDto ToDto(global::HRM.Models.ScopeScheduleAssignment source) => new()
    {
        Id = source.Id,
        OrganisationId = source.OrganisationId,
        OrganisationStructureId = source.OrganisationStructureId,
        GroupId = source.GroupId,
        WorkTemplateId = source.WorkTemplateId,
        SchedulePurpose = source.SchedulePurpose,
        ScheduleMode = source.ScheduleMode,
        WorkingDaysMask = source.WorkingDaysMask,
        RotationAnchorDate = source.RotationAnchorDate,
        RotationTemplateIds = source.RotationTemplateIds,
        EffectiveFrom = source.EffectiveFrom,
        EffectiveTo = source.EffectiveTo,
        Priority = source.Priority,
        IsActive = source.IsActive,
        Version = source.Version,
        OperationLogId = source.OperationLogId,
    };

    public static global::HRM.Models.ScopeScheduleAssignment ToDomain(global::HRM.DTOs.Contracts.ScopeScheduleAssignmentDto source) => new()
    {
        Id = source.Id,
        OrganisationId = source.OrganisationId,
        OrganisationStructureId = source.OrganisationStructureId,
        GroupId = source.GroupId,
        WorkTemplateId = source.WorkTemplateId,
        SchedulePurpose = source.SchedulePurpose,
        ScheduleMode = source.ScheduleMode,
        WorkingDaysMask = source.WorkingDaysMask,
        RotationAnchorDate = source.RotationAnchorDate,
        RotationTemplateIds = source.RotationTemplateIds,
        EffectiveFrom = source.EffectiveFrom,
        EffectiveTo = source.EffectiveTo,
        Priority = source.Priority,
        IsActive = source.IsActive,
        Version = source.Version,
        OperationLogId = source.OperationLogId,
    };

    public static global::HRM.DTOs.Contracts.ScopeScheduleSettingsDataResponse ToDto(global::HRM.Services.ScopeScheduleSettingsData source) => new()
    {
        Structures = source.Structures,
        Groups = source.Groups,
        Templates = source.Templates,
        Assignments = source.Assignments.Select(item => ContractDtoMappings.ToDto(item)).ToList(),
    };

    public static global::HRM.DTOs.Contracts.WorkingHoursSettingsDataResponse ToDto(global::HRM.Services.WorkingHoursSettingsData source) => new()
    {
        Organisation = source.Organisation,
        JobTypes = source.JobTypes,
        Rules = source.Rules.Select(item => ContractDtoMappings.ToDto(item)).ToList(),
        Amendments = source.Amendments.Select(item => ContractDtoMappings.ToDto(item)).ToList(),
        Requirements = source.Requirements.Select(item => ContractDtoMappings.ToDto(item)).ToList(),
        CanConfigure = source.CanConfigure,
        CanAmend = source.CanAmend,
    };

    public static global::HRM.Models.Archives.LeaveSet ToDomain(global::HRM.DTOs.Contracts.LeaveSetDto source) => new()
    {
        LeaveSetId = source.LeaveSetId,
        Name = source.Name,
        OrganisationId = source.OrganisationId,
        OperationLogId = source.OperationLogId,
        IsPublic = source.IsPublic,
        IsGlobal = source.IsGlobal
    };

}

