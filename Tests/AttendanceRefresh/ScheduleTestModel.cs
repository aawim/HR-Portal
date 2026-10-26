using HRM.Models;
using HRM.Models.WorkPlanning;
using Microsoft.EntityFrameworkCore;

static class ScheduleTestModel
{
    public static Type[] EligibilityTypes => [typeof(JobPosition), typeof(JobPositionsRequest), typeof(Request), typeof(WorkTemplate), typeof(WorkTemplateSegment), typeof(WorkPlanSegment),
        typeof(WorkAssignmentSegment), typeof(WorkAssignmentOwner), typeof(WorkTemplateType), typeof(WorkSegmentType)];
    public static bool Navigation(Type type, string property) =>
        ((type == typeof(JobWorkTemplateAssignment) || type == typeof(JobWorkTemplate)) && (property == "Job" || property == "WorkTemplate")) ||
        (type == typeof(WorkPlan) && (property == "WorkAssignments" || property == "WorkPlanSegments")) ||
        (type == typeof(WorkTemplate) && property == "WorkTemplateSegments") ||
        (type == typeof(WorkTemplateSegment) && property == "WorkSegmentType") ||
        (type == typeof(WorkAssignment) && property is "WorkPlan" or "WorkTemplateType" or "WorkAssignmentSegments" or "WorkAssignmentOwners") ||
        (type == typeof(WorkAssignmentSegment) && property is "WorkAssignment" or "WorkSegmentType") ||
        (type == typeof(WorkAssignmentOwner) && property == "WorkAssignment");
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<JobWorkTemplateAssignment>().HasKey(x => x.JobWorkTemplateAssignmentID);
        builder.Entity<JobWorkTemplate>().HasKey(x => x.JobWorkTemplateId);
        builder.Entity<WorkPlan>().HasKey(x => x.WorkPlanId);
        builder.Entity<WorkAssignment>().HasKey(x => x.WorkAssignmentId);
        builder.Entity<JobWorkTemplateAssignment>().HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobID);
        builder.Entity<JobWorkTemplate>().HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobId);
        builder.Entity<WorkPlan>().HasMany(x => x.WorkAssignments).WithOne(x => x.WorkPlan).HasForeignKey(x => x.WorkPlanId);
        builder.Entity<WorkAssignment>().HasOne(x => x.WorkTemplateType).WithMany().HasForeignKey(x => x.WorkTemplateTypeId);
        builder.Entity<WorkAssignment>().HasMany(x => x.WorkAssignmentSegments).WithOne(x => x.WorkAssignment).HasForeignKey(x => x.WorkAssignmentId);
        builder.Entity<WorkAssignment>().HasMany(x => x.WorkAssignmentOwners).WithOne(x => x.WorkAssignment).HasForeignKey(x => x.WorkAssignmentId);
        builder.Entity<WorkAssignmentSegment>().HasOne(x => x.WorkSegmentType).WithMany().HasForeignKey(x => x.WorkSegmentTypeId);
        builder.Entity<JobPosition>().HasKey(x => x.JobPositionId);
        builder.Entity<JobPositionsRequest>().HasKey(x => x.JobPositionsRequestId);
        builder.Entity<Request>().HasKey(x => x.RequestId);
        builder.Entity<WorkTemplate>().HasKey(x => x.WorkTemplateId);
        builder.Entity<WorkTemplateSegment>().HasKey(x => x.WorkTemplateSegmentId);
        builder.Entity<WorkTemplateSegment>().HasOne(x=>x.WorkSegmentType).WithMany().HasForeignKey(x=>x.WorkSegmentTypeId);
        builder.Entity<WorkPlanSegment>().HasKey(x => x.WorkPlanSegmentId);
        builder.Entity<JobWorkTemplateAssignment>().HasOne(x => x.WorkTemplate).WithMany().HasForeignKey(x => x.WorkTemplateID);
        builder.Entity<JobWorkTemplate>().HasOne(x => x.WorkTemplate).WithMany().HasForeignKey(x => x.WorkTemplateId);
        builder.Entity<WorkTemplate>().HasMany(x => x.WorkTemplateSegments).WithOne().HasForeignKey(x => x.WorkTemplateId);
        builder.Entity<WorkPlan>().HasMany(x => x.WorkPlanSegments).WithOne().HasForeignKey(x => x.WorkPlanId);
    }
}
