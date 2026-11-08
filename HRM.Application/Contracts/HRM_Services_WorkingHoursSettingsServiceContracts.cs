using System.Data;
using HRM.Components.Shared;
using HRM.Services.Interfaces;
namespace HRM.Services.Contracts;
public abstract class WorkingHoursSettingsServiceContracts
{
    public const string ConfigurePermission = HRM.Authorization.Permissions.WorkingHoursConfigure;

    public const string AmendPermission = HRM.Authorization.Permissions.OfficialScheduleAmend;

    public const string ReviewPermission = HRM.Authorization.Permissions.WorkingHoursReview;

public interface IOperations
{
Task<global::HRM.DTOs.Contracts.WorkingHoursSettingsDataResponse> LoadAsync()
;
Task SaveRuleAsync(global::HRM.DTOs.Contracts.JobTypeWorkingHourRuleDto model)
;
Task SaveRequirementAsync(global::HRM.DTOs.Contracts.OrganisationWorkingHourRequirementDto model)
;
Task SaveAmendmentAsync(global::HRM.DTOs.Contracts.OrganisationScheduleAmendmentDto model)
;
}
}
