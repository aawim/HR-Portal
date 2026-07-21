using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HRM.Audit.Models
{
    [Table("AuditLogActionTypes")]
    public class AuditLogActionType
    {
        [Key]
        public int AuditLogActionTypeID { get; set; }

        [MaxLength(100)] // Optional: Adjust to match your SQL column size
        public string ActionTypeName { get; set; } = string.Empty;
    }
}
