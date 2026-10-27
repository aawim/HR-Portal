using System.Data;
using HRM.DTOs;
using HRM.Services.Interfaces;
namespace HRM.Services.Contracts;
public abstract class RequestTransitionSettingsServiceContracts
{

public interface IOperations
{
Task<RequestTransitionSettingsDto> LoadAsync()
;
Task<List<AllowedRequestTransitionDto>> GetAsync(int requestType)
;
Task<ServiceResult> SetAsync(int requestType, int from, int to, bool allowed)
;
}
}
