using Intranet.Domain.Entities;
namespace Intranet.Application.Services;
public interface IPostService
{
    Task<IntranetPost> CreateAsync(IntranetPost post, CancellationToken ct = default);
    Task<IntranetPost?> GetByIdAsync(long id, long orgId, CancellationToken ct = default);
    Task<IReadOnlyList<IntranetPost>> ListAsync(long orgId, int page, int pageSize, CancellationToken ct = default);
    Task<IntranetPost> UpdateAsync(IntranetPost post, CancellationToken ct = default);
    Task PublishAsync(long id, long actorId, CancellationToken ct = default);
    Task ArchiveAsync(long id, long actorId, CancellationToken ct = default);
}