using HRM.Enum;
using HRM.Models.Leadership;
using HRM.Services.Attendance.Reconciliation;
using Xunit;
namespace HRM.Api.UnitTests;
public sealed partial class AttendanceReconciliationTests
{
 [Theory]
 [InlineData(false,AttendanceDailyStatus.Exempt)]
 [InlineData(true,AttendanceDailyStatus.Incomplete)]
 public async Task Leadership_policy_controls_real_reconciliation_without_fabricated_payable_time(bool required,AttendanceDailyStatus expected)
 {
  var factory=new QueryFactory(ignoreTransactions:true);
  await using(var db=factory.CreateDbContext()){
   await SeedPlanAsync(db,DefaultWorkDate,(new TimeOnly(8,0),new TimeOnly(14,0),0,false));
   db.LeadershipAppointments.Add(new LeadershipAppointment{LeadershipAppointmentId=91,IndividualId=IndividualId,AdministrativeOrganisationId=OrgId,EffectiveFrom=DefaultWorkDate.AddDays(-1)});
   db.LeadershipAttendancePolicies.Add(new LeadershipAttendancePolicy{LeadershipAppointmentId=91,ManagingOrganisationId=OrgId,AttendanceRequired=required,EffectiveFrom=DefaultWorkDate.AddDays(-1)});await db.SaveChangesAsync();
  }
  var result=await new AttendanceReconciliationService(factory).ReconcileAsync(IndividualId,JobId,OrgId,DefaultWorkDate);
  Assert.Equal(expected,result.DailyStatus);Assert.Equal(0,result.TotalCreditedPayableMinutes);
 }
}
