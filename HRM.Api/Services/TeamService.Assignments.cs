using HRM.Components.Shared;
using HRM.DTOs;
using HRM.Models;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

/// <summary>Staff-to-project membership and staff-to-task assignment. Membership and task ownership are kept separate.</summary>
public partial class TeamService
{
    private static readonly string[] ClosedProjectStatuses = ["Completed", "Archived", "Cancelled", "Closed"];
    private static readonly string[] TaskPriorities = ["Low", "Medium", "High", "Critical"];

    public async Task<List<ProjectLookupDto>> SearchAssignableProjectsAsync(string? query, CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync();
        var orgId = await access.GetCurrentOrganisationIdAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesExistAsync(db, ct);
        query = (query ?? "").Trim();
        if (query.Length > 100) query = query[..100];

        var q = db.Projects.AsNoTracking().Include(p => p.Team)
            .Where(p => (actor.IsSuperAdministrator || p.OrganisationId == orgId || p.OrganisationId == 0) && !ClosedProjectStatuses.Contains(p.Status));
        if (query.Length > 0) q = q.Where(p => p.Name.Contains(query) || p.Reference.Contains(query));

        var candidates = await q.OrderBy(p => p.Name).Take(50).ToListAsync(ct);
        var result = new List<ProjectLookupDto>();
        foreach (var p in candidates)
        {
            if (!await CanManageProjectAsync(db, p)) continue;
            result.Add(new ProjectLookupDto
            {
                ProjectId = p.ProjectId, Reference = p.Reference, Name = p.Name, Description = p.Description,
                Status = p.Status, TeamName = p.Team?.Name ?? "", ProjectManagerName = p.ProjectManagerName,
                StartDate = p.StartDate, PlannedEndDate = p.PlannedEndDate
            });
        }
        return result;
    }

    public async Task<int> AssignStaffToProjectAsync(AssignProjectRequestDto request, CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync();
        var role = (request.Role ?? "").Trim();
        if (request.ProjectId <= 0 || request.StaffId <= 0) throw new ArgumentException("Select a project and an employee.");
        if (role.Length is < 1 or > 100) throw new ArgumentException("Enter an assignment role.");
        if (request.StartDate == default) throw new ArgumentException("Enter the assignment start date.");
        if (request.ExpectedEndDate.HasValue && request.ExpectedEndDate.Value.Date < request.StartDate.Date)
            throw new ArgumentException("Expected end date cannot be before the start date.");
        var remarks = (request.Remarks ?? "").Trim();
        if (remarks.Length > 1000) throw new ArgumentException("Remarks are too long.");

        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesExistAsync(db, ct);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var project = await db.Projects.FirstOrDefaultAsync(p => p.ProjectId == request.ProjectId, ct)
            ?? throw new ArgumentException("Project not found.");
        await RequireManageProjectAsync(db, project);
        RequireOpenProject(project);
        if (project.PlannedEndDate.HasValue && request.StartDate.Date > project.PlannedEndDate.Value.Date)
            throw new ArgumentException("Start date is after the project's planned end date.");
        await EnsureStaffInOrganisationAsync(db, request.StaffId, ct);


        if (await db.ProjectMembers.AnyAsync(m => m.ProjectId == project.ProjectId && m.StaffId == request.StaffId && m.IsActive, ct))
            throw new InvalidOperationException("This employee is already actively assigned to the project.");

        var names = await StaffNamesAsync(db, [request.StaffId], ct);
        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.TEAM_STAFF_UPDATE,
            $"Assign staff {request.StaffId} to project {project.ProjectId}.");
        var member = new ProjectMember
        {
            ProjectId = project.ProjectId, StaffId = request.StaffId, StaffName = names.GetValueOrDefault(request.StaffId, $"Staff #{request.StaffId}"),
            Role = role, AssignedDate = request.StartDate.Date, ExpectedEndDate = request.ExpectedEndDate?.Date,
            Remarks = remarks, IsActive = true, AssignedByStaffId = actor.IndividualId
        };
        db.ProjectMembers.Add(member);
        AddHistory(db, project.ProjectId, null, request.StaffId, null, "ProjectAssigned", $"Role: {role}. {remarks}".Trim(), actor.IndividualId, audit.OperationLogId);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return member.ProjectMemberId;
    }

    public async Task<bool> UpdateProjectAssignmentAsync(UpdateProjectAssignmentRequestDto request, CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync();
        var role = (request.Role ?? "").Trim();
        var remarks = (request.Remarks ?? "").Trim();
        if (role.Length is < 1 or > 100) throw new ArgumentException("Enter an assignment role.");
        if (remarks.Length > 1000) throw new ArgumentException("Remarks are too long.");

        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesExistAsync(db, ct);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var member = await db.ProjectMembers.Include(m => m.Project).FirstOrDefaultAsync(m => m.ProjectMemberId == request.ProjectMemberId, ct);
        if (member == null) return false;
        await RequireManageProjectAsync(db, member.Project);
        if (!member.IsActive) throw new InvalidOperationException("Only active assignments can be updated.");
        if (request.ExpectedEndDate.HasValue && request.ExpectedEndDate.Value.Date < member.AssignedDate.Date)
            throw new ArgumentException("Expected end date cannot be before the start date.");

        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.TEAM_STAFF_UPDATE,
            $"Update project assignment {member.ProjectMemberId}.");
        var before = $"Role {member.Role} -> {role}; expected end {member.ExpectedEndDate:yyyy-MM-dd} -> {request.ExpectedEndDate:yyyy-MM-dd}.";
        member.Role = role; member.ExpectedEndDate = request.ExpectedEndDate?.Date; member.Remarks = remarks;
        member.UpdatedAt = DateTime.UtcNow; member.UpdatedByStaffId = actor.IndividualId;
        AddHistory(db, member.ProjectId, null, member.StaffId, null, "ProjectUpdated", before, actor.IndividualId, audit.OperationLogId);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public async Task<bool> RemoveProjectAssignmentAsync(int projectMemberId, string? remarks, CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync();
        remarks = (remarks ?? "").Trim();
        if (remarks.Length > 1000) throw new ArgumentException("Remarks are too long.");

        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var member = await db.ProjectMembers.Include(m => m.Project).FirstOrDefaultAsync(m => m.ProjectMemberId == projectMemberId, ct);
        if (member == null || !member.IsActive) return false;
        await RequireManageProjectAsync(db, member.Project);

        // Business rule not defined: tasks are never reassigned or removed automatically. They are only reported in the history.
        var openTasks = await db.ProjectTasks.CountAsync(t => t.ProjectId == member.ProjectId && t.AssignedStaffId == member.StaffId && t.Status != "Completed", ct);
        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.TEAM_STAFF_UPDATE,
            $"Remove staff {member.StaffId} from project {member.ProjectId}.");
        member.IsActive = false; member.RemovedAt = DateTime.UtcNow; member.RemovedByStaffId = actor.IndividualId;
        AddHistory(db, member.ProjectId, null, member.StaffId, null, "ProjectRemoved",
            $"{remarks} {openTasks} open task(s) remain assigned and need review.".Trim(), actor.IndividualId, audit.OperationLogId);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public async Task<bool> AssignTaskAsync(AssignTaskRequestDto request, CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync();
        var priority = (request.Priority ?? "").Trim();
        var remarks = (request.Remarks ?? "").Trim();
        if (request.TaskId <= 0 || request.StaffId <= 0) throw new ArgumentException("Select a task and an employee.");
        if (!TaskPriorities.Contains(priority)) throw new ArgumentException("Select a valid priority.");
        if (remarks.Length > 1000) throw new ArgumentException("Remarks are too long.");
        if (request.StartDate.HasValue && request.DueDate.HasValue && request.DueDate.Value.Date < request.StartDate.Value.Date)
            throw new ArgumentException("Due date cannot be before the start date.");

        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesExistAsync(db, ct);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var task = await db.ProjectTasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.TaskId == request.TaskId, ct)
            ?? throw new ArgumentException("Task not found.");
        await RequireManageProjectAsync(db, task.Project);
        RequireOpenProject(task.Project);
        if (task.Status == "Completed") throw new InvalidOperationException("Completed tasks cannot be assigned.");
        await EnsureStaffInOrganisationAsync(db, request.StaffId, ct);

        var previous = task.AssignedStaffId;
        var names = await StaffNamesAsync(db, [request.StaffId], ct);
        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.TEAM_STAFF_UPDATE,
            $"Assign task {task.TaskId} to staff {request.StaffId}.");

        if (!await db.ProjectMembers.AnyAsync(m => m.ProjectId == task.ProjectId && m.StaffId == request.StaffId && m.IsActive, ct))
        {
            var autoMember = new ProjectMember
            {
                ProjectId = task.ProjectId,
                StaffId = request.StaffId,
                StaffName = names.GetValueOrDefault(request.StaffId, $"Staff #{request.StaffId}"),
                Role = "Member",
                AssignedDate = DateTime.UtcNow.Date,
                Remarks = "Assigned via task assignment",
                IsActive = true,
                AssignedByStaffId = actor.IndividualId
            };
            db.ProjectMembers.Add(autoMember);
            AddHistory(db, task.ProjectId, null, request.StaffId, null, "ProjectAssigned", "Assigned via task assignment", actor.IndividualId, audit.OperationLogId);
        }


        task.AssignedStaffId = request.StaffId;
        task.AssignedStaffName = names.GetValueOrDefault(request.StaffId, $"Staff #{request.StaffId}");
        task.Priority = priority;
        if (request.StartDate.HasValue) task.StartDate = request.StartDate.Value.Date;
        if (request.DueDate.HasValue) task.ExpectedEndDate = request.DueDate.Value.Date;
        if (remarks.Length > 0) task.Remarks = remarks;
        task.AssignedAt = DateTime.UtcNow; task.AssignedByStaffId = actor.IndividualId;
        var action = previous.HasValue && previous != request.StaffId ? "TaskReassigned" : "TaskAssigned";
        AddHistory(db, task.ProjectId, task.TaskId, request.StaffId, previous != request.StaffId ? previous : null, action,
            $"{task.TaskReference} {task.Title}. Priority {priority}. {remarks}".Trim(), actor.IndividualId, audit.OperationLogId);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public async Task<List<AssignmentHistoryDto>> GetAssignmentHistoryAsync(int projectId, int? staffId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesExistAsync(db, ct);
        var project = await db.Projects.AsNoTracking().FirstOrDefaultAsync(p => p.ProjectId == projectId, ct);
        if (project == null) return [];
        await EnsureOrganisationAsync(project.OrganisationId);

        var q = db.ProjectAssignmentHistories.AsNoTracking().Where(h => h.ProjectId == projectId);
        if (staffId.HasValue) q = q.Where(h => h.StaffId == staffId || h.PreviousStaffId == staffId);
        var rows = await q.OrderByDescending(h => h.PerformedAt).Take(200).ToListAsync(ct);

        var ids = rows.SelectMany(h => new[] { h.StaffId, h.PreviousStaffId, h.PerformedByStaffId }).Where(i => i.HasValue).Select(i => i!.Value).Distinct().ToList();
        var names = await StaffNamesAsync(db, ids, ct);
        var taskIds = rows.Where(h => h.TaskId.HasValue).Select(h => h.TaskId!.Value).Distinct().ToList();
        var titles = await db.ProjectTasks.AsNoTracking().Where(t => taskIds.Contains(t.TaskId)).ToDictionaryAsync(t => t.TaskId, t => t.Title, ct);
        string Name(int? id) => id.HasValue ? names.GetValueOrDefault(id.Value, $"Staff #{id}") : "";

        return rows.Select(h => new AssignmentHistoryDto
        {
            HistoryId = h.HistoryId, ProjectId = h.ProjectId, TaskId = h.TaskId,
            TaskTitle = h.TaskId.HasValue ? titles.GetValueOrDefault(h.TaskId.Value, "") : "",
            Action = h.Action, StaffName = Name(h.StaffId), PreviousStaffName = Name(h.PreviousStaffId),
            Details = h.Details, PerformedByName = Name(h.PerformedByStaffId), PerformedAt = h.PerformedAt
        }).ToList();
    }

    public async Task<int> CreateProjectAsync(CreateProjectInputDto input, CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync();
        var currentOrgId = await access.GetCurrentOrganisationIdAsync();
        var name = (input.Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Project name is required.");
        if (name.Length > 200) throw new ArgumentException("Project name cannot exceed 200 characters.");
        if (input.StartDate == default) throw new ArgumentException("Project start date is required.");
        if (input.PlannedEndDate.HasValue && input.PlannedEndDate.Value.Date < input.StartDate.Date)
            throw new ArgumentException("Planned end date cannot be earlier than start date.");

        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesExistAsync(db, ct);

        int orgId = currentOrgId;
        if (input.TeamId.HasValue && input.TeamId.Value > 0)
        {
            var team = await db.Teams.AsNoTracking().FirstOrDefaultAsync(t => t.TeamId == input.TeamId.Value, ct)
                ?? throw new ArgumentException("Team not found.");
            orgId = team.OrganisationId;
            if (!actor.IsSuperAdministrator && team.OrganisationId != currentOrgId && !await access.HasOrganisationAccessAsync(team.OrganisationId))
                throw new UnauthorizedAccessException("Not authorized for this team's organisation.");
        }
        else if (input.OrganisationStructureId.HasValue && input.OrganisationStructureId.Value > 0)
        {
            var dept = await db.OrganisationStructures.AsNoTracking().FirstOrDefaultAsync(d => d.OrganisationStructureId == input.OrganisationStructureId.Value, ct)
                ?? throw new ArgumentException("Department / Organisation structure not found.");
            orgId = dept.OrganisationBusinessEntityID;
            if (!actor.IsSuperAdministrator && dept.OrganisationBusinessEntityID != currentOrgId && !await access.HasOrganisationAccessAsync(dept.OrganisationBusinessEntityID))
                throw new UnauthorizedAccessException("Not authorized for this department's organisation.");
        }

        bool canManage = actor.IsSuperAdministrator ||
            await access.HasPermissionAsync("Admin") ||
            await access.HasPermissionAsync("Organisation.Edit") ||
            (input.TeamId.HasValue && await db.TeamStaffs.AnyAsync(x => x.TeamId == input.TeamId.Value && x.StaffId == actor.IndividualId && x.IsSuperVisor && x.IsValid, ct)) ||
            (input.OrganisationStructureId.HasValue && await db.OrganisationStructureSupervisors.AnyAsync(x => x.OrganisationStructureId == input.OrganisationStructureId.Value && x.SupervisorStaffId == actor.IndividualId && x.IsValid && x.IsActive, ct)) ||
            (input.ProjectManagerStaffId.HasValue && input.ProjectManagerStaffId.Value == actor.IndividualId);

        if (!canManage)
            throw new UnauthorizedAccessException("Only department/team supervisors, project managers, or administrators can create projects.");

        var reference = (input.Reference ?? "").Trim();
        if (string.IsNullOrWhiteSpace(reference))
        {
            var year = DateTime.UtcNow.Year;
            var prefix = $"PRJ-{year}-";
            var count = await db.Projects.CountAsync(p => p.Reference.StartsWith(prefix), ct);
            reference = $"{prefix}{(count + 1):D3}";
        }

        string? pmName = input.ProjectManagerName?.Trim();
        if (input.ProjectManagerStaffId.HasValue && input.ProjectManagerStaffId.Value > 0)
        {
            var names = await StaffNamesAsync(db, [input.ProjectManagerStaffId.Value], ct);
            if (names.TryGetValue(input.ProjectManagerStaffId.Value, out var resolvedName))
            {
                pmName = resolvedName;
            }
        }

        var project = new Project
        {
            OrganisationId = orgId,
            TeamId = input.TeamId > 0 ? input.TeamId : null,
            OrganisationStructureId = input.OrganisationStructureId > 0 ? input.OrganisationStructureId : null,
            Name = name,
            Reference = reference,
            Description = input.Description?.Trim(),
            ProjectManagerStaffId = input.ProjectManagerStaffId > 0 ? input.ProjectManagerStaffId : null,
            ProjectManagerName = pmName,
            StartDate = input.StartDate.Date,
            PlannedEndDate = input.PlannedEndDate?.Date,
            Status = string.IsNullOrWhiteSpace(input.Status) ? "Active" : input.Status.Trim(),
            ProgressPercentage = 0,
            CreatedAt = DateTime.UtcNow
        };

        db.Projects.Add(project);
        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.TEAM_STAFF_UPDATE,
            $"Created project {reference}: {name}.");
        await db.SaveChangesAsync(ct);

        AddHistory(db, project.ProjectId, null, input.ProjectManagerStaffId, null, "ProjectCreated",
            $"Created project {reference}: {name}." + (input.TeamId.HasValue ? $" Assigned to team #{input.TeamId.Value}." : "") + (input.OrganisationStructureId.HasValue ? $" Assigned to department #{input.OrganisationStructureId.Value}." : ""),
            actor.IndividualId, audit.OperationLogId);
        await db.SaveChangesAsync(ct);

        return project.ProjectId;
    }

    public async Task<int> CreateTaskAsync(CreateTaskInputDto input, CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync();
        if (input.ProjectId <= 0) throw new ArgumentException("A valid project must be selected.");
        var title = (input.Title ?? "").Trim();
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Task title is required.");
        if (title.Length > 200) throw new ArgumentException("Task title cannot exceed 200 characters.");
        var priority = string.IsNullOrWhiteSpace(input.Priority) ? "Medium" : input.Priority.Trim();
        if (!TaskPriorities.Contains(priority)) throw new ArgumentException("Invalid priority.");
        if (input.StartDate.HasValue && input.ExpectedEndDate.HasValue && input.ExpectedEndDate.Value.Date < input.StartDate.Value.Date)
            throw new ArgumentException("Expected end date cannot be earlier than start date.");

        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesExistAsync(db, ct);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

        var project = await db.Projects.FirstOrDefaultAsync(p => p.ProjectId == input.ProjectId, ct)
            ?? throw new ArgumentException("Project not found.");
        await RequireManageProjectAsync(db, project);
        RequireOpenProject(project);

        var taskRef = (input.TaskReference ?? "").Trim();
        if (string.IsNullOrWhiteSpace(taskRef))
        {
            var count = await db.ProjectTasks.CountAsync(t => t.ProjectId == project.ProjectId, ct);
            taskRef = $"TSK-{(count + 1):D3}";
        }

        string? assignedStaffName = null;
        if (input.AssignedStaffId.HasValue && input.AssignedStaffId.Value > 0)
        {
            await EnsureStaffInOrganisationAsync(db, input.AssignedStaffId.Value, ct);
            var names = await StaffNamesAsync(db, [input.AssignedStaffId.Value], ct);
            assignedStaffName = names.GetValueOrDefault(input.AssignedStaffId.Value, $"Staff #{input.AssignedStaffId.Value}");

            if (!await db.ProjectMembers.AnyAsync(m => m.ProjectId == project.ProjectId && m.StaffId == input.AssignedStaffId.Value && m.IsActive, ct))
            {
                var autoMember = new ProjectMember
                {
                    ProjectId = project.ProjectId,
                    StaffId = input.AssignedStaffId.Value,
                    StaffName = assignedStaffName,
                    Role = "Member",
                    AssignedDate = DateTime.UtcNow.Date,
                    Remarks = "Assigned via task creation",
                    IsActive = true,
                    AssignedByStaffId = actor.IndividualId
                };
                db.ProjectMembers.Add(autoMember);
            }
        }

        var task = new ProjectTask
        {
            ProjectId = project.ProjectId,
            TaskReference = taskRef,
            Title = title,
            Description = input.Description?.Trim(),
            AssignedStaffId = input.AssignedStaffId > 0 ? input.AssignedStaffId : null,
            AssignedStaffName = assignedStaffName,
            StartDate = input.StartDate?.Date,
            ExpectedEndDate = input.ExpectedEndDate?.Date,
            Status = string.IsNullOrWhiteSpace(input.Status) ? "Ongoing" : input.Status.Trim(),
            Priority = priority,
            Progress = 0,
            Remarks = input.Remarks?.Trim(),
            AssignedAt = input.AssignedStaffId > 0 ? DateTime.UtcNow : null,
            AssignedByStaffId = input.AssignedStaffId > 0 ? actor.IndividualId : null,
            CreatedAt = DateTime.UtcNow
        };

        db.ProjectTasks.Add(task);
        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.TEAM_STAFF_UPDATE,
            $"Created task {taskRef}: {title} in project {project.Reference}.");
        await db.SaveChangesAsync(ct);

        AddHistory(db, project.ProjectId, task.TaskId, input.AssignedStaffId, null,
            input.AssignedStaffId > 0 ? "TaskAssigned" : "TaskCreated",
            $"Task {taskRef} ({title}) created. Priority: {priority}." + (input.AssignedStaffId > 0 ? $" Assigned to {assignedStaffName}." : ""),
            actor.IndividualId, audit.OperationLogId);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return task.TaskId;
    }

    public async Task<bool> UpdateProjectAsync(UpdateProjectInputDto input, CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync();
        if (input.ProjectId <= 0) throw new ArgumentException("Select a valid project.");
        var name = (input.Name ?? "").Trim();
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Project name is required.");
        if (name.Length > 200) throw new ArgumentException("Project name cannot exceed 200 characters.");
        if (input.StartDate == default) throw new ArgumentException("Start date is required.");
        if (input.PlannedEndDate.HasValue && input.PlannedEndDate.Value.Date < input.StartDate.Date)
            throw new ArgumentException("Planned end date cannot be earlier than start date.");

        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesExistAsync(db, ct);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

        var project = await db.Projects.FirstOrDefaultAsync(p => p.ProjectId == input.ProjectId, ct)
            ?? throw new ArgumentException("Project not found.");
        await RequireManageProjectAsync(db, project);

        string? pmName = input.ProjectManagerName?.Trim();
        if (input.ProjectManagerStaffId.HasValue && input.ProjectManagerStaffId.Value > 0)
        {
            var names = await StaffNamesAsync(db, [input.ProjectManagerStaffId.Value], ct);
            if (names.TryGetValue(input.ProjectManagerStaffId.Value, out var resolvedName))
            {
                pmName = resolvedName;
            }
        }

        project.Name = name;
        if (!string.IsNullOrWhiteSpace(input.Reference)) project.Reference = input.Reference.Trim();
        project.Description = input.Description?.Trim();
        project.TeamId = input.TeamId > 0 ? input.TeamId : null;
        project.OrganisationStructureId = input.OrganisationStructureId > 0 ? input.OrganisationStructureId : null;
        project.ProjectManagerStaffId = input.ProjectManagerStaffId > 0 ? input.ProjectManagerStaffId : null;
        project.ProjectManagerName = pmName;
        project.StartDate = input.StartDate.Date;
        project.PlannedEndDate = input.PlannedEndDate?.Date;
        project.ActualEndDate = input.ActualEndDate?.Date;
        if (!string.IsNullOrWhiteSpace(input.Status)) project.Status = input.Status.Trim();
        if (input.ProgressPercentage >= 0 && input.ProgressPercentage <= 100) project.ProgressPercentage = input.ProgressPercentage;

        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.TEAM_STAFF_UPDATE,
            $"Updated project {project.Reference}: {project.Name}.");
        AddHistory(db, project.ProjectId, null, project.ProjectManagerStaffId, null, "ProjectUpdated",
            $"Updated project details. Status: {project.Status}, Progress: {project.ProgressPercentage}%.",
            actor.IndividualId, audit.OperationLogId);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public async Task<bool> DeleteProjectAsync(int projectId, CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync();
        if (projectId <= 0) return false;

        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesExistAsync(db, ct);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

        var project = await db.Projects.Include(p => p.Tasks).Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId, ct);
        if (project == null) return false;
        await RequireManageProjectAsync(db, project);

        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.TEAM_STAFF_UPDATE,
            $"Deleted project {project.Reference}: {project.Name}.");

        var histories = await db.ProjectAssignmentHistories.Where(h => h.ProjectId == projectId).ToListAsync(ct);
        db.ProjectAssignmentHistories.RemoveRange(histories);
        db.ProjectTasks.RemoveRange(project.Tasks);
        db.ProjectMembers.RemoveRange(project.Members);
        db.Projects.Remove(project);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public async Task<List<TeamProjectDto>> GetAllProjectsAsync(int? organisationId = null, int? organisationStructureId = null, int? teamId = null, CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync();
        var orgId = organisationId ?? await access.GetCurrentOrganisationIdAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesExistAsync(db, ct);

        bool isAdmin = actor.IsSuperAdministrator ||
            await access.HasPermissionAsync("Admin") ||
            await access.HasPermissionAsync("Organisation.Edit");

        var q = db.Projects.AsNoTracking()
            .Include(p => p.Team)
            .Include(p => p.OrganisationStructure)
            .Include(p => p.Members)
            .Include(p => p.Tasks)
            .AsQueryable();

        if (orgId > 0 && !actor.IsSuperAdministrator)
        {
            q = q.Where(p => p.OrganisationId == orgId || p.OrganisationId == 0);
        }

        if (organisationStructureId.HasValue && organisationStructureId.Value > 0)
        {
            q = q.Where(p => p.OrganisationStructureId == organisationStructureId.Value);
        }

        if (teamId.HasValue && teamId.Value > 0)
        {
            q = q.Where(p => p.TeamId == teamId.Value);
        }

        var candidateProjects = await q.OrderByDescending(p => p.StartDate).ToListAsync(ct);
        var dtos = new List<TeamProjectDto>();
        foreach (var p in candidateProjects)
        {
            if (!isAdmin && !await CanViewProjectAsync(db, p)) continue;
            var dto = MapProjectDto(p);
            dto.CanManageAssignments = isAdmin || await CanManageProjectAsync(db, p);
            dtos.Add(dto);
        }
        return dtos;
    }

    public async Task<List<TeamProjectDto>> GetDepartmentProjectsAsync(int organisationStructureId, CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync();
        var orgId = await access.GetCurrentOrganisationIdAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesExistAsync(db, ct);

        var dept = await db.OrganisationStructures.AsNoTracking().FirstOrDefaultAsync(d => d.OrganisationStructureId == organisationStructureId, ct);
        if (dept == null) return [];

        if (!actor.IsSuperAdministrator && dept.OrganisationBusinessEntityID != orgId && !await access.HasOrganisationAccessAsync(dept.OrganisationBusinessEntityID))
            return [];

        // ONLY supervisors can see their department projects and tasks
        bool isSupervisor = actor.IsSuperAdministrator ||
            await access.HasPermissionAsync("Admin") ||
            await access.HasPermissionAsync("Organisation.Edit") ||
            await db.OrganisationStructureSupervisors.AnyAsync(x => x.OrganisationStructureId == organisationStructureId && x.SupervisorStaffId == actor.IndividualId && x.IsValid && x.IsActive, ct);

        if (!isSupervisor) return [];

        var projects = await db.Projects.AsNoTracking()
            .Include(p => p.Team)
            .Include(p => p.OrganisationStructure)
            .Include(p => p.Members)
            .Include(p => p.Tasks)
            .Where(p => p.OrganisationStructureId == organisationStructureId)
            .OrderByDescending(p => p.StartDate)
            .ToListAsync(ct);

        var dtos = new List<TeamProjectDto>();
        foreach (var p in projects)
        {
            var dto = MapProjectDto(p);
            dto.CanManageAssignments = await CanManageProjectAsync(db, p);
            dtos.Add(dto);
        }
        return dtos;
    }

    public async Task<List<OrganisationStructureDropdownDto>> GetOrganisationStructuresAsync(int? organisationId = null, CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync();
        var orgId = organisationId ?? await access.GetCurrentOrganisationIdAsync();
        await using var db = await factory.CreateDbContextAsync(ct);

        var q = db.OrganisationStructures.AsNoTracking().AsQueryable();
        if (orgId > 0 && !actor.IsSuperAdministrator)
        {
            q = q.Where(s => s.OrganisationBusinessEntityID == orgId);
        }
        return await q.OrderBy(s => s.Name)
            .Select(s => new OrganisationStructureDropdownDto
            {
                OrganisationStructureId = s.OrganisationStructureId,
                Name = s.Name
            })
            .ToListAsync(ct);
    }

    public async Task<bool> UpdateTaskAsync(UpdateTaskInputDto input, CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync();
        if (input.TaskId <= 0) throw new ArgumentException("Select a valid task.");
        var title = (input.Title ?? "").Trim();
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Task title is required.");
        if (title.Length > 200) throw new ArgumentException("Task title cannot exceed 200 characters.");
        var priority = string.IsNullOrWhiteSpace(input.Priority) ? "Medium" : input.Priority.Trim();
        if (!TaskPriorities.Contains(priority)) throw new ArgumentException("Invalid priority.");
        if (input.StartDate.HasValue && input.ExpectedEndDate.HasValue && input.ExpectedEndDate.Value.Date < input.StartDate.Value.Date)
            throw new ArgumentException("Expected end date cannot be earlier than start date.");

        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesExistAsync(db, ct);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

        var task = await db.ProjectTasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.TaskId == input.TaskId, ct)
            ?? throw new ArgumentException("Task not found.");
        await RequireManageProjectAsync(db, task.Project);

        var previousStaffId = task.AssignedStaffId;
        string? assignedStaffName = task.AssignedStaffName;

        if (input.AssignedStaffId.HasValue && input.AssignedStaffId.Value > 0 && input.AssignedStaffId.Value != task.AssignedStaffId)
        {
            await EnsureStaffInOrganisationAsync(db, input.AssignedStaffId.Value, ct);
            var names = await StaffNamesAsync(db, [input.AssignedStaffId.Value], ct);
            assignedStaffName = names.GetValueOrDefault(input.AssignedStaffId.Value, $"Staff #{input.AssignedStaffId.Value}");

            if (!await db.ProjectMembers.AnyAsync(m => m.ProjectId == task.ProjectId && m.StaffId == input.AssignedStaffId.Value && m.IsActive, ct))
            {
                var autoMember = new ProjectMember
                {
                    ProjectId = task.ProjectId,
                    StaffId = input.AssignedStaffId.Value,
                    StaffName = assignedStaffName,
                    Role = "Member",
                    AssignedDate = DateTime.UtcNow.Date,
                    Remarks = "Assigned via task update",
                    IsActive = true,
                    AssignedByStaffId = actor.IndividualId
                };
                db.ProjectMembers.Add(autoMember);
            }
        }
        else if (input.AssignedStaffId == null || input.AssignedStaffId <= 0)
        {
            assignedStaffName = null;
        }

        task.Title = title;
        if (!string.IsNullOrWhiteSpace(input.TaskReference)) task.TaskReference = input.TaskReference.Trim();
        task.Description = input.Description?.Trim();
        task.AssignedStaffId = input.AssignedStaffId > 0 ? input.AssignedStaffId : null;
        task.AssignedStaffName = assignedStaffName;
        task.Priority = priority;
        if (!string.IsNullOrWhiteSpace(input.Status)) task.Status = input.Status.Trim();
        if (input.Progress >= 0 && input.Progress <= 100) task.Progress = input.Progress;
        task.StartDate = input.StartDate?.Date;
        task.ExpectedEndDate = input.ExpectedEndDate?.Date;
        task.ActualCompletionDate = input.ActualCompletionDate?.Date;
        if (task.Status == "Completed" && !task.ActualCompletionDate.HasValue)
        {
            task.ActualCompletionDate = DateTime.UtcNow.Date;
            task.Progress = 100;
        }
        task.Remarks = input.Remarks?.Trim();

        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.TEAM_STAFF_UPDATE,
            $"Updated task {task.TaskReference}: {task.Title}.");

        string action = previousStaffId != task.AssignedStaffId ? "TaskReassigned" : "TaskUpdated";
        AddHistory(db, task.ProjectId, task.TaskId, task.AssignedStaffId, previousStaffId, action,
            $"Task updated: {task.Title}. Priority: {task.Priority}, Status: {task.Status}.",
            actor.IndividualId, audit.OperationLogId);

        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    public async Task<bool> DeleteTaskAsync(int taskId, CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync();
        if (taskId <= 0) return false;

        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesExistAsync(db, ct);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);

        var task = await db.ProjectTasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.TaskId == taskId, ct);
        if (task == null) return false;
        await RequireManageProjectAsync(db, task.Project);

        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.TEAM_STAFF_UPDATE,
            $"Deleted task {task.TaskReference}: {task.Title} from project {task.Project.Reference}.");

        AddHistory(db, task.ProjectId, task.TaskId, task.AssignedStaffId, null, "TaskDeleted",
            $"Deleted task {task.TaskReference} ({task.Title}).", actor.IndividualId, audit.OperationLogId);

        db.ProjectTasks.Remove(task);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return true;
    }

    // Project managers, active department supervisors, and active team supervisors may manage it; so may administrators.
    private async Task<bool> CanManageProjectAsync(HrmTeContext db, Project project)
    {
        var actor = await access.RequireContextAsync();
        if (actor.IsSuperAdministrator) return true;
        var currentOrgId = await access.GetCurrentOrganisationIdAsync();
        if (project.OrganisationId != 0 && project.OrganisationId != currentOrgId && !await access.HasOrganisationAccessAsync(project.OrganisationId))
            return false;
        if (await access.HasPermissionAsync("Admin") || await access.HasPermissionAsync("Organisation.Edit")) return true;
        if (project.ProjectManagerStaffId == actor.IndividualId) return true;
        if (project.TeamId.HasValue && await db.TeamStaffs.AnyAsync(x =>
            x.TeamId == project.TeamId && x.StaffId == actor.IndividualId && x.IsSuperVisor && x.IsValid, default))
            return true;
        if (project.OrganisationStructureId.HasValue && await db.OrganisationStructureSupervisors.AnyAsync(x =>
            x.OrganisationStructureId == project.OrganisationStructureId && x.SupervisorStaffId == actor.IndividualId && x.IsValid && x.IsActive, default))
            return true;
        return false;
    }

    private async Task<bool> CanViewProjectAsync(HrmTeContext db, Project project)
    {
        return await CanManageProjectAsync(db, project);
    }

    private async Task RequireManageProjectAsync(HrmTeContext db, Project project)
    {
        if (!await CanManageProjectAsync(db, project)) throw new UnauthorizedAccessException();
    }

    private static void RequireOpenProject(Project project)
    {
        if (ClosedProjectStatuses.Contains(project.Status))
            throw new InvalidOperationException($"The project is {project.Status.ToLowerInvariant()} and cannot receive new assignments.");
    }

    private static void AddHistory(HrmTeContext db, int projectId, int? taskId, int? staffId, int? previousStaffId, string action, string details, int performedBy, int operationLogId)
        => db.ProjectAssignmentHistories.Add(new ProjectAssignmentHistory
        {
            ProjectId = projectId, TaskId = taskId, StaffId = staffId, PreviousStaffId = previousStaffId, Action = action,
            Details = details.Length > 1000 ? details[..1000] : details, PerformedByStaffId = performedBy,
            PerformedAt = DateTime.UtcNow, OperationLogId = operationLogId
        });

    private static async Task<Dictionary<int, string>> StaffNamesAsync(HrmTeContext db, List<int> ids, CancellationToken ct)
    {
        if (ids.Count == 0) return [];
        var rows = await db.Staffs.AsNoTracking().Include(s => s.Individual).Where(s => ids.Contains(s.IndividualId)).ToListAsync(ct);
        return rows.ToDictionary(s => s.IndividualId,
            s => (s.Individual.FirstNameEnglish + " " + (s.Individual.MiddleNameEnglish ?? "") + " " + (s.Individual.LastNameEnglish ?? "")).Trim());
    }
}
