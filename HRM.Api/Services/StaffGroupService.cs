using HRM.Models;
using HRM.Services.Contracts;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using static HRM.Services.Contracts.StaffGroupServiceContracts;

namespace HRM.Services;

public sealed class StaffGroupService(
    IDbContextFactory<HrmTeContext> factory,
    IUserAccessService access,
    IOperationLogService logs,
    HRM.AuditLogs.Abstractions.IAuditLogService? auditLogs = null) : IOperations
{
    private async Task<(int OrgId, int UserId)> RequireHrWriteAsync()
    {
        var actor = await access.RequireContextAsync();
        if (!actor.IsSuperAdministrator &&
            !await access.HasPermissionAsync("Admin") &&
            !await access.HasUserGroupAsync("Human Resource: Staff") &&
            !await access.HasUserGroupAsync("Human Resource :Head"))
        {
            throw new UnauthorizedAccessException("HR or Administrator access is required to manage Staff Groups.");
        }
        return (await access.GetCurrentOrganisationIdAsync(), actor.UserId);
    }

    private async Task<int> RequireHrReadAsync()
    {
        var actor = await access.RequireContextAsync();
        if (!actor.IsSuperAdministrator &&
            !await access.HasPermissionAsync("Admin") &&
            !await access.HasPermissionAsync("Employee.View") &&
            !await access.HasUserGroupAsync("Human Resource: Staff") &&
            !await access.HasUserGroupAsync("Human Resource :Head"))
        {
            throw new UnauthorizedAccessException("Access denied.");
        }
        return await access.GetCurrentOrganisationIdAsync();
    }

    public async Task<StaffGroupWorkspaceDto> LoadAsync()
    {
        var orgId = await RequireHrReadAsync();
        await using var db = await factory.CreateDbContextAsync();

        var orgName = await db.Organisations.AsNoTracking()
            .Where(o => o.BusinessEntityID == orgId)
            .Select(o => o.OrganisationName)
            .FirstOrDefaultAsync() ?? "Current Organisation";

        var groups = await db.StaffGroups.AsNoTracking()
            .Where(g => g.OrganisationId == orgId)
            .OrderByDescending(g => g.IsActive)
            .ThenBy(g => g.Name)
            .Select(g => new StaffGroupDto
            {
                StaffGroupId = g.StaffGroupId,
                OrganisationId = g.OrganisationId,
                OrganisationName = orgName,
                Name = g.Name,
                Code = g.Code,
                Description = g.Description,
                WorkTemplateId = g.WorkTemplateId,
                WorkTemplateName = g.WorkTemplate != null ? g.WorkTemplate.Name : null,
                ScheduleMode = g.ScheduleMode,
                WorkingDaysMask = g.WorkingDaysMask,
                WorkTypeId = g.WorkTypeId,
                WorkTypeName = g.WorkType != null ? g.WorkType.Name : null,
                OvertimeEnabled = g.OvertimeEnabled,
                MaximumDailyOvertimeHours = g.MaximumDailyOvertimeHours,
                MaximumWeeklyOvertimeHours = g.MaximumWeeklyOvertimeHours,
                OvertimeRateMultiplier = g.OvertimeRateMultiplier,
                LeaveSetId = g.LeaveSetId,
                LeaveSetName = g.LeaveSet != null ? g.LeaveSet.Name : null,
                AdminPortalAccessEnabled = g.AdminPortalAccessEnabled,
                EffectiveFrom = g.EffectiveFrom,
                EffectiveTo = g.EffectiveTo,
                IsActive = g.IsActive,
                IsDissolved = g.IsDissolved,
                DissolvedAtUtc = g.DissolvedAtUtc,
                DissolveReason = g.DissolveReason,
                ActiveMemberCount = g.Members.Count(m => m.IsActive),
                CreatedAtUtc = g.CreatedAtUtc
            })
            .ToListAsync();

        var workTemplates = await db.WorkTemplates.AsNoTracking()
            .Where(t => t.IsActive && (t.IsGlobal || t.OrganisationBusinessEntityId == orgId))
            .OrderBy(t => t.Name)
            .Select(t => new StaffGroupOptionDto(t.WorkTemplateId, t.Name, t.Code))
            .ToListAsync();

        var workTypes = await db.WorkTypes.AsNoTracking()
            .Where(w => w.IsValid)
            .OrderBy(w => w.Name)
            .Select(w => new StaffGroupOptionDto(w.WorkTypeId, w.Name, null))
            .ToListAsync();

        var leaveSets = await db.LeaveSets.AsNoTracking()
            .Where(l => l.OrganisationId == orgId)
            .OrderBy(l => l.Name)
            .Select(l => new StaffGroupOptionDto(l.LeaveSetId, l.Name, null))
            .ToListAsync();

        var today = DateTime.UtcNow.AddHours(5).Date;
        var activeMemberships = await db.StaffGroupMembers.AsNoTracking()
            .Where(m => m.OrganisationId == orgId && m.IsActive &&
                        m.StaffGroup.IsActive && !m.StaffGroup.IsDissolved &&
                        (!m.StaffGroup.EffectiveTo.HasValue || m.StaffGroup.EffectiveTo >= today))
            .Select(m => new { m.IndividualId, m.StaffGroupId, GroupName = m.StaffGroup.Name })
            .ToDictionaryAsync(m => m.IndividualId, m => (m.StaffGroupId, m.GroupName));

        var adminUsers = (await db.Users.AsNoTracking()
            .Where(u => u.UserOrganisationId == orgId && u.IsActive && u.LocalUserStateId == 1 &&
                        u.AdminPortalAccessEnabled && u.AdminPortalAccessOrganisationId == orgId)
            .Select(u => u.BusinessEntityID)
            .ToListAsync()).ToHashSet();

        var jobs = await db.Jobs.AsNoTracking()
            .Where(j => j.OrganisationID == orgId && j.JobStateId == 4 && j.TerminatedDate == null)
            .Select(j => new
            {
                j.IndividualID,
                j.JobId,
                FullName = (j.Individual.FirstNameEnglish + " " + (j.Individual.MiddleNameEnglish ?? "") + " " + (j.Individual.LastNameEnglish ?? "")).Trim(),
                EmployeeNumber = j.Individual.Staff != null ? j.Individual.Staff.EmployeeNumber : null,
                DepartmentName = j.OrganisationStructure.Name,
                JobTypeName = j.JobType.TypeName,
                JobTitleName = j.JobPositions.Where(p => p.JobPositionStateId == 4).OrderByDescending(p => p.FromDate).Select(p => p.Position.Name).FirstOrDefault() ?? ""
            })
            .OrderBy(j => j.FullName)
            .ToListAsync();

        var candidates = jobs.GroupBy(j => j.IndividualID).Select(g =>
        {
            var j = g.First();
            activeMemberships.TryGetValue(j.IndividualID, out var activeGroup);
            return new StaffGroupStaffCandidateDto
            {
                IndividualId = j.IndividualID,
                JobId = j.JobId,
                FullName = j.FullName,
                EmployeeNumber = j.EmployeeNumber,
                DepartmentName = j.DepartmentName,
                JobTypeName = j.JobTypeName,
                JobTitleName = j.JobTitleName,
                ActiveStaffGroupId = activeGroup.StaffGroupId == 0 ? null : activeGroup.StaffGroupId,
                ActiveStaffGroupName = activeGroup.GroupName,
                HasAdminPortalAccess = adminUsers.Contains(j.IndividualID)
            };
        }).ToList();

        return new StaffGroupWorkspaceDto
        {
            OrganisationName = orgName,
            Groups = groups,
            WorkTemplates = workTemplates,
            WorkTypes = workTypes,
            LeaveSets = leaveSets,
            StaffCandidates = candidates
        };
    }

    public async Task<StaffGroupDetailDto> GetDetailAsync(int staffGroupId)
    {
        var orgId = await RequireHrReadAsync();
        await using var db = await factory.CreateDbContextAsync();

        var orgName = await db.Organisations.AsNoTracking()
            .Where(o => o.BusinessEntityID == orgId)
            .Select(o => o.OrganisationName)
            .FirstOrDefaultAsync() ?? "Current Organisation";

        var g = await db.StaffGroups.AsNoTracking()
            .Include(x => x.WorkTemplate)
            .Include(x => x.WorkType)
            .Include(x => x.LeaveSet)
            .SingleOrDefaultAsync(x => x.StaffGroupId == staffGroupId && x.OrganisationId == orgId)
            ?? throw new KeyNotFoundException("Staff group not found.");

        var memberRows = await db.StaffGroupMembers.AsNoTracking()
            .Where(m => m.StaffGroupId == staffGroupId && m.OrganisationId == orgId)
            .OrderByDescending(m => m.IsActive)
            .ThenBy(m => m.JoinedAtUtc)
            .ToListAsync();

        var indIds = memberRows.Select(m => m.IndividualId).Distinct().ToList();
        var jobInfo = await db.Jobs.AsNoTracking()
            .Where(j => j.OrganisationID == orgId && indIds.Contains(j.IndividualID))
            .Select(j => new
            {
                j.JobId,
                j.IndividualID,
                FullName = (j.Individual.FirstNameEnglish + " " + (j.Individual.MiddleNameEnglish ?? "") + " " + (j.Individual.LastNameEnglish ?? "")).Trim(),
                EmployeeNumber = j.Individual.Staff != null ? j.Individual.Staff.EmployeeNumber : null,
                DepartmentName = j.OrganisationStructure.Name,
                JobTypeName = j.JobType.TypeName,
                JobTitleName = j.JobPositions.Where(p => p.JobPositionStateId == 4).OrderByDescending(p => p.FromDate).Select(p => p.Position.Name).FirstOrDefault() ?? ""
            })
            .ToListAsync();

        var adminUsers = (await db.Users.AsNoTracking()
            .Where(u => u.UserOrganisationId == orgId && indIds.Contains(u.BusinessEntityID) &&
                        u.IsActive && u.LocalUserStateId == 1 &&
                        u.AdminPortalAccessEnabled && u.AdminPortalAccessOrganisationId == orgId)
            .Select(u => u.BusinessEntityID)
            .ToListAsync()).ToHashSet();

        var members = memberRows.Select(m =>
        {
            var info = jobInfo.FirstOrDefault(j => j.JobId == m.JobId) ?? jobInfo.FirstOrDefault(j => j.IndividualID == m.IndividualId);
            return new StaffGroupMemberDto
            {
                StaffGroupMemberId = m.StaffGroupMemberId,
                StaffGroupId = m.StaffGroupId,
                IndividualId = m.IndividualId,
                JobId = m.JobId,
                FullName = info?.FullName ?? $"Individual #{m.IndividualId}",
                EmployeeNumber = info?.EmployeeNumber,
                DepartmentName = info?.DepartmentName ?? "",
                JobTypeName = info?.JobTypeName ?? "",
                JobTitleName = info?.JobTitleName ?? "",
                JoinedAtUtc = m.JoinedAtUtc,
                LeftAtUtc = m.LeftAtUtc,
                IsActive = m.IsActive,
                HadDirectAdminPortalAccess = m.HadDirectAdminPortalAccess,
                GrantedAdminPortalAccess = m.GrantedAdminPortalAccess,
                CurrentAdminPortalAccess = adminUsers.Contains(m.IndividualId)
            };
        }).OrderByDescending(m => m.IsActive).ThenBy(m => m.FullName).ToList();

        return new StaffGroupDetailDto
        {
            Group = new StaffGroupDto
            {
                StaffGroupId = g.StaffGroupId,
                OrganisationId = g.OrganisationId,
                OrganisationName = orgName,
                Name = g.Name,
                Code = g.Code,
                Description = g.Description,
                WorkTemplateId = g.WorkTemplateId,
                WorkTemplateName = g.WorkTemplate?.Name,
                ScheduleMode = g.ScheduleMode,
                WorkingDaysMask = g.WorkingDaysMask,
                WorkTypeId = g.WorkTypeId,
                WorkTypeName = g.WorkType?.Name,
                OvertimeEnabled = g.OvertimeEnabled,
                MaximumDailyOvertimeHours = g.MaximumDailyOvertimeHours,
                MaximumWeeklyOvertimeHours = g.MaximumWeeklyOvertimeHours,
                OvertimeRateMultiplier = g.OvertimeRateMultiplier,
                LeaveSetId = g.LeaveSetId,
                LeaveSetName = g.LeaveSet?.Name,
                AdminPortalAccessEnabled = g.AdminPortalAccessEnabled,
                EffectiveFrom = g.EffectiveFrom,
                EffectiveTo = g.EffectiveTo,
                IsActive = g.IsActive,
                IsDissolved = g.IsDissolved,
                DissolvedAtUtc = g.DissolvedAtUtc,
                DissolveReason = g.DissolveReason,
                ActiveMemberCount = members.Count(m => m.IsActive),
                CreatedAtUtc = g.CreatedAtUtc
            },
            Members = members
        };
    }

    public async Task<StaffGroupDto> SaveGroupAsync(StaffGroupSaveRequest request)
    {
        var (orgId, userId) = await RequireHrWriteAsync();
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Group name is required.");

        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        var name = request.Name.Trim();
        var duplicate = await db.StaffGroups.AnyAsync(g =>
            g.OrganisationId == orgId &&
            g.StaffGroupId != request.StaffGroupId &&
            g.IsActive &&
            g.Name.Trim().ToUpper() == name.ToUpper());
        if (duplicate)
            throw new InvalidOperationException("An active staff group with this name already exists.");

        var logId = await logs.CreateLogAsync(1, request.StaffGroupId == 0 ? "CreateStaffGroup" : "UpdateStaffGroup");

        StaffGroup entity;
        bool previousAdminAccess = false;
        if (request.StaffGroupId == 0)
        {
            entity = new StaffGroup
            {
                OrganisationId = orgId,
                CreatedByUserId = userId,
                CreatedAtUtc = DateTime.UtcNow,
                IsActive = true,
                IsDissolved = false
            };
            db.StaffGroups.Add(entity);
        }
        else
        {
            entity = await db.StaffGroups
                .Include(g => g.Members)
                .SingleOrDefaultAsync(g => g.StaffGroupId == request.StaffGroupId && g.OrganisationId == orgId)
                ?? throw new KeyNotFoundException("Staff group not found.");
            if (entity.IsDissolved)
                throw new InvalidOperationException("A dissolved group cannot be modified.");
            previousAdminAccess = entity.AdminPortalAccessEnabled;
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }

        var effectiveFrom = request.EffectiveFrom == default ? DateTime.UtcNow.AddHours(5).Date : request.EffectiveFrom.Date;
        var effectiveTo = request.EffectiveTo?.Date;
        if (effectiveTo.HasValue && effectiveTo.Value < effectiveFrom)
            throw new InvalidOperationException("Effective To date cannot be earlier than Effective From date.");

        entity.Name = name;
        entity.Code = string.IsNullOrWhiteSpace(request.Code) ? null : request.Code.Trim();
        entity.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        entity.WorkTemplateId = request.WorkTemplateId;
        entity.ScheduleMode = string.IsNullOrWhiteSpace(request.ScheduleMode) ? "Standard" : request.ScheduleMode.Trim();
        entity.WorkingDaysMask = request.WorkingDaysMask <= 0 ? 31 : request.WorkingDaysMask;
        entity.WorkTypeId = request.WorkTypeId;
        entity.OvertimeEnabled = request.OvertimeEnabled;
        entity.MaximumDailyOvertimeHours = request.MaximumDailyOvertimeHours;
        entity.MaximumWeeklyOvertimeHours = request.MaximumWeeklyOvertimeHours;
        entity.OvertimeRateMultiplier = request.OvertimeRateMultiplier;
        entity.LeaveSetId = request.LeaveSetId;
        entity.AdminPortalAccessEnabled = request.AdminPortalAccessEnabled;
        entity.EffectiveFrom = effectiveFrom;
        entity.EffectiveTo = effectiveTo;
        entity.OperationLogId = logId;

        await db.SaveChangesAsync();

        if (request.StaffGroupId != 0 && previousAdminAccess != request.AdminPortalAccessEnabled)
        {
            await ApplyGroupAdminAccessChangeAsync(db, entity, request.AdminPortalAccessEnabled);
            await db.SaveChangesAsync();
        }

        if (request.StaffGroupId != 0 && entity.Members.Any(m => m.IsActive))
        {
            var memberJobIds = entity.Members.Where(m => m.IsActive).Select(m => m.JobId).Distinct().ToList();
            await InvalidateUnusedGeneratedPlansAsync(db, orgId, memberJobIds, DateTime.UtcNow.AddHours(5).Date);
            await db.SaveChangesAsync();
        }

        await tx.CommitAsync();

        var detail = await GetDetailAsync(entity.StaffGroupId);
        return detail.Group;
    }

    public async Task AddMembersAsync(StaffGroupMembersRequest request)
    {
        var (orgId, userId) = await RequireHrWriteAsync();
        var ids = request.IndividualIds.Where(i => i > 0).Distinct().ToList();
        if (ids.Count == 0) return;

        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        var group = await db.StaffGroups
            .SingleOrDefaultAsync(g => g.StaffGroupId == request.StaffGroupId && g.OrganisationId == orgId)
            ?? throw new KeyNotFoundException("Staff group not found.");
        if (!group.IsActive || group.IsDissolved)
            throw new InvalidOperationException("Cannot add staff to an inactive or dissolved group.");

        var today = DateTime.UtcNow.AddHours(5).Date;
        var conflicts = await db.StaffGroupMembers.AsNoTracking()
            .Where(m => m.OrganisationId == orgId && m.IsActive && ids.Contains(m.IndividualId) &&
                        m.StaffGroup.IsActive && !m.StaffGroup.IsDissolved &&
                        (!m.StaffGroup.EffectiveTo.HasValue || m.StaffGroup.EffectiveTo >= today))
            .Select(m => new { m.IndividualId, GroupName = m.StaffGroup.Name, m.StaffGroupId })
            .ToListAsync();

        var otherGroupConflict = conflicts.FirstOrDefault(c => c.StaffGroupId != group.StaffGroupId);
        if (otherGroupConflict != null)
            throw new InvalidOperationException($"Staff cannot be in multiple groups at the same time (already in '{otherGroupConflict.GroupName}').");

        var existingIds = conflicts.Select(c => c.IndividualId).ToHashSet();
        var toAdd = ids.Where(i => !existingIds.Contains(i)).ToList();
        if (toAdd.Count == 0) return;

        var jobs = await db.Jobs.AsNoTracking()
            .Where(j => j.OrganisationID == orgId && j.JobStateId == 4 && j.TerminatedDate == null && toAdd.Contains(j.IndividualID))
            .GroupBy(j => j.IndividualID)
            .Select(g => g.First())
            .ToDictionaryAsync(j => j.IndividualID, j => j.JobId);

        var users = await db.Users
            .Where(u => u.UserOrganisationId == orgId && u.IsActive && u.LocalUserStateId == 1 && toAdd.Contains(u.BusinessEntityID))
            .ToDictionaryAsync(u => u.BusinessEntityID);

        var logId = await logs.CreateLogAsync(1, "AddStaffGroupMembers");
        var now = DateTime.UtcNow;

        foreach (var indId in toAdd)
        {
            if (!jobs.TryGetValue(indId, out var jobId))
                throw new InvalidOperationException("One or more selected staff do not have an active job in this organisation.");

            users.TryGetValue(indId, out var user);
            var hadDirect = user != null && user.AdminPortalAccessEnabled && user.AdminPortalAccessOrganisationId == orgId;
            var grantedByGroup = false;

            if (group.AdminPortalAccessEnabled && user != null && !hadDirect)
            {
                user.AdminPortalAccessEnabled = true;
                user.AdminPortalAccessOrganisationId = orgId;
                grantedByGroup = true;
            }

            db.StaffGroupMembers.Add(new StaffGroupMember
            {
                StaffGroupId = group.StaffGroupId,
                OrganisationId = orgId,
                IndividualId = indId,
                JobId = jobId,
                JoinedAtUtc = now,
                IsActive = true,
                HadDirectAdminPortalAccess = hadDirect,
                GrantedAdminPortalAccess = grantedByGroup,
                AddedByUserId = userId,
                OperationLogId = logId
            });
        }

        if (group.WorkTemplateId.HasValue)
        {
            await InvalidateUnusedGeneratedPlansAsync(db, orgId, jobs.Values.Distinct().ToList(), today);
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task RemoveMemberAsync(int staffGroupId, int individualId)
    {
        var (orgId, userId) = await RequireHrWriteAsync();
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        var member = await db.StaffGroupMembers
            .SingleOrDefaultAsync(m => m.StaffGroupId == staffGroupId && m.OrganisationId == orgId && m.IndividualId == individualId && m.IsActive)
            ?? throw new KeyNotFoundException("Active group member not found.");

        var logId = await logs.CreateLogAsync(1, "RemoveStaffGroupMember");

        member.IsActive = false;
        member.LeftAtUtc = DateTime.UtcNow;
        member.RemovedByUserId = userId;
        member.OperationLogId = logId;

        if (member.GrantedAdminPortalAccess && !member.HadDirectAdminPortalAccess)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.BusinessEntityID == individualId && u.UserOrganisationId == orgId && u.IsActive);
            if (user != null)
            {
                user.AdminPortalAccessEnabled = false;
                user.AdminPortalAccessOrganisationId = null;
            }
        }

        await InvalidateUnusedGeneratedPlansAsync(db, orgId, [member.JobId], DateTime.UtcNow.AddHours(5).Date);
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }

    public async Task SetAdminPortalAccessAsync(int staffGroupId, bool enabled)
    {
        var (orgId, userId) = await RequireHrWriteAsync();
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        var group = await db.StaffGroups
            .Include(g => g.Members)
            .SingleOrDefaultAsync(g => g.StaffGroupId == staffGroupId && g.OrganisationId == orgId)
            ?? throw new KeyNotFoundException("Staff group not found.");
        if (group.IsDissolved)
            throw new InvalidOperationException("Cannot modify a dissolved group.");

        var logId = await logs.CreateLogAsync(1, enabled ? "GrantStaffGroupAdminAccess" : "RevokeStaffGroupAdminAccess");
        group.AdminPortalAccessEnabled = enabled;
        group.UpdatedAtUtc = DateTime.UtcNow;
        group.OperationLogId = logId;

        await ApplyGroupAdminAccessChangeAsync(db, group, enabled);
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        if (auditLogs is not null)
        {
            await auditLogs.RecordPermissionChangeAsync(
                action: enabled ? "GRANT_ACCESS" : "REVOKE_ACCESS",
                entityType: nameof(StaffGroup),
                entityId: group.StaffGroupId.ToString(),
                description: $"{(enabled ? "Enabled" : "Revoked")} Admin Portal access for Staff Group '{group.Name}'.",
                organisationId: orgId);
        }
    }

    public async Task DissolveGroupAsync(int staffGroupId, string? reason)
    {
        var (orgId, userId) = await RequireHrWriteAsync();
        await using var db = await factory.CreateDbContextAsync();
        await using var tx = await db.Database.BeginTransactionAsync();

        var group = await db.StaffGroups
            .Include(g => g.Members)
            .SingleOrDefaultAsync(g => g.StaffGroupId == staffGroupId && g.OrganisationId == orgId)
            ?? throw new KeyNotFoundException("Staff group not found.");
        if (group.IsDissolved) return;

        var logId = await logs.CreateLogAsync(1, "DissolveStaffGroup");
        var now = DateTime.UtcNow;

        group.IsActive = false;
        group.IsDissolved = true;
        group.DissolvedAtUtc = now;
        group.DissolvedByUserId = userId;
        group.DissolveReason = string.IsNullOrWhiteSpace(reason) ? "Dissolved by HR" : reason.Trim();
        group.UpdatedAtUtc = now;
        group.OperationLogId = logId;

        var activeMembers = group.Members.Where(m => m.IsActive).ToList();
        var revokeIndIds = activeMembers
            .Where(m => m.GrantedAdminPortalAccess && !m.HadDirectAdminPortalAccess)
            .Select(m => m.IndividualId)
            .ToList();

        if (revokeIndIds.Count > 0)
        {
            var users = await db.Users
                .Where(u => u.UserOrganisationId == orgId && u.IsActive && revokeIndIds.Contains(u.BusinessEntityID))
                .ToListAsync();
            foreach (var u in users)
            {
                u.AdminPortalAccessEnabled = false;
                u.AdminPortalAccessOrganisationId = null;
            }
        }

        foreach (var m in activeMembers)
        {
            m.IsActive = false;
            m.LeftAtUtc = now;
            m.RemovedByUserId = userId;
            m.OperationLogId = logId;
        }

        if (activeMembers.Count > 0)
        {
            await InvalidateUnusedGeneratedPlansAsync(db, orgId, activeMembers.Select(m => m.JobId).Distinct().ToList(), DateTime.UtcNow.AddHours(5).Date);
        }

        await db.SaveChangesAsync();
        await tx.CommitAsync();

        if (auditLogs is not null)
        {
            await auditLogs.RecordAsync(new HRM.AuditLogs.Models.AuditLogRequest
            {
                Action = "DISSOLVE",
                Module = "StaffGroups",
                EntityType = nameof(StaffGroup),
                EntityId = group.StaffGroupId.ToString(),
                OrganisationId = orgId,
                Description = $"Dissolved Staff Group '{group.Name}' ({activeMembers.Count} active member(s) removed).",
                Reason = group.DissolveReason
            });
        }
    }

    private static async Task InvalidateUnusedGeneratedPlansAsync(HrmTeContext db, int orgId, List<int> jobIds, DateTime fromDate)
    {
        if (jobIds.Count == 0) return;
        var plans = await db.WorkPlans
            .Include(p => p.WorkPlanSegments)
            .Include(p => p.WorkAssignments).ThenInclude(a => a.WorkAssignmentSegments)
            .Include(p => p.WorkAssignments).ThenInclude(a => a.WorkAssignmentOwners)
            .Where(p => p.OrganisationBusinessEntityId == orgId && jobIds.Contains(p.JobId) &&
                        p.IsValid && p.IsGenerated && !p.IsManual && !p.IsFinalized &&
                        p.WorkDate >= fromDate.Date &&
                        !db.AttendanceLogResolutions.Any(r => r.WorkPlanId == p.WorkPlanId) &&
                        !db.AttendanceLogs.Any(l => l.OrganisationId == orgId && l.IndividualId == p.IndividualId && l.Date.Date == p.WorkDate.Date) &&
                        !p.WorkPlanSegments.Any(s => s.AttendanceId.HasValue || s.IsCompleted))
            .ToListAsync();

        foreach (var plan in plans)
        {
            plan.IsValid = false;
            foreach (var seg in plan.WorkPlanSegments) seg.IsValid = false;
            foreach (var a in plan.WorkAssignments)
            {
                a.IsValid = false;
                foreach (var seg in a.WorkAssignmentSegments) seg.IsValid = false;
                foreach (var owner in a.WorkAssignmentOwners) owner.IsValid = false;
            }
        }
    }

    private static async Task ApplyGroupAdminAccessChangeAsync(HrmTeContext db, StaffGroup group, bool enabled)
    {
        var activeMembers = group.Members.Where(m => m.IsActive).ToList();
        if (activeMembers.Count == 0) return;

        var indIds = activeMembers.Select(m => m.IndividualId).Distinct().ToList();
        var users = await db.Users
            .Where(u => u.UserOrganisationId == group.OrganisationId && u.IsActive && u.LocalUserStateId == 1 && indIds.Contains(u.BusinessEntityID))
            .ToDictionaryAsync(u => u.BusinessEntityID);

        if (enabled)
        {
            foreach (var m in activeMembers)
            {
                if (!users.TryGetValue(m.IndividualId, out var user)) continue;
                var alreadyHas = user.AdminPortalAccessEnabled && user.AdminPortalAccessOrganisationId == group.OrganisationId;
                if (!alreadyHas)
                {
                    m.HadDirectAdminPortalAccess = false;
                    m.GrantedAdminPortalAccess = true;
                    user.AdminPortalAccessEnabled = true;
                    user.AdminPortalAccessOrganisationId = group.OrganisationId;
                }
                else if (!m.GrantedAdminPortalAccess)
                {
                    m.HadDirectAdminPortalAccess = true;
                }
            }
        }
        else
        {
            foreach (var m in activeMembers)
            {
                if (m.GrantedAdminPortalAccess && !m.HadDirectAdminPortalAccess && users.TryGetValue(m.IndividualId, out var user))
                {
                    user.AdminPortalAccessEnabled = false;
                    user.AdminPortalAccessOrganisationId = null;
                }
                m.GrantedAdminPortalAccess = false;
            }
        }
    }
}
