namespace HRM.DTOs.Contracts;

public sealed class JobTypeDto
{
    public int JobTypeId { get; set; }
    public string TypeName { get; set; } = "";
    public string? TypeNameDhivehi { get; set; }
    public int? CscjobTypePrimaryKey { get; set; }
    public bool? IsActive { get; set; }
    public int? OperationLogId { get; set; }
}
