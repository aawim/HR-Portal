using HRM.Services;
using HRM.Services.Contracts;
using HRM.DTOs.Attendance;
using HRM.Models;
using HRM.Services.Interfaces;
namespace HRM.UI.Services.Generated;
public sealed class WeeklyWorkingHoursServiceApiClient(HRM.Application.Transport.IApiOperationClient api): HRM.Services.Contracts.WeeklyWorkingHoursServiceContracts, HRM.Services.Contracts.WeeklyWorkingHoursServiceContracts.IOperations
{
public Task<WeeklyWorkedHoursDto> LoadAsync(int person, DateTime date) => api.InvokeAsync<WeeklyWorkedHoursDto>(typeof(HRM.Services.Contracts.WeeklyWorkingHoursServiceContracts.IOperations),nameof(LoadAsync), [HRM.Application.Transport.ApiArgument.From(person),HRM.Application.Transport.ApiArgument.From(date)],default);
}
