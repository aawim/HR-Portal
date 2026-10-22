namespace HRM.DTOs;

public sealed class PositionOverviewData
{
    public string Organisation { get; set; } = "";
    public List<PositionOverviewRow> Positions { get; set; } = [];
}

public sealed class PositionOverviewRow
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsActive { get; set; }
    public bool IsGlobal { get; set; }
    public List<PositionOverviewStaff> Staff { get; set; } = [];
}

public sealed class PositionOverviewStaff
{
    public int IndividualId { get; set; }
    public int JobId { get; set; }
    public string Name { get; set; } = "";
    public string Department { get; set; } = "";
    public DateTime From { get; set; }
    public DateTime? To { get; set; }
    public bool IsCurrent { get; set; }
    public string Status { get; set; } = "";
}
