using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HRM.DTOs;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
namespace HRM.Services.Contracts;
public abstract class SupervisorChangeRequestServiceContracts
{
    public const string PageUrl="/online/supervisor-requests";

public interface IOperations
{
Task<ServiceResult> SubmitAsync(int organisationId,string kind,int target,int person,bool assign,string reason)
;
Task<List<SupervisorRequestRow>> LoadAsync(int? organisationId=null,bool pendingOnly=false)
;
Task<ServiceResult> ActAsync(int id,int expectedState,string action,string? comment,int? organisationId=null)
;
}
}
