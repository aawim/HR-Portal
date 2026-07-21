namespace HRM.DTOs.Leave
{
    public class LeaveApplicationDto
    {
        public int JobId { get; set; }
        public int LeaveTypeId { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int Duration { get; set; }
        public string? Explanation { get; set; }
        public int? LeaveReasonId { get; set; }

        public int CountryId { get; set; }
        public string? Country { get; set; }
        public string? Atoll { get; set; }
        public int AtollId { get; set; }
        public int? IslandId { get; set; }
        public string? Island { get; set; }
        public string? Address { get; set; }
        public string? Phone { get; set; }


        // Sick Leave
        public string? MedicalCertificateNo { get; set; }

        public string? HospitalName { get; set; }

        // Official Trip
        public string? Destination { get; set; }

        public string? Purpose { get; set; }



    }
}
