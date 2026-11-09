using System;

namespace HRM.Models;

public partial class StaffEmergencyContact
{
    public int Id { get; set; }

    public int IndividualId { get; set; }

    public string ContactName { get; set; } = string.Empty;

    public string Relationship { get; set; } = string.Empty;

    public string PrimaryPhone { get; set; } = string.Empty;

    public string? SecondaryPhone { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public bool IsPrimary { get; set; }

    public string? Notes { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public virtual Individual? Individual { get; set; }
}
