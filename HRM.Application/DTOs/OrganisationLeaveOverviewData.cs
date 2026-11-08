namespace HRM.DTOs;

public sealed class OrganisationLeaveOverviewFilter
{
    public int OrganisationId {get;set;}
    public int Year {get;set;}
    public int Month {get;set;}
    public int DepartmentId {get;set;}
    public int JobTypeId {get;set;}
    public string Search {get;set;}="";
}
public sealed class OrganisationLeaveOverviewData
{
    public bool OrganisationWide {get;set;}
    public List<OrganisationProcessingLeave> ProcessingLeaves {get;set;}=[];
    public List<OrganisationMonthlyLeavePlan> MonthlyPlans {get;set;}=[];
    public List<string> Calendars {get;set;}=[];
    public List<OrganisationCalendarEntry> CalendarEntries {get;set;}=[];
}
public sealed record OrganisationProcessingLeave(int RequestId,string Reference,string Applicant,string LeaveType,string Status,DateTime From,DateTime To,int Days,bool CanOpen);
public sealed record OrganisationMonthlyLeavePlan(int Month,int StaffCount);
public sealed record OrganisationCalendarEntry(string Key,string Title,DateTime From,DateTime To,string Calendar);
