using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;

public sealed class StaffChatConversation
{
    public long Id { get; set; }
    public int OrganisationId { get; set; }
    public bool IsGroup { get; set; }
    public string? Title { get; set; }
    public int FirstUserId { get; set; }
    public int SecondUserId { get; set; }
    public long FirstReadMessageId { get; set; }
    public long SecondReadMessageId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public ICollection<StaffChatMembership> Members { get; set; } = new List<StaffChatMembership>();
    public ICollection<StaffChatMessage> Messages { get; set; } = new List<StaffChatMessage>();
}
public sealed class StaffChatMessage
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public int SenderUserId { get; set; }
    public Guid ClientMessageId { get; set; }
    public string Body { get; set; } = "";
    public int? RequestId { get; set; }
    public DateTime SentAtUtc { get; set; }
    public StaffChatConversation Conversation { get; set; } = null!;
}
public sealed class StaffChatConversationMapping : IEntityTypeConfiguration<StaffChatConversation>
{
    public void Configure(EntityTypeBuilder<StaffChatConversation> b)
    {
        b.ToTable("CH_Conversations", t => t.HasCheckConstraint("CK_CH_Conversations_Participants", "[FirstUserId] < [SecondUserId]"));
        b.HasKey(x => x.Id); b.Property(x => x.Title).HasMaxLength(120);
        b.HasIndex(x => new { x.OrganisationId, x.FirstUserId, x.SecondUserId }).IsUnique().HasFilter("[IsGroup] = 0");
        b.HasIndex(x => new { x.OrganisationId, x.UpdatedAtUtc });
        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.FirstUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.SecondUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class StaffChatMembership
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public int UserId { get; set; }
    public bool IsManager { get; set; }
    public DateTime JoinedAtUtc { get; set; }
    public int? AddedByUserId { get; set; }
    public long JoinedAfterMessageId { get; set; }
    public long LastReadMessageId { get; set; }
    public DateTime? LeftAtUtc { get; set; }
    public int? RemovedByUserId { get; set; }
    public long? LeftAfterMessageId { get; set; }
    public StaffChatConversation Conversation { get; set; } = null!;
}
public sealed class StaffChatMembershipMapping : IEntityTypeConfiguration<StaffChatMembership>
{
    public void Configure(EntityTypeBuilder<StaffChatMembership> b)
    {
        b.ToTable("CH_Memberships"); b.HasKey(x => x.Id);
        b.HasIndex(x => new { x.ConversationId, x.UserId }).IsUnique().HasFilter("[LeftAtUtc] IS NULL");
        b.HasIndex(x => new { x.UserId, x.ConversationId, x.LeftAtUtc });
        b.HasOne(x => x.Conversation).WithMany(x => x.Members).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.AddedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.RemovedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class StaffChatEvent
{
    public long Id { get; set; }
    public long ConversationId { get; set; }
    public int ActorUserId { get; set; }
    public int? SubjectUserId { get; set; }
    public string Action { get; set; } = "";
    public string? Detail { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
public sealed class StaffChatEventMapping : IEntityTypeConfiguration<StaffChatEvent>
{
    public void Configure(EntityTypeBuilder<StaffChatEvent> b)
    {
        b.ToTable("CH_Events"); b.HasKey(x => x.Id); b.Property(x => x.Action).HasMaxLength(24); b.Property(x => x.Detail).HasMaxLength(120);
        b.HasIndex(x => new { x.ConversationId, x.Id });
        b.HasOne<StaffChatConversation>().WithMany().HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.SubjectUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
public sealed class StaffChatMessageMapping : IEntityTypeConfiguration<StaffChatMessage>
{
    public void Configure(EntityTypeBuilder<StaffChatMessage> b)
    {
        b.ToTable("CH_Messages"); b.HasKey(x => x.Id); b.Property(x => x.Body).HasMaxLength(4000);
        b.HasIndex(x => new { x.ConversationId, x.Id });
        b.HasIndex(x => new { x.SenderUserId, x.ClientMessageId }).IsUnique();
        b.HasIndex(x => new { x.SenderUserId, x.SentAtUtc });
        b.HasOne(x => x.Conversation).WithMany(x => x.Messages).HasForeignKey(x => x.ConversationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.SenderUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<Request>().WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
    }
}
public partial class HrmTeContext
{
    public DbSet<StaffChatConversation> StaffChatConversations => Set<StaffChatConversation>();
    public DbSet<StaffChatEvent> StaffChatEvents => Set<StaffChatEvent>();
    public DbSet<StaffChatMembership> StaffChatMemberships => Set<StaffChatMembership>();
    public DbSet<StaffChatMessage> StaffChatMessages => Set<StaffChatMessage>();
}
