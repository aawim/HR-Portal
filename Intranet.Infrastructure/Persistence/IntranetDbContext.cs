using Intranet.Domain.Entities;
using Microsoft.EntityFrameworkCore;
namespace Intranet.Infrastructure.Persistence;
public class IntranetDbContext : DbContext
{
    public IntranetDbContext(DbContextOptions<IntranetDbContext> options) : base(options) { }
    public DbSet<IntranetPost> IntranetPosts => Set<IntranetPost>();
    public DbSet<IntranetPostCategory> IntranetPostCategories => Set<IntranetPostCategory>();
    public DbSet<IntranetPostCategoryAssignment> IntranetPostCategoryAssignments => Set<IntranetPostCategoryAssignment>();
    public DbSet<IntranetPostAudience> IntranetPostAudiences => Set<IntranetPostAudience>();
    public DbSet<IntranetPostAttachment> IntranetPostAttachments => Set<IntranetPostAttachment>();
    public DbSet<IntranetNewJoinerProfile> IntranetNewJoinerProfiles => Set<IntranetNewJoinerProfile>();
    public DbSet<IntranetPostComment> IntranetPostComments => Set<IntranetPostComment>();
    public DbSet<IntranetPostReaction> IntranetPostReactions => Set<IntranetPostReaction>();
    public DbSet<IntranetPostAcknowledgement> IntranetPostAcknowledgements => Set<IntranetPostAcknowledgement>();
    public DbSet<IntranetPostReadState> IntranetPostReadStates => Set<IntranetPostReadState>();
    public DbSet<IntranetPublicationHistory> IntranetPublicationHistory => Set<IntranetPublicationHistory>();
    public DbSet<IntranetOutboxMessage> IntranetOutboxMessages => Set<IntranetOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<IntranetPost>().HasKey(x => x.IntranetPostID);
        b.Entity<IntranetPost>().Property(x => x.IntranetPostID).ValueGeneratedOnAdd();
        b.Entity<IntranetPost>().Property(x => x.RowVersion).IsRowVersion();
        b.Entity<IntranetPost>().HasQueryFilter(x => !x.IsDeleted);
        b.Entity<IntranetPostCategory>().HasKey(x => x.IntranetPostCategoryID);
        b.Entity<IntranetPostCategory>().Property(x => x.IntranetPostCategoryID).ValueGeneratedOnAdd();
        b.Entity<IntranetPostCategoryAssignment>().HasKey(x => new { x.IntranetPostID, x.IntranetPostCategoryID });
        b.Entity<IntranetPostAudience>().HasKey(x => x.IntranetPostAudienceID);
        b.Entity<IntranetPostAudience>().Property(x => x.IntranetPostAudienceID).ValueGeneratedOnAdd();
        b.Entity<IntranetPostAttachment>().HasKey(x => x.IntranetPostAttachmentID);
        b.Entity<IntranetPostAttachment>().Property(x => x.IntranetPostAttachmentID).ValueGeneratedOnAdd();
        b.Entity<IntranetNewJoinerProfile>().HasKey(x => x.IntranetNewJoinerProfileID);
        b.Entity<IntranetNewJoinerProfile>().Property(x => x.IntranetNewJoinerProfileID).ValueGeneratedOnAdd();
        b.Entity<IntranetPostComment>().HasKey(x => x.IntranetPostCommentID);
        b.Entity<IntranetPostComment>().Property(x => x.IntranetPostCommentID).ValueGeneratedOnAdd();
        b.Entity<IntranetPostReaction>().HasKey(x => x.IntranetPostReactionID);
        b.Entity<IntranetPostReaction>().Property(x => x.IntranetPostReactionID).ValueGeneratedOnAdd();
        b.Entity<IntranetPostAcknowledgement>().HasKey(x => x.IntranetPostAcknowledgementID);
        b.Entity<IntranetPostAcknowledgement>().Property(x => x.IntranetPostAcknowledgementID).ValueGeneratedOnAdd();
        b.Entity<IntranetPostReadState>().HasKey(x => new { x.IntranetPostID, x.IndividualID });
        b.Entity<IntranetPublicationHistory>().HasKey(x => x.IntranetPublicationHistoryID);
        b.Entity<IntranetPublicationHistory>().Property(x => x.IntranetPublicationHistoryID).ValueGeneratedOnAdd();
        b.Entity<IntranetOutboxMessage>().HasKey(x => x.OutboxMessageID);
        b.Entity<IntranetOutboxMessage>().Property(x => x.OutboxMessageID).ValueGeneratedOnAdd();
        b.Entity<IntranetPost>().HasIndex(x => new { x.OrganisationBusinessEntityID, x.Status });
        b.Entity<IntranetPost>().HasIndex(x => x.PublishedAt);
        b.Entity<IntranetPostAudience>().HasIndex(x => new { x.IntranetPostID, x.AudienceType });
        b.Entity<IntranetOutboxMessage>().HasIndex(x => x.Status);
    }
}