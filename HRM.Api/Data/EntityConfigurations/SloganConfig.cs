using HRM.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Models;

public partial class HrmTeContext
{
    public DbSet<Slogan> Slogans => Set<Slogan>();
    public DbSet<DocumentSloganAssignment> DocumentSloganAssignments => Set<DocumentSloganAssignment>();
}

public sealed class SloganMapping : IEntityTypeConfiguration<Slogan>
{
    public void Configure(EntityTypeBuilder<Slogan> b)
    {
        b.ToTable("SL_Slogans");
        b.HasKey(x => x.SloganId);
        b.Property(x => x.Name).HasMaxLength(120).IsRequired();
        b.Property(x => x.SloganType).HasMaxLength(20).IsRequired();
        b.Property(x => x.TextContent).HasMaxLength(500);
        b.Property(x => x.TextColor).HasMaxLength(30);
        b.Property(x => x.BackgroundColor).HasMaxLength(30);
        b.Property(x => x.BorderColor).HasMaxLength(30);
        b.Property(x => x.BoxStyle).HasMaxLength(30).IsRequired();
        b.Property(x => x.ImageContentType).HasMaxLength(100);
        b.Property(x => x.Placement).HasMaxLength(20).IsRequired();
        b.Property(x => x.Alignment).HasMaxLength(20).IsRequired();
        b.HasIndex(x => new { x.OrganisationId, x.IsActive, x.IsDefault });
    }
}

public sealed class DocumentSloganAssignmentMapping : IEntityTypeConfiguration<DocumentSloganAssignment>
{
    public void Configure(EntityTypeBuilder<DocumentSloganAssignment> b)
    {
        b.ToTable("SL_DocumentSlogans");
        b.HasKey(x => x.DocumentSloganAssignmentId);
        b.Property(x => x.DocumentType).HasMaxLength(60).IsRequired();
        b.HasIndex(x => new { x.OrganisationId, x.DocumentType }).IsUnique();
        b.HasOne(x => x.Slogan)
            .WithMany()
            .HasForeignKey(x => x.SloganId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
