using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Models;

public sealed class StaffSeparationMapping :
    IEntityTypeConfiguration<SeparationType>,
    IEntityTypeConfiguration<ClearanceTemplate>,
    IEntityTypeConfiguration<ClearanceTemplateTask>,
    IEntityTypeConfiguration<StaffSeparation>,
    IEntityTypeConfiguration<ClearanceTask>,
    IEntityTypeConfiguration<SeparationWorkHandover>,
    IEntityTypeConfiguration<SeparationWorkHandoverItem>,
    IEntityTypeConfiguration<SeparationAssetClearance>,
    IEntityTypeConfiguration<SeparationFinanceItem>,
    IEntityTypeConfiguration<SeparationAccessRevocation>,
    IEntityTypeConfiguration<SeparationDocument>,
    IEntityTypeConfiguration<SeparationEvent>
{
    public void Configure(EntityTypeBuilder<SeparationType> b)
    {
        b.ToTable("SEP_SeparationTypes");
        b.HasKey(x => x.SeparationTypeId);
        b.Property(x => x.Code).HasMaxLength(50);
        b.Property(x => x.Name).HasMaxLength(150);
        b.Property(x => x.Category).HasMaxLength(50);
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2");

        b.HasIndex(x => new { x.Code, x.OrganisationId }).IsUnique();
        b.HasOne(x => x.Organisation).WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<ClearanceTemplate> b)
    {
        b.ToTable("SEP_ClearanceTemplates");
        b.HasKey(x => x.ClearanceTemplateId);
        b.Property(x => x.TemplateName).HasMaxLength(150);
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2");

        b.HasIndex(x => new { x.OrganisationId, x.SeparationTypeId, x.JobTypeId });
        b.HasOne(x => x.Organisation).WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.SeparationType).WithMany(x => x.ClearanceTemplates).HasForeignKey(x => x.SeparationTypeId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.JobType).WithMany().HasForeignKey(x => x.JobTypeId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<ClearanceTemplateTask> b)
    {
        b.ToTable("SEP_ClearanceTemplateTasks");
        b.HasKey(x => x.ClearanceTemplateTaskId);
        b.Property(x => x.Domain).HasMaxLength(50);
        b.Property(x => x.TaskName).HasMaxLength(200);
        b.Property(x => x.TaskDescription).HasMaxLength(500);
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2");

        b.HasIndex(x => new { x.ClearanceTemplateId, x.SequenceOrder });
        b.HasOne(x => x.ClearanceTemplate).WithMany(x => x.Tasks).HasForeignKey(x => x.ClearanceTemplateId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.AssignedOrganisationStructure).WithMany().HasForeignKey(x => x.AssignedOrganisationStructureId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.AssignedRole).WithMany().HasForeignKey(x => x.AssignedRoleId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<StaffSeparation> b)
    {
        b.ToTable("SEP_StaffSeparations");
        b.HasKey(x => x.StaffSeparationId);

        b.Property(x => x.ReasonCategory).HasMaxLength(50);
        b.Property(x => x.InitiationSource).HasMaxLength(50);
        b.Property(x => x.RequestedNoticeDate).HasColumnType("date");
        b.Property(x => x.LastWorkingDate).HasColumnType("date");
        b.Property(x => x.EffectiveSeparationDate).HasColumnType("date");
        b.Property(x => x.Status).HasMaxLength(50);
        b.Property(x => x.EmployeeAcknowledgementStatus).HasMaxLength(50);
        b.Property(x => x.EmployeeAcknowledgedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.ExitInterviewDate).HasColumnType("date");
        b.Property(x => x.NoticeShortfallPenaltyAmount).HasColumnType("decimal(18,2)");
        b.Property(x => x.BasicSalarySnapshot).HasColumnType("decimal(18,2)");

        b.Property(x => x.FullNameSnapshot).HasMaxLength(250);
        b.Property(x => x.EmployeeNumberSnapshot).HasMaxLength(50);
        b.Property(x => x.ProtectedIdCardSnapshot).HasMaxLength(100);
        b.Property(x => x.PositionTitleSnapshot).HasMaxLength(200);
        b.Property(x => x.OrganisationNameSnapshot).HasMaxLength(250);
        b.Property(x => x.DepartmentNameSnapshot).HasMaxLength(250);
        b.Property(x => x.DateJoinedSnapshot).HasColumnType("date");
        b.Property(x => x.SupervisorNameSnapshot).HasMaxLength(250);
        b.Property(x => x.OfficialEmailSnapshot).HasMaxLength(128);
        b.Property(x => x.PersonalContactNumber).HasMaxLength(50);
        b.Property(x => x.PersonalEmail).HasMaxLength(128);
        b.Property(x => x.ForwardingAddress).HasMaxLength(500);

        b.Property(x => x.HrAdminReviewedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.TerminationExecutedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.IndividualId, x.JobId, x.Status });
        b.HasIndex(x => new { x.OrganisationId, x.Status, x.EffectiveSeparationDate });

        b.HasOne(x => x.Request).WithMany().HasForeignKey(x => x.RequestId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Individual).WithMany().HasForeignKey(x => x.IndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Organisation).WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.OrganisationStructure).WithMany().HasForeignKey(x => x.OrganisationStructureId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.SeparationType).WithMany(x => x.StaffSeparations).HasForeignKey(x => x.SeparationTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.SupervisorIndividual).WithMany().HasForeignKey(x => x.SupervisorIndividualId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.HrAdminReviewerUser).WithMany().HasForeignKey(x => x.HrAdminReviewerUserId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.ExecutedByUser).WithMany().HasForeignKey(x => x.ExecutedByUserId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.CreatedByUser).WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<ClearanceTask> b)
    {
        b.ToTable("SEP_ClearanceTasks");
        b.HasKey(x => x.ClearanceTaskId);
        b.Property(x => x.Domain).HasMaxLength(50);
        b.Property(x => x.TaskName).HasMaxLength(200);
        b.Property(x => x.TaskDescription).HasMaxLength(1000);
        b.Property(x => x.Status).HasMaxLength(50);
        b.Property(x => x.ClearedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.OutstandingObligationAmount).HasColumnType("decimal(18,2)");
        b.Property(x => x.RowVersion).IsRowVersion();

        b.HasIndex(x => new { x.StaffSeparationId, x.Domain, x.Status });

        b.HasOne(x => x.StaffSeparation).WithMany(x => x.ClearanceTasks).HasForeignKey(x => x.StaffSeparationId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.AssignedOrganisationStructure).WithMany().HasForeignKey(x => x.AssignedOrganisationStructureId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.AssignedToUser).WithMany().HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.AssignedToRole).WithMany().HasForeignKey(x => x.AssignedToRoleId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.ClearedByUser).WithMany().HasForeignKey(x => x.ClearedByUserId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<SeparationWorkHandover> b)
    {
        b.ToTable("SEP_WorkHandovers");
        b.HasKey(x => x.WorkHandoverId);
        b.Property(x => x.SupervisorStatus).HasMaxLength(50);
        b.Property(x => x.SupervisorReviewedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.HodStatus).HasMaxLength(50);
        b.Property(x => x.HodReviewedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.CreatedAtUtc).HasColumnType("datetime2");
        b.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2");

        b.HasIndex(x => x.StaffSeparationId).IsUnique();

        b.HasOne(x => x.StaffSeparation).WithOne(x => x.WorkHandover).HasForeignKey<SeparationWorkHandover>(x => x.StaffSeparationId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.SupervisorIndividual).WithMany().HasForeignKey(x => x.SupervisorIndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.HodIndividual).WithMany().HasForeignKey(x => x.HodIndividualId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<SeparationWorkHandoverItem> b)
    {
        b.ToTable("SEP_WorkHandoverItems");
        b.HasKey(x => x.WorkHandoverItemId);
        b.Property(x => x.ItemType).HasMaxLength(50);
        b.Property(x => x.ItemTitle).HasMaxLength(250);
        b.Property(x => x.TargetCompletionDate).HasColumnType("date");
        b.Property(x => x.Status).HasMaxLength(50);
        b.Property(x => x.VerifiedAtUtc).HasColumnType("datetime2");

        b.HasIndex(x => new { x.WorkHandoverId, x.Status });
        b.HasIndex(x => x.StaffSeparationId);

        b.HasOne(x => x.WorkHandover).WithMany(x => x.Items).HasForeignKey(x => x.WorkHandoverId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.StaffSeparation).WithMany(x => x.WorkHandoverItems).HasForeignKey(x => x.StaffSeparationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.RecipientIndividual).WithMany().HasForeignKey(x => x.RecipientIndividualId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.VerifiedByIndividual).WithMany().HasForeignKey(x => x.VerifiedByIndividualId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<SeparationAssetClearance> b)
    {
        b.ToTable("SEP_AssetClearances");
        b.HasKey(x => x.SeparationAssetClearanceId);
        b.Property(x => x.DisposalAction).HasMaxLength(50);
        b.Property(x => x.AssessedDamageCost).HasColumnType("decimal(18,2)");
        b.Property(x => x.Status).HasMaxLength(50);
        b.Property(x => x.VerifiedAtUtc).HasColumnType("datetime2");

        b.HasIndex(x => new { x.StaffSeparationId, x.Status });

        b.HasOne(x => x.StaffSeparation).WithMany(x => x.AssetClearances).HasForeignKey(x => x.StaffSeparationId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.AssetCustody).WithMany().HasForeignKey(x => x.AssetCustodyId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.AssetItem).WithMany().HasForeignKey(x => x.AssetItemId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.AssetReturn).WithMany().HasForeignKey(x => x.AssetReturnId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.ConditionOnReturn).WithMany().HasForeignKey(x => x.ConditionOnReturnId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.VerifiedByUser).WithMany().HasForeignKey(x => x.VerifiedByUserId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<SeparationFinanceItem> b)
    {
        b.ToTable("SEP_FinanceItems");
        b.HasKey(x => x.SeparationFinanceItemId);
        b.Property(x => x.Category).HasMaxLength(50);
        b.Property(x => x.Description).HasMaxLength(250);
        b.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        b.Property(x => x.PayrollBatchReference).HasMaxLength(100);
        b.Property(x => x.VerifiedAtUtc).HasColumnType("datetime2");

        b.HasIndex(x => new { x.StaffSeparationId, x.Category, x.IsCleared });

        b.HasOne(x => x.StaffSeparation).WithMany(x => x.FinanceItems).HasForeignKey(x => x.StaffSeparationId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.VerifiedByUser).WithMany().HasForeignKey(x => x.VerifiedByUserId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<SeparationAccessRevocation> b)
    {
        b.ToTable("SEP_AccessRevocations");
        b.HasKey(x => x.SeparationAccessRevocationId);
        b.Property(x => x.SystemType).HasMaxLength(50);
        b.Property(x => x.SystemName).HasMaxLength(150);
        b.Property(x => x.AccountIdentifier).HasMaxLength(150);
        b.Property(x => x.DoorBuildingName).HasMaxLength(150);
        b.Property(x => x.DoorCardNumber).HasMaxLength(100);
        b.Property(x => x.RevocationAction).HasMaxLength(50);
        b.Property(x => x.ScheduledRevocationDate).HasColumnType("date");
        b.Property(x => x.Status).HasMaxLength(50);
        b.Property(x => x.RevokedAtUtc).HasColumnType("datetime2");

        b.HasIndex(x => new { x.StaffSeparationId, x.Status });

        b.HasOne(x => x.StaffSeparation).WithMany(x => x.AccessRevocations).HasForeignKey(x => x.StaffSeparationId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.RevokedByUser).WithMany().HasForeignKey(x => x.RevokedByUserId).OnDelete(DeleteBehavior.SetNull);
    }

    public void Configure(EntityTypeBuilder<SeparationDocument> b)
    {
        b.ToTable("SEP_SeparationDocuments");
        b.HasKey(x => x.SeparationDocumentId);
        b.Property(x => x.DocumentNumber).HasMaxLength(100);
        b.Property(x => x.DocumentVariant).HasMaxLength(50);
        b.Property(x => x.Title).HasMaxLength(250);
        b.Property(x => x.ContentType).HasMaxLength(100);
        b.Property(x => x.Sha256Hash).HasMaxLength(64);
        b.Property(x => x.GeneratedAtUtc).HasColumnType("datetime2");

        b.HasIndex(x => new { x.StaffSeparationId, x.DocumentVariant });

        b.HasOne(x => x.StaffSeparation).WithMany(x => x.Documents).HasForeignKey(x => x.StaffSeparationId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.GeneratedByUser).WithMany().HasForeignKey(x => x.GeneratedByUserId).OnDelete(DeleteBehavior.Restrict);
    }

    public void Configure(EntityTypeBuilder<SeparationEvent> b)
    {
        b.ToTable("SEP_SeparationEvents");
        b.HasKey(x => x.SeparationEventId);
        b.Property(x => x.EventType).HasMaxLength(50);
        b.Property(x => x.EventTitle).HasMaxLength(200);
        b.Property(x => x.ActorName).HasMaxLength(150);
        b.Property(x => x.OccurredAtUtc).HasColumnType("datetime2");

        b.HasIndex(x => new { x.StaffSeparationId, x.OccurredAtUtc });

        b.HasOne(x => x.StaffSeparation).WithMany(x => x.Events).HasForeignKey(x => x.StaffSeparationId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.ActorUser).WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public partial class HrmTeContext
{
    public DbSet<SeparationType> SeparationTypes => Set<SeparationType>();
    public DbSet<ClearanceTemplate> ClearanceTemplates => Set<ClearanceTemplate>();
    public DbSet<ClearanceTemplateTask> ClearanceTemplateTasks => Set<ClearanceTemplateTask>();
    public DbSet<StaffSeparation> StaffSeparations => Set<StaffSeparation>();
    public DbSet<ClearanceTask> ClearanceTasks => Set<ClearanceTask>();
    public DbSet<SeparationWorkHandover> SeparationWorkHandovers => Set<SeparationWorkHandover>();
    public DbSet<SeparationWorkHandoverItem> SeparationWorkHandoverItems => Set<SeparationWorkHandoverItem>();
    public DbSet<SeparationAssetClearance> SeparationAssetClearances => Set<SeparationAssetClearance>();
    public DbSet<SeparationFinanceItem> SeparationFinanceItems => Set<SeparationFinanceItem>();
    public DbSet<SeparationAccessRevocation> SeparationAccessRevocations => Set<SeparationAccessRevocation>();
    public DbSet<SeparationDocument> SeparationDocuments => Set<SeparationDocument>();
    public DbSet<SeparationEvent> SeparationEvents => Set<SeparationEvent>();
}
