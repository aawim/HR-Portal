using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace HRM.Models;

public sealed class JobTypeOvertimeRuleMapping : IEntityTypeConfiguration<JobTypeOvertimeRule>
{
    public void Configure(EntityTypeBuilder<JobTypeOvertimeRule> b)
    {
        b.ToTable("OT_JobTypeOvertimeRules", t => t.HasCheckConstraint("CK_OT_JobTypeOvertimeRules_Limits",
            "([IsAllowed] = 0 AND [MaximumWeeklyHours] = 0 AND [MaximumDailyHours] IS NULL) OR ([IsAllowed] = 1 AND [MaximumWeeklyHours] > 0 AND [MaximumWeeklyHours] <= 168 AND ([MaximumDailyHours] IS NULL OR ([MaximumDailyHours] > 0 AND [MaximumDailyHours] <= 24 AND [MaximumDailyHours] <= [MaximumWeeklyHours])))"));
        b.HasKey(x => x.Id);
        b.Property(x => x.MaximumWeeklyHours).HasPrecision(5, 2);
        b.Property(x => x.MaximumDailyHours).HasPrecision(5, 2);
        b.Property(x => x.Version).IsConcurrencyToken();
        b.HasIndex(x => new { x.OrganisationId, x.JobTypeId }).IsUnique();
        b.HasOne<Organisation>().WithMany().HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<JobType>().WithMany().HasForeignKey(x => x.JobTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne<OperationLog>().WithMany().HasForeignKey(x => x.OperationLogId).OnDelete(DeleteBehavior.Restrict);
    }
}


public partial class HrmTeContext
{
    public DbSet<JobTypeOvertimeRule> JobTypeOvertimeRules => Set<JobTypeOvertimeRule>();
}
