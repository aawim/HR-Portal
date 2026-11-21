using HRM.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Models;

public partial class HrmTeContext
{
    public DbSet<StaffGroup> StaffGroups => Set<StaffGroup>();
    public DbSet<StaffGroupMember> StaffGroupMembers => Set<StaffGroupMember>();
}

public sealed class StaffGroupMapping : IEntityTypeConfiguration<StaffGroup>
{
    public void Configure(EntityTypeBuilder<StaffGroup> b)
    {
        b.ToTable("SG_StaffGroups");
        b.HasKey(x => x.StaffGroupId);
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Code).HasMaxLength(50);
        b.Property(x => x.Description).HasMaxLength(500);
        b.Property(x => x.ScheduleMode).HasMaxLength(30).IsRequired();
        b.Property(x => x.MaximumDailyOvertimeHours).HasColumnType("decimal(5,2)");
        b.Property(x => x.MaximumWeeklyOvertimeHours).HasColumnType("decimal(5,2)");
        b.Property(x => x.OvertimeRateMultiplier).HasColumnType("decimal(5,2)");
        b.Property(x => x.DissolveReason).HasMaxLength(500);
        b.HasIndex(x => new { x.OrganisationId, x.IsActive, x.IsDissolved });

        b.HasOne(x => x.WorkTemplate)
            .WithMany()
            .HasForeignKey(x => x.WorkTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.WorkType)
            .WithMany()
            .HasForeignKey(x => x.WorkTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.LeaveSet)
            .WithMany()
            .HasForeignKey(x => x.LeaveSetId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasMany(x => x.Members)
            .WithOne(x => x.StaffGroup)
            .HasForeignKey(x => x.StaffGroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StaffGroupMemberMapping : IEntityTypeConfiguration<StaffGroupMember>
{
    public void Configure(EntityTypeBuilder<StaffGroupMember> b)
    {
        b.ToTable("SG_StaffGroupMembers");
        b.HasKey(x => x.StaffGroupMemberId);
        b.HasIndex(x => new { x.StaffGroupId, x.IsActive });
        b.HasIndex(x => new { x.OrganisationId, x.IndividualId })
            .HasDatabaseName("IX_SG_StaffGroupMembers_ActiveIndividual")
            .IsUnique()
            .HasFilter("[IsActive] = 1");
    }
}
