namespace HRM.Models.Recruitment;
public sealed class RecruitmentApplicationDocument
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public long StoredObjectId { get; set; }
    public string Category { get; set; } = "";
    public string FileName { get; set; } = "";
    public DateTime CreatedAtUtc { get; set; }
}
public sealed class RecruitmentApplicationEvent
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public string Kind { get; set; } = "";
    public string Message { get; set; } = "";
    public string SnapshotJson { get; set; } = "{}";
    public DateTime CreatedAtUtc { get; set; }
}
