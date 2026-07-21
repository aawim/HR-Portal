using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HRM.Audit.Models
{
    [Table("AuditLogDataTypes")]
    public class AuditLogDataType
    {
        [Key]
        public int AuditLogDataTypeID { get; set; }

        [MaxLength(100)] // Optional: Adjust to match your SQL column size
        public string DataTypeName { get; set; } = string.Empty;
    }
}
