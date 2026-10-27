using System.Data;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
using HRM.Components.Shared;
namespace HRM.Services;
public sealed record LeaveConfigurationData(List<Organisation> Organisations,List<LeaveConfigurableValueType> Types,List<JobType> JobTypes,List<LeaveConfigurableValue> Values);
