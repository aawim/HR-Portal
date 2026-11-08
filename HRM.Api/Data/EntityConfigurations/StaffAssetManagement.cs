using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Models;

public sealed class StaffAssetManagementMapping :
    IEntityTypeConfiguration<AssetCategory>,
    IEntityTypeConfiguration<AssetCondition>,
    IEntityTypeConfiguration<AssetItem>,
    IEntityTypeConfiguration<AssetIssuance>,
    IEntityTypeConfiguration<AssetIssuanceItem>,
    IEntityTypeConfiguration<AssetAcknowledgement>,
    IEntityTypeConfiguration<AssetHrDecision>,
    IEntityTypeConfiguration<AssetCustody>,
    IEntityTypeConfiguration<AssetReturn>,
    IEntityTypeConfiguration<AssetReturnItem>,
    IEntityTypeConfiguration<AssetTransferDisposition>,
    IEntityTypeConfiguration<AssetEvent>,
    IEntityTypeConfiguration<AssetAttachment>,
    IEntityTypeConfiguration<AssetDocument>
{
    public void Configure(EntityTypeBuilder<AssetCategory> b)
    {
        b.ToTable("AM_AssetCategories");
        b.HasKey(x => x.AssetCategoryId);
        b.Property(x => x.Code).HasMaxLength(50);
        b.Property(x => x.Name).HasMaxLength(150);
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2");

        b.HasIndex(x => new { x.OrganisationId, x.Code }).IsUnique();
        b.HasIndex(x => new { x.OrganisationId, x.Name }).IsUnique();

        b.HasOne(x => x.Organisation).WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.OperationLog).WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<AssetCondition> b)
    {
        b.ToTable("AM_AssetConditions");
        b.HasKey(x => x.AssetConditionId);
        b.Property(x => x.Code).HasMaxLength(40);
        b.Property(x => x.Name).HasMaxLength(100);
        b.Property(x => x.Description).HasMaxLength(500);

        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => x.Name).IsUnique();
    }

    public void Configure(EntityTypeBuilder<AssetItem> b)
    {
        b.ToTable("AM_AssetItems", t =>
            t.HasCheckConstraint("CK_AM_AssetItems_Status",
                "[Status] IN ('Available','Reserved','PendingHandover','Assigned','ReturnPending','Maintenance','Lost','Disposed')"));

        b.HasKey(x => x.AssetItemId);
        b.Property(x => x.AssetCode).HasMaxLength(100);
        b.Property(x => x.SerialNumber).HasMaxLength(120);
        b.Property(x => x.Name).HasMaxLength(200);
        b.Property(x => x.Brand).HasMaxLength(120);
        b.Property(x => x.Model).HasMaxLength(120);
        b.Property(x => x.Location).HasMaxLength(200);
        b.Property(x => x.Status).HasMaxLength(30);
        b.Property(x => x.WarrantyExpiryDate).HasColumnType("date");
        b.Property(x => x.DefaultAccessories).HasMaxLength(500);
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => x.AssetCode).IsUnique().HasDatabaseName("UX_AM_AssetItems_AssetCode");
        b.HasIndex(x => new { x.OwnerOrganisationId, x.SerialNumber })
            .IsUnique()
            .HasFilter("[SerialNumber] IS NOT NULL AND [SerialNumber] <> ''")
            .HasDatabaseName("UX_AM_AssetItems_SerialNumber");
        b.HasIndex(x => new { x.OwnerOrganisationId, x.Status, x.CategoryId });

        b.HasOne(x => x.Category).WithMany(x => x.Assets).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.OwnerOrganisation).WithMany().HasForeignKey(x => x.OwnerOrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.OwnerStructure).WithMany().HasForeignKey(x => x.OwnerStructureId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CurrentCondition).WithMany().HasForeignKey(x => x.CurrentConditionId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.OperationLog).WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<AssetIssuance> b)
    {
        b.ToTable("AM_AssetIssuances", t =>
            t.HasCheckConstraint("CK_AM_AssetIssuances_Status",
                "[Status] IN ('Draft','AwaitingHandover','PendingStaffAcknowledgement','PendingHRApproval','ReturnedForCorrection','Completed','PartiallyCompleted','Cancelled')"));

        b.HasKey(x => x.AssetIssuanceId);
        b.Property(x => x.ReferenceNumber).HasMaxLength(60);
        b.Property(x => x.Reason).HasMaxLength(1000);
        b.Property(x => x.Status).HasMaxLength(40);
        b.Property(x => x.IssueDate).HasColumnType("date");
        b.Property(x => x.SubmittedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.FinalizedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.FinalizationIdempotencyKey).HasMaxLength(80);
        b.Property(x => x.EmployeeNameSnapshot).HasMaxLength(250);
        b.Property(x => x.StaffNumberSnapshot).HasMaxLength(60);
        b.Property(x => x.PositionTitleSnapshot).HasMaxLength(250);
        b.Property(x => x.DepartmentNameSnapshot).HasMaxLength(250);
        b.Property(x => x.OrganisationNameSnapshot).HasMaxLength(250);
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => x.ReferenceNumber).IsUnique();
        b.HasIndex(x => x.RequestId).IsUnique().HasFilter("[RequestId] IS NOT NULL");
        b.HasIndex(x => new { x.IndividualId, x.Status }).HasDatabaseName("IX_AM_AssetIssuances_Staff_Status");
        b.HasIndex(x => new { x.OrganisationId, x.Status, x.IssueDate });

        b.HasOne(x => x.Request).WithOne().HasForeignKey<AssetIssuance>(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Individual).WithMany().HasForeignKey(x => x.IndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Organisation).WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Structure).WithMany().HasForeignKey(x => x.StructureId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.FinalizedByUser).WithMany().HasForeignKey(x => x.FinalizedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.OperationLog).WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<AssetIssuanceItem> b)
    {
        b.ToTable("AM_AssetIssuanceItems", t =>
        {
            t.HasCheckConstraint("CK_AM_AssetIssuanceItems_Status",
                "[Status] IN ('Proposed','Reserved','HandedOver','Accepted','Rejected','HRApproved','ReturnedForCorrection','Closed')");
            t.HasCheckConstraint("CK_AM_AssetIssuanceItems_Version",
                "[Version] >= 1");
        });

        b.HasKey(x => x.AssetIssuanceItemId);
        b.Property(x => x.Accessories).HasMaxLength(500);
        b.Property(x => x.HandedOverAtUtc).HasColumnType("datetime2");
        b.Property(x => x.HandoverNotes).HasMaxLength(1000);
        b.Property(x => x.Status).HasMaxLength(40);
        b.Property(x => x.ExpectedReturnDate).HasColumnType("date");
        b.Property(x => x.AssetCodeSnapshot).HasMaxLength(100);
        b.Property(x => x.SerialNumberSnapshot).HasMaxLength(120);
        b.Property(x => x.CategoryNameSnapshot).HasMaxLength(150);
        b.Property(x => x.ItemNameSnapshot).HasMaxLength(200);
        b.Property(x => x.BrandSnapshot).HasMaxLength(120);
        b.Property(x => x.ModelSnapshot).HasMaxLength(120);
        b.Property(x => x.ConditionNameSnapshot).HasMaxLength(100);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => x.AssetItemId).HasDatabaseName("IX_AM_AssetIssuanceItems_Asset");
        b.HasIndex(x => x.AssetItemId)
            .IsUnique()
            .HasFilter("[IsActiveReservation] = 1")
            .HasDatabaseName("UX_AM_AssetIssuanceItems_ActiveReservation");
        b.HasIndex(x => new { x.IssuanceId, x.Status });

        b.HasOne(x => x.Issuance).WithMany(x => x.Lines).HasForeignKey(x => x.IssuanceId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AssetItem).WithMany(x => x.IssuanceItems).HasForeignKey(x => x.AssetItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ConditionAtIssue).WithMany().HasForeignKey(x => x.ConditionAtIssueId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.HandedOverByUser).WithMany().HasForeignKey(x => x.HandedOverByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ReplacedFromIssuanceItem).WithMany().HasForeignKey(x => x.ReplacedFromIssuanceItemId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<AssetAcknowledgement> b)
    {
        b.ToTable("AM_AssetAcknowledgements", t =>
            t.HasCheckConstraint("CK_AM_AssetAcknowledgements_Decision",
                "[Decision] IN ('Accepted','Rejected') AND ([Decision] = 'Accepted' OR ([Remarks] IS NOT NULL AND LEN(LTRIM(RTRIM([Remarks]))) > 0))"));

        b.HasKey(x => x.AcknowledgementId);
        b.Property(x => x.Decision).HasMaxLength(20);
        b.Property(x => x.RejectionReasonCode).HasMaxLength(50);
        b.Property(x => x.Remarks).HasMaxLength(1000);
        b.Property(x => x.AcknowledgementStatement).HasMaxLength(1000);
        b.Property(x => x.ConfirmedAtUtc).HasColumnType("datetime2");

        b.HasIndex(x => new { x.IssuanceItemId, x.ItemVersion });
        b.HasIndex(x => new { x.IndividualId, x.ConfirmedAtUtc });

        b.HasOne(x => x.IssuanceItem).WithMany(x => x.Acknowledgements).HasForeignKey(x => x.IssuanceItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Individual).WithMany().HasForeignKey(x => x.IndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ConfirmedByUser).WithMany().HasForeignKey(x => x.ConfirmedByUserId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<AssetHrDecision> b)
    {
        b.ToTable("AM_AssetHrDecisions", t =>
            t.HasCheckConstraint("CK_AM_AssetHrDecisions_Decision",
                "[Decision] IN ('HRApproved','ReturnedForCorrection','Closed')"));

        b.HasKey(x => x.HrDecisionId);
        b.Property(x => x.Decision).HasMaxLength(30);
        b.Property(x => x.Comments).HasMaxLength(1000);
        b.Property(x => x.DecidedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.IdempotencyKey).HasMaxLength(80);

        b.HasIndex(x => new { x.IssuanceItemId, x.ItemVersion, x.IdempotencyKey }).IsUnique();

        b.HasOne(x => x.IssuanceItem).WithMany(x => x.HrDecisions).HasForeignKey(x => x.IssuanceItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DecidedByUser).WithMany().HasForeignKey(x => x.DecidedByUserId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<AssetCustody> b)
    {
        b.ToTable("AM_AssetCustodies", t =>
        {
            t.HasCheckConstraint("CK_AM_AssetCustodies_Status",
                "[Status] IN ('Active','ReturnPending','Returned','Lost','Transferred','Closed')");
            t.HasCheckConstraint("CK_AM_AssetCustodies_Dates",
                "[CustodyToUtc] IS NULL OR [CustodyToUtc] >= [CustodyFromUtc]");
        });

        b.HasKey(x => x.AssetCustodyId);
        b.Property(x => x.CustodyFromUtc).HasColumnType("datetime2");
        b.Property(x => x.CustodyToUtc).HasColumnType("datetime2");
        b.Property(x => x.ExpectedReturnDate).HasColumnType("date");
        b.Property(x => x.Status).HasMaxLength(30);
        b.Property(x => x.ClosedReason).HasMaxLength(500);
        b.Property(x => x.IdempotencyKey).HasMaxLength(80);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => x.AssetItemId)
            .IsUnique()
            .HasFilter("[CustodyToUtc] IS NULL")
            .HasDatabaseName("UX_AM_AssetCustodies_Active");
        b.HasIndex(x => x.IssuanceItemId).IsUnique().HasDatabaseName("UX_AM_AssetCustodies_IssuanceItem");
        b.HasIndex(x => x.IdempotencyKey).IsUnique().HasDatabaseName("UX_AM_AssetCustodies_Idempotency");
        b.HasIndex(x => new { x.IndividualId, x.Status });
        b.HasIndex(x => new { x.OrganisationId, x.Status });

        b.HasOne(x => x.AssetItem).WithMany(x => x.Custodies).HasForeignKey(x => x.AssetItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Individual).WithMany().HasForeignKey(x => x.IndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Organisation).WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Structure).WithMany().HasForeignKey(x => x.StructureId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.IssuanceItem).WithOne(x => x.Custody).HasForeignKey<AssetCustody>(x => x.IssuanceItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ClosedByUser).WithMany().HasForeignKey(x => x.ClosedByUserId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<AssetReturn> b)
    {
        b.ToTable("AM_AssetReturns", t =>
            t.HasCheckConstraint("CK_AM_AssetReturns_Status",
                "[Status] IN ('ReturnPending','Inspected','Closed','Cancelled') AND [ReturnType] IN ('StandardReturn','DamageReport','LossReport','TransferReturn','TerminationClearance')"));

        b.HasKey(x => x.AssetReturnId);
        b.Property(x => x.ReferenceNumber).HasMaxLength(60);
        b.Property(x => x.ReturnType).HasMaxLength(30);
        b.Property(x => x.ReturnDate).HasColumnType("datetime2");
        b.Property(x => x.CompletedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.Status).HasMaxLength(30);
        b.Property(x => x.Notes).HasMaxLength(1000);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => x.ReferenceNumber).IsUnique();
        b.HasIndex(x => new { x.OrganisationId, x.Status, x.ReturnDate });
        b.HasIndex(x => new { x.IndividualId, x.Status });

        b.HasOne(x => x.Organisation).WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Individual).WithMany().HasForeignKey(x => x.IndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StaffTransfer).WithMany().HasForeignKey(x => x.StaffTransferId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.RequestedByUser).WithMany().HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ReturnedToUser).WithMany().HasForeignKey(x => x.ReturnedToUserId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<AssetReturnItem> b)
    {
        b.ToTable("AM_AssetReturnItems", t =>
            t.HasCheckConstraint("CK_AM_AssetReturnItems_Status",
                "[Status] IN ('ReturnPending','Inspected','Closed') AND [InspectionResult] IN ('Pending','GoodAvailable','DamagedMaintenance','LostConfirmed','Disposed','ReplacementRequired')"));

        b.HasKey(x => x.AssetReturnItemId);
        b.Property(x => x.AccessoryChecklist).HasMaxLength(500);
        b.Property(x => x.MissingAccessories).HasMaxLength(500);
        b.Property(x => x.DamageOrLossEvidence).HasMaxLength(1000);
        b.Property(x => x.InspectionResult).HasMaxLength(40);
        b.Property(x => x.PostReturnDisposition).HasMaxLength(30);
        b.Property(x => x.ReceivedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.Status).HasMaxLength(30);

        b.HasIndex(x => new { x.AssetReturnId, x.AssetCustodyId }).IsUnique();
        b.HasIndex(x => new { x.AssetCustodyId, x.Status });

        b.HasOne(x => x.AssetReturn).WithMany(x => x.Items).HasForeignKey(x => x.AssetReturnId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AssetCustody).WithMany(x => x.ReturnItems).HasForeignKey(x => x.AssetCustodyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AssetItem).WithMany().HasForeignKey(x => x.AssetItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ConditionAtReturn).WithMany().HasForeignKey(x => x.ConditionAtReturnId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ReceivedByUser).WithMany().HasForeignKey(x => x.ReceivedByUserId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<AssetTransferDisposition> b)
    {
        b.ToTable("AM_AssetTransferDispositions", t =>
            t.HasCheckConstraint("CK_AM_AssetTransferDispositions_Decision",
                "[Decision] IN ('Retain','Return','ChangeOwningDepartment','Replace') AND [Status] IN ('Pending','Approved','Executed','Cancelled')"));

        b.HasKey(x => x.AssetTransferDispositionId);
        b.Property(x => x.Decision).HasMaxLength(30);
        b.Property(x => x.Status).HasMaxLength(30);
        b.Property(x => x.DecidedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.ExecutedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.Remarks).HasMaxLength(1000);

        b.HasIndex(x => new { x.StaffTransferId, x.AssetCustodyId });
        b.HasIndex(x => new { x.IndividualId, x.Status });

        b.HasOne(x => x.StaffTransfer).WithMany().HasForeignKey(x => x.StaffTransferId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AssetCustody).WithMany(x => x.TransferDispositions).HasForeignKey(x => x.AssetCustodyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AssetItem).WithMany().HasForeignKey(x => x.AssetItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Individual).WithMany().HasForeignKey(x => x.IndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.FromJob).WithMany().HasForeignKey(x => x.FromJobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ToJob).WithMany().HasForeignKey(x => x.ToJobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.FromOrganisation).WithMany().HasForeignKey(x => x.FromOrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ToOrganisation).WithMany().HasForeignKey(x => x.ToOrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.FromStructure).WithMany().HasForeignKey(x => x.FromStructureId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ToStructure).WithMany().HasForeignKey(x => x.ToStructureId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.DecidedByUser).WithMany().HasForeignKey(x => x.DecidedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ApprovedByUser).WithMany().HasForeignKey(x => x.ApprovedByUserId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<AssetEvent> b)
    {
        b.ToTable("AM_AssetEvents");
        b.HasKey(x => x.AssetEventId);
        b.Property(x => x.RelatedEntityType).HasMaxLength(50);
        b.Property(x => x.EventType).HasMaxLength(50);
        b.Property(x => x.FromStatus).HasMaxLength(40);
        b.Property(x => x.ToStatus).HasMaxLength(40);
        b.Property(x => x.AtUtc).HasColumnType("datetime2");
        b.Property(x => x.MetadataJson).HasMaxLength(4000);

        b.HasIndex(x => new { x.AssetItemId, x.AtUtc });
        b.HasIndex(x => new { x.RelatedEntityType, x.RelatedEntityId, x.AtUtc });
        b.HasIndex(x => new { x.OrganisationId, x.AtUtc });

        b.HasOne(x => x.Organisation).WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AssetItem).WithMany(x => x.Events).HasForeignKey(x => x.AssetItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ActorUser).WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<AssetAttachment> b)
    {
        b.ToTable("AM_AssetAttachments", t =>
            t.HasCheckConstraint("CK_AM_AssetAttachments_Scope",
                "[AccessScope] IN ('EmployeeAndHr','HrOnly','RestrictedSecurity')"));

        b.HasKey(x => x.AttachmentId);
        b.Property(x => x.EntityType).HasMaxLength(50);
        b.Property(x => x.StorageKey).HasMaxLength(200);
        b.Property(x => x.FileName).HasMaxLength(255);
        b.Property(x => x.ContentType).HasMaxLength(100);
        b.Property(x => x.Sha256).HasMaxLength(64);
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.AccessScope).HasMaxLength(30);

        b.HasIndex(x => x.StorageKey).IsUnique();
        b.HasIndex(x => new { x.EntityType, x.EntityId });

        b.HasOne(x => x.Organisation).WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.UploadedByUser).WithMany().HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<AssetDocument> b)
    {
        b.ToTable("AM_AssetDocuments", t =>
            t.HasCheckConstraint("CK_AM_AssetDocuments_Variant",
                "[DocumentVariant] IN ('StandardHandoverRecord','RestrictedSecurityRecord')"));

        b.HasKey(x => x.AssetDocumentId);
        b.Property(x => x.DocumentNumber).HasMaxLength(60);
        b.Property(x => x.DocumentVariant).HasMaxLength(30);
        b.Property(x => x.Sha256Hash).HasMaxLength(64);
        b.Property(x => x.ContentType).HasMaxLength(60);
        b.Property(x => x.GeneratedAtUtc).HasColumnType("datetime2");

        b.HasIndex(x => x.DocumentNumber).IsUnique();
        b.HasIndex(x => new { x.AssetIssuanceId, x.DocumentVariant, x.DocumentVersion });

        b.HasOne(x => x.AssetIssuance).WithMany(x => x.Documents).HasForeignKey(x => x.AssetIssuanceId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.GeneratedByUser).WithMany().HasForeignKey(x => x.GeneratedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public partial class HrmTeContext
{
    public DbSet<AssetCategory> AssetCategories => Set<AssetCategory>();
    public DbSet<AssetCondition> AssetConditions => Set<AssetCondition>();
    public DbSet<AssetItem> AssetItems => Set<AssetItem>();
    public DbSet<AssetIssuance> AssetIssuances => Set<AssetIssuance>();
    public DbSet<AssetIssuanceItem> AssetIssuanceItems => Set<AssetIssuanceItem>();
    public DbSet<AssetAcknowledgement> AssetAcknowledgements => Set<AssetAcknowledgement>();
    public DbSet<AssetHrDecision> AssetHrDecisions => Set<AssetHrDecision>();
    public DbSet<AssetCustody> AssetCustodies => Set<AssetCustody>();
    public DbSet<AssetReturn> AssetReturns => Set<AssetReturn>();
    public DbSet<AssetReturnItem> AssetReturnItems => Set<AssetReturnItem>();
    public DbSet<AssetTransferDisposition> AssetTransferDispositions => Set<AssetTransferDisposition>();
    public DbSet<AssetEvent> AssetEvents => Set<AssetEvent>();
    public DbSet<AssetAttachment> AssetAttachments => Set<AssetAttachment>();
    public DbSet<AssetDocument> AssetDocuments => Set<AssetDocument>();
}
