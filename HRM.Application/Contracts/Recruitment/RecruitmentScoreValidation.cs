using System.Text.Json;

namespace HRM.Application.Contracts.Recruitment;

public static class RecruitmentScoreValidation
{
    public static bool TryCalculate(string definitionJson, string scoresJson, out decimal total)
    {
        total = 0;
        try
        {
            using var definition = JsonDocument.Parse(definitionJson);
            using var scores = JsonDocument.Parse(scoresJson);
            if (scores.RootElement.ValueKind != JsonValueKind.Object) return false;
            var supplied = scores.RootElement.EnumerateObject().ToList();
            var components = definition.RootElement.GetProperty("components").EnumerateArray().ToList();
            if (supplied.Count != components.Count || supplied.Select(x => x.Name).Distinct(StringComparer.Ordinal).Count() != supplied.Count) return false;
            decimal maximum = 0;
            foreach (var component in components)
            {
                var name = component[0].GetString()!;
                var limit = component[1].GetDecimal();
                if (!scores.RootElement.TryGetProperty(name, out var value) || !value.TryGetDecimal(out var score) || score < 0 || score > limit) return false;
                total += score;
                maximum += limit;
            }
            return maximum == definition.RootElement.GetProperty("total").GetDecimal();
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException or ArgumentException)
        {
            total = 0;
            return false;
        }
    }
}
