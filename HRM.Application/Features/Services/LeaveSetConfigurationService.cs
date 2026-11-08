using System.Data;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
namespace HRM.Services;
public sealed record LeaveSetConfigurationData(List<Organisation> Organisations,List<LeaveSet> Sets,bool IsSuperAdmin);
