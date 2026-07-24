using HRM.Enum;
using Microsoft.AspNetCore.Http.HttpResults;
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

        Payroll,

        Performance,

        Documents,

        Staff,

        Teams,

        Reports,

        Analytics,

        Calendar,

        OrgDaily,

        Position,

        [Display(Name = "Leave Type")]
        LeaveType,

        [Display(Name = "Shift/Work")]
        ShiftWork,
        Assignments,
        [Display(Name = "Create Assignment")]
        AssignmentCreate
    }
}


 