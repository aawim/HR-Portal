namespace HRM.Models;

public class UserTodo
{
    public int UserTodoId { get; set; }
    public int UserId { get; set; }
    public string Title { get; set; } = "";
    public string? Notes { get; set; }
    public string Priority { get; set; } = "Normal";
    public DateTime? DueDate { get; set; }
    public bool IsCompleted { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];
}
