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

            var user = await _access.RequireContextAsync();

            var request = new Request
            {
                RequestTypeId = dto.RequestTypeId,

                RequestStateId = SharedConfig.RequestStates.PENDING_VERIFICATION,

                ReferenceNumber = await _sequence.GenerateRequestNumberAsync(dto.RequestTypeId),

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

            var context =
                await _access.RequireContextAsync();

            var request =
                await db.Requests
                    .FirstOrDefaultAsync(x =>
                        x.RequestId == requestId);

            if (request == null)
                return false;

            request.RequestStateId = requestStateId;

            request.StateChangeRemarks = remarks;

            request.LastStateChangedByUserId =
                context.UserId;

            request.LastStateChangedDate =
                DateTime.Now;

            await db.SaveChangesAsync();

            return true;
        }

        private async Task<string> GenerateReferenceNumberAsync(
            HrmTeContext db,
            int requestTypeId)
        {
            var year = DateTime.Now.Year;

            var count = await db.Requests.CountAsync(x =>
                x.RequestTypeId == requestTypeId &&
                x.ApplicationDate.Year == year);

            return $"{requestTypeId}-{year}-{(count + 1):D6}";
        }

    }
}
