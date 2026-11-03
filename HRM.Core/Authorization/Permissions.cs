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
        public const string WorkingHoursConfigure = "Attendance.WorkingHours.Configure";
        public const string WorkingHoursReview = "Attendance.WorkingHours.Review";
        public const string OfficialScheduleAmend = "Attendance.Schedule.Amend";

        // =========================
        // Administration
        // =========================

        public const string UserManage = "User.Manage";


        public const string RoleManage = "Role.Manage";

        // =========================
        // Staff Transfer
        // =========================

        public const string StaffTransferView = "StaffTransfer.View";
        public const string StaffTransferCreate = "StaffTransfer.Create";
        public const string StaffTransferEdit = "StaffTransfer.Edit";
        public const string StaffTransferComment = "StaffTransfer.Comment";
        public const string StaffTransferConfirm = "StaffTransfer.Confirm";
        public const string StaffTransferApprove = "StaffTransfer.Approve";
        public const string StaffTransferActivate = "StaffTransfer.Activate";
        public const string StaffTransferProvisionAccess = "StaffTransfer.ProvisionAccess";
        public const string StaffTransferExportRestrictedPdf = "StaffTransfer.ExportRestrictedPdf";

        // =========================
        // Staff Asset Management
        // =========================

        public const string AssetView = "Asset.View";
        public const string AssetInventoryManage = "Asset.Inventory.Manage";
        public const string AssetIssuanceInitiate = "Asset.Issuance.Initiate";
        public const string AssetAcknowledge = "Asset.Acknowledge";
        public const string AssetHrApprove = "Asset.HR.Approve";
        public const string AssetReturnInspect = "Asset.Return.Inspect";
        public const string AssetAuditView = "Asset.Audit.View";
        public const string AssetExportRestrictedPdf = "Asset.ExportRestrictedPdf";
        public const string AssetServiceRequest = "Asset.Service.Request";
        public const string AssetServiceManage = "Asset.Service.Manage";
        public const string AssetTechnicianExecute = "Asset.Technician.Execute";
        public const string AssetUpgradeAuthorize = "Asset.Upgrade.Authorize";
        public const string AssetClearanceAuthorize = "Asset.Clearance.Authorize";
    }
}
