using System.Data;
using HRM.Components.Shared;
using HRM.Models;
using HRM.Services.Interfaces;
namespace HRM.Services;

public sealed record WorkingHourOption(int Id, string Name);

public sealed record WorkingHoursSettingsData(string Organisation, List<WorkingHourOption> JobTypes,
    List<JobTypeWorkingHourRule> Rules, List<OrganisationScheduleAmendment> Amendments, List<OrganisationWorkingHourRequirement> Requirements, bool CanConfigure, bool CanAmend);
