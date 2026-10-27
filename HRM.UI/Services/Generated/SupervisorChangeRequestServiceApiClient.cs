using HRM.Services;
using HRM.Services.Contracts;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HRM.DTOs;
using HRM.Models;
using HRM.Services.Interfaces;
using HRM.Services.Interfaces.AppovalWF;
namespace HRM.UI.Services.Generated;
public sealed class SupervisorChangeRequestServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.SupervisorChangeRequestServiceContracts, HRM.Services.Contracts.SupervisorChangeRequestServiceContracts.IOperations
{
public Task<ServiceResult> SubmitAsync(int organisationId,string kind,int target,int person,bool assign,string reason) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.SupervisorChangeRequestServiceContracts.IOperations),nameof(SubmitAsync), [HRM.Application.Transport.ApiArgument.From(organisationId),HRM.Application.Transport.ApiArgument.From(kind),HRM.Application.Transport.ApiArgument.From(target),HRM.Application.Transport.ApiArgument.From(person),HRM.Application.Transport.ApiArgument.From(assign),HRM.Application.Transport.ApiArgument.From(reason)],default);
public Task<List<SupervisorRequestRow>> LoadAsync(int? organisationId=null,bool pendingOnly=false) => api.InvokeAsync<List<SupervisorRequestRow>>(typeof(HRM.Services.Contracts.SupervisorChangeRequestServiceContracts.IOperations),nameof(LoadAsync), [HRM.Application.Transport.ApiArgument.From(organisationId),HRM.Application.Transport.ApiArgument.From(pendingOnly)],default);
public Task<ServiceResult> ActAsync(int id,int expectedState,string action,string? comment,int? organisationId=null) => api.InvokeAsync<ServiceResult>(typeof(HRM.Services.Contracts.SupervisorChangeRequestServiceContracts.IOperations),nameof(ActAsync), [HRM.Application.Transport.ApiArgument.From(id),HRM.Application.Transport.ApiArgument.From(expectedState),HRM.Application.Transport.ApiArgument.From(action),HRM.Application.Transport.ApiArgument.From(comment),HRM.Application.Transport.ApiArgument.From(organisationId)],default);
}
