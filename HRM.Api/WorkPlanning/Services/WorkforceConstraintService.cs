using System.Text.Json;
using HRM.Models;
using HRM.WorkPlanning.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HRM.WorkPlanning.Services;

public sealed class WorkforceConstraintService(IDbContextFactory<HrmTeContext> factory, IWorkforcePolicyResolver policies) : IWorkforceConstraintService
{
    private sealed record DutyRow(int IndividualId, int JobId, long DutyOccurrenceId, int DutyTypeId, DateTime StartAt, DateTime EndAt, int OrganisationId);

    public async Task<IReadOnlyList<WorkforceConstraintViolation>> ValidateAsync(int rosterVersionId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        var rows = await db.DutyAssignments.AsNoTracking().Where(x => x.DutyOccurrence.WorkforceRosterVersionId == rosterVersionId && x.Status == "Assigned")
            .Select(x => new DutyRow(x.IndividualId, x.JobId, x.DutyOccurrenceId, x.DutyOccurrence.DutyTypeId, x.DutyOccurrence.StartAt, x.DutyOccurrence.EndAt, x.DutyOccurrence.WorkforceRosterVersion.WorkforceRoster.OrganisationId)).ToListAsync(ct);
        if (rows.Count == 0) return [];
        var ids = rows.Select(x => x.IndividualId).Distinct().ToList();
        var org = rows[0].OrganisationId;
        var neighbours = await db.DutyAssignments.AsNoTracking().Where(x => ids.Contains(x.IndividualId) && x.Status == "Assigned" &&
            x.DutyOccurrence.WorkforceRosterVersionId != rosterVersionId && x.DutyOccurrence.Status == "Planned" &&
            x.DutyOccurrence.WorkforceRosterVersion.Status == "Published" && x.DutyOccurrence.WorkforceRosterVersion.WorkforceRoster.Status == "Published" &&
            x.DutyOccurrence.WorkforceRosterVersion.WorkforceRoster.OrganisationId == org)
            .Select(x => new DutyRow(x.IndividualId, x.JobId, x.DutyOccurrenceId, x.DutyOccurrence.DutyTypeId, x.DutyOccurrence.StartAt, x.DutyOccurrence.EndAt, org)).ToListAsync(ct);
        var currentIds = rows.Select(x => x.DutyOccurrenceId).ToHashSet();
        var result = new List<WorkforceConstraintViolation>();
        foreach (var group in rows.Concat(neighbours).GroupBy(x => x.IndividualId))
        {
            var ordered = group.OrderBy(x => x.StartAt).ThenBy(x => x.DutyOccurrenceId).ToList();
            var prior = ordered[0];
            foreach (var row in ordered.Skip(1))
            {
                if (currentIds.Contains(prior.DutyOccurrenceId) || currentIds.Contains(row.DutyOccurrenceId))
                {
                    foreach (var policy in await policies.ResolveAsync(new(row.OrganisationId, DateOnly.FromDateTime(row.StartAt), row.JobId, row.IndividualId, row.DutyTypeId), ct))
                    {
                        using var doc = JsonDocument.Parse(policy.RulesJson);
                        if (!doc.RootElement.TryGetProperty("MIN_REST_BETWEEN_SHIFTS", out var rule) || !rule.TryGetProperty("minimumMinutes", out var minutes)) continue;
                        var rest = (row.StartAt - prior.EndAt).TotalMinutes;
                        if (rest < minutes.GetInt32())
                            result.Add(new("MIN_REST_BETWEEN_SHIFTS", Severity(rule), $"Staff member {row.IndividualId} has only {rest:0} rest minutes before duty {row.DutyOccurrenceId}.", row.DutyOccurrenceId, row.IndividualId));
                    }
                }
                if (row.EndAt > prior.EndAt) prior = row;
            }
        }
        foreach (var row in rows)
        {
            foreach (var policy in await policies.ResolveAsync(new(row.OrganisationId, DateOnly.FromDateTime(row.StartAt), row.JobId, row.IndividualId, row.DutyTypeId), ct))
            {
                using var doc = JsonDocument.Parse(policy.RulesJson);
                if (!doc.RootElement.TryGetProperty("QUALIFICATION_RULE", out var rule) || !rule.TryGetProperty("requiredJobMatrixIds", out var required) || required.ValueKind != JsonValueKind.Array) continue;
                var matrices = required.EnumerateArray().Where(x => x.TryGetInt32(out _)).Select(x => x.GetInt32()).ToArray();
                if (matrices.Length == 0) continue;
                var qualified = await db.JobPositions.AsNoTracking().Where(x => x.JobId == row.JobId && x.FromDate <= row.StartAt && (!x.ToDate.HasValue || x.ToDate >= row.StartAt))
                    .Join(db.Positions.AsNoTracking(), x => x.PositionId, x => x.PositionId, (x, p) => p.PositionMatrixId)
                    .Join(db.PositionMatrices.AsNoTracking(), x => x, x => x.PositionMatrixId, (x, m) => m.JobMatrixId).AnyAsync(x => matrices.Contains(x), ct);
                if (!qualified) result.Add(new("QUALIFICATION_RULE", Severity(rule), $"Staff member {row.IndividualId} does not hold a position mapped to a required qualification matrix for duty {row.DutyOccurrenceId}.", row.DutyOccurrenceId, row.IndividualId));
            }
        }
        return result;
    }
    private static string Severity(JsonElement rule) => rule.TryGetProperty("enforcement", out var enforcement) ? enforcement.GetString() ?? "HARD_BLOCK" : "HARD_BLOCK";
}
