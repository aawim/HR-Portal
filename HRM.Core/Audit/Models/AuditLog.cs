using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HRM.Audit.Models
{
    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key]
        public int AuditLogID { get; set; }

        public int AuditLogDataTypeID { get; set; }

        public int AuditLogActionTypeID { get; set; }

        public int DataItemID { get; set; }

        public string Data { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? URL { get; set; }

        public int? CreatedByUserID { get; set; }

        public DateTime CreatedDate { get; set; }

        [MaxLength(50)]
        public string? CreatedByIPAddress { get; set; }

        // --- Navigation Properties ---
        // These allow you to easily `.Include()` the type names in queries

        [ForeignKey("AuditLogActionTypeID")]
        public virtual AuditLogActionType ActionType { get; set; } = null!;

        [ForeignKey("AuditLogDataTypeID")]
        public virtual AuditLogDataType DataType { get; set; } = null!;
    }
}
