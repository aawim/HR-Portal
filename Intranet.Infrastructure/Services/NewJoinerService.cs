using Intranet.Application.Services;
using Intranet.Contracts.Events;
using Intranet.Domain.Entities;
using Intranet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
namespace Intranet.Infrastructure.Services;
public class NewJoinerService : INewJoinerService
{
    private readonly IntranetDbContext _db;
    public NewJoinerService(IntranetDbContext db) => _db = db;
    public async Task<IntranetPost> CreateDraftFromEventAsync(EmployeeJoinedIntegrationEvent evt, CancellationToken ct = default)
    {
        // Idempotency: skip if a new-joiner post already exists for this individual+org
        var existing = await _db.IntranetPosts
            .FirstOrDefaultAsync(p => p.OrganisationBusinessEntityID == evt.OrganisationBusinessEntityID
                && p.PostType == "NewJoiner" && p.NewJoinerProfile != null
                && p.NewJoinerProfile.IndividualID == evt.IndividualID, ct);
        if (existing is not null) return existing;

        var post = new IntranetPost
        {
            OrganisationBusinessEntityID = evt.OrganisationBusinessEntityID,
            PostType = "NewJoiner",
            Title = "Welcome new colleague",
            Status = Domain.Enums.PostStatus.Draft,
            CreatedBy = 0,
            CreatedDate = DateTime.UtcNow,
            NewJoinerProfile = new IntranetNewJoinerProfile
            {
                IndividualID = evt.IndividualID,
                JobID = evt.JobID,
                JoiningDate = evt.JoiningDate,
                ConsentStatus = "Pending"
            }
        };
        _db.IntranetPosts.Add(post);
        await _db.SaveChangesAsync(ct);
        return post;
    }
}