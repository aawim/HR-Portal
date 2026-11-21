using System.Text.Json;
using HRM.Communications.Contracts;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Communications;

public static class CommunicationEligibility
{
    public static async Task<bool> SourceIsEligibleAsync(HrmTeContext db, CommunicationOutbox source, CancellationToken ct = default)
    {
        if (source.AggregateType == "CommunicationTest" && source.EventCode == CommunicationEventCodes.TelegramTestRequested)
            return long.TryParse(source.AggregateId, out var testDestination) && testDestination > 0 && source.OccurredAtUtc >= DateTime.UtcNow.AddMinutes(-10);
        JsonDocument payload;
        try { payload = JsonDocument.Parse(source.PayloadJson); } catch (JsonException) { return false; }
        using (payload)
        {
            if (payload.RootElement.ValueKind != JsonValueKind.Object) return false;
            if (source.AggregateType == "EmployeeSchedule" && source.EventCode == CommunicationEventCodes.EmployeeScheduleChanged)
            {
                if (!payload.RootElement.TryGetProperty("JobId", out var job) || job.ValueKind != JsonValueKind.Number || !job.TryGetInt32(out var jobId) ||
                    !payload.RootElement.TryGetProperty("IndividualId", out var person) || person.ValueKind != JsonValueKind.Number || !person.TryGetInt32(out var individualId) ||
                    !payload.RootElement.TryGetProperty("ScopeScheduleAssignmentId", out var scope) || scope.ValueKind != JsonValueKind.Number || !scope.TryGetInt32(out var scopeId) ||
                    !payload.RootElement.TryGetProperty("ScopeVersion", out var version) || version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var scopeVersion)) return false;
                var today = DateTime.UtcNow.AddHours(5).Date;
                return await db.Jobs.AnyAsync(j => j.JobId == jobId && j.IndividualID == individualId && j.OrganisationID == source.OrganisationBusinessEntityId && j.JobStateId == 4 && j.JoinedDate <= today && (!j.TerminatedDate.HasValue || j.TerminatedDate >= today), ct) &&
                    await db.ScopeScheduleAssignments.AnyAsync(s => s.Id == scopeId && s.OrganisationId == source.OrganisationBusinessEntityId && s.Version == scopeVersion, ct);
            }
            if (source.AggregateType != "OrganisationEvent" || !int.TryParse(source.AggregateId, out var eventId) ||
                !payload.RootElement.TryGetProperty("OrganisationWideAudience", out var audience) || audience.ValueKind != JsonValueKind.True) return false;
            var status = source.EventCode switch { CommunicationEventCodes.OrganisationEventPublished => "Published", CommunicationEventCodes.OrganisationEventCancelled => "Cancelled", _ => null };
            return status is not null && await db.OrganisationEvents.AnyAsync(e => e.OrganisationEventId == eventId && e.OrganisationBusinessEntityId == source.OrganisationBusinessEntityId && e.IsValid && e.Status == status &&
                e.Audiences.Any() && !e.Audiences.Any(a => !a.IsIncluded || a.AudienceType != "Organisation"), ct);
        }
    }

    public static async Task<bool> CanDeliverToAsync(HrmTeContext db, CommunicationOutbox source, CommunicationDestination destination, string providerCode, CancellationToken ct = default)
    {
        if (destination.OrganisationBusinessEntityId != source.OrganisationBusinessEntityId || !await SourceIsEligibleAsync(db, source, ct)) return false;
        if (source.AggregateType == "CommunicationTest") return providerCode == "TELEGRAM" && long.TryParse(source.AggregateId, out var id) && id == destination.CommunicationDestinationId;
        using var payload = JsonDocument.Parse(source.PayloadJson);
        if (source.AggregateType == "EmployeeSchedule")
        {
            if (providerCode != "WHATSAPP" || !payload.RootElement.TryGetProperty("IndividualId", out var person) || !person.TryGetInt32(out var individual) || destination.IndividualId != individual) return false;
            return await db.CommunicationConsents.AnyAsync(c => c.OrganisationBusinessEntityId == source.OrganisationBusinessEntityId && c.CommunicationDestinationId == destination.CommunicationDestinationId &&
                c.IndividualId == individual && c.Purpose == "ScheduleUpdates" && c.RevokedAtUtc == null, ct);
        }
        if (providerCode == "WHATSAPP" || destination.IndividualId.HasValue) return false;
        if (providerCode == "FACEBOOK_PAGE")
            return payload.RootElement.TryGetProperty("PublicAnnouncement", out var isPublic) && isPublic.ValueKind == JsonValueKind.True && int.TryParse(source.AggregateId, out var eventId) &&
                await db.OrganisationEvents.AnyAsync(e => e.OrganisationEventId == eventId && e.OrganisationBusinessEntityId == source.OrganisationBusinessEntityId && e.Visibility == "Public", ct);
        return providerCode is "TELEGRAM" or "MSTEAMS";
    }
}
