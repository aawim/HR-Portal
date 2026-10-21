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

            return await db.Requests
                .Include(x => x.RequestState)
                .Include(x => x.RequestType)
                .FirstOrDefaultAsync(x =>
                    x.RequestId == requestId);
        }

        public async Task<bool> ChangeStateAsync(
            int requestId,
            int requestStateId,
            string? remarks = null)
        {
            await using var db =
                await _dbFactory.CreateDbContextAsync();
            await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

            var context =
                await _access.RequireContextAsync();

            var request =
                await db.Requests
                    .FirstOrDefaultAsync(x =>
                        x.RequestId == requestId);

            if (request == null)
                return false;

            await RequestTransitionPolicy.RequireAllowedAsync(db, request.RequestTypeId, request.RequestStateId, requestStateId);
            request.RequestStateId = requestStateId;

            request.StateChangeRemarks = remarks;

            request.LastStateChangedByUserId =
                context.UserId;

            request.LastStateChangedDate =
                DateTime.Now;

            await db.SaveChangesAsync();

            await transaction.CommitAsync();
            return true;
        }

    

    }
}
