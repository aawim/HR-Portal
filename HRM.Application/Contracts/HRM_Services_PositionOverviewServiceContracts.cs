using HRM.DTOs;
using HRM.Services.Interfaces;
namespace HRM.Services.Contracts;
public abstract class PositionOverviewServiceContracts
{

public interface IOperations
{
Task<PositionOverviewData> LoadAsync()
;
}
}
