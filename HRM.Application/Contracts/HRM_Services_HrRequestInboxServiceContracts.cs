using HRM.Services.Interfaces;
namespace HRM.Services.Contracts;
public abstract class HrRequestInboxServiceContracts
{

public interface IOperations
{
Task<HrInboxData> LoadAsync(int? typeId=null,int page=1,string? search=null,string status="all",DateTime? from=null,DateTime? to=null,CancellationToken cancellationToken=default)
;
}
}
