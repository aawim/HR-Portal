using Intranet.Application.Services;
using Intranet.Domain.Entities;
using Intranet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace Intranet.Infrastructure.Services;
public class PostService : IPostService
{
    private readonly IntranetDbContext _db;
    public PostService(IntranetDbContext db) => _db = db;
    public async Task<IntranetPost> CreateAsync(IntranetPost post, CancellationToken ct = default)
    { post.CreatedDate = DateTime.UtcNow; _db.IntranetPosts.Add(post); await _db.SaveChangesAsync(ct); return post; }
    public async Task<IntranetPost?> GetByIdAsync(long id, long orgId, CancellationToken ct = default)
        => await _db.IntranetPosts.FirstOrDefaultAsync(p => p.IntranetPostID == id && p.OrganisationBusinessEntityID == orgId, ct);
    public async Task<IReadOnlyList<IntranetPost>> ListAsync(long orgId, int page, int pageSize, CancellationToken ct = default)
        => await _db.IntranetPosts.Where(p => p.OrganisationBusinessEntityID == orgId)
            .OrderByDescending(p => p.CreatedDate).Skip((page-1)*pageSize).Take(pageSize).ToListAsync(ct);
    public async Task<IntranetPost> UpdateAsync(IntranetPost post, CancellationToken ct = default)
    { post.UpdatedDate = DateTime.UtcNow; _db.IntranetPosts.Update(post); await _db.SaveChangesAsync(ct); return post; }
    public async Task PublishAsync(long id, long actorId, CancellationToken ct = default)
    {
        var p = await _db.IntranetPosts.FindAsync(new object[]{id}, ct);
        if (p is null) throw new KeyNotFoundException();
        p.Status = Domain.Enums.PostStatus.Published;
        p.PublishedAt = DateTime.UtcNow;
        p.ApprovedBy = actorId; p.ApprovedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
    public async Task ArchiveAsync(long id, long actorId, CancellationToken ct = default)
    {
        var p = await _db.IntranetPosts.FindAsync(new object[]{id}, ct);
        if (p is null) throw new KeyNotFoundException();
        p.Status = Domain.Enums.PostStatus.Archived;
        p.UpdatedBy = actorId; p.UpdatedDate = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }
}