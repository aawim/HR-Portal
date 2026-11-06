using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Models;

public sealed class ProjectMapping : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> b)
    {
        b.ToTable("PR_Projects");
        b.HasKey(x => x.ProjectId);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Reference).HasMaxLength(50).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.ProjectManagerName).HasMaxLength(200);
        b.Property(x => x.Status).HasMaxLength(50);
        b.HasOne(x => x.Team).WithMany().HasForeignKey(x => x.TeamId).OnDelete(DeleteBehavior.SetNull);
        b.HasMany(x => x.Members).WithOne(x => x.Project).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.Tasks).WithOne(x => x.Project).HasForeignKey(x => x.ProjectId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.OrganisationId, x.Status });
    }
}

public sealed class ProjectMemberMapping : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> b)
    {
        b.ToTable("PR_ProjectMembers");
        b.HasKey(x => x.ProjectMemberId);
        b.Property(x => x.StaffName).HasMaxLength(200);
        b.Property(x => x.Role).HasMaxLength(100);
        b.HasIndex(x => new { x.ProjectId, x.StaffId });
    }
}

public sealed class ProjectTaskMapping : IEntityTypeConfiguration<ProjectTask>
{
    public void Configure(EntityTypeBuilder<ProjectTask> b)
    {
        b.ToTable("PR_ProjectTasks");
        b.HasKey(x => x.TaskId);
        b.Property(x => x.TaskReference).HasMaxLength(50).IsRequired();
        b.Property(x => x.Title).HasMaxLength(200).IsRequired();
        b.Property(x => x.Description).HasMaxLength(1000);
        b.Property(x => x.AssignedStaffName).HasMaxLength(200);
        b.Property(x => x.Status).HasMaxLength(50);
        b.Property(x => x.Priority).HasMaxLength(20);
        b.Property(x => x.Remarks).HasMaxLength(1000);
        b.HasIndex(x => new { x.ProjectId, x.Status });
        b.HasIndex(x => x.AssignedStaffId);
    }
}

public sealed class EvaluationMapping : IEntityTypeConfiguration<Evaluation>
{
    public void Configure(EntityTypeBuilder<Evaluation> b)
    {
        b.ToTable("EV_Evaluations");
        b.HasKey(x => x.EvaluationId);
        b.Property(x => x.StaffName).HasMaxLength(200);
        b.Property(x => x.EvaluatorName).HasMaxLength(200);
        b.Property(x => x.Period).HasMaxLength(50);
        b.Property(x => x.OverallRating).HasColumnType("decimal(3,1)");
        b.Property(x => x.Status).HasMaxLength(50);
        b.Property(x => x.GeneralComments).HasMaxLength(2000);
        b.Property(x => x.Strengths).HasMaxLength(1000);
        b.Property(x => x.AreasForImprovement).HasMaxLength(1000);
        b.Property(x => x.Goals).HasMaxLength(1000);
        b.HasMany(x => x.Scores).WithOne(x => x.Evaluation).HasForeignKey(x => x.EvaluationId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.OrganisationId, x.StaffId });
    }
}

public sealed class EvaluationScoreMapping : IEntityTypeConfiguration<EvaluationScore>
{
    public void Configure(EntityTypeBuilder<EvaluationScore> b)
    {
        b.ToTable("EV_EvaluationScores");
        b.HasKey(x => x.ScoreId);
        b.Property(x => x.CriteriaName).HasMaxLength(100).IsRequired();
        b.Property(x => x.Comments).HasMaxLength(500);
        b.HasIndex(x => x.EvaluationId);
    }
}

public partial class HrmTeContext
{
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<ProjectMember> ProjectMembers => Set<ProjectMember>();
    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();
    public DbSet<Evaluation> Evaluations => Set<Evaluation>();
    public DbSet<EvaluationScore> EvaluationScores => Set<EvaluationScore>();
}
