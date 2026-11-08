using HRM.Models.Archives;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HRM.Models;

// The WorkTypes and AssignedWorkTypes tables are deployed legacy tables. The archived
// entity classes were removed during the model review, but the tables and their data were
// deliberately retained. These mappings re-attach them to the runtime model without letting
// EF Core create, alter or drop them.
public sealed class WorkTypeMapping : IEntityTypeConfiguration<WorkType>
{
    public void Configure(EntityTypeBuilder<WorkType> b)
    {
        b.ToTable("WorkTypes", t => t.ExcludeFromMigrations());
        b.HasKey(x => x.WorkTypeId);
        b.Property(x => x.WorkTypeId).HasColumnName("WorkTypeID").ValueGeneratedNever();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.IsValid).HasDefaultValue(true);
    }
}

public sealed class AssignedWorkTypeMapping : IEntityTypeConfiguration<AssignedWorkType>
{
    public void Configure(EntityTypeBuilder<AssignedWorkType> b)
    {
        b.ToTable("AssignedWorkTypes", t => t.ExcludeFromMigrations());
        b.HasKey(x => x.AssignedWorkTypeId);
        b.Property(x => x.AssignedWorkTypeId).HasColumnName("AssignedWorkTypeID");
        b.Property(x => x.WorkTypeId).HasColumnName("WorkTypeID");
        b.Property(x => x.JobId).HasColumnName("JobID");
        b.Property(x => x.OrganisationId).HasColumnName("OrganisationID");
        b.Property(x => x.FromDate).HasColumnType("date");
        b.Property(x => x.ToDate).HasColumnType("date");
        b.Property(x => x.Location).HasMaxLength(250);
        b.Property(x => x.IsValid).HasDefaultValue(true);
        b.Property(x => x.AssignedByUserId).HasColumnName("AssignedByUserID");
        b.Property(x => x.CreatedDate).HasColumnType("date");

        b.HasOne(x => x.WorkType).WithMany(x => x.AssignedWorkTypes)
            .HasForeignKey(x => x.WorkTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Job).WithMany()
            .HasForeignKey(x => x.JobId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Organisation).WithMany()
            .HasForeignKey(x => x.OrganisationId).OnDelete(DeleteBehavior.Restrict);

        b.HasIndex(x => x.JobId).HasDatabaseName("IX_AssignedWorkTypes_JobID");
        b.HasIndex(x => x.OrganisationId).HasDatabaseName("IX_AssignedWorkTypes_OrganisationID");
        b.HasIndex(x => x.WorkTypeId).HasDatabaseName("IX_AssignedWorkTypes_WorkTypeID");
    }
}

public partial class HrmTeContext
{
    public DbSet<WorkType> WorkTypes => Set<WorkType>();
    public DbSet<AssignedWorkType> AssignedWorkTypes => Set<AssignedWorkType>();
}
