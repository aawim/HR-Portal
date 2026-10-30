namespace HRM.DTOs.Leave
{
    public class LeaveApplicationDto
    {
        public int JobId { get; set; }
        public int? JobLeaveTypeId { get; set; }
        public int? LeaveTypeId { get; set; }
        public int? LeaveSubtypeId {get;set;}
        public string? CertificateFileName {get;set;}
        public byte[]? CertificateContent {get;set;}
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int Duration { get; set; }
        public int? HandoverJobId { get; set; }
        public string? HandoverNotes { get; set; }
        public List<LeaveHandoverTaskInput> HandoverTasks { get; set; } = [];
        public string? Explanation { get; set; }
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Select a reason for your leave application.")]
        [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "Select a reason for your leave application.")]
        public int? LeaveReasonId { get; set; }

        public int CountryId { get; set; }
        public string? Country { get; set; }
        public string? Atoll { get; set; }
        public int AtollId { get; set; }
        public int? IslandId { get; set; }
        public string? Island { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }
        [System.ComponentModel.DataAnnotations.EmailAddress(ErrorMessage = "Enter a valid contact email address.")]
        public string? ContactEmail { get; set; }


        // Sick Leave
        public string? MedicalCertificateNo { get; set; }

        public string? HospitalName { get; set; }

        // Official Trip
        public string? Destination { get; set; }

        public string? Purpose { get; set; }



    }
}
