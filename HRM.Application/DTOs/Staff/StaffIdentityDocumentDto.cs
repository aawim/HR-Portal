using System;

namespace HRM.DTOs.Staff;

public class StaffIdentityDocumentDto
{
    public int IndividualId { get; set; }
    public string? IdentityCardNumber { get; set; }
    public DateTime? IdentityCardExpiryDate { get; set; }
    public bool IsIdentityCardExpired => IdentityCardExpiryDate.HasValue && IdentityCardExpiryDate.Value.Date < DateTime.UtcNow.AddHours(5).Date;
    public string? PassportNumber { get; set; }
    public int? PassportCountryId { get; set; }
    public string? PassportCountryName { get; set; }
    public DateTime? PassportIssuedDate { get; set; }
    public DateTime? PassportExpiryDate { get; set; }
    public bool IsPassportExpired => PassportExpiryDate.HasValue && PassportExpiryDate.Value.Date < DateTime.UtcNow.AddHours(5).Date;
}

public class StaffIdentityDocumentUpdateInput
{
    public int IndividualId { get; set; }
    public string? IdentityCardNumber { get; set; }
    public DateTime? IdentityCardExpiryDate { get; set; }
    public string? PassportNumber { get; set; }
    public int? PassportCountryId { get; set; }
    public string? PassportCountryName { get; set; }
    public DateTime? PassportIssuedDate { get; set; }
    public DateTime? PassportExpiryDate { get; set; }
}
