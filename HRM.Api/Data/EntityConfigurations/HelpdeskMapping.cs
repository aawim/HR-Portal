using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Models;

public sealed class HelpdeskTicketMapping : IEntityTypeConfiguration<HelpdeskTicket>
{
    public void Configure(EntityTypeBuilder<HelpdeskTicket> b)
    {
        b.ToTable("HD_HelpdeskTickets");
        b.HasKey(x => x.Id);
        b.Property(x => x.TicketNumber).HasMaxLength(32).IsRequired();
        b.Property(x => x.Subject).HasMaxLength(200).IsRequired();
        b.Property(x => x.Category).HasMaxLength(60).IsRequired();
        b.Property(x => x.Status).HasMaxLength(32).IsRequired();
        b.Property(x => x.Priority).HasMaxLength(20).IsRequired();
        b.Property(x => x.EscalationReason).HasMaxLength(300);

        b.HasIndex(x => x.TicketNumber).IsUnique();
        b.HasIndex(x => new { x.RequesterUserId, x.UpdatedAtUtc });
        b.HasIndex(x => new { x.Status, x.UpdatedAtUtc });
        b.HasIndex(x => new { x.OrganisationId, x.Status });

        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.RequesterUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.AssignedAgentUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class HelpdeskMessageMapping : IEntityTypeConfiguration<HelpdeskMessage>
{
    public void Configure(EntityTypeBuilder<HelpdeskMessage> b)
    {
        b.ToTable("HD_HelpdeskMessages");
        b.HasKey(x => x.Id);
        b.Property(x => x.SenderType).HasMaxLength(16).IsRequired();
        b.Property(x => x.SenderName).HasMaxLength(150).IsRequired();
        b.Property(x => x.Body).HasMaxLength(4000).IsRequired();
        b.Property(x => x.Confidence).HasPrecision(5, 4);

        b.HasIndex(x => new { x.TicketId, x.Id });

        b.HasOne(x => x.Ticket).WithMany(x => x.Messages).HasForeignKey(x => x.TicketId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne<User>().WithMany().HasForeignKey(x => x.SenderUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class HelpdeskArticleMapping : IEntityTypeConfiguration<HelpdeskArticle>
{
    public void Configure(EntityTypeBuilder<HelpdeskArticle> b)
    {
        b.ToTable("HD_HelpdeskArticles");
        b.HasKey(x => x.Id);
        b.Property(x => x.Category).HasMaxLength(60).IsRequired();
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Keywords).HasMaxLength(500).IsRequired();
        b.Property(x => x.Answer).HasMaxLength(4000).IsRequired();

        b.HasIndex(x => new { x.IsActive, x.Category, x.SortOrder });
    }
}

public partial class HrmTeContext
{
    public DbSet<HelpdeskTicket> HelpdeskTickets => Set<HelpdeskTicket>();
    public DbSet<HelpdeskMessage> HelpdeskMessages => Set<HelpdeskMessage>();
    public DbSet<HelpdeskArticle> HelpdeskArticles => Set<HelpdeskArticle>();
}
