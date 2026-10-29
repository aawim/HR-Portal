using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;

public sealed class PayFrameworkMapping:IEntityTypeConfiguration<FrameworkJobMatrixStep>,IEntityTypeConfiguration<FrameworkJobMatrixTitle>,
    IEntityTypeConfiguration<FrameworkJobMatrixBenefit>,IEntityTypeConfiguration<FrameworkPositionMatrixHistory>,IEntityTypeConfiguration<FrameworkJobStepHistory>
{
    public void Configure(EntityTypeBuilder<FrameworkJobMatrixStep> b)
    {
        b.ToTable("PF_JobMatrixSteps",t=>t.ExcludeFromMigrations());b.HasKey(x=>x.JobMatrixStepId);
        b.Property(x=>x.JobMatrixStepId).HasColumnName("JobMatrixStepID");b.Property(x=>x.JobMatrixId).HasColumnName("JobMatrixID");
        b.Property(x=>x.JobAllowance).HasPrecision(18,2);b.Property(x=>x.BasicSalaryOverride).HasPrecision(18,2);
    }
    public void Configure(EntityTypeBuilder<FrameworkJobMatrixTitle> b)
    {
        b.ToTable("PF_JobMatrixTitles",t=>t.ExcludeFromMigrations());b.HasKey(x=>new{x.JobMatrixId,x.JobTitleId});
        b.Property(x=>x.JobMatrixId).HasColumnName("JobMatrixID");b.Property(x=>x.JobTitleId).HasColumnName("JobTitleID");
    }
    public void Configure(EntityTypeBuilder<FrameworkJobMatrixBenefit> b)
    {
        b.ToTable("PF_JobMatrixBenefits",t=>t.ExcludeFromMigrations());b.HasKey(x=>x.JobMatrixBenefitId);
        b.Property(x=>x.JobMatrixBenefitId).HasColumnName("JobMatrixBenefitID");b.Property(x=>x.JobMatrixId).HasColumnName("JobMatrixID");
        b.Property(x=>x.Amount).HasPrecision(18,2);b.Property(x=>x.BenefitName).HasMaxLength(200);b.Property(x=>x.FrequencyCode).HasMaxLength(20).IsUnicode(false);
    }
    public void Configure(EntityTypeBuilder<FrameworkPositionMatrixHistory> b)
    {
        b.ToTable("PF_PositionMatrixHistory",t=>t.ExcludeFromMigrations());b.HasKey(x=>x.PositionMatrixHistoryId);
        b.Property(x=>x.PositionMatrixHistoryId).HasColumnName("PositionMatrixHistoryID");b.Property(x=>x.PositionMatrixId).HasColumnName("PositionMatrixID");
        b.Property(x=>x.OperationLogId).HasColumnName("OperationLogID");b.Property(x=>x.ChangeReason).HasMaxLength(1000);
    }
    public void Configure(EntityTypeBuilder<FrameworkJobStepHistory> b)
    {
        b.ToTable("PF_JobStepHistory",t=>t.ExcludeFromMigrations());b.HasKey(x=>x.JobStepHistoryId);
        b.Property(x=>x.JobStepHistoryId).HasColumnName("JobStepHistoryID");b.Property(x=>x.PositionMatrixId).HasColumnName("PositionMatrixID");
        b.Property(x=>x.JobId).HasColumnName("JobID");b.Property(x=>x.JobMatrixStepId).HasColumnName("JobMatrixStepID");b.Property(x=>x.OperationLogId).HasColumnName("OperationLogID");
        b.Property(x=>x.BasicSalarySnapshot).HasPrecision(18,2);b.Property(x=>x.JobAllowanceSnapshot).HasPrecision(18,2);
        b.Property(x=>x.EffectiveFrom).HasColumnType("date");b.Property(x=>x.EffectiveTo).HasColumnType("date");
    }
}
public partial class HrmTeContext
{
    public DbSet<FrameworkJobMatrixStep> FrameworkJobMatrixSteps=>Set<FrameworkJobMatrixStep>();
    public DbSet<FrameworkJobMatrixTitle> FrameworkJobMatrixTitles=>Set<FrameworkJobMatrixTitle>();
    public DbSet<FrameworkJobMatrixBenefit> FrameworkJobMatrixBenefits=>Set<FrameworkJobMatrixBenefit>();
    public DbSet<FrameworkPositionMatrixHistory> FrameworkPositionMatrixHistories=>Set<FrameworkPositionMatrixHistory>();
    public DbSet<FrameworkJobStepHistory> FrameworkJobStepHistories=>Set<FrameworkJobStepHistory>();
}
