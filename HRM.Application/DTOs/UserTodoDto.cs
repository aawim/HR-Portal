namespace HRM.DTOs;

public class UserTodoDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string? Notes { get; set; }
    public string Priority { get; set; } = "Normal";
    public DateTime? DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
public record UserTodoPage(List<UserTodoDto> Items, int Total, int PendingCount);
