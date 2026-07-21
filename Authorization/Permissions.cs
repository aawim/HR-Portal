namespace HRM.Authorization
{
    public static class Permissions
    {

        // =========================
        // Individual
        // =========================
        public const string IndividualView = "Individual.View";


        public const string IndividualCreate = "Individual.Create";


        public const string IndividualEdit = "Individual.Edit";


        public const string IndividualDelete = "Individual.Delete";





        // =========================
        // Employee
        // =========================

        public const string EmployeeView = "Employee.View";


        public const string EmployeeCreate = "Employee.Create";


        public const string EmployeeEdit = "Employee.Edit";


        public const string EmployeeDelete = "Employee.Delete";



        // =========================
        // Leave
        // =========================




     
        public const string LeaveView = "Leave.View";

        public const string LeaveCreate = "Leave.Create";


        public const string LeaveApprove = "Leave.Approve";


        public const string LeaveReject = "Leave.Reject";

        public const string LeaveEdit = "Leave.Edit";

        public const string LeaveCancel = "Leave.Cancel";
        


        // =========================
        // Attendance
        // =========================

        public const string AttendanceView = "Attendance.View";


        public const string AttendanceAdjust = "Attendance.Adjust";
        public const string AttendanceApprove = "Attendance.Approve";
        public const string AttendanceReject = "Attendance.Reject";
        public const string AttendanceCancel = "Attendance.Cancel";
        public const string AttendanceCreate = "Attendance.Create";

        // =========================
        // Administration
        // =========================

        public const string UserManage = "User.Manage";


        public const string RoleManage = "Role.Manage";
    }
}
