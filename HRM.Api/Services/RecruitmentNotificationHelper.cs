using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public static class RecruitmentNotificationHelper
{
    public static async Task NotifyHrApplicationSubmittedAsync(
        HrmTeContext db,
        int organisationId,
        string applicationReference,
        string vacancyReference,
        string applicantName,
        bool isResubmission,
        CancellationToken ct)
    {
        var hrUsers = await db.Users.AsNoTracking()
            .Where(u => u.IsActive
                && u.LocalUserStateId == 1
                && u.UserOrganisation != null
                && u.UserOrganisation.BusinessEntityID == organisationId
                && (
                    u.UserRoles.Any(r => r.IsActive && (
                        r.Role.RoleKey == "Recruitment.Manage"
                        || r.Role.RoleKey == "Recruitment.View"
                        || r.Role.RoleKey == "Admin"))
                    || u.UserAssignedUserGroups.Any(g =>
                        (g.UserGroup.IsGlobal || g.UserGroup.UserOrganisationId == u.UserOrganisationId)
                        && (
                            g.UserGroup.GroupName == "Human Resource: Staff"
                            || g.UserGroup.GroupName == "Human Resource :Head"
                            || g.UserGroup.GroupName == "SuperAdministrator"
                            || g.UserGroup.UserGroupRoles.Any(gr => gr.IsActive && (
                                gr.Role.RoleKey == "Recruitment.Manage"
                                || gr.Role.RoleKey == "Recruitment.View"
                                || gr.Role.RoleKey == "Admin"))))
                ))
            .Select(u => new { u.UserId, u.BusinessEntityID })
            .Distinct()
            .ToListAsync(ct);

        if (hrUsers.Count == 0)
            return;

        var now = DateTime.UtcNow;
        var kind = isResubmission ? "RecruitmentResubmitted" : "RecruitmentSubmitted";
        var displayName = string.IsNullOrWhiteSpace(applicantName) ? "An applicant" : applicantName.Trim();
        var message = isResubmission
            ? $"{displayName} resubmitted corrected application ({applicationReference}) for vacancy {vacancyReference}."
            : $"{displayName} submitted a new application ({applicationReference}) for vacancy {vacancyReference}.";

        foreach (var target in hrUsers)
        {
            db.StaffNotifications.Add(new StaffNotification
            {
                UserId = target.UserId,
                IndividualId = target.BusinessEntityID,
                OrganisationId = organisationId,
                Kind = kind,
                Message = message,
                Url = "/admin/hr/recruitment",
                CreatedAtUtc = now
            });
        }
    }

    public static async Task NotifyHrOfferResponseAsync(
        HrmTeContext db,
        int organisationId,
        string applicantName,
        string vacancyReference,
        bool accepted,
        string? declineReason,
        CancellationToken ct)
    {
        var hrUsers = await db.Users.AsNoTracking()
            .Where(u => u.IsActive
                && u.LocalUserStateId == 1
                && u.UserOrganisation != null
                && u.UserOrganisation.BusinessEntityID == organisationId
                && (
                    u.UserRoles.Any(r => r.IsActive && (
                        r.Role.RoleKey == "Recruitment.Manage"
                        || r.Role.RoleKey == "Recruitment.View"
                        || r.Role.RoleKey == "Admin"))
                    || u.UserAssignedUserGroups.Any(g =>
                        (g.UserGroup.IsGlobal || g.UserGroup.UserOrganisationId == u.UserOrganisationId)
                        && (
                            g.UserGroup.GroupName == "Human Resource: Staff"
                            || g.UserGroup.GroupName == "Human Resource :Head"
                            || g.UserGroup.GroupName == "SuperAdministrator"
                            || g.UserGroup.UserGroupRoles.Any(gr => gr.IsActive && (
                                gr.Role.RoleKey == "Recruitment.Manage"
                                || gr.Role.RoleKey == "Recruitment.View"
                                || gr.Role.RoleKey == "Admin"))))
                ))
            .Select(u => new { u.UserId, u.BusinessEntityID })
            .Distinct()
            .ToListAsync(ct);

        if (hrUsers.Count == 0)
            return;

        var now = DateTime.UtcNow;
        var displayName = string.IsNullOrWhiteSpace(applicantName) ? "Candidate" : applicantName.Trim();
        var kind = accepted ? "RecruitmentOfferAccepted" : "RecruitmentOfferDeclined";
        var suffix = !accepted && !string.IsNullOrWhiteSpace(declineReason) ? $" Reason: {declineReason.Trim()}" : "";
        var message = accepted
            ? $"{displayName} accepted the job offer for vacancy {vacancyReference}. You can now initiate onboarding."
            : $"{displayName} declined the job offer for vacancy {vacancyReference}.{suffix}";

        foreach (var target in hrUsers)
        {
            db.StaffNotifications.Add(new StaffNotification
            {
                UserId = target.UserId,
                IndividualId = target.BusinessEntityID,
                OrganisationId = organisationId,
                Kind = kind,
                Message = message,
                Url = "/admin/hr/recruitment",
                CreatedAtUtc = now
            });
        }
    }
}
