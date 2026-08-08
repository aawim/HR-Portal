namespace HRM.DTOs.LeaveTypePolicy
{
    public sealed class LeavePolicyBucketDto
    {
        public int LeavePolicyBucketId { get; set; }

        public int LeavePolicyId { get; set; }

        public string Code { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public decimal AllocationDays { get; set; }

        public bool RequiresCertificate { get; set; }

        public decimal? CertificateRequiredAfterDays { get; set; }

        public int SequenceNumber { get; set; }

        public bool IsActive { get; set; }
    }
}
