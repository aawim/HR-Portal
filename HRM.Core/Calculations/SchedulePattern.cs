using HRM.Models.WorkPlanning;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
namespace HRM.Helpers;

public sealed record RotationSlot(
    int TemplateId,
    TimeOnly? StartTime = null,
    TimeOnly? EndTime = null,
    string? Label = null)
{
    public bool HasCustomWindow => StartTime.HasValue || EndTime.HasValue || !string.IsNullOrWhiteSpace(Label);
}

public static class SchedulePattern
{
    private sealed class RotationSlotWire
    {
        [JsonPropertyName("t")]
        public int T { get; set; }

        [JsonPropertyName("s")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? S { get; set; }

        [JsonPropertyName("e")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? E { get; set; }

        [JsonPropertyName("l")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? L { get; set; }
    }

    // Bit 0 = Sunday, through bit 6 = Saturday. Normal week = Sunday–Thursday.
    public static bool IsWeekend(DateTime date) => date.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday;

    public static DateTime AlignToSunday(DateTime date) => date.Date.AddDays(-(int)date.Date.DayOfWeek);

    public static RotationSlot? SlotOn(JobWorkTemplateAssignment assignment, DateTime date)
    {
        if (date.Date < assignment.EffectiveFrom.Date || (assignment.EffectiveTo.HasValue && date.Date > assignment.EffectiveTo.Value.Date))
            return null;

        if (assignment.ScheduleMode == "Rotation")
        {
            var slots = ParseSlots(assignment.RotationTemplateIds);
            if (slots.Count == 0 || !assignment.RotationAnchorDate.HasValue) return null;
            var offset = (date.Date - assignment.RotationAnchorDate.Value.Date).Days;
            var slot = slots[((offset % slots.Count) + slots.Count) % slots.Count];
            return slot.TemplateId > 0 ? slot : null;
        }

        if (assignment.SchedulePurpose == "Duty" && assignment.ScheduleMode == "Standard")
            return assignment.WorkTemplateID > 0 ? new RotationSlot(assignment.WorkTemplateID, assignment.ScheduledStartTime) : null;

        return (assignment.WorkingDaysMask & (1 << (int)date.DayOfWeek)) != 0 && assignment.WorkTemplateID > 0
            ? new RotationSlot(assignment.WorkTemplateID, assignment.ScheduledStartTime)
            : null;
    }

    public static int TemplateOn(JobWorkTemplateAssignment assignment, DateTime date)
        => SlotOn(assignment, date)?.TemplateId ?? 0;

    public static List<int> Parse(string? json)
        => ParseSlots(json).Select(s => s.TemplateId).ToList();

    public static List<RotationSlot> ParseSlots(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return [];
        try
        {
            using var doc = JsonDocument.Parse(json);
            if (doc.RootElement.ValueKind != JsonValueKind.Array) return [];
            var result = new List<RotationSlot>();
            foreach (var el in doc.RootElement.EnumerateArray())
            {
                if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var id))
                {
                    result.Add(new RotationSlot(id));
                }
                else if (el.ValueKind == JsonValueKind.Object)
                {
                    var templateId = ReadInt(el, "t") ?? ReadInt(el, "templateId") ?? ReadInt(el, "TemplateId") ?? 0;
                    var start = ReadTime(el, "s") ?? ReadTime(el, "startTime") ?? ReadTime(el, "StartTime");
                    var end = ReadTime(el, "e") ?? ReadTime(el, "endTime") ?? ReadTime(el, "EndTime");
                    var label = ReadString(el, "l") ?? ReadString(el, "label") ?? ReadString(el, "Label");
                    result.Add(new RotationSlot(
                        templateId,
                        templateId > 0 ? start : null,
                        templateId > 0 ? end : null,
                        templateId > 0 && !string.IsNullOrWhiteSpace(label) ? label.Trim() : null));
                }
                else
                {
                    return [];
                }
            }
            return result;
        }
        catch (JsonException)
        {
            return [];
        }
    }

    public static string SerializeSlots(IEnumerable<RotationSlot> slots)
    {
        var list = slots.Select(s => s.TemplateId > 0
            ? new RotationSlot(s.TemplateId, s.StartTime, s.EndTime, string.IsNullOrWhiteSpace(s.Label) ? null : s.Label.Trim())
            : new RotationSlot(0)).ToList();

        if (!list.Any(s => s.HasCustomWindow))
            return JsonSerializer.Serialize(list.Select(s => s.TemplateId).ToList());

        var wire = list.Select(s => new RotationSlotWire
        {
            T = s.TemplateId,
            S = s.StartTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
            E = s.EndTime?.ToString("HH:mm", CultureInfo.InvariantCulture),
            L = s.Label
        }).ToList();

        return JsonSerializer.Serialize(wire);
    }

    private static int? ReadInt(JsonElement el, string name)
        => el.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var v) ? v : null;

    private static string? ReadString(JsonElement el, string name)
        => el.TryGetProperty(name, out var prop) && prop.ValueKind == JsonValueKind.String ? prop.GetString() : null;

    private static TimeOnly? ReadTime(JsonElement el, string name)
    {
        var raw = ReadString(el, name);
        if (string.IsNullOrWhiteSpace(raw)) return null;
        return TimeOnly.TryParseExact(raw.Trim(), ["HH:mm", "H:mm", "HH:mm:ss"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var t) ? t : null;
    }
}
