using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Models;

public sealed class CertificateModelBuilderExtensions :
    IEntityTypeConfiguration<CertificateRequest>,
    IEntityTypeConfiguration<IssuedCertificate>,
    IEntityTypeConfiguration<CertificateTemplate>,
    IEntityTypeConfiguration<CertificateAuditLog>
{
    public void Configure(EntityTypeBuilder<CertificateRequest> b)
    {
        b.ToTable("EC_CertificateRequests");
        b.HasKey(x => x.CertificateRequestId);
        b.Property(x => x.CertificateType).HasMaxLength(50).IsRequired();
        b.Property(x => x.Purpose).HasMaxLength(100).IsRequired();
        b.Property(x => x.PurposeDetails).HasMaxLength(500);
        b.Property(x => x.RequestScope).HasMaxLength(50).IsRequired().HasDefaultValue("FullCareer");
        b.Property(x => x.AddressedTo).HasMaxLength(200).HasDefaultValue("To Whom It May Concern");
        b.Property(x => x.EmployeeComments).HasMaxLength(1000);
        b.Property(x => x.Status).HasMaxLength(50).IsRequired().HasDefaultValue("Submitted");
        b.Property(x => x.ReviewerComments).HasMaxLength(1000);
        b.Property(x => x.ReturnedReason).HasMaxLength(1000);
        b.Property(x => x.RejectionReason).HasMaxLength(1000);

        b.HasIndex(x => new { x.IndividualId, x.Status });
        b.HasIndex(x => new { x.OrganisationId, x.Status });
        b.HasIndex(x => x.RequestId);

        b.HasOne(x => x.Request).WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Individual).WithMany().HasForeignKey(x => x.IndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Organisation).WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<IssuedCertificate> b)
    {
        b.ToTable("EC_IssuedCertificates");
        b.HasKey(x => x.IssuedCertificateId);
        b.Property(x => x.CertificateNumber).HasMaxLength(50).IsRequired();
        b.Property(x => x.VerificationToken).HasMaxLength(64).IsRequired();
        b.Property(x => x.CertificateType).HasMaxLength(50).IsRequired();
        b.Property(x => x.IssuingOfficerName).HasMaxLength(200).IsRequired();
        b.Property(x => x.IssuingOfficerTitle).HasMaxLength(200).IsRequired();
        b.Property(x => x.SignatoryName).HasMaxLength(200).IsRequired();
        b.Property(x => x.SignatoryTitle).HasMaxLength(200).IsRequired();
        b.Property(x => x.SnapshotJson).IsRequired();
        b.Property(x => x.Sha256Hash).HasMaxLength(64).IsRequired();
        b.Property(x => x.Status).HasMaxLength(50).IsRequired().HasDefaultValue("Active");
        b.Property(x => x.RevokedReason).HasMaxLength(500);

        b.HasIndex(x => x.CertificateNumber).IsUnique().HasDatabaseName("UQ_EC_IssuedCertificates_CertificateNumber");
        b.HasIndex(x => x.VerificationToken).IsUnique().HasDatabaseName("UQ_EC_IssuedCertificates_VerificationToken");
        b.HasIndex(x => new { x.IndividualId, x.Status });
        b.HasIndex(x => new { x.OrganisationId, x.Status });

        b.HasOne(x => x.CertificateRequest).WithMany(x => x.IssuedCertificates).HasForeignKey(x => x.CertificateRequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Individual).WithMany().HasForeignKey(x => x.IndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Organisation).WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<CertificateTemplate> b)
    {
        b.ToTable("EC_CertificateTemplates");
        b.HasKey(x => x.CertificateTemplateId);
        b.Property(x => x.CertificateType).HasMaxLength(50).IsRequired();
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.BodyIntroduction).HasMaxLength(1000);
        b.Property(x => x.FooterNotes).HasMaxLength(1000);
        b.Property(x => x.DefaultSignatoryName).HasMaxLength(200);
        b.Property(x => x.DefaultSignatoryTitle).HasMaxLength(200);

        b.HasIndex(x => new { x.OrganisationId, x.CertificateType });
        b.HasOne(x => x.Organisation).WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<CertificateAuditLog> b)
    {
        b.ToTable("EC_CertificateAuditLogs");
        b.HasKey(x => x.CertificateAuditLogId);
        b.Property(x => x.Action).HasMaxLength(50).IsRequired();
        b.Property(x => x.PerformedByUserName).HasMaxLength(200).IsRequired();
        b.Property(x => x.IpAddress).HasMaxLength(50);
        b.Property(x => x.Details).HasMaxLength(1000);

        b.HasIndex(x => x.CertificateRequestId);
        b.HasIndex(x => x.IssuedCertificateId);
        b.HasIndex(x => new { x.OrganisationId, x.Timestamp });

        b.HasOne(x => x.CertificateRequest).WithMany(x => x.AuditLogs).HasForeignKey(x => x.CertificateRequestId).OnDelete(DeleteBehavior.Restrict);
    }
}
