using System;

namespace HRM.DTOs.Profile
{
    public class ProfileDocumentDto
    {
        public long DocumentId { get; set; }
        public string DocumentNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string DocumentTypeName { get; set; } = string.Empty;
        public string Classification { get; set; } = "Internal";
        public string Status { get; set; } = "Active";
        public int CurrentVersionNumber { get; set; } = 1;
        public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public bool IsVerified { get; set; }
        public bool IsOfficialRecord { get; set; }
        public DateTime CreatedAtUtc { get; set; }
    }
}
