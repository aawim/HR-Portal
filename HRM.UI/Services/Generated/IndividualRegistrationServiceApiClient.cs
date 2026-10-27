using HRM.Services;
using HRM.Services.Contracts;
using System.Data;
using HRM.Components.Shared;
using HRM.Models;
using HRM.Models.Archives;
using HRM.Services.Interfaces;
namespace HRM.UI.Services.Generated;
public sealed class IndividualRegistrationServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.IndividualRegistrationServiceContracts, HRM.Services.Contracts.IndividualRegistrationServiceContracts.IOperations
{
public Task<RegistrationOptions> LoadAsync() => api.InvokeAsync<RegistrationOptions>(typeof(HRM.Services.Contracts.IndividualRegistrationServiceContracts.IOperations),nameof(LoadAsync), [],default);
public Task<int> RegisterAsync(IndividualRegistrationModel model) => api.InvokeAsync<int>(typeof(HRM.Services.Contracts.IndividualRegistrationServiceContracts.IOperations),nameof(RegisterAsync), [HRM.Application.Transport.ApiArgument.From(model)],default);
}
