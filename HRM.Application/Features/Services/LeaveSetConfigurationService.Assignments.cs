using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HRM.Models;
using HRM.Models.Archives;
namespace HRM.Services;
public sealed record LeaveSetTypeOption(int Id,string Name);

public sealed record LeaveSetContentsData(string Name,int AuditId,bool CanEdit,List<LeaveSetTypeOption> Types,List<int> Selected);

public sealed record JobTypeSetOption(int Id,string Name,List<LeaveSetTypeOption> Types);

public sealed record JobTypeLeaveSetData(string Organisation,string JobType,List<JobTypeSetOption> Sets,List<int> Selected,string Version);
