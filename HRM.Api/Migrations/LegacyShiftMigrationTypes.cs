// These types exist solely so applied migration snapshots remain compilable.
// They are intentionally excluded from the active DbContext and domain projects.
namespace HRM.Models.Archives;

internal sealed class ShiftSchedule
{
    public int ScheduleId { get; set; }
}

internal sealed class ShiftScheduleDay
{
    public int ShiftScheduleDayId { get; set; }
}
