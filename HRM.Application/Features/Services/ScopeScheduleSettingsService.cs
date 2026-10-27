using System.Data;
using HRM.Models;
using HRM.Services.Interfaces;
namespace HRM.Services;

public sealed record ScopeScheduleOption(int Id,string Name,bool IsActive=true);

public sealed record ScopeScheduleStructureOption(int Id, string Name, int TypeId, string TypeName);

public sealed record ScopeScheduleSettingsData(List<ScopeScheduleStructureOption> Structures,List<ScopeScheduleOption> Groups,List<ScopeScheduleOption> Templates,List<ScopeScheduleAssignment> Assignments);
