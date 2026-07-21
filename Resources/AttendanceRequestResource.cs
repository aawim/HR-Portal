using HRM.Components.Shared;

namespace HRM.Resources
{
    public class AttendanceRequestResource : ResourceBase
    {
        public int AttendanceLogRequestID { get; set; }

        public int RequestID { get; set; }

        public int AttendanceLogID { get; set; }

        public int? OrganisationStructureID { get; set; }


        public int RequestStateID { get; set; }


        public int AttendanceLogStateID { get; set; }


        public int AttendanceLogModeID { get; set; }


        public string? Comments { get; set; }


        public bool IsPending =>
            RequestStateID ==
            SharedConfig.RequestStates.PENDING_HR_VERIFICATION;


        public bool IsApproved =>
            RequestStateID ==
            SharedConfig.RequestStates.APPROVED;


        public bool IsRejected =>
            RequestStateID ==
            SharedConfig.RequestStates.REJECTED;


        public bool IsAttendanceValid =>
            AttendanceLogStateID ==
            SharedConfig.AttendanceLogStates.Valid;


        public bool IsAttendanceInvalid =>
            AttendanceLogStateID ==
            SharedConfig.AttendanceLogStates.Invalid;
    }
}
