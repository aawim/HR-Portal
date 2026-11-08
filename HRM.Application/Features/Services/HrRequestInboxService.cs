using HRM.Models;
using HRM.Services.Interfaces;
namespace HRM.Services;
public sealed record HrRequestGroup(int Id,string Name,int Count,int Processing);

public sealed record HrInboxRow(int Id,string Reference,string Applicant,string Type,string Status,DateTime AppliedAt,bool IsFinal,string? Url);

public sealed record HrRequestType(int Id,string Name);

public sealed record HrRequestStatus(int Id,string Name);

public sealed record HrInboxData(string Organisation,bool OrganisationWide,List<HrRequestGroup> Groups,List<HrInboxRow> Items,int TypeId,int Page,int TotalPages)
{
    public List<HrRequestType> Types { get; init; } = [];
    public List<HrRequestStatus> Statuses { get; init; } = [];
}
