using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;
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
