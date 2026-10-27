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


        public async Task<WorkflowRequestContextDto> ResolveAsync(int requestId,int subjectJobId,
            CancellationToken cancellationToken = default)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync(cancellationToken);
            return await ResolveAsync(db, requestId, subjectJobId, cancellationToken);
        }

        public async Task<WorkflowRequestContextDto> ResolveAsync(
            HrmTeContext db, int requestId, int subjectJobId,
            CancellationToken cancellationToken = default)
        {
            if (requestId <= 0)
                throw new ArgumentOutOfRangeException(nameof(requestId));

            var request = await db.Requests
                       .AsNoTracking()
                       .FirstOrDefaultAsync(
                           x => x.RequestId == requestId,
                           cancellationToken);

            if (request == null)
            {
                throw new InvalidOperationException(
                    $"Request {requestId} was not found.");
            }

            if (!request.OrganisationBusinessEntityID.HasValue)
            {
                throw new InvalidOperationException(
                    $"Request {requestId} has no organisation.");
            }

            if (!request.ApplicantBusinessEntityId.HasValue)
            {
                throw new InvalidOperationException(
                    $"Request {requestId} has no applicant.");
            }


            var job = await db.Jobs
                .AsNoTracking()
                .Where(x => x.JobId == subjectJobId)
                .Select(x => new
                {
                    x.JobId,
                    x.JobTypeId,
                    x.OrganisationID,
                    x.IndividualID
                })
                .FirstOrDefaultAsync(cancellationToken);

            if (job == null)
            {
                throw new InvalidOperationException(
                    $"JobID {subjectJobId} was not found.");
            }

            if (job.OrganisationID != request.OrganisationBusinessEntityID)
            {
                throw new InvalidOperationException(
                    $"JobID {subjectJobId} does not belong to the request organisation.");
            }








            int? fallbackApprovalTypeId = null;
            int? leaveTypeId = null, leaveRequestKind = null;
            if (HRM.Helpers.LeaveChangeRequestTypes.IsChange(request.RequestTypeId))
            {
                var source = await db.LeaveChangeRequests.AsNoTracking()
                    .Where(x => x.RequestId == requestId && x.Leave.JobId == subjectJobId)
                    .Select(x => new { x.Leave.LeaveType.RequestTypeId, x.Leave.LeaveTypeId, x.LeaveChangeRequestTypeId })
                    .SingleOrDefaultAsync(cancellationToken);
                if (source == null)
                    throw new InvalidOperationException("The leave change is not linked to the supplied job.");
                if (request.RequestTypeId != HRM.Helpers.LeaveChangeRequestTypes.Legacy &&
                    HRM.Helpers.LeaveChangeRequestTypes.ForKind((HRM.DTOs.Leave.LeaveChangeKind)source.LeaveChangeRequestTypeId) != request.RequestTypeId)
                    throw new InvalidOperationException("The Request Type does not match the linked leave change.");
                fallbackApprovalTypeId = source.RequestTypeId;
                leaveTypeId = source.LeaveTypeId;
                leaveRequestKind = source.LeaveChangeRequestTypeId;
            }
            else
            {
                var application = await db.LeaveRequests.AsNoTracking()
                    .Where(x => x.RequestId == requestId && x.Leave.JobId == subjectJobId)
                    .Select(x => new { x.Leave.LeaveTypeId }).SingleOrDefaultAsync(cancellationToken);
                if (application != null) { leaveTypeId = application.LeaveTypeId; leaveRequestKind = 0; }
            }

            return new WorkflowRequestContextDto
            {
                RequestId = request.RequestId,

                RequestTypeId = request.RequestTypeId,
                LeaveTypeId = leaveTypeId,
                LeaveRequestKind = leaveRequestKind,
                FallbackApprovalRequestTypeId = fallbackApprovalTypeId,

                OrganisationBusinessEntityId =
                    request.OrganisationBusinessEntityID.Value,

                ApplicantBusinessEntityId =
                    request.ApplicantBusinessEntityId.Value,

                RequestStateId = request.RequestStateId,

                SubjectJobId = job.JobId,

                JobTypeId = job.JobTypeId,

                EffectiveDate = DateOnly.FromDateTime(
                    request.RequestEffectiveDate
                    ?? request.ApplicationDate)
            };
        }


 


     

        //public sealed class OutOfOfficeWorkflowContextRow
        //{
        //    public int RequestId { get; set; }

        //    public int RequestTypeId { get; set; }

        //    public int OrganisationBusinessEntityId { get; set; }

        //    public int ApplicantBusinessEntityId { get; set; }

        //    public int SubjectJobId { get; set; }

        //    public int? JobTypeId { get; set; }

        //    public DateTime EffectiveDate { get; set; }
        //}


    }
}
