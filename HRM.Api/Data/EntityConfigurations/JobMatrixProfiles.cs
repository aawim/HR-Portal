using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;
public sealed class JobMatrixProfileMapping:IEntityTypeConfiguration<FrameworkJobMatrix>,IEntityTypeConfiguration<FrameworkMatrixDuty>,IEntityTypeConfiguration<FrameworkMatrixQualification>
{
    public void Configure(EntityTypeBuilder<FrameworkJobMatrix> b)
    {
        b.Property(x=>x.Description).HasColumnType("nvarchar(max)");b.Property(x=>x.PublicServiceRank).HasMaxLength(50);b.Property(x=>x.InstitutionalCategory).HasMaxLength(100);b.Property(x=>x.GradeCode).HasMaxLength(50);
        b.Property(x=>x.AttendanceBenefitDaily).HasPrecision(18,2);b.Property(x=>x.RevisedDate).HasColumnType("date");b.Property(x=>x.JobFamilyName).HasMaxLength(250);b.Property(x=>x.MisscoCode).HasMaxLength(10);
        b.Property(x=>x.LicensesAndTraining).HasColumnType("nvarchar(max)");b.Property(x=>x.FieldsOfStudy).HasColumnType("nvarchar(max)");b.Property(x=>x.ExperienceAreas).HasColumnType("nvarchar(max)");
    }
    public void Configure(EntityTypeBuilder<FrameworkMatrixDuty> b){b.ToTable("PF_JobMatrixDuties",t=>t.ExcludeFromMigrations());b.HasKey(x=>x.JobMatrixDutyId);b.Property(x=>x.JobMatrixDutyId).HasColumnName("JobMatrixDutyID");b.Property(x=>x.JobMatrixId).HasColumnName("JobMatrixID");b.Property(x=>x.DutyDescription).HasColumnType("nvarchar(max)");}
    public void Configure(EntityTypeBuilder<FrameworkMatrixQualification> b){b.ToTable("PF_JobMatrixQualifications",t=>t.ExcludeFromMigrations());b.HasKey(x=>x.JobMatrixQualificationId);b.Property(x=>x.JobMatrixQualificationId).HasColumnName("JobMatrixQualificationID");b.Property(x=>x.JobMatrixId).HasColumnName("JobMatrixID");b.Property(x=>x.RequirementText).HasColumnType("nvarchar(max)");}
}
public partial class HrmTeContext
{
    public DbSet<FrameworkMatrixDuty> FrameworkMatrixDuties=>Set<FrameworkMatrixDuty>();
    public DbSet<FrameworkMatrixQualification> FrameworkMatrixQualifications=>Set<FrameworkMatrixQualification>();
}
