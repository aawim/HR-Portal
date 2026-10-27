using HRM.Services;
using HRM.Services.Contracts;
using HRM.DTOs;
using HRM.Models;
using HRM.Services.Interfaces;
namespace HRM.UI.Services.Generated;
public sealed class PositionOverviewServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.PositionOverviewServiceContracts, HRM.Services.Contracts.PositionOverviewServiceContracts.IOperations
{
public Task<PositionOverviewData> LoadAsync() => api.InvokeAsync<PositionOverviewData>(typeof(HRM.Services.Contracts.PositionOverviewServiceContracts.IOperations),nameof(LoadAsync), [],default);
}
