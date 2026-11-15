using HRM.DTOs;
using HRM.DTOs.Leadership;
using HRM.Models.Leadership;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;
public sealed partial class LeadershipAdministrationService
{
 public async Task<IReadOnlyList<LeadershipManagementRecord>> ListRecordsAsync(CancellationToken ct=default)
 {
  await RequireAdministratorAsync();await using var db=await factory.CreateDbContextAsync(ct);var day=DateOnly.FromDateTime(DateTime.UtcNow);var rows=new List<LeadershipManagementRecord>();
  foreach(var x in await db.LeadershipAppointments.AsNoTracking().ToListAsync(ct))rows.Add(new("Appointment",x.LeadershipAppointmentId,x.LeadershipAppointmentId,x.AdministrativeOrganisationId,x.PositionTitle,x.Status!="Active"?x.Status:x.EffectiveFrom>day?"Scheduled":x.EffectiveTo<day?"Expired":"Active"));
  foreach(var x in await db.LeadershipAppointmentOrganisations.AsNoTracking().ToListAsync(ct))rows.Add(new("Responsibility",x.LeadershipAppointmentOrganisationId,x.LeadershipAppointmentId,x.OrganisationId,x.RelationshipType,x.EffectiveFrom>day?"Scheduled":x.EffectiveTo<day?"Expired":"Active"));
  foreach(var x in await db.LeadershipAuthorityGrants.AsNoTracking().Join(db.LeadershipAppointmentOrganisations,g=>g.LeadershipAppointmentOrganisationId,o=>o.LeadershipAppointmentOrganisationId,(g,o)=>new{g,o}).ToListAsync(ct))rows.Add(new("Grant",x.g.LeadershipAuthorityGrantId,x.o.LeadershipAppointmentId,x.o.OrganisationId,x.g.PermissionCode,!x.g.IsActive?"Revoked":x.g.ValidFrom>day?"Scheduled":x.g.ValidTo<day?"Expired":"Active"));
  foreach(var x in await db.LeadershipDelegations.AsNoTracking().ToListAsync(ct))rows.Add(new("Delegation",x.LeadershipDelegationId,x.FromLeadershipAppointmentId,x.OrganisationId,$"{x.PermissionScope} → user {x.ToUserId}",!x.IsActive?"Revoked":x.ValidFrom>day?"Scheduled":x.ValidTo<day?"Expired":"Active"));
  foreach(var x in await db.LeadershipAttendancePolicies.AsNoTracking().ToListAsync(ct))rows.Add(new("AttendancePolicy",x.LeadershipAttendancePolicyId,x.LeadershipAppointmentId,x.ManagingOrganisationId,x.VerificationMode,x.EffectiveFrom>day?"Scheduled":x.EffectiveTo<day?"Expired":"Active"));return rows;
 }
 public async Task<IReadOnlyList<LeadershipAuditDto>> ListAuditAsync(CancellationToken ct=default){await RequireAdministratorAsync();await using var db=await factory.CreateDbContextAsync(ct);return await db.LeadershipAuditLogs.AsNoTracking().OrderByDescending(x=>x.TimestampUtc).Take(500).Select(x=>new LeadershipAuditDto(x.LeadershipAuditLogId,x.LeadershipAppointmentId,x.Action,x.OrganisationId,x.PerformedByUserId,x.TimestampUtc,x.Detail)).ToListAsync(ct);}
 public async Task<ServiceResult> RevokeRecordAsync(string kind,int id,CancellationToken ct=default)
 {
  var user=await RequireAdministratorAsync();await using var db=await factory.CreateDbContextAsync(ct);var end=DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1);int appointment;
  switch(kind){case "Appointment":var a=await db.LeadershipAppointments.SingleAsync(x=>x.LeadershipAppointmentId==id,ct);a.Status="Revoked";appointment=a.LeadershipAppointmentId;break;
  case "Grant":var g=await db.LeadershipAuthorityGrants.SingleAsync(x=>x.LeadershipAuthorityGrantId==id,ct);g.IsActive=false;appointment=await db.LeadershipAppointmentOrganisations.Where(x=>x.LeadershipAppointmentOrganisationId==g.LeadershipAppointmentOrganisationId).Select(x=>x.LeadershipAppointmentId).SingleAsync(ct);break;
  case "Delegation":var d=await db.LeadershipDelegations.SingleAsync(x=>x.LeadershipDelegationId==id,ct);d.IsActive=false;appointment=d.FromLeadershipAppointmentId;break;
  case "Responsibility":var r=await db.LeadershipAppointmentOrganisations.SingleAsync(x=>x.LeadershipAppointmentOrganisationId==id,ct);r.EffectiveTo=end;appointment=r.LeadershipAppointmentId;break;
  case "AttendancePolicy":var p=await db.LeadershipAttendancePolicies.SingleAsync(x=>x.LeadershipAttendancePolicyId==id,ct);p.EffectiveTo=end;appointment=p.LeadershipAppointmentId;break;
  default:return ServiceResult.Fail("Unsupported leadership record.");}
  db.LeadershipAuditLogs.Add(new LeadershipAuditLog{LeadershipAppointmentId=appointment,Action="Revoked",PerformedByUserId=user,TimestampUtc=DateTime.UtcNow,Detail=$"Revoked {kind} #{id}."});await db.SaveChangesAsync(ct);return ServiceResult.Ok("Leadership record revoked.");
 }
}
