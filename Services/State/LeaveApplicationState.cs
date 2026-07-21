using HRM.Components.Pages.Online.Leave.Forms;
using HRM.DTOs.Leave;
using HRM.Models;
using HRM.Models.Archives;
namespace HRM.Services.State
{
    public class LeaveApplicationState
    {
        // ==========================
        // Main Leave Application
        // ==========================
        public LeaveApplicationDto Model { get; set; } = new();

        // ==========================
        // Lookup Data
        // ==========================
        public List<JobLeaveTypeDto> JobLeaveTypes { get; set; } = new();
 
        public List<LeaveReasonDto> Reasons { get; set; } = new();

       
        public List<Country> Countries { get; set; } = new();

        public List<Atoll> Atolls { get; set; } = new();

       public List<Island> Islands { get; set; } = new();

        // ==========================
        // Selected Leave
        // ==========================
        public JobLeaveTypeDto? SelectedLeaveType { get; set; }

        public int RemainingDays { get; set; }

        // ==========================
        // Employee Leave Balances
        // ==========================
        public List<JobLeaveType> LeaveBalances { get; set; } = new();

        // ==========================
        // Work Handover
        // ==========================
        public bool WorkHandoverCompleted { get; set; }

        public string? HandoverEmployeeId { get; set; }

        public string? HandoverNotes { get; set; }

        // ==========================
        // Medical Certificate
        // ==========================
        public string? MedicalCertificateFileName { get; set; }

        public byte[]? MedicalCertificate { get; set; }

        // ==========================
        // Draft / Status
        // ==========================
        public bool IsLoaded { get; set; }

        public bool IsSubmitting { get; set; }

        public bool IsDraft { get; set; }

        // ==========================
        // Approval
        // ==========================
        public List<int> SelectedApprovers { get; set; } = new();

        // 🔥 central mapping (no UI logic anymore)
        private readonly Dictionary<string, Type> _formMap = new()
        {
            ["Annual Leave"] = typeof(AnnualLeaveForm),
            ["Sick Leave"] = typeof(Components.Pages.Online.Leave.Forms.SickLeaveForm),
            ["No Pay Leave"] = typeof(NoPayLeaveForm),
            ["Official Trip"] = typeof(OfficialTripForm)
        };

        public void ResolveForm(string leaveTypeCode)
        {
            SelectedFormComponent =
                _formMap.TryGetValue(leaveTypeCode, out var formType)
                    ? formType
                    : typeof(DefaultLeaveForm);
        }

        // ==========================
        // Helper Properties
        // ==========================
        public bool HasLeaveType =>
            Model.LeaveTypeId > 0;

        public bool HasReason =>
            Model.LeaveReasonId > 0;

        public bool IsWorkHandoverRequired =>
            Model.Duration > 3;

        public bool CanSubmit =>
            HasLeaveType &&
            HasReason &&
            Model.Duration > 0;


        /// <summary>
        /// Current DynamicComponent to display.
        /// </summary>
        public Type SelectedFormComponent { get; set; }
            = typeof(HRM.Components.Pages.Online.Leave.Forms.DefaultLeaveForm);

        // ==========================
        // Reset
        // ==========================
        public void Reset()
        {
            Model = new LeaveApplicationDto();

            JobLeaveTypes.Clear();

            Reasons.Clear();

            SelectedLeaveType = null;

            RemainingDays = 0;

            WorkHandoverCompleted = false;

            HandoverEmployeeId = null;

            HandoverNotes = null;

            MedicalCertificate = null;

            MedicalCertificateFileName = null;

            SelectedApprovers.Clear();

            IsLoaded = false;

            IsSubmitting = false;

            IsDraft = false;
        }
    }
}
