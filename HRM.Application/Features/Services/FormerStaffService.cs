using HRM.Models;
using HRM.Services.Interfaces;
using System.Globalization;
namespace HRM.Services;

public sealed record FormerStaffRow(int Id,string Name,string EmployeeNumber,string NationalId,string Passport,string Department,string JobType,DateTime Joined,DateTime? Ended)
{
    public int DepartmentId {get;init;}public int JobTypeId {get;init;}
    public int? VerifiedStateId {get;init;}public int? BusinessEntityStateId {get;init;}
    public string VerificationState {get;init;}="Not set";public string EntityState {get;init;}="Not set";
}

public sealed record FormerStaffChoice(int Id,string Name);

public sealed record FormerStaffFilters(int? DepartmentId=null,int? JobTypeId=null,DateTime? From=null,DateTime? To=null,int? VerifiedStateId=null,int? BusinessEntityStateId=null);

public sealed record FormerStaffData(string Organisation,List<FormerStaffRow> Staff)
{
    public List<FormerStaffChoice> Departments {get;init;}=[];public List<FormerStaffChoice> JobTypes {get;init;}=[];
    public List<FormerStaffChoice> VerifiedStates {get;init;}=[];public List<FormerStaffChoice> EntityStates {get;init;}=[];
    public string FilterDescription {get;init;}="All former staff";
}
