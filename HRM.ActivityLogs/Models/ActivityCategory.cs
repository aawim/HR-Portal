namespace HRM.ActivityLogs.Models;

public enum ActivityCategory
{
    Authentication = 1,
    Navigation = 2,
    EmployeeManagement = 3,
    Attendance = 4,
    Leave = 5,
    Payroll = 6,
    Recruitment = 7,
    Transfers = 8,
    Assets = 9,
    Projects = 10,
    Teams = 11,
    Organisation = 12,
    Workflow = 13,
    Communication = 14,
    Administration = 15
}

public static class ActivityTypes
{
    public const string UserSignedIn = "UserSignedIn";
    public const string UserSignedOut = "UserSignedOut";
    public const string UserSwitchedOrganisation = "UserSwitchedOrganisation";
    public const string UserSwitchedTeam = "UserSwitchedTeam";
    public const string EmployeeProfileViewed = "EmployeeProfileViewed";
    public const string EmployeeProfileUpdated = "EmployeeProfileUpdated";
    public const string AttendanceRequestSubmitted = "AttendanceRequestSubmitted";
    public const string AttendanceCorrectionApplied = "AttendanceCorrectionApplied";
    public const string LeaveRequestSubmitted = "LeaveRequestSubmitted";
    public const string PayrollGenerationStarted = "PayrollGenerationStarted";
    public const string RecruitmentActionPerformed = "RecruitmentActionPerformed";
    public const string EmployeeTransferSubmitted = "EmployeeTransferSubmitted";
    public const string WorkflowSubmitted = "WorkflowSubmitted";
    public const string WorkflowApproved = "WorkflowApproved";
    public const string WorkflowRejected = "WorkflowRejected";
    public const string AssetIssued = "AssetIssued";
    public const string AssetAccepted = "AssetAccepted";
    public const string ProjectTaskAssigned = "ProjectTaskAssigned";
    public const string AnnouncementPublished = "AnnouncementPublished";
    public const string DocumentDownloaded = "DocumentDownloaded";
    public const string AdministrativeAction = "AdministrativeAction";
    public const string ActivityExported = "ActivityExported";
    public const string CrossOrganisationActivityViewed = "CrossOrganisationActivityViewed";
}
