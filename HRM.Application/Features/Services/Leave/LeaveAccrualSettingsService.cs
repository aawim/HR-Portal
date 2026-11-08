using System.Data;
using HRM.Components.Shared;
using HRM.Models;
using HRM.Models.LeaveTypes;
using HRM.Services.Interfaces;
namespace HRM.Services.Leave;

public sealed record AccrualOption(int Id, string Name);

public sealed record LeaveAccrualSettingsData(int OrganisationId, string Organisation, List<AccrualOption> JobTypes,
    List<AccrualOption> LeaveTypes, List<JobTypeLeaveAccrualRule> Rules);
