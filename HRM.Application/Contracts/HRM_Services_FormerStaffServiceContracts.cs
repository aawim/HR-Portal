using HRM.Services.Interfaces;
using System.Globalization;
namespace HRM.Services.Contracts;
public abstract class FormerStaffServiceContracts
{

public interface IOperations
{
Task<FormerStaffData> SearchAsync(string? search=null,CancellationToken ct=default,FormerStaffFilters? filters=null)
;
Task<global::HRM.DTOs.Contracts.ExportReportDto> DetailAsync(int person,CancellationToken ct=default)
;
}
}
