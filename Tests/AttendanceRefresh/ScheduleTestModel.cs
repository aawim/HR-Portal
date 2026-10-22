using HRM.Models;
using HRM.Models.WorkPlanning;
using Microsoft.EntityFrameworkCore;

static class ScheduleTestModel
{
    public static Type[] EligibilityTypes => [typeof(JobPosition), typeof(JobPositionsRequest), typeof(Request), typeof(WorkTemplate), typeof(WorkTemplateSegment), typeof(WorkPlanSegment)];
    public static bool Navigation(Type type, string property) =>
        ((type == typeof(JobWorkTemplateAssignment) || type == typeof(JobWorkTemplate)) && (property == "Job" || property == "WorkTemplate")) ||
        (type == typeof(WorkPlan) && (property == "WorkAssignments" || property == "WorkPlanSegments")) ||
        (type == typeof(WorkTemplate) && property == "WorkTemplateSegments");
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<JobWorkTemplateAssignment>().HasKey(x => x.JobWorkTemplateAssignmentID);
        builder.Entity<JobWorkTemplate>().HasKey(x => x.JobWorkTemplateId);
        builder.Entity<WorkPlan>().HasKey(x => x.WorkPlanId);
        builder.Entity<WorkAssignment>().HasKey(x => x.WorkAssignmentId);
        builder.Entity<JobWorkTemplateAssignment>().HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobID);
        builder.Entity<JobWorkTemplate>().HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobId);
        builder.Entity<WorkPlan>().HasMany(x => x.WorkAssignments).WithOne().HasForeignKey(x => x.WorkPlanId);
        builder.Entity<JobPosition>().HasKey(x => x.JobPositionId);
        builder.Entity<JobPositionsRequest>().HasKey(x => x.JobPositionsRequestId);
        builder.Entity<Request>().HasKey(x => x.RequestId);
        builder.Entity<WorkTemplate>().HasKey(x => x.WorkTemplateId);
        builder.Entity<WorkTemplateSegment>().HasKey(x => x.WorkTemplateSegmentId);
        builder.Entity<WorkPlanSegment>().HasKey(x => x.WorkPlanSegmentId);
        builder.Entity<JobWorkTemplateAssignment>().HasOne(x => x.WorkTemplate).WithMany().HasForeignKey(x => x.WorkTemplateID);
        builder.Entity<JobWorkTemplate>().HasOne(x => x.WorkTemplate).WithMany().HasForeignKey(x => x.WorkTemplateId);
        builder.Entity<WorkTemplate>().HasMany(x => x.WorkTemplateSegments).WithOne().HasForeignKey(x => x.WorkTemplateId);
        builder.Entity<WorkPlan>().HasMany(x => x.WorkPlanSegments).WithOne().HasForeignKey(x => x.WorkPlanId);
    }
}
