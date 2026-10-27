using HRM.Services;
using HRM.Services.Contracts;
using HRM.Models;
using HRM.Services.Interfaces;
namespace HRM.UI.Services.Generated;
public sealed class HrRequestInboxServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.HrRequestInboxServiceContracts, HRM.Services.Contracts.HrRequestInboxServiceContracts.IOperations
{
public Task<HrInboxData> LoadAsync(int? typeId=null,int page=1,string? search=null,string status="all",DateTime? from=null,DateTime? to=null,CancellationToken cancellationToken=default) => api.InvokeAsync<HrInboxData>(typeof(HRM.Services.Contracts.HrRequestInboxServiceContracts.IOperations),nameof(LoadAsync), [HRM.Application.Transport.ApiArgument.From(typeId),HRM.Application.Transport.ApiArgument.From(page),HRM.Application.Transport.ApiArgument.From(search),HRM.Application.Transport.ApiArgument.From(status),HRM.Application.Transport.ApiArgument.From(from),HRM.Application.Transport.ApiArgument.From(to)],cancellationToken);
}
