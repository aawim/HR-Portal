using HRM.Models.WorkPlanning;
using System.Text.Json;
namespace HRM.Helpers;
public static class SchedulePattern
{
    // Bit 0 = Sunday, through bit 6 = Saturday. Normal week = Sunday–Thursday.
    public static bool IsWeekend(DateTime date) => date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday;
    public static int TemplateOn(JobWorkTemplateAssignment assignment, DateTime date)
    {
        if (date.Date < assignment.EffectiveFrom.Date || (assignment.EffectiveTo.HasValue && date.Date > assignment.EffectiveTo.Value.Date)) return 0;
        if (assignment.ScheduleMode == "Rotation")
        {
            var pattern = Parse(assignment.RotationTemplateIds);
            if (pattern.Count == 0 || !assignment.RotationAnchorDate.HasValue) return 0;
            var offset = (date.Date - assignment.RotationAnchorDate.Value.Date).Days;
            return pattern[((offset % pattern.Count) + pattern.Count) % pattern.Count];
        }
        if (assignment.SchedulePurpose == "Duty" && assignment.ScheduleMode == "Standard") return assignment.WorkTemplateID;
        return (assignment.WorkingDaysMask & (1 << (int)date.DayOfWeek)) != 0 ? assignment.WorkTemplateID : 0;
    }
    public static List<int> Parse(string? json)
    {
        try { return string.IsNullOrWhiteSpace(json) ? [] : JsonSerializer.Deserialize<List<int>>(json) ?? []; }
        catch (JsonException) { return []; }
    }

}
