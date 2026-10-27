namespace HRM.Enum
{
    public enum AccessCode
    {
        Success = 0,

        // User
        UserNotFound,
        UserInactive,
        UserNotEligible,

        // Job
        JobNotFound,
        NoActiveJob,

        // Organisation
        OrganisationNotFound,
        OrganisationAccessDenied,

        // Permission
        PermissionDenied,

        // Supervisor
        SupervisorRequired,

        // HR
        HRStaffRequired,
        HRHeadRequired,

        // Leave
        LeaveAlreadyApproved,
        LeaveAlreadyRejected,
        LeaveCancelled,

        // Payroll
        PayrollAlreadyProcessed,
        PayrollAlreadyApproved,

        // Attendance
        AttendanceLocked,

        // General
        InvalidOperation,
        Unknown,
        ResourceNotFound,
        BusinessRuleDenied,
        InvalidState


    }


}
