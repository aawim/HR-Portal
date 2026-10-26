using HRM.Components.Shared;
using HRM.DTOs;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services
{
    public class RequestService : IRequestService
    {
        private readonly IDbContextFactory<HrmTeContext> _dbFactory;
        private readonly IUserAccessService _access;
        private readonly ISequenceNumberService _sequence;

        public RequestService(
            IDbContextFactory<HrmTeContext> dbFactory,
            IUserAccessService access,
            ISequenceNumberService sequence )
        {
            _dbFactory = dbFactory;
            _access = access;
            _sequence = sequence;
        }

        public async Task<Request> CreateAsync(CreateRequestDto dto)
        {
            await using var db = await _dbFactory.CreateDbContextAsync();
            await using var transaction = await db.Database.BeginTransactionAsync();
            try
            {
                var request = await CreateAsync(db, dto);
                await transaction.CommitAsync();
                return request;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<Request> CreateAsync(HrmTeContext db, CreateRequestDto dto)
        {
            if (db.Database.CurrentTransaction == null)
                throw new InvalidOperationException("Request creation requires an active transaction.");

            var user = await _access.RequireContextAsync();

            var request = new Request
            {
                RequestTypeId = dto.RequestTypeId,

                RequestStateId = SharedConfig.RequestStates.PENDING_VERIFICATION,

                ReferenceNumber = await _sequence.GenerateRequestNumberAsync(db, dto.RequestTypeId),

                ApplicationDate = DateTime.Now,

                LastStateChangedDate = DateTime.Now,

                LastStateChangedByUserId = user.UserId,

                ApplicantBusinessEntityId =
                    dto.ApplicantBusinessEntityId,

                OrganisationBusinessEntityID =
                    dto.OrganisationBusinessEntityId,

                OperationLogId =
                    dto.OperationLogId,

                ServiceId =
                    dto.ServiceId,

                RequestEffectiveDate =
                    dto.EffectiveDate,

                CurrentApplicationTab =
                    dto.CurrentApplicationTab
            };

            db.Requests.Add(request);

            await db.SaveChangesAsync();

            return request;
        }

        public async Task<Request?> GetByIdAsync(int requestId)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync();

            var actor=await _access.RequireContextAsync();var org=await _access.GetCurrentOrganisationIdAsync();
            var allowed=actor.IsSuperAdministrator||await _access.HasPermissionAsync("Admin")||await _access.HasPermissionAsync("Requests.View");
            if(org<=0||!await _access.HasOrganisationAccessAsync(org))throw new UnauthorizedAccessException("Select your current organisation.");
            return await db.Requests.Include(x=>x.RequestState).Include(x=>x.RequestType)
                .SingleOrDefaultAsync(x=>x.RequestId==requestId&&x.OrganisationBusinessEntityID==org&&
                    (allowed||x.ApplicantBusinessEntityId==actor.IndividualId||db.WfRequestApprovals.Any(a=>a.ApproverUserId==actor.UserId&&a.RequestStageInstance.WorkflowRequestInstance.RequestId==x.RequestId)));
        }

        public async Task<bool> ChangeStateAsync(int requestId,int requestStateId,string? remarks=null)
        {
            _ = await GetByIdAsync(requestId) ?? throw new UnauthorizedAccessException("Request unavailable in this organisation.");
            throw new InvalidOperationException("Use the request's assigned approval workflow to perform this action.");
        }
    }
}
