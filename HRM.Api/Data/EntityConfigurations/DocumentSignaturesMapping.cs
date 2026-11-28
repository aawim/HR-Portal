using HRM.Models;
using HRM.Models.Signatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Api.Data.EntityConfigurations;

public sealed class UserSignaturesMapping : IEntityTypeConfiguration<UserSignature>
{
    public void Configure(EntityTypeBuilder<UserSignature> b)
    {
        b.ToTable("DS_UserSignatures");
        b.HasKey(x => x.Id);
        b.Property(x => x.SignatureDataUrl).IsRequired();
        b.Property(x => x.ContentSha256).HasMaxLength(64).IsRequired();
        b.Property(x => x.PinHash).HasMaxLength(128);
        b.Property(x => x.PinSalt).HasMaxLength(64);
        b.HasIndex(x => x.UserId).IsUnique();
    }
}

public sealed class DocumentSignaturesMapping : IEntityTypeConfiguration<DocumentSignature>
{
    public void Configure(EntityTypeBuilder<DocumentSignature> b)
    {
        b.ToTable("DS_DocumentSignatures");
        b.HasKey(x => x.Id);
        b.Property(x => x.DocumentType).HasMaxLength(80).IsRequired();
        b.Property(x => x.DocumentId).HasMaxLength(80).IsRequired();
        b.Property(x => x.SlotKey).HasMaxLength(80).IsRequired();
        b.Property(x => x.SlotLabel).HasMaxLength(150).IsRequired();
        b.Property(x => x.SignerName).HasMaxLength(250).IsRequired();
        b.Property(x => x.SignerPosition).HasMaxLength(250);
        b.Property(x => x.SignatureImageDataUrl).IsRequired();
        b.Property(x => x.DocumentContentHash).HasMaxLength(64).IsRequired();
        b.Property(x => x.VerificationCode).HasMaxLength(40).IsRequired();
        b.Property(x => x.IpAddress).HasMaxLength(64);
        b.Property(x => x.Status).HasMaxLength(30).IsRequired();
        b.Property(x => x.RevokeReason).HasMaxLength(500);
        b.HasIndex(x => x.VerificationCode).IsUnique();
        b.HasIndex(x => new { x.OrganisationId, x.DocumentType, x.DocumentId, x.SlotKey });
        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
    }
}
