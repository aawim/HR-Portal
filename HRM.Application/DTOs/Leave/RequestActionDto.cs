namespace HRM.DTOs.Leave;

public class RequestActionDto
{
    public int FromStateId { get; set; }
    public int ToStateId { get; set; }
    public string Label { get; set; } = string.Empty;
    public string NextStatus { get; set; } = string.Empty;
    public bool CommentRequired { get; set; }
}

public class RequestActionSubmissionDto
{
    public int FromStateId { get; set; }
    public int ToStateId { get; set; }
    public string? Comments { get; set; }
}
