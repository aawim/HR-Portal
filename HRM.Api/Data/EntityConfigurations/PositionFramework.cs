using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;
public sealed class PositionFrameworkMapping:IEntityTypeConfiguration<PositionFramework>,IEntityTypeConfiguration<PositionMatrix>,IEntityTypeConfiguration<FrameworkJobMatrix>,IEntityTypeConfiguration<FrameworkJobTitle>,IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> b){b.Property(x=>x.PositionFrameworkId).HasColumnName("PositionFrameworkID");b.Property(x=>x.PositionMatrixId).HasColumnName("PositionMatrixID");}
    public void Configure(EntityTypeBuilder<PositionFramework> b){b.ToTable("PF_Frameworks",t=>t.ExcludeFromMigrations());b.HasKey(x=>x.FrameworkId);b.Property(x=>x.FrameworkId).HasColumnName("FrameworkID").ValueGeneratedNever();b.Property(x=>x.Code).HasMaxLength(30).IsUnicode(false);b.Property(x=>x.Name).HasMaxLength(150);}
    public void Configure(EntityTypeBuilder<PositionMatrix> b){b.ToTable("PF_PositionMatrix",t=>t.ExcludeFromMigrations());b.HasKey(x=>x.PositionMatrixId);b.Property(x=>x.PositionMatrixId).HasColumnName("PositionMatrixID");b.Property(x=>x.OrganisationId).HasColumnName("OrganisationID");b.Property(x=>x.OrganisationStructureId).HasColumnName("OrganisationStructureID");b.Property(x=>x.JobMatrixId).HasColumnName("JobMatrixID");b.Property(x=>x.JobTitleId).HasColumnName("JobTitleID");b.Property(x=>x.JobMatrixStepId).HasColumnName("JobMatrixStepID");b.Property(x=>x.EffectiveFrom).HasColumnType("date");b.Property(x=>x.EffectiveTo).HasColumnType("date");}
    public void Configure(EntityTypeBuilder<FrameworkJobMatrix> b){b.ToTable("PF_JobMatrix",t=>t.ExcludeFromMigrations());b.HasKey(x=>x.JobMatrixId);b.Property(x=>x.JobMatrixId).HasColumnName("JobMatrixID");b.Property(x=>x.BasicSalary).HasPrecision(18,2);b.Property(x=>x.MatrixCode).HasMaxLength(60);b.Property(x=>x.Name).HasMaxLength(250);b.Property(x=>x.FrameworkId).HasColumnName("FrameworkID");b.Property(x=>x.CurrencyTypeId).HasColumnName("CurrencyTypeID");b.Property(x=>x.OperationLogId).HasColumnName("OperationLogID");b.Property(x=>x.EffectiveFrom).HasColumnType("date");b.Property(x=>x.EffectiveTo).HasColumnType("date");}
    public void Configure(EntityTypeBuilder<FrameworkJobTitle> b){b.ToTable("PF_JobTitles",t=>t.ExcludeFromMigrations());b.HasKey(x=>x.JobTitleId);b.Property(x=>x.JobTitleId).HasColumnName("JobTitleID");b.Property(x=>x.Name).HasMaxLength(250);}
}
public partial class HrmTeContext
{
    public DbSet<PositionFramework> PositionFrameworks=>Set<PositionFramework>();
    public DbSet<PositionMatrix> PositionMatrices=>Set<PositionMatrix>();
    public DbSet<FrameworkJobMatrix> FrameworkJobMatrices=>Set<FrameworkJobMatrix>();
    public DbSet<FrameworkJobTitle> FrameworkJobTitles=>Set<FrameworkJobTitle>();
}
