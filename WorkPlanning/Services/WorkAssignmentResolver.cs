using HRM.DTOs.WorkPlanning;
using HRM.Models;
using HRM.Models.WorkPlanning;
using HRM.WorkPlanning.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace HRM.WorkPlanning.Services
{
    public class WorkAssignmentResolver : IWorkAssignmentResolver
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;

        public WorkAssignmentResolver(
            IDbContextFactory<HrmTeContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<WorkAssignmentResolutionResult> ResolveAsync(
            int individualId,
            DateTime attendanceDateTime,
            CancellationToken cancellationToken = default)
        {
            if (individualId <= 0)
            {
                return WorkAssignmentResolutionResult.Failed(
                    "A valid individual ID is required.");
            }

            await using var db =
                await _dbFactory.CreateDbContextAsync(
                    cancellationToken);

            /*
             * Load a wider date range so the resolver can support:
             *
             * 1. overnight assignments,
             * 2. early attendance inside grace-before,
             * 3. late attendance inside grace-after.
             *
             * Exact assignment and segment matching is performed
             * after loading the candidates.
             */
            var searchFrom = attendanceDateTime.AddDays(-1);
            var searchTo = attendanceDateTime.AddDays(1);

            var candidateOwners =
                await db.WorkAssignmentOwners
                    .AsNoTracking()
                    .Include(x => x.WorkAssignment)
                        .ThenInclude(x =>
                            x.WorkAssignmentSegments)
                    .Where(x =>
                        x.IndividualId == individualId &&
                        x.IsValid &&

                        x.EffectiveFrom <= attendanceDateTime &&

                        (!x.EffectiveTo.HasValue ||
                         x.EffectiveTo.Value >=
                         attendanceDateTime) &&

                        (!x.RelievedDate.HasValue ||
                         x.RelievedDate.Value >
                         attendanceDateTime) &&

                        x.WorkAssignment.IsValid &&

                        !x.WorkAssignment.CancelledDate
                            .HasValue &&

                        x.WorkAssignment.StartDateTime <=
                            searchTo &&

                        x.WorkAssignment.EndDateTime >=
                            searchFrom)
                    .ToListAsync(cancellationToken);

            if (candidateOwners.Count == 0)
            {
                return WorkAssignmentResolutionResult.Failed(
                    $"No work assignment was found for individual " +
                    $"{individualId} at " +
                    $"{attendanceDateTime:dd MMM yyyy HH:mm}.");
            }

            var matches = new List<AssignmentMatch>();

            /*
             * Only assignments whose real segment windows include
             * the attendance event are included in diagnostics.
             *
             * This prevents a daytime assignment from the previous
             * day from being reported as a relevant assignment.
             */
            var applicableOwners =
                new List<WorkAssignmentOwner>();

            foreach (var owner in candidateOwners)
            {
                var assignment = owner.WorkAssignment;

                var assignmentSegments = assignment.WorkAssignmentSegments
                    .Where(x => x.IsValid)
                    .OrderBy(x => x.SequenceNumber)
                    .ToList();

                /*
                 * First determine whether the attendance event is relevant
                 * to the assignment itself.
                 *
                 * Do not use the segment dates to reject the assignment,
                 * because incorrectly generated segment dates would make a
                 * valid assignment appear as though it does not exist.
                 */
                var maximumGraceBeforeMinutes =
                    assignmentSegments.Count > 0
                        ? assignmentSegments.Max(x =>
                            x.GraceBeforeMinutes)
                        : 0;

                var maximumGraceAfterMinutes =
                    assignmentSegments.Count > 0
                        ? assignmentSegments.Max(x =>
                            x.GraceAfterMinutes)
                        : 0;

                var assignmentWindowStart =
                    assignment.StartDateTime.AddMinutes(
                        -maximumGraceBeforeMinutes);

                var assignmentWindowEnd =
                    assignment.EndDateTime.AddMinutes(
                        maximumGraceAfterMinutes);

                if (attendanceDateTime < assignmentWindowStart ||
                    attendanceDateTime > assignmentWindowEnd)
                {
                    continue;
                }

                applicableOwners.Add(owner);

                foreach (var segment in assignmentSegments)
                {
                    var segmentEnd =
                        segment.EndDateTime ??
                        segment.StartDateTime;

                    if (segmentEnd < segment.StartDateTime)
                    {
                        continue;
                    }

                    var isPointInTimeSegment =
                        !segment.EndDateTime.HasValue ||
                        segmentEnd == segment.StartDateTime;

                    var matchingWindowStart =
                        segment.StartDateTime.AddMinutes(
                            -segment.GraceBeforeMinutes);

                    var matchingWindowEnd =
                        segmentEnd.AddMinutes(
                            segment.GraceAfterMinutes);

                    var isInsideScheduledPeriod =
                        isPointInTimeSegment
                            ? attendanceDateTime ==
                              segment.StartDateTime
                            : attendanceDateTime >=
                                  segment.StartDateTime &&
                              attendanceDateTime <=
                                  segmentEnd;

                    var isInsideGracePeriod =
                        attendanceDateTime >= matchingWindowStart &&
                        attendanceDateTime <= matchingWindowEnd;

                    if (!isInsideGracePeriod)
                    {
                        continue;
                    }

                    matches.Add(new AssignmentMatch
                    {
                        Owner = owner,
                        Assignment = assignment,
                        Segment = segment,
                        SegmentEndDateTime = segmentEnd,
                        IsInsideScheduledPeriod =
                            isInsideScheduledPeriod,
                        IsInsideGracePeriod =
                            isInsideGracePeriod,
                        DistanceFromSegmentStartMinutes =
                            Math.Abs(
                                (attendanceDateTime -
                                 segment.StartDateTime)
                                .TotalMinutes)
                    });
                }
            }

            /*
             * Candidate assignments existed in the wider search,
             * but none had an actual segment window covering the
             * attendance time.
             */
            if (applicableOwners.Count == 0)
            {
                var candidateDetails = candidateOwners
                    .Select(x =>
                        $"{x.WorkAssignment.Name}: " +
                        $"{x.WorkAssignment.StartDateTime:dd MMM yyyy HH:mm} - " +
                        $"{x.WorkAssignment.EndDateTime:dd MMM yyyy HH:mm}")
                    .ToList();

                var details = candidateDetails.Count > 0
                    ? string.Join("; ", candidateDetails)
                    : "No candidate assignments were loaded.";

                return WorkAssignmentResolutionResult.Failed(
                    $"No work assignment matches attendance time " +
                    $"{attendanceDateTime:dd MMM yyyy HH:mm} for individual " +
                    $"{individualId}. Candidate assignments: {details}");
            }

            if (matches.Count == 0)
            {
                var availableSegments =
                    applicableOwners
                        .SelectMany(x =>
                            x.WorkAssignment
                                .WorkAssignmentSegments)
                        .Where(x => x.IsValid)
                        .OrderBy(x => x.StartDateTime)
                        .ThenBy(x => x.SequenceNumber)
                        .Select(FormatSegmentDetails)
                        .ToList();

                var segmentDetails =
                    availableSegments.Count > 0
                        ? string.Join(
                            "; ",
                            availableSegments)
                        : "No valid segments exist.";

                return WorkAssignmentResolutionResult.Failed(
                    $"A work assignment exists for individual " +
                    $"{individualId}, but no segment matches " +
                    $"{attendanceDateTime:dd MMM yyyy HH:mm}. " +
                    $"Available segments: {segmentDetails}");
            }

            /*
             * Prefer an event inside the scheduled segment period.
             *
             * Only use grace-period matches when no exact scheduled
             * period match exists.
             */
            var scheduledMatches =
                matches
                    .Where(x =>
                        x.IsInsideScheduledPeriod)
                    .ToList();

            var applicableMatches =
                scheduledMatches.Count > 0
                    ? scheduledMatches
                    : matches;

            /*
             * Higher assignment priority takes precedence.
             */
            var highestPriority =
                applicableMatches.Max(x =>
                    x.Assignment.Priority);

            var priorityMatches =
                applicableMatches
                    .Where(x =>
                        x.Assignment.Priority ==
                        highestPriority)
                    .ToList();

            /*
             * Different assignments with the same highest priority
             * create an ambiguous attendance resolution.
             */
            var matchingAssignmentIds =
                priorityMatches
                    .Select(x =>
                        x.Assignment.WorkAssignmentId)
                    .Distinct()
                    .ToList();

            if (matchingAssignmentIds.Count > 1)
            {
                var assignmentList =
                    string.Join(
                        ", ",
                        priorityMatches
                            .GroupBy(x =>
                                x.Assignment
                                    .WorkAssignmentId)
                            .Select(x =>
                                $"{x.First().Assignment.Name} " +
                                $"({x.Key})"));

                return WorkAssignmentResolutionResult.Failed(
                    "Multiple work assignments match this " +
                    "attendance time with the same priority: " +
                    $"{assignmentList}. Please correct the " +
                    "overlapping work assignments.");
            }

            /*
             * Multiple segments in the same assignment may overlap.
             *
             * Selection order:
             *
             * 1. scheduled-period match,
             * 2. lowest segment sequence number,
             * 3. segment start closest to attendance time.
             */
            var selectedMatch =
                priorityMatches
                    .OrderByDescending(x =>
                        x.IsInsideScheduledPeriod)
                    .ThenBy(x =>
                        x.Segment.SequenceNumber)
                    .ThenBy(x =>
                        x.DistanceFromSegmentStartMinutes)
                    .First();

            return CreateSuccessfulResult(
                selectedMatch,
                individualId);
        }

        private static string FormatSegmentDetails(
            WorkAssignmentSegment segment)
        {
            var graceBeforeText =
                segment.GraceBeforeMinutes > 0
                    ? $" -{segment.GraceBeforeMinutes}m before"
                    : string.Empty;

            var graceAfterText =
                segment.GraceAfterMinutes > 0
                    ? $" +{segment.GraceAfterMinutes}m after"
                    : string.Empty;

            if (!segment.EndDateTime.HasValue ||
                segment.EndDateTime.Value ==
                segment.StartDateTime)
            {
                return
                    $"{segment.Name}: " +
                    $"{segment.StartDateTime:dd MMM yyyy HH:mm} " +
                    $"(point-in-time{graceBeforeText}" +
                    $"{graceAfterText})";
            }

            return
                $"{segment.Name}: " +
                $"{segment.StartDateTime:dd MMM yyyy HH:mm} - " +
                $"{segment.EndDateTime.Value:dd MMM yyyy HH:mm}" +
                $"{graceBeforeText}{graceAfterText}";
        }

        private static WorkAssignmentResolutionResult
            CreateSuccessfulResult(
                AssignmentMatch match,
                int individualId)
        {
            var owner = match.Owner;
            var assignment = match.Assignment;
            var segment = match.Segment;

            var matchType =
                match.IsInsideScheduledPeriod
                    ? "inside the scheduled period"
                    : "inside the allowed grace period";

            return new WorkAssignmentResolutionResult
            {
                Success = true,

                Message =
                    $"Assignment '{assignment.Name}' and " +
                    $"segment '{segment.Name}' were successfully " +
                    $"resolved {matchType}.",

                WorkPlanId =
                    assignment.WorkPlanId,

                WorkAssignmentId =
                    assignment.WorkAssignmentId,

                WorkAssignmentSegmentId =
                    segment.WorkAssignmentSegmentId,

                IndividualId =
                    individualId,

                JobId =
                    owner.JobId,

                AssignmentName =
                    assignment.Name,

                SegmentName =
                    segment.Name,

                AssignmentStartDateTime =
                    assignment.StartDateTime,

                AssignmentEndDateTime =
                    assignment.EndDateTime,

                SegmentStartDateTime =
                    segment.StartDateTime,

                SegmentEndDateTime =
                    match.SegmentEndDateTime,

                GraceBeforeMinutes =
                    segment.GraceBeforeMinutes,

                GraceAfterMinutes =
                    segment.GraceAfterMinutes,

                RequiresAttendance =
                    segment.RequiresAttendance,

                RequiresCheckOut =
                    assignment.RequiresCheckOut,

                RequiresLocationValidation =
                    segment.RequiresLocationValidation,

                RequiresDeviceValidation =
                    segment.RequiresDeviceValidation,

                IsInsideScheduledPeriod =
                    match.IsInsideScheduledPeriod,

                IsInsideGracePeriod =
                    match.IsInsideGracePeriod
            };
        }

        private sealed class AssignmentMatch
        {
            public WorkAssignmentOwner Owner { get; set; }
                = null!;

            public WorkAssignment Assignment { get; set; }
                = null!;

            public WorkAssignmentSegment Segment { get; set; }
                = null!;

            public DateTime SegmentEndDateTime { get; set; }

            public bool IsInsideScheduledPeriod { get; set; }

            public bool IsInsideGracePeriod { get; set; }

            public double DistanceFromSegmentStartMinutes
            {
                get;
                set;
            }
        }
    }
}