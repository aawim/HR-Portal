namespace HRM.DTOs.Leave;

public record LeaveCalendarEntry(int Id, int PersonId, string Person, DateTime From, DateTime To, string Title, string Status, bool IsPlan, bool IsOwner);
