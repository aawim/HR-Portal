using Microsoft.EntityFrameworkCore;
namespace HRM.Models;

public partial class HrmTeContext
{
    public DbSet<UserTodo> UserTodos => Set<UserTodo>();

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration<HRM.Models.Leadership.LeadershipAppointmentType>(new HRM.Api.Data.EntityConfigurations.LeadershipAppointmentsMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.Leadership.LeadershipAppointment>(new HRM.Api.Data.EntityConfigurations.LeadershipAppointmentsMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.Leadership.LeadershipAppointmentOrganisation>(new HRM.Api.Data.EntityConfigurations.LeadershipAppointmentsMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.Leadership.LeadershipAuthorityGrant>(new HRM.Api.Data.EntityConfigurations.LeadershipAppointmentsMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.Leadership.LeadershipAttendancePolicy>(new HRM.Api.Data.EntityConfigurations.LeadershipAppointmentsMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.Leadership.LeadershipDeviceAccess>(new HRM.Api.Data.EntityConfigurations.LeadershipAppointmentsMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.Leadership.LeadershipDelegation>(new HRM.Api.Data.EntityConfigurations.LeadershipAppointmentsMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.Leadership.LeadershipAuditLog>(new HRM.Api.Data.EntityConfigurations.LeadershipAppointmentsMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.EdgeAttendance.EdgeAttendanceAgent>(new HRM.Api.Data.EntityConfigurations.EdgeAttendanceMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.EdgeAttendance.EdgeAttendanceTrustedIssuer>(new HRM.Api.Data.EntityConfigurations.EdgeAttendanceMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.EdgeAttendance.EdgeAttendanceAgentDevice>(new HRM.Api.Data.EntityConfigurations.EdgeAttendanceMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.EdgeAttendance.EdgeAttendanceEventReceipt>(new HRM.Api.Data.EntityConfigurations.EdgeAttendanceMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.EdgeAttendance.EdgeAttendanceIngestionFailure>(new HRM.Api.Data.EntityConfigurations.EdgeAttendanceMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.Recruitment.RecruitmentPolicyProfile>(new HRM.Api.Data.EntityConfigurations.RecruitmentComplianceMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.Recruitment.RecruitmentPolicyVersion>(new HRM.Api.Data.EntityConfigurations.RecruitmentComplianceMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.Recruitment.RecruitmentComplianceRule>(new HRM.Api.Data.EntityConfigurations.RecruitmentComplianceMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.Recruitment.RecruitmentPolicyAssignment>(new HRM.Api.Data.EntityConfigurations.RecruitmentComplianceMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.Recruitment.RecruitmentRequisition>(new HRM.Api.Data.EntityConfigurations.RecruitmentOperationsMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.Recruitment.RecruitmentPositionReservation>(new HRM.Api.Data.EntityConfigurations.RecruitmentOperationsMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.Recruitment.RecruitmentVacancy>(new HRM.Api.Data.EntityConfigurations.RecruitmentOperationsMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.Recruitment.RecruitmentApplicant>(new HRM.Api.Data.EntityConfigurations.RecruitmentApplicantsMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.Recruitment.RecruitmentApplication>(new HRM.Api.Data.EntityConfigurations.RecruitmentApplicantsMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.Recruitment.RecruitmentOfficialTemplate>(new HRM.Api.Data.EntityConfigurations.RecruitmentOfficialTemplatesMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.Recruitment.RecruitmentScreeningDecision>(new HRM.Api.Data.EntityConfigurations.RecruitmentScreeningMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.Recruitment.RecruitmentPanelConflictDeclaration>(new HRM.Api.Data.EntityConfigurations.RecruitmentScreeningMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.Recruitment.RecruitmentAssessment>(new HRM.Api.Data.EntityConfigurations.RecruitmentAssessmentsMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.Recruitment.RecruitmentPanelScorecard>(new HRM.Api.Data.EntityConfigurations.RecruitmentAssessmentsMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.Recruitment.RecruitmentResultPublication>(new HRM.Api.Data.EntityConfigurations.RecruitmentResultsMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.Recruitment.RecruitmentMeritResult>(new HRM.Api.Data.EntityConfigurations.RecruitmentResultsMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.Recruitment.RecruitmentObjection>(new HRM.Api.Data.EntityConfigurations.RecruitmentResultsMapping());
        modelBuilder.ApplyConfiguration<HRM.Models.Recruitment.RecruitmentOffer>(new HRM.Api.Data.EntityConfigurations.RecruitmentOffersMapping());
        modelBuilder.Entity<UserTodo>(entity =>
        {
            entity.ToTable("UT_UserTodos", "dbo", table => table.HasCheckConstraint("CK_UserTodos_Priority", "[Priority] IN ('Normal','Medium','High')"));
            entity.HasKey(x => x.UserTodoId);
            entity.Property(x => x.Title).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.Priority).HasMaxLength(10).IsRequired();
            entity.Property(x => x.DueDate).HasColumnType("date");
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(x => new { x.UserId, x.IsDeleted, x.IsCompleted, x.DueDate });
        });
    }
}
