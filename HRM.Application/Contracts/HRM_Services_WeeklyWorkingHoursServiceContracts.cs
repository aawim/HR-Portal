using HRM.DTOs.Attendance;
using HRM.Services.Interfaces;
namespace HRM.Services.Contracts;
public abstract class WeeklyWorkingHoursServiceContracts
{

public interface IOperations
{
Task<WeeklyWorkedHoursDto> LoadAsync(int person, DateTime date)
;
}
}
