using HRM.Models;
using HRM.Models.WorkPlanning;
using Microsoft.EntityFrameworkCore;

static class ScheduleTestModel
{
    public static bool Navigation(Type type, string property) =>
        ((type == typeof(JobWorkTemplateAssignment) || type == typeof(JobWorkTemplate)) && property == "Job") ||
        (type == typeof(WorkPlan) && property == "WorkAssignments");
    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<JobWorkTemplateAssignment>().HasKey(x => x.JobWorkTemplateAssignmentID);
        builder.Entity<JobWorkTemplate>().HasKey(x => x.JobWorkTemplateId);
        builder.Entity<WorkPlan>().HasKey(x => x.WorkPlanId);
        builder.Entity<WorkAssignment>().HasKey(x => x.WorkAssignmentId);
        builder.Entity<JobWorkTemplateAssignment>().HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobID);
        builder.Entity<JobWorkTemplate>().HasOne(x => x.Job).WithMany().HasForeignKey(x => x.JobId);
        builder.Entity<WorkPlan>().HasMany(x => x.WorkAssignments).WithOne().HasForeignKey(x => x.WorkPlanId);
    }
}
