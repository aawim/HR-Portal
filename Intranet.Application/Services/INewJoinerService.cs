using Intranet.Contracts.Events;
using Intranet.Domain.Entities;
namespace Intranet.Application.Services;
public interface INewJoinerService
{
    Task<IntranetPost> CreateDraftFromEventAsync(EmployeeJoinedIntegrationEvent evt, CancellationToken ct = default);
}