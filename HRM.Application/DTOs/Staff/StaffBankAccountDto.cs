using System;

namespace HRM.DTOs.Staff;

public class StaffBankAccountDto
{
    public int Id { get; set; }
    public int IndividualId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string? BankCode { get; set; }
    public string? BranchName { get; set; }
    public string AccountHolderName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string Currency { get; set; } = "MVR";
    public bool IsPrimary { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public class StaffBankAccountInputDto
{
    public int Id { get; set; }
    public int IndividualId { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string? BankCode { get; set; }
    public string? BranchName { get; set; }
    public string AccountHolderName { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string Currency { get; set; } = "MVR";
    public bool IsPrimary { get; set; }
    public string? Remarks { get; set; }
}
