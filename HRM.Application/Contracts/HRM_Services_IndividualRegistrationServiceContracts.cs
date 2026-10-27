using System.Data;
using HRM.Components.Shared;
using HRM.Services.Interfaces;
namespace HRM.Services.Contracts;
public abstract class IndividualRegistrationServiceContracts
{

public interface IOperations
{
Task<RegistrationOptions> LoadAsync()
;
Task<int> RegisterAsync(IndividualRegistrationModel model)
;
}
}
