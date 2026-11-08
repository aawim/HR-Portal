using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Models;

public sealed class DocumentManagementModelBuilderExtensions :
    IEntityTypeConfiguration<DmsCategory>,
    IEntityTypeConfiguration<DmsDocumentType>,
    IEntityTypeConfiguration<DmsRetentionPolicy>,
    IEntityTypeConfiguration<DmsStoredObject>,
    IEntityTypeConfiguration<DmsDocument>,
    IEntityTypeConfiguration<DmsDocumentVersion>,
    IEntityTypeConfiguration<DmsDocumentLink>,
    IEntityTypeConfiguration<DmsDocumentPermission>,
    IEntityTypeConfiguration<DmsDocumentEvent>,
    IEntityTypeConfiguration<DmsDocumentTemplate>,
    IEntityTypeConfiguration<DmsDocumentRequest>,
    IEntityTypeConfiguration<DmsLegalHold>,
    IEntityTypeConfiguration<DmsDocumentSignature>
{
    public void Configure(EntityTypeBuilder<DmsCategory> b)
    {
        b.ToTable("DMS_Categories");
        b.HasKey(x => x.CategoryId);
        b.Property(x => x.Code).HasMaxLength(60).IsRequired();
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.TargetDomain).HasMaxLength(50).IsRequired().HasDefaultValue("General");

        b.HasIndex(x => new { x.OrganisationId, x.Code }).IsUnique();
        b.HasOne(x => x.Organisation).WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<DmsDocumentType> b)
    {
        b.ToTable("DMS_DocumentTypes");
        b.HasKey(x => x.DmsDocumentTypeId);
        b.Property(x => x.Code).HasMaxLength(60).IsRequired();
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.NameDhivehi).HasMaxLength(150);
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.DefaultClassification).HasMaxLength(40).IsRequired().HasDefaultValue("Internal");
        b.Property(x => x.DefaultAccessScope).HasMaxLength(40).IsRequired().HasDefaultValue("EmployeeAndHr");
        b.Property(x => x.PermittedExtensions).HasMaxLength(200).IsRequired().HasDefaultValue(".pdf,.png,.jpg,.jpeg,.docx,.xlsx");

        b.HasIndex(x => new { x.OrganisationId, x.Code }).IsUnique();
        b.HasIndex(x => new { x.CategoryId, x.IsActive });

        b.HasOne(x => x.Category).WithMany(x => x.DocumentTypes).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.LegacyDocumentType).WithMany().HasForeignKey(x => x.LegacyDocumentTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Organisation).WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.RetentionPolicy).WithMany().HasForeignKey(x => x.RetentionPolicyId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<DmsRetentionPolicy> b)
    {
        b.ToTable("DMS_RetentionPolicies");
        b.HasKey(x => x.RetentionPolicyId);
        b.Property(x => x.Code).HasMaxLength(60).IsRequired();
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.TriggerEvent).HasMaxLength(40).IsRequired().HasDefaultValue("CreationDate");
        b.Property(x => x.DispositionAction).HasMaxLength(40).IsRequired().HasDefaultValue("PermanentKeep");
        b.Property(x => x.PolicyReferenceNumber).HasMaxLength(100);

        b.HasIndex(x => new { x.OrganisationId, x.Code }).IsUnique();
        b.HasOne(x => x.Organisation).WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<DmsStoredObject> b)
    {
        b.ToTable("DMS_StoredObjects");
        b.HasKey(x => x.StoredObjectId);
        b.Property(x => x.StorageKey).HasMaxLength(80).IsRequired();
        b.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
        b.Property(x => x.FileExtension).HasMaxLength(20).IsRequired();
        b.Property(x => x.Sha256Hash).HasMaxLength(64).IsRequired();
        b.Property(x => x.StorageProvider).HasMaxLength(50).IsRequired().HasDefaultValue("SqlServerPrivateStore");
        b.Property(x => x.MalwareScanStatus).HasMaxLength(30).IsRequired().HasDefaultValue("Clean");
        b.Property(x => x.MalwareScanDetails).HasMaxLength(500);

        b.HasIndex(x => x.StorageKey).IsUnique().HasDatabaseName("UQ_DMS_StoredObjects_StorageKey");
        b.HasIndex(x => new { x.OrganisationId, x.Sha256Hash });

        b.HasOne(x => x.Organisation).WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<DmsDocument> b)
    {
        b.ToTable("DMS_Documents");
        b.HasKey(x => x.DocumentId);
        b.Property(x => x.DocumentNumber).HasMaxLength(60).IsRequired();
        b.Property(x => x.Title).HasMaxLength(250).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.ReferenceCode).HasMaxLength(100);
        b.Property(x => x.Classification).HasMaxLength(40).IsRequired().HasDefaultValue("Internal");
        b.Property(x => x.AccessScope).HasMaxLength(40).IsRequired().HasDefaultValue("EmployeeAndHr");
        b.Property(x => x.Status).HasMaxLength(40).IsRequired().HasDefaultValue("Active");
        b.Property(x => x.CheckedOutByUserName).HasMaxLength(200);
        b.Property(x => x.CheckOutNote).HasMaxLength(500);
        b.Property(x => x.Tags).HasMaxLength(500);
        b.Property(x => x.VerifiedByUserName).HasMaxLength(200);
        b.Property(x => x.VerificationNotes).HasMaxLength(500);
        b.Property(x => x.ApprovedByUserName).HasMaxLength(200);
        b.Property(x => x.RevokedReason).HasMaxLength(500);
        b.Property(x => x.DisposalCertificateRef).HasMaxLength(100);
        b.Property(x => x.CreatedByUserName).HasMaxLength(200).IsRequired();
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => x.DocumentNumber).IsUnique().HasDatabaseName("UQ_DMS_Documents_DocumentNumber");
        b.HasIndex(x => new { x.OrganisationId, x.Status, x.IsDeleted });
        b.HasIndex(x => new { x.OrganisationId, x.SubjectIndividualId, x.IsDeleted });
        b.HasIndex(x => new { x.OrganisationId, x.CategoryId, x.DmsDocumentTypeId });

        b.HasOne(x => x.Organisation).WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.SubjectIndividual).WithMany().HasForeignKey(x => x.SubjectIndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Category).WithMany(x => x.Documents).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DocumentType).WithMany(x => x.Documents).HasForeignKey(x => x.DmsDocumentTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.RetentionPolicy).WithMany().HasForeignKey(x => x.RetentionPolicyId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.LinkedRequest).WithMany().HasForeignKey(x => x.LinkedRequestId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<DmsDocumentVersion> b)
    {
        b.ToTable("DMS_DocumentVersions");
        b.HasKey(x => x.DocumentVersionId);
        b.Property(x => x.VersionLabel).HasMaxLength(30).IsRequired();
        b.Property(x => x.OriginalFileName).HasMaxLength(260).IsRequired();
        b.Property(x => x.ContentType).HasMaxLength(120).IsRequired();
        b.Property(x => x.FileExtension).HasMaxLength(20).IsRequired();
        b.Property(x => x.Sha256Hash).HasMaxLength(64).IsRequired();
        b.Property(x => x.ChangeComments).HasMaxLength(1000);
        b.Property(x => x.CreatedByUserName).HasMaxLength(200).IsRequired();

        b.HasIndex(x => new { x.DocumentId, x.VersionNumber }).IsUnique();
        b.HasIndex(x => new { x.DocumentId, x.IsCurrentVersion });

        b.HasOne(x => x.Document).WithMany(x => x.Versions).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StoredObject).WithMany().HasForeignKey(x => x.StoredObjectId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<DmsDocumentLink> b)
    {
        b.ToTable("DMS_DocumentLinks");
        b.HasKey(x => x.DocumentLinkId);
        b.Property(x => x.TargetEntityType).HasMaxLength(50).IsRequired();
        b.Property(x => x.TargetReferenceCode).HasMaxLength(100);
        b.Property(x => x.TargetDisplayName).HasMaxLength(250);
        b.Property(x => x.LinkRelationship).HasMaxLength(50).IsRequired().HasDefaultValue("Primary");
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.LinkedByUserName).HasMaxLength(200).IsRequired();

        b.HasIndex(x => new { x.OrganisationId, x.TargetEntityType, x.TargetEntityId, x.IsActive });
        b.HasIndex(x => new { x.DocumentId, x.TargetEntityType, x.TargetEntityId });

        b.HasOne(x => x.Document).WithMany(x => x.Links).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Organisation).WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<DmsDocumentPermission> b)
    {
        b.ToTable("DMS_DocumentPermissions");
        b.HasKey(x => x.DocumentPermissionId);
        b.Property(x => x.PrincipalType).HasMaxLength(40).IsRequired();
        b.Property(x => x.PrincipalName).HasMaxLength(200);
        b.Property(x => x.PermissionLevel).HasMaxLength(40).IsRequired();
        b.Property(x => x.GrantReason).HasMaxLength(500);
        b.Property(x => x.GrantedByUserName).HasMaxLength(200).IsRequired();

        b.HasIndex(x => new { x.DocumentId, x.IsActive });
        b.HasIndex(x => new { x.OrganisationId, x.PrincipalType, x.PrincipalId });

        b.HasOne(x => x.Document).WithMany(x => x.Permissions).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<DmsDocumentEvent> b)
    {
        b.ToTable("DMS_DocumentEvents");
        b.HasKey(x => x.DocumentEventId);
        b.Property(x => x.EventType).HasMaxLength(50).IsRequired();
        b.Property(x => x.EventDescription).HasMaxLength(1000).IsRequired();
        b.Property(x => x.PerformedByUserName).HasMaxLength(200).IsRequired();
        b.Property(x => x.IpAddress).HasMaxLength(60);

        b.HasIndex(x => new { x.DocumentId, x.OccurredAtUtc });
        b.HasIndex(x => new { x.OrganisationId, x.OccurredAtUtc });

        b.HasOne(x => x.Document).WithMany(x => x.Events).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<DmsDocumentTemplate> b)
    {
        b.ToTable("DMS_DocumentTemplates");
        b.HasKey(x => x.DocumentTemplateId);
        b.Property(x => x.Code).HasMaxLength(60).IsRequired();
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.DocumentTitlePattern).HasMaxLength(250).IsRequired();
        b.Property(x => x.HeaderText).HasMaxLength(1000);
        b.Property(x => x.BodyTemplate).IsRequired();
        b.Property(x => x.FooterText).HasMaxLength(1000);
        b.Property(x => x.SignatoryName).HasMaxLength(200);
        b.Property(x => x.SignatoryTitle).HasMaxLength(200);

        b.HasIndex(x => new { x.OrganisationId, x.Code, x.VersionNumber }).IsUnique();
        b.HasOne(x => x.Organisation).WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.DocumentType).WithMany().HasForeignKey(x => x.DmsDocumentTypeId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<DmsDocumentRequest> b)
    {
        b.ToTable("DMS_DocumentRequests");
        b.HasKey(x => x.DocumentRequestId);
        b.Property(x => x.RequestKind).HasMaxLength(60).IsRequired();
        b.Property(x => x.Purpose).HasMaxLength(200).IsRequired();
        b.Property(x => x.AddressedTo).HasMaxLength(200).HasDefaultValue("To Whom It May Concern");
        b.Property(x => x.EmployeeNotes).HasMaxLength(1000);
        b.Property(x => x.Status).HasMaxLength(40).IsRequired().HasDefaultValue("Submitted");
        b.Property(x => x.IssuedDocumentNumber).HasMaxLength(60);
        b.Property(x => x.VerificationToken).HasMaxLength(64);
        b.Property(x => x.ReviewedByUserName).HasMaxLength(200);
        b.Property(x => x.ReviewComments).HasMaxLength(1000);
        b.Property(x => x.ApprovedByUserName).HasMaxLength(200);

        b.HasIndex(x => x.RequestId);
        b.HasIndex(x => new { x.OrganisationId, x.Status });
        b.HasIndex(x => new { x.ApplicantIndividualId, x.Status });

        b.HasOne(x => x.Request).WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Organisation).WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ApplicantIndividual).WithMany().HasForeignKey(x => x.ApplicantIndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DocumentType).WithMany().HasForeignKey(x => x.DmsDocumentTypeId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.DocumentTemplate).WithMany().HasForeignKey(x => x.DocumentTemplateId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.IssuedDocument).WithMany().HasForeignKey(x => x.IssuedDocumentId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<DmsLegalHold> b)
    {
        b.ToTable("DMS_LegalHolds");
        b.HasKey(x => x.LegalHoldId);
        b.Property(x => x.HoldReference).HasMaxLength(80).IsRequired();
        b.Property(x => x.CaseOrMatterName).HasMaxLength(200).IsRequired();
        b.Property(x => x.HoldReason).HasMaxLength(1000).IsRequired();
        b.Property(x => x.PlacedByUserName).HasMaxLength(200).IsRequired();
        b.Property(x => x.ReleasedByUserName).HasMaxLength(200);
        b.Property(x => x.ReleaseNotes).HasMaxLength(1000);

        b.HasIndex(x => new { x.DocumentId, x.IsActive });
        b.HasIndex(x => new { x.OrganisationId, x.IsActive });

        b.HasOne(x => x.Document).WithMany(x => x.LegalHolds).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Organisation).WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<DmsDocumentSignature> b)
    {
        b.ToTable("DMS_DocumentSignatures");
        b.HasKey(x => x.DocumentSignatureId);
        b.Property(x => x.SignerName).HasMaxLength(200).IsRequired();
        b.Property(x => x.SignerTitle).HasMaxLength(200).IsRequired();
        b.Property(x => x.SignatureRole).HasMaxLength(50).IsRequired();
        b.Property(x => x.SignatureStatus).HasMaxLength(30).IsRequired();
        b.Property(x => x.ContentSha256AtSigning).HasMaxLength(64).IsRequired();
        b.Property(x => x.SignatureDigest).HasMaxLength(128).IsRequired();
        b.Property(x => x.Remarks).HasMaxLength(500);

        b.HasIndex(x => new { x.DocumentId, x.DocumentVersionId });
        b.HasIndex(x => new { x.OrganisationId, x.SignedAtUtc });

        b.HasOne(x => x.Document).WithMany(x => x.Signatures).HasForeignKey(x => x.DocumentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DocumentVersion).WithMany().HasForeignKey(x => x.DocumentVersionId).OnDelete(DeleteBehavior.Restrict);
    }
}
