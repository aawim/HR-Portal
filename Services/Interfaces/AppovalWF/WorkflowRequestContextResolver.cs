using HRM.DTOs.ApprovalFlow;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services.Interfaces.AppovalWF
{
    public class WorkflowRequestContextResolver : IWorkflowRequestContextResolver
    {

        private readonly IDbContextFactory<HrmTeContext> _dbFactory;

        public WorkflowRequestContextResolver(
            IDbContextFactory<HrmTeContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }


        public async Task<WorkflowRequestContextDto> ResolveAsync(
       int requestId,
       CancellationToken cancellationToken = default)
        {
            if (requestId <= 0)
                throw new ArgumentOutOfRangeException(nameof(requestId));

            await using var db =
                await _dbFactory.CreateDbContextAsync(cancellationToken);

            var rows = await db.Database
                .SqlQueryRaw<LeaveWorkflowContextRow>(
                    """
                SELECT
                    r.RequestID AS RequestId,
                    r.RequestTypeID AS RequestTypeId,
                    r.OrganisationBusinessEntityID
                        AS OrganisationBusinessEntityId,
                    r.ApplicantBusinessEntityID
                        AS ApplicantBusinessEntityId,
                    l.JobID AS SubjectJobId,
                    j.JobTypeID AS JobTypeId,
                    CAST(
                        COALESCE(
                            r.RequestEffectiveDate,
                            r.ApplicationDate
                        ) AS date
                    ) AS EffectiveDate
                FROM dbo.Requests r
                INNER JOIN dbo.LeaveRequests lr
                    ON lr.RequestID = r.RequestID
                INNER JOIN dbo.Leaves l
                    ON l.LeaveID = lr.LeaveID
                INNER JOIN dbo.Jobs j
                    ON j.JobID = l.JobID
                WHERE r.RequestID = {0}
                """,
                    requestId)
                .ToListAsync(cancellationToken);

            if (rows.Count != 1)
            {
                throw new InvalidOperationException(
                    $"Request {requestId} does not have exactly one " +
                    "associated leave/job record. " +
                    "This resolver currently supports leave requests only.");
            }

            var row = rows[0];

            if (!row.OrganisationBusinessEntityId.HasValue ||
                !row.ApplicantBusinessEntityId.HasValue ||
                !row.JobTypeId.HasValue ||
                row.JobTypeId.Value <= 0)
            {
                throw new InvalidOperationException(
                    "Request organisation, applicant or job type is missing.");
            }

            return new WorkflowRequestContextDto
            {
                RequestId = row.RequestId,
                RequestTypeId = row.RequestTypeId,
                OrganisationBusinessEntityId =
                    row.OrganisationBusinessEntityId.Value,
                ApplicantBusinessEntityId =
                    row.ApplicantBusinessEntityId.Value,
                SubjectJobId = row.SubjectJobId,
                JobTypeId = row.JobTypeId.Value,
                EffectiveDate =
                    DateOnly.FromDateTime(row.EffectiveDate)
            };
        }

        private sealed class LeaveWorkflowContextRow
        {
            public int RequestId { get; set; }
            public int RequestTypeId { get; set; }
            public int? OrganisationBusinessEntityId { get; set; }
            public int? ApplicantBusinessEntityId { get; set; }
            public int SubjectJobId { get; set; }
            public int? JobTypeId { get; set; }
            public DateTime EffectiveDate { get; set; }
        }




    }
}
