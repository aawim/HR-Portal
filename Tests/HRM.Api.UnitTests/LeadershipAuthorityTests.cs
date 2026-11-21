using HRM.Models.Leadership;
using HRM.Services;
using Xunit;
namespace HRM.Api.UnitTests;
public sealed class LeadershipAuthorityTests
{
 [Theory]
 [InlineData("Active",true,0,2,true)]
 [InlineData("Revoked",true,0,2,false)]
 [InlineData("Active",false,0,2,false)]
 [InlineData("Active",true,-2,-1,false)]
 public async Task Authority_requires_active_scoped_unexpired_grant(string status,bool enabled,int from,int to,bool expected)
 {
  var factory=new QueryFactory();var access=new FakeAccess();var day=DateOnly.FromDateTime(DateTime.UtcNow);
  await using(var db=factory.CreateDbContext()){
   db.LeadershipAppointments.Add(new LeadershipAppointment{LeadershipAppointmentId=1,IndividualId=100,Status=status,EffectiveFrom=day.AddDays(-10)});
   db.LeadershipAppointmentOrganisations.Add(new LeadershipAppointmentOrganisation{LeadershipAppointmentOrganisationId=1,LeadershipAppointmentId=1,OrganisationId=20,EffectiveFrom=day.AddDays(-10)});
   db.LeadershipAuthorityGrants.Add(new LeadershipAuthorityGrant{LeadershipAppointmentOrganisationId=1,PermissionCode="Leave.Approve",IsActive=enabled,ValidFrom=day.AddDays(from),ValidTo=day.AddDays(to)});await db.SaveChangesAsync();
  }
  var service=new LeadershipAuthorityService(factory,access);
  Assert.Equal(expected,await service.HasAuthorityAsync(20,"Leave.Approve"));
  Assert.False(await service.HasAuthorityAsync(10,"Leave.Approve"));
  Assert.False(await service.HasAuthorityAsync(20,"Payroll.Approve"));
  Assert.Equal(10,access.Organisation);
 }
 [Fact]
 public async Task Ordinary_hr_access_does_not_allow_leadership_administration()
 {
  var service=new LeadershipAdministrationService(new QueryFactory(),new FakeAccess());
  await Assert.ThrowsAsync<UnauthorizedAccessException>(()=>service.ListRecordsAsync());
 }
}
