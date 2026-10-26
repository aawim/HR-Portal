using HRM.DTOs;
using HRM.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services.Interfaces;
public class UserContext(AuthenticationStateProvider auth, IDbContextFactory<HrmTeContext> factory) : IUserContext
{
 public async Task<string?> GetUsernameAsync() {
  var state = await auth.GetAuthenticationStateAsync();
  return state.User.Identity?.IsAuthenticated == true ? state.User.FindFirst("idnumber")?.Value : null;
 }
 public async Task<int?> GetOrganisationIdAsync() => (await GetSessionAsync())?.OrganisationId;
 public async Task<string> GetOrganisationNameAsync() => (await GetSessionAsync())?.OrganisationName ?? "";
 public async Task<int> GetIndividualIdAsync() => (await GetSessionAsync())?.IndividualId ?? 0;
 public async Task<int> GetJobIdAsync() => (await GetSessionAsync())?.JobId ?? 0;
 public async Task<int> GetUserIdAsync() => (await GetSessionAsync())?.UserId ?? 0;
 public async Task<UserSessionDto?> GetSessionAsync() {
  var username = await GetUsernameAsync();
  if (string.IsNullOrWhiteSpace(username)) return null;
  await using var db = await factory.CreateDbContextAsync();
  var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Username == username && u.IsActive && u.LocalUserStateId == 1);
  if (user == null) return null;
  var organisation = await db.UserOrganisations.AsNoTracking().Where(x => x.UserOrganisationID == user.UserOrganisationId)
   .Select(x => new { Id = x.BusinessEntityID, Name = x.Organisation.OrganisationName }).SingleOrDefaultAsync();
  var jobId = organisation == null ? 0 : await db.Jobs.AsNoTracking().Where(j => j.IndividualID == user.BusinessEntityID && j.OrganisationID == organisation.Id && j.JobStateId == 4 && j.TerminatedDate == null && j.JoinedDate <= DateTime.Today)
   .OrderByDescending(j => j.JoinedDate).ThenByDescending(j => j.JobId).Select(j => j.JobId).FirstOrDefaultAsync();
  // Portal admission and individual resource permissions are separate grants.
  var canAdmin = user.AdminPortalAccessEnabled && user.AdminPortalAccessOrganisationId == user.UserOrganisationId;
  return new UserSessionDto { UserId = user.UserId, IndividualId = user.BusinessEntityID, JobId = jobId, OrganisationId = organisation?.Id, OrganisationName = organisation?.Name ?? "", CanAccessSystem = true, CanAccessAdminPortal = organisation != null && canAdmin };
 }
}
