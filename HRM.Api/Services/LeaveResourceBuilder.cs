using HRM.Components.Shared;
using HRM.Models;
using HRM.Resources;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services
{
    public class LeaveResourceBuilder : ILeaveResourceBuilder
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;

        public LeaveResourceBuilder(IDbContextFactory<HrmTeContext> dbFactory)
        {
            _dbFactory = dbFactory;
        }

        public async Task<LeaveResource?> BuildAsync(int requestId)
        {
            using var db = await _dbFactory.CreateDbContextAsync();

            var leaveRequest = await db.LeaveRequests
                .AsNoTracking()
                .Include(x => x.Leave)
                .FirstOrDefaultAsync(x => x.RequestId == requestId);

            if (leaveRequest == null)
                return null;

            var job = await db.Jobs
                .AsNoTracking()
                .Where(x => x.JobId == leaveRequest.Leave.JobId)
                .Select(x => new
                {
                    x.JobId,
                    x.IndividualID,
                    x.OrganisationID,
                    x.OrganisationStructureId
                })
                .FirstOrDefaultAsync();

            if (job == null)
                return null;

            return new LeaveResource
            {
                LeaveRequestID = leaveRequest.RequestId,

                LeaveID = leaveRequest.LeaveId,

                IndividualID = job.IndividualID,

                OrganisationID = job.OrganisationID,

                JobID = job.JobId,

                OrganisationStructureID = job.OrganisationStructureId,

                LeaveTypeID = leaveRequest.Leave.LeaveTypeId,

                LeaveStateID = leaveRequest.Leave.LeaveStateId,

                FromDate = leaveRequest.Leave.FromDate,

                ToDate = leaveRequest.Leave.ToDate,

                IsApproved =
                    leaveRequest.Leave.LeaveStateId ==
                    SharedConfig.LeaveStates.APPROVED,

                IsRejected =
                    leaveRequest.Leave.LeaveStateId ==
                    SharedConfig.LeaveStates.REJECTED,

                IsCancelled =
                    leaveRequest.Leave.LeaveStateId ==
                    SharedConfig.LeaveStates.CANCELLED
            };
        }



    }
}
