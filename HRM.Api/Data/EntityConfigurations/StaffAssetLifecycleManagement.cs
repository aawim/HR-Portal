using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Models;

public sealed class StaffAssetLifecycleMapping :
    IEntityTypeConfiguration<AssetServiceRequest>,
    IEntityTypeConfiguration<AssetServiceRequestItem>,
    IEntityTypeConfiguration<AssetMaintenanceJob>,
    IEntityTypeConfiguration<AssetMaintenanceAction>,
    IEntityTypeConfiguration<AssetComponent>,
    IEntityTypeConfiguration<AssetComponentTransaction>,
    IEntityTypeConfiguration<AssetServicePart>,
    IEntityTypeConfiguration<AssetReplacementLink>,
    IEntityTypeConfiguration<AssetTemporaryHandover>,
    IEntityTypeConfiguration<AssetInspection>,
    IEntityTypeConfiguration<AssetServiceDocument>,
    IEntityTypeConfiguration<AssetConfigurationSnapshot>
{
    public void Configure(EntityTypeBuilder<AssetServiceRequest> b)
    {
        b.ToTable("AM_AssetServiceRequests");
        b.HasKey(x => x.AssetServiceRequestId);
        b.Property(x => x.ReferenceNumber).HasMaxLength(60).IsRequired();
        b.Property(x => x.RequestType).HasMaxLength(40).IsRequired();
        b.Property(x => x.Priority).HasMaxLength(20).IsRequired().HasDefaultValue("Standard");
        b.Property(x => x.Title).HasMaxLength(250).IsRequired();
        b.Property(x => x.Description).IsRequired();
        b.Property(x => x.Status).HasMaxLength(40).IsRequired();
        b.Property(x => x.EstimatedCost).HasColumnType("decimal(18,2)");
        b.Property(x => x.ActualCost).HasColumnType("decimal(18,2)");
        b.Property(x => x.SubmittedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.CompletedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.ResolutionSummary).HasMaxLength(1000);
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => x.ReferenceNumber).IsUnique().HasDatabaseName("UX_AM_AssetServiceRequests_ReferenceNumber");
        b.HasIndex(x => new { x.OrganisationId, x.Status, x.RequestType });
        b.HasIndex(x => new { x.IndividualId, x.JobId });

        b.HasOne(x => x.Request).WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Individual).WithMany().HasForeignKey(x => x.IndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Organisation).WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Structure).WithMany().HasForeignKey(x => x.StructureId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.StaffTransfer).WithMany().HasForeignKey(x => x.StaffTransferId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.RequestedByUser).WithMany().HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AssignedTechnicianUser).WithMany().HasForeignKey(x => x.AssignedTechnicianUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.OperationLog).WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<AssetServiceRequestItem> b)
    {
        b.ToTable("AM_AssetServiceRequestItems");
        b.HasKey(x => x.AssetServiceRequestItemId);
        b.Property(x => x.ItemRequestType).HasMaxLength(40).IsRequired();
        b.Property(x => x.ReportedFaultOrRequirement).HasMaxLength(1000).IsRequired();
        b.Property(x => x.TargetComponentCategory).HasMaxLength(60);
        b.Property(x => x.TargetSpecification).HasMaxLength(500);
        b.Property(x => x.Status).HasMaxLength(40).IsRequired();
        b.Property(x => x.StaffDecision).HasMaxLength(30);
        b.Property(x => x.StaffRemarks).HasMaxLength(1000);
        b.Property(x => x.StaffConfirmedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.AssetServiceRequestId, x.AssetItemId });
        b.HasIndex(x => x.AssetCustodyId);

        b.HasOne(x => x.ServiceRequest).WithMany(x => x.Items).HasForeignKey(x => x.AssetServiceRequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AssetItem).WithMany().HasForeignKey(x => x.AssetItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AssetCustody).WithMany(c => c.ServiceRequestItems).HasForeignKey(x => x.AssetCustodyId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<AssetMaintenanceJob> b)
    {
        b.ToTable("AM_AssetMaintenanceJobs");
        b.HasKey(x => x.AssetMaintenanceJobId);
        b.Property(x => x.WorkOrderNumber).HasMaxLength(60).IsRequired();
        b.Property(x => x.JobType).HasMaxLength(40).IsRequired();
        b.Property(x => x.Priority).HasMaxLength(20).IsRequired().HasDefaultValue("Standard");
        b.Property(x => x.Status).HasMaxLength(40).IsRequired();
        b.Property(x => x.StartedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.CompletedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.DiagnosticSummary);
        b.Property(x => x.WorkPerformedSummary);
        b.Property(x => x.HardwareTestSummary);
        b.Property(x => x.DataSanitizationMethod).HasMaxLength(100);
        b.Property(x => x.VendorWarrantyClaimNumber).HasMaxLength(100);
        b.Property(x => x.PartsCost).HasColumnType("decimal(18,2)");
        b.Property(x => x.LabourCost).HasColumnType("decimal(18,2)");
        b.Property(x => x.TotalCost).HasColumnType("decimal(18,2)");
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => x.WorkOrderNumber).IsUnique().HasDatabaseName("UX_AM_AssetMaintenanceJobs_WorkOrderNumber");
        b.HasIndex(x => new { x.AssetItemId, x.Status });
        b.HasIndex(x => new { x.AssignedTechnicianUserId, x.Status });

        b.HasOne(x => x.ServiceRequest).WithMany(x => x.MaintenanceJobs).HasForeignKey(x => x.AssetServiceRequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AssetItem).WithMany(x => x.MaintenanceJobs).HasForeignKey(x => x.AssetItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AssignedTechnicianUser).WithMany().HasForeignKey(x => x.AssignedTechnicianUserId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<AssetMaintenanceAction> b)
    {
        b.ToTable("AM_AssetMaintenanceActions");
        b.HasKey(x => x.AssetMaintenanceActionId);
        b.Property(x => x.ActionType).HasMaxLength(50).IsRequired();
        b.Property(x => x.ActionAtUtc).HasColumnType("datetime2");
        b.Property(x => x.Remarks).HasMaxLength(1000).IsRequired();
        b.Property(x => x.MetadataJson);

        b.HasIndex(x => new { x.AssetMaintenanceJobId, x.ActionAtUtc });

        b.HasOne(x => x.MaintenanceJob).WithMany(x => x.Actions).HasForeignKey(x => x.AssetMaintenanceJobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TechnicianUser).WithMany().HasForeignKey(x => x.TechnicianUserId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<AssetComponent> b)
    {
        b.ToTable("AM_AssetComponents");
        b.HasKey(x => x.AssetComponentId);
        b.Property(x => x.ComponentType).HasMaxLength(40).IsRequired();
        b.Property(x => x.Brand).HasMaxLength(100);
        b.Property(x => x.Model).HasMaxLength(100);
        b.Property(x => x.PartNumber).HasMaxLength(100);
        b.Property(x => x.SerialNumber).HasMaxLength(120);
        b.Property(x => x.Specification).HasMaxLength(250).IsRequired();
        b.Property(x => x.CapacityOrRating).HasMaxLength(100);
        b.Property(x => x.SlotOrPortLocation).HasMaxLength(60);
        b.Property(x => x.Status).HasMaxLength(30).IsRequired();
        b.Property(x => x.Cost).HasColumnType("decimal(18,2)");
        b.Property(x => x.InstalledAtUtc).HasColumnType("datetime2");
        b.Property(x => x.RemovedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.AssetItemId, x.ComponentType, x.Status });
        b.HasIndex(x => x.SerialNumber).HasFilter("[SerialNumber] IS NOT NULL AND [SerialNumber] <> ''");

        b.HasOne(x => x.OwnerOrganisation).WithMany().HasForeignKey(x => x.OwnerOrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AssetItem).WithMany(x => x.Components).HasForeignKey(x => x.AssetItemId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<AssetComponentTransaction> b)
    {
        b.ToTable("AM_AssetComponentTransactions");
        b.HasKey(x => x.AssetComponentTransactionId);
        b.Property(x => x.TransactionType).HasMaxLength(40).IsRequired();
        b.Property(x => x.FromStatus).HasMaxLength(30);
        b.Property(x => x.ToStatus).HasMaxLength(30).IsRequired();
        b.Property(x => x.TransactedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.Notes).HasMaxLength(1000);

        b.HasIndex(x => new { x.AssetComponentId, x.TransactedAtUtc });

        b.HasOne(x => x.AssetComponent).WithMany(x => x.Transactions).HasForeignKey(x => x.AssetComponentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AssetItem).WithMany().HasForeignKey(x => x.AssetItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.MaintenanceJob).WithMany(x => x.ComponentTransactions).HasForeignKey(x => x.AssetMaintenanceJobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.TechnicianUser).WithMany().HasForeignKey(x => x.TechnicianUserId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<AssetServicePart> b)
    {
        b.ToTable("AM_AssetServiceParts");
        b.HasKey(x => x.AssetServicePartId);
        b.Property(x => x.PartCode).HasMaxLength(100).IsRequired();
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Category).HasMaxLength(50).IsRequired();
        b.Property(x => x.Specification).HasMaxLength(250).IsRequired();
        b.Property(x => x.CompatibleModels).HasMaxLength(250);
        b.Property(x => x.UnitCost).HasColumnType("decimal(18,2)").HasDefaultValue(0m);
        b.Property(x => x.LocationBin).HasMaxLength(100);
        b.Property(x => x.IsActive).HasDefaultValue(true);

        b.HasIndex(x => new { x.OrganisationId, x.PartCode }).IsUnique().HasDatabaseName("UX_AM_AssetServiceParts_Organisation_PartCode");
        b.HasIndex(x => new { x.OrganisationId, x.Category, x.IsActive });

        b.HasOne(x => x.Organisation).WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<AssetReplacementLink> b)
    {
        b.ToTable("AM_AssetReplacementLinks");
        b.HasKey(x => x.AssetReplacementLinkId);
        b.Property(x => x.ReplacementType).HasMaxLength(40).IsRequired();
        b.Property(x => x.ReplacementReason).HasMaxLength(40).IsRequired();
        b.Property(x => x.OriginalAssetDisposition).HasMaxLength(60).IsRequired();
        b.Property(x => x.StaffDecision).HasMaxLength(30);
        b.Property(x => x.StaffRemarks).HasMaxLength(1000);
        b.Property(x => x.StaffConfirmedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.RecordedAtUtc).HasColumnType("datetime2");

        b.HasIndex(x => new { x.AssetServiceRequestId, x.OriginalAssetItemId });
        b.HasIndex(x => x.ReplacementAssetItemId);

        b.HasOne(x => x.ServiceRequest).WithMany(x => x.ReplacementLinks).HasForeignKey(x => x.AssetServiceRequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.OriginalAssetItem).WithMany(x => x.OriginalReplacementLinks).HasForeignKey(x => x.OriginalAssetItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.OriginalAssetCustody).WithMany(c => c.OriginalReplacementLinks).HasForeignKey(x => x.OriginalAssetCustodyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ReplacementAssetItem).WithMany(x => x.NewReplacementLinks).HasForeignKey(x => x.ReplacementAssetItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ReplacementAssetCustody).WithMany(c => c.NewReplacementLinks).HasForeignKey(x => x.ReplacementAssetCustodyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.RecordedByUser).WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<AssetTemporaryHandover> b)
    {
        b.ToTable("AM_AssetTemporaryHandovers");
        b.HasKey(x => x.AssetTemporaryHandoverId);
        b.Property(x => x.HandedOverAtUtc).HasColumnType("datetime2");
        b.Property(x => x.ExpectedReturnDate).HasColumnType("date");
        b.Property(x => x.ActualReturnedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.Status).HasMaxLength(40).IsRequired();
        b.Property(x => x.StaffConfirmedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.Notes).HasMaxLength(1000);

        b.HasIndex(x => new { x.PrimaryAssetItemId, x.Status });
        b.HasIndex(x => new { x.LoanerAssetItemId, x.Status });
        b.HasIndex(x => new { x.IndividualId, x.Status });

        b.HasOne(x => x.ServiceRequest).WithMany(x => x.TemporaryHandovers).HasForeignKey(x => x.AssetServiceRequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.PrimaryAssetItem).WithMany(x => x.PrimaryHandovers).HasForeignKey(x => x.PrimaryAssetItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.LoanerAssetItem).WithMany(x => x.LoanerHandovers).HasForeignKey(x => x.LoanerAssetItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Individual).WithMany().HasForeignKey(x => x.IndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Organisation).WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.HandedOverByUser).WithMany().HasForeignKey(x => x.HandedOverByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ReceivedBackByUser).WithMany().HasForeignKey(x => x.ReceivedBackByUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ConditionOut).WithMany().HasForeignKey(x => x.ConditionOutId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ConditionIn).WithMany().HasForeignKey(x => x.ConditionInId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<AssetInspection> b)
    {
        b.ToTable("AM_AssetInspections");
        b.HasKey(x => x.AssetInspectionId);
        b.Property(x => x.InspectionStage).HasMaxLength(40).IsRequired();
        b.Property(x => x.InspectedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.PhysicalDamageReport).HasMaxLength(1000);
        b.Property(x => x.FunctionalTestSummary).HasMaxLength(1000);
        b.Property(x => x.AccessoriesChecklist).HasMaxLength(500);
        b.Property(x => x.DataSanitizationVerified).HasDefaultValue(false);
        b.Property(x => x.InspectorDecision).HasMaxLength(40).IsRequired();
        b.Property(x => x.Notes).HasMaxLength(1000);

        b.HasIndex(x => new { x.AssetItemId, x.InspectionStage });
        b.HasIndex(x => x.AssetServiceRequestId);

        b.HasOne(x => x.AssetItem).WithMany(x => x.Inspections).HasForeignKey(x => x.AssetItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AssetCustody).WithMany().HasForeignKey(x => x.AssetCustodyId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ServiceRequest).WithMany(x => x.Inspections).HasForeignKey(x => x.AssetServiceRequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ReturnItem).WithMany().HasForeignKey(x => x.AssetReturnItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.InspectorUser).WithMany().HasForeignKey(x => x.InspectorUserId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.OverallCondition).WithMany().HasForeignKey(x => x.OverallConditionId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<AssetServiceDocument> b)
    {
        b.ToTable("AM_AssetServiceDocuments");
        b.HasKey(x => x.AssetServiceDocumentId);
        b.Property(x => x.DocumentType).HasMaxLength(40).IsRequired();
        b.Property(x => x.DocumentNumber).HasMaxLength(60).IsRequired();
        b.Property(x => x.SnapshotJson).IsRequired();
        b.Property(x => x.Sha256Hash).HasMaxLength(64).IsRequired();
        b.Property(x => x.GeneratedAtUtc).HasColumnType("datetime2");

        b.HasIndex(x => x.DocumentNumber).IsUnique().HasDatabaseName("UX_AM_AssetServiceDocuments_DocumentNumber");
        b.HasIndex(x => new { x.AssetServiceRequestId, x.DocumentType });
        b.HasIndex(x => new { x.IndividualId, x.DocumentType });

        b.HasOne(x => x.ServiceRequest).WithMany(x => x.ServiceDocuments).HasForeignKey(x => x.AssetServiceRequestId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.MaintenanceJob).WithMany(x => x.ServiceDocuments).HasForeignKey(x => x.AssetMaintenanceJobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AssetItem).WithMany().HasForeignKey(x => x.AssetItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Individual).WithMany().HasForeignKey(x => x.IndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.GeneratedByUser).WithMany().HasForeignKey(x => x.GeneratedByUserId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<AssetConfigurationSnapshot> b)
    {
        b.ToTable("AM_AssetConfigurationSnapshots");
        b.HasKey(x => x.AssetConfigurationSnapshotId);
        b.Property(x => x.TriggerEvent).HasMaxLength(50).IsRequired();
        b.Property(x => x.ProcessorSpec).HasMaxLength(150);
        b.Property(x => x.RamSummary).HasMaxLength(150).IsRequired();
        b.Property(x => x.StorageSummary).HasMaxLength(250).IsRequired();
        b.Property(x => x.GraphicsSpec).HasMaxLength(150);
        b.Property(x => x.OsVersion).HasMaxLength(100);
        b.Property(x => x.ComponentsJson).IsRequired();
        b.Property(x => x.CapturedAtUtc).HasColumnType("datetime2");

        b.HasIndex(x => new { x.AssetItemId, x.CapturedAtUtc });

        b.HasOne(x => x.AssetItem).WithMany(x => x.ConfigurationSnapshots).HasForeignKey(x => x.AssetItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.CapturedByUser).WithMany().HasForeignKey(x => x.CapturedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public partial class HrmTeContext
{
    public DbSet<AssetServiceRequest> AssetServiceRequests => Set<AssetServiceRequest>();
    public DbSet<AssetServiceRequestItem> AssetServiceRequestItems => Set<AssetServiceRequestItem>();
    public DbSet<AssetMaintenanceJob> AssetMaintenanceJobs => Set<AssetMaintenanceJob>();
    public DbSet<AssetMaintenanceAction> AssetMaintenanceActions => Set<AssetMaintenanceAction>();
    public DbSet<AssetComponent> AssetComponents => Set<AssetComponent>();
    public DbSet<AssetComponentTransaction> AssetComponentTransactions => Set<AssetComponentTransaction>();
    public DbSet<AssetServicePart> AssetServiceParts => Set<AssetServicePart>();
    public DbSet<AssetReplacementLink> AssetReplacementLinks => Set<AssetReplacementLink>();
    public DbSet<AssetTemporaryHandover> AssetTemporaryHandovers => Set<AssetTemporaryHandover>();
    public DbSet<AssetInspection> AssetInspections => Set<AssetInspection>();
    public DbSet<AssetServiceDocument> AssetServiceDocuments => Set<AssetServiceDocument>();
    public DbSet<AssetConfigurationSnapshot> AssetConfigurationSnapshots => Set<AssetConfigurationSnapshot>();
}
