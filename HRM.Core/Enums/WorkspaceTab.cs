using HRM.Enum;

using System.ComponentModel.DataAnnotations;

namespace HRM.Enum
{
    public enum WorkspaceTab
    {
        Overview,

        Profile,

        Attendance,

        Leave,

        OT,
        JobRelatedItems,
        [Display(Name = "Personal Items")]
        PersonalItems,

        Payroll,

        Performance,

        Documents,

        Staff,

        Teams,

        Departments,

        Reports,

        Analytics,

        Calendar,

        OrgDaily,

        Position,

        [Display(Name = "Job History")]
        JobHistory,

        Schedule,

        [Display(Name = "Leave Type")]
        LeaveType,

        [Display(Name = "Shift/Work")]
        ShiftWork,
        Assignments,
        [Display(Name = "Create Assignment")]
        AssignmentCreate,
        AssignedRoles,
        Permissions,
        WorkTypes,
        Finance,
        [Display(Name = "Staff Transfer")]
        StaffTransfer,
        [Display(Name = "Separation & Offboarding")]
        StaffSeparation,
        Onboarding,
        Certificates,
        [Display(Name = "Goods Loans")]
        GoodsLoans,
        [Display(Name = "Bank Details")]
        Bank,
        [Display(Name = "Emergency Contacts")]
        EmergencyContact
    }
}
