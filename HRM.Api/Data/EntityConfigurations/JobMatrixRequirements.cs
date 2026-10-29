using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;
public sealed class MatrixRequirementsMapping:IEntityTypeConfiguration<MatrixCredentialRequirement>,IEntityTypeConfiguration<MatrixQualificationPathway>,IEntityTypeConfiguration<MatrixQualificationCondition>
{
    public void Configure(EntityTypeBuilder<MatrixCredentialRequirement> b)
    {
        b.ToTable("PF_CredentialRequirements",t=>{t.HasCheckConstraint("CK_PF_CredentialRequirement_Kind","[Kind] IN ('Licence','Registration','Certification','Training')");t.HasCheckConstraint("CK_PF_CredentialRequirement_Status","[Requirement] IN ('Mandatory','Preferred','Conditional')");t.HasCheckConstraint("CK_PF_CredentialRequirement_Amounts","([ValidityMonths] IS NULL OR [ValidityMonths] BETWEEN 1 AND 600) AND ([TrainingHours] IS NULL OR [TrainingHours] BETWEEN 0 AND 10000)");});b.HasKey(x=>x.Id);
        b.Property(x=>x.Name).HasMaxLength(250);b.Property(x=>x.Kind).HasMaxLength(30);b.Property(x=>x.Requirement).HasMaxLength(30);b.Property(x=>x.Issuer).HasMaxLength(250);b.Property(x=>x.Condition).HasMaxLength(2000);b.Property(x=>x.Equivalents).HasMaxLength(2000);b.Property(x=>x.VerificationInstructions).HasMaxLength(2000);b.Property(x=>x.SourceReference).HasMaxLength(2000);b.Property(x=>x.TrainingHours).HasPrecision(10,2);
        b.HasIndex(x=>new{x.JobMatrixId,x.IsActive});b.HasOne<FrameworkJobMatrix>().WithMany().HasForeignKey(x=>x.JobMatrixId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<MatrixQualificationPathway> b)
    {
        b.ToTable("PF_QualificationPathways",t=>t.HasCheckConstraint("CK_PF_QualificationPathway_Months","[RelevantExperienceMonths] BETWEEN 0 AND 1200 AND [PostQualificationMonths] BETWEEN 0 AND 1200 AND [ManagerialExperienceMonths] BETWEEN 0 AND 1200"));b.HasKey(x=>x.Id);b.Property(x=>x.Name).HasMaxLength(250);b.Property(x=>x.Conditions).HasMaxLength(4000);b.Property(x=>x.SourceReference).HasMaxLength(2000);
        b.HasIndex(x=>new{x.JobMatrixId,x.IsActive});b.HasOne<FrameworkJobMatrix>().WithMany().HasForeignKey(x=>x.JobMatrixId).OnDelete(DeleteBehavior.Restrict);
    }
    public void Configure(EntityTypeBuilder<MatrixQualificationCondition> b)
    {
        b.ToTable("PF_PathwayQualifications",t=>t.HasCheckConstraint("CK_PF_PathwayQualification_Levels","([MinimumLevel] IS NULL AND [MaximumLevel] IS NULL) OR ([MinimumLevel] IS NOT NULL AND [MinimumLevel] BETWEEN 1 AND 10 AND ([MaximumLevel] IS NULL OR [MaximumLevel] BETWEEN [MinimumLevel] AND 10))"));b.HasKey(x=>x.Id);b.Property(x=>x.Qualification).HasMaxLength(250);b.Property(x=>x.FieldOfStudy).HasMaxLength(1000);
        b.HasIndex(x=>new{x.PathwayId,x.IsActive});b.HasOne<MatrixQualificationPathway>().WithMany().HasForeignKey(x=>x.PathwayId).OnDelete(DeleteBehavior.Restrict);
    }
}
public partial class HrmTeContext
{
    public DbSet<MatrixCredentialRequirement> MatrixCredentialRequirements=>Set<MatrixCredentialRequirement>();
    public DbSet<MatrixQualificationPathway> MatrixQualificationPathways=>Set<MatrixQualificationPathway>();
    public DbSet<MatrixQualificationCondition> MatrixQualificationConditions=>Set<MatrixQualificationCondition>();
}
