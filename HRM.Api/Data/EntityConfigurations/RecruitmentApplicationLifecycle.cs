using HRM.Models;
using HRM.Models.Recruitment;
using Microsoft.EntityFrameworkCore;
namespace HRM.Api.Data.EntityConfigurations;
public static class RecruitmentApplicationLifecycleMapping
{
    public static void ConfigureRecruitmentLifecycle(this ModelBuilder b)
    {
        b.Entity<RecruitmentApplicationDocument>(e=>{
            e.ToTable("RM_RecruitmentApplicationDocuments");e.HasKey(x=>x.Id);
            e.Property(x=>x.Category).HasMaxLength(40).IsRequired();e.Property(x=>x.FileName).HasMaxLength(260).IsRequired();
            e.HasOne<RecruitmentApplication>().WithMany().HasForeignKey(x=>x.ApplicationId).OnDelete(DeleteBehavior.Restrict);
            e.HasOne<DmsStoredObject>().WithMany().HasForeignKey(x=>x.StoredObjectId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x=>new{x.ApplicationId,x.StoredObjectId}).IsUnique();
        });
        b.Entity<RecruitmentApplicationEvent>(e=>{
            e.ToTable("RM_RecruitmentApplicationEvents");e.HasKey(x=>x.Id);
            e.Property(x=>x.Kind).HasMaxLength(40).IsRequired();e.Property(x=>x.Message).HasMaxLength(2000).IsRequired();
            e.Property(x=>x.SnapshotJson).HasMaxLength(16000).IsRequired();
            e.HasOne<RecruitmentApplication>().WithMany().HasForeignKey(x=>x.ApplicationId).OnDelete(DeleteBehavior.Restrict);
            e.HasIndex(x=>new{x.ApplicationId,x.CreatedAtUtc});
        });
    }
}
