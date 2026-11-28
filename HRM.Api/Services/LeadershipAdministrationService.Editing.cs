using HRM.DTOs;
using HRM.DTOs.Leadership;
using HRM.Models.Leadership;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;
public sealed partial class LeadershipAdministrationService
{
 public async Task<LeadershipRecordEditInput> GetRecordAsync(string kind,int id,CancellationToken ct=default)
 {
  await RequireAdministratorAsync();await using var db=await factory.CreateDbContextAsync(ct);var result=new LeadershipRecordEditInput{Kind=kind,Id=id};
  switch(kind){case "Appointment":var a=await db.LeadershipAppointments.AsNoTracking().SingleAsync(x=>x.LeadershipAppointmentId==id,ct);result.Description=a.PositionTitle;result.From=a.EffectiveFrom;result.To=a.EffectiveTo;break;
  case "Responsibility":var r=await db.LeadershipAppointmentOrganisations.AsNoTracking().SingleAsync(x=>x.LeadershipAppointmentOrganisationId==id,ct);result.Description=r.RelationshipType;result.From=r.EffectiveFrom;result.To=r.EffectiveTo;break;
  case "Grant":var g=await db.LeadershipAuthorityGrants.AsNoTracking().SingleAsync(x=>x.LeadershipAuthorityGrantId==id,ct);result.PermissionCode=g.PermissionCode;result.From=g.ValidFrom;result.To=g.ValidTo;break;
  case "Delegation":var d=await db.LeadershipDelegations.AsNoTracking().SingleAsync(x=>x.LeadershipDelegationId==id,ct);result.PermissionCode=d.PermissionScope;result.DelegateUserId=d.ToUserId;result.From=d.ValidFrom;result.To=d.ValidTo;break;
  case "AttendancePolicy":var p=await db.LeadershipAttendancePolicies.AsNoTracking().SingleAsync(x=>x.LeadershipAttendancePolicyId==id,ct);result.Description=p.VerificationMode;result.AttendanceRequired=p.AttendanceRequired;result.From=p.EffectiveFrom;result.To=p.EffectiveTo;break;
  default:throw new ArgumentException("Unsupported leadership record.");}return result;
 }
 public async Task<ServiceResult> UpdateRecordAsync(LeadershipRecordEditInput input,CancellationToken ct=default)
 {
  var actor=await RequireAdministratorAsync();if(input.Id<=0||input.From==default||input.To<input.From)return ServiceResult.Fail("A valid record, start date and end date are required.");await using var db=await factory.CreateDbContextAsync(ct);int appointment;
  if(input.Kind is "Grant" or "Delegation"&&!await db.Roles.AnyAsync(x=>x.RoleKey==input.PermissionCode,ct))return ServiceResult.Fail("Select an existing permission.");
  switch(input.Kind){case "Appointment":if(string.IsNullOrWhiteSpace(input.Description)||input.Description.Length>200)return ServiceResult.Fail("Position title is required (maximum 200 characters).");var a=await db.LeadershipAppointments.SingleAsync(x=>x.LeadershipAppointmentId==input.Id,ct);if(a.Status!="Active")return ServiceResult.Fail("A revoked appointment cannot be edited.");a.PositionTitle=input.Description.Trim();a.EffectiveFrom=input.From;a.EffectiveTo=input.To;appointment=a.LeadershipAppointmentId;break;
  case "Responsibility":if(string.IsNullOrWhiteSpace(input.Description)||input.Description.Length>50)return ServiceResult.Fail("Relationship description is required (maximum 50 characters).");var r=await db.LeadershipAppointmentOrganisations.SingleAsync(x=>x.LeadershipAppointmentOrganisationId==input.Id,ct);r.RelationshipType=input.Description.Trim();r.EffectiveFrom=input.From;r.EffectiveTo=input.To;appointment=r.LeadershipAppointmentId;break;
  case "Grant":var g=await db.LeadershipAuthorityGrants.SingleAsync(x=>x.LeadershipAuthorityGrantId==input.Id,ct);if(!g.IsActive)return ServiceResult.Fail("A revoked grant cannot be edited.");g.PermissionCode=input.PermissionCode;g.ValidFrom=input.From;g.ValidTo=input.To;appointment=await db.LeadershipAppointmentOrganisations.Where(x=>x.LeadershipAppointmentOrganisationId==g.LeadershipAppointmentOrganisationId).Select(x=>x.LeadershipAppointmentId).SingleAsync(ct);break;
  case "Delegation":if(!await db.Users.AnyAsync(x=>x.UserId==input.DelegateUserId&&x.IsActive,ct))return ServiceResult.Fail("Select an active delegate user.");var d=await db.LeadershipDelegations.SingleAsync(x=>x.LeadershipDelegationId==input.Id,ct);if(!d.IsActive)return ServiceResult.Fail("A revoked delegation cannot be edited.");d.PermissionScope=input.PermissionCode;d.ToUserId=input.DelegateUserId!.Value;d.ValidFrom=input.From;d.ValidTo=input.To;appointment=d.FromLeadershipAppointmentId;break;
  case "AttendancePolicy":if(input.Description!="Standard")return ServiceResult.Fail("Only Standard verification is supported.");var p=await db.LeadershipAttendancePolicies.SingleAsync(x=>x.LeadershipAttendancePolicyId==input.Id,ct);p.VerificationMode="Standard";p.AttendanceRequired=input.AttendanceRequired;p.EffectiveFrom=input.From;p.EffectiveTo=input.To;appointment=p.LeadershipAppointmentId;break;
  default:return ServiceResult.Fail("Unsupported leadership record.");}
  db.LeadershipAuditLogs.Add(new LeadershipAuditLog{LeadershipAppointmentId=appointment,Action="Updated",PerformedByUserId=actor,TimestampUtc=DateTime.UtcNow,Detail=$"Updated {input.Kind} #{input.Id}; effective {input.From} to {input.To}."});await db.SaveChangesAsync(ct);return ServiceResult.Ok("Leadership record updated.");
 }
}
