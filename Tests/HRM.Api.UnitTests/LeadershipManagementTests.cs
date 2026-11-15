using HRM.Models.Leadership;
using HRM.Services;
using HRM.DTOs.Leadership;
using Xunit;
namespace HRM.Api.UnitTests;
public sealed class LeadershipManagementTests
{
 [Fact]
 public async Task Editing_is_audited_and_revocation_cannot_be_undone_by_editing()
 {
  var factory=new QueryFactory();var access=new FakeAccess();access.Permissions.Add("Leadership.Appointment.Manage");var day=DateOnly.FromDateTime(DateTime.UtcNow);
  await using(var db=factory.CreateDbContext()){db.LeadershipAppointments.Add(new LeadershipAppointment{LeadershipAppointmentId=1,IndividualId=100,PositionTitle="Minister",EffectiveFrom=day});await db.SaveChangesAsync();}
  var service=new LeadershipAdministrationService(factory,access);var edit=await service.GetRecordAsync("Appointment",1);edit.Description="State Minister";Assert.True((await service.UpdateRecordAsync(edit)).Success);Assert.Single(await service.ListAuditAsync());
  edit.To=day.AddDays(-1);Assert.False((await service.UpdateRecordAsync(edit)).Success);edit.To=null;
  Assert.True((await service.RevokeRecordAsync("Appointment",1)).Success);Assert.False((await service.UpdateRecordAsync(edit)).Success);
 }
 [Fact]
 public async Task Attendance_policy_is_scoped_to_administrative_organisation_and_expires()
 {
  var factory=new QueryFactory();var day=DateOnly.FromDateTime(DateTime.UtcNow);
  await using var db=factory.CreateDbContext();db.LeadershipAppointments.Add(new LeadershipAppointment{LeadershipAppointmentId=1,IndividualId=100,AdministrativeOrganisationId=10,EffectiveFrom=day.AddDays(-10)});db.LeadershipAttendancePolicies.Add(new LeadershipAttendancePolicy{LeadershipAppointmentId=1,ManagingOrganisationId=10,AttendanceRequired=false,EffectiveFrom=day.AddDays(-1),EffectiveTo=day.AddDays(1)});await db.SaveChangesAsync();
  Assert.False((await LeadershipAttendancePolicyResolver.ResolveAsync(db,100,10,day))!.AttendanceRequired);
  await Assert.ThrowsAsync<InvalidOperationException>(()=>LeadershipAttendancePolicyResolver.ResolveAsync(db,100,20,day));
  Assert.Null(await LeadershipAttendancePolicyResolver.ResolveAsync(db,100,10,day.AddDays(2)));
  var appointment=await db.LeadershipAppointments.FindAsync(1);appointment!.Status="Revoked";await db.SaveChangesAsync();Assert.Null(await LeadershipAttendancePolicyResolver.ResolveAsync(db,100,10,day));
 }
}
