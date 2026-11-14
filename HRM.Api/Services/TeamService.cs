using HRM.Components.Shared;
using HRM.DTOs;
using HRM.Models;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HRM.Services;

public partial class TeamService(IDbContextFactory<HrmTeContext> factory, IUserAccessService access, IOperationLogService logs) : ITeam
{
    public async Task<int> CreateAsync(int organisationId, string name, string? nameDhivehi, DateTime startDate, CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync();
        if (!actor.IsSuperAdministrator && (organisationId != await access.GetCurrentOrganisationIdAsync() ||
            !await access.HasOrganisationAccessAsync(organisationId))) throw new UnauthorizedAccessException();
        if (!actor.IsSuperAdministrator && !await access.HasPermissionAsync("Admin") && !await access.HasPermissionAsync("Organisation.Edit"))
            throw new UnauthorizedAccessException();
        name = name.Trim(); nameDhivehi = nameDhivehi?.Trim();
        if (organisationId <= 0 || name.Length is < 1 or > 200 || nameDhivehi?.Length > 200 || startDate == default)
            throw new ArgumentException("Enter a team name and start date.");
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        if (!await db.Organisations.AnyAsync(o => o.BusinessEntityID == organisationId, ct)) throw new ArgumentException("Organisation unavailable.");
        if (await db.Teams.AnyAsync(t => t.OrganisationId == organisationId && t.Name == name, ct)) throw new InvalidOperationException("This team already exists.");
        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.TEAM_CREATE, $"Create team {name} in organisation {organisationId}.");
        var entity = new Team { OrganisationId = organisationId, Name = name, NameDhivehi = nameDhivehi,
            StartDate = startDate.Date, IsValid = true, OperationLogId = audit.OperationLogId };
        db.Teams.Add(entity); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return entity.TeamId;
    }

    public async Task<List<TeamDto>> GetMyTeamMembersAsync(int teamId)
        => (await GetMyTeamsAsync()).Where(x => x.TeamId == teamId).ToList();

    public async Task<int> AddStaffAsync(int teamId, int staffId, DateTime startDate, bool isSupervisor = false, CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync();
        if (!actor.IsSuperAdministrator && !await access.HasPermissionAsync("Admin") && !await access.HasPermissionAsync("Organisation.Edit"))
            throw new UnauthorizedAccessException();
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var team = await db.Teams.FirstOrDefaultAsync(t => t.TeamId == teamId, ct);
        if (team == null) throw new ArgumentException("Team not found.");
        if (!actor.IsSuperAdministrator && team.OrganisationId != await access.GetCurrentOrganisationIdAsync())
            throw new UnauthorizedAccessException();
        if (await db.TeamStaffs.AnyAsync(x => x.TeamId == teamId && x.StaffId == staffId && x.IsValid, ct))
            throw new InvalidOperationException("Staff member is already assigned to this team.");
        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.TEAM_STAFF_CREATE, $"Add staff {staffId} to team {teamId}.");
        var entity = new TeamStaff { TeamId = teamId, StaffId = staffId, IsSuperVisor = isSupervisor, StartDate = startDate.Date, IsValid = true, OperationLogId = audit.OperationLogId };
        db.TeamStaffs.Add(entity); await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return entity.TeamStaffsId;
    }

    public async Task<bool> RemoveStaffAsync(int teamId, int staffId, CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync();
        if (!actor.IsSuperAdministrator && !await access.HasPermissionAsync("Admin") && !await access.HasPermissionAsync("Organisation.Edit"))
            throw new UnauthorizedAccessException();
        await using var db = await factory.CreateDbContextAsync(ct);
        await using var tx = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var team = await db.Teams.FirstOrDefaultAsync(t => t.TeamId == teamId, ct);
        if (team == null) throw new ArgumentException("Team not found.");
        if (!actor.IsSuperAdministrator && team.OrganisationId != await access.GetCurrentOrganisationIdAsync())
            throw new UnauthorizedAccessException();
        var assignment = await db.TeamStaffs.FirstOrDefaultAsync(x => x.TeamId == teamId && x.StaffId == staffId && x.IsValid, ct);
        if (assignment == null) return false;
        var audit = await logs.CreateAsync(db, SharedConfig.OperationLogActionTypes.TEAM_STAFF_UPDATE, $"Remove staff {staffId} from team {teamId}.");
        assignment.IsValid = false; assignment.EndDate = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Indian/Maldives").Date; assignment.OperationLogId = audit.OperationLogId;
        await db.SaveChangesAsync(ct); await tx.CommitAsync(ct); return true;
    }

    public async Task<List<TeamDto>> GetMyTeamsAsync()
    {
        var actor = await access.RequireContextAsync();
        if (actor.ActiveJob == null) return [];
        await using var db = await factory.CreateDbContextAsync();
        var today = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Indian/Maldives").Date;
        var organisationId = await db.Jobs.Where(x => x.JobId == actor.ActiveJob.JobId && x.IndividualID == actor.IndividualId &&
            x.JobStateId == SharedConfig.JobStates.APPROVED && x.TerminatedDate == null && x.JoinedDate <= today)
            .Select(x => (int?)x.OrganisationID).SingleOrDefaultAsync();
        if (organisationId == null) return [];
        var teamIds = db.TeamStaffs.Where(x => x.StaffId == actor.IndividualId && x.IsSuperVisor && x.IsValid &&
            x.StartDate <= today && (x.EndDate == null || x.EndDate >= today)).Select(x => x.TeamId);
        var teams = await db.Teams.AsNoTracking().Where(x => teamIds.Contains(x.TeamId) && x.OrganisationId == organisationId &&
            x.IsValid == true && (x.StartDate == null || x.StartDate <= today) && (x.EndDate == null || x.EndDate >= today))
            .OrderBy(x => x.Name).Select(x => new TeamDto
            {
                TeamId = x.TeamId, ParentTeamId = x.ParentTeamId, OrganisationStructureId = x.OrganisationStructureId,
                Name = x.Name, NameDhivehi = x.NameDhivehi ?? "",
                SubTeamCount = db.Teams.Count(st => st.ParentTeamId == x.TeamId && st.IsValid == true && (st.StartDate == null || st.StartDate <= today) && (st.EndDate == null || st.EndDate >= today)),
                Staffs = x.TeamStaffs.Where(m => m.IsValid && m.StartDate <= today && (m.EndDate == null || m.EndDate >= today) &&
                    db.Jobs.Any(j => j.IndividualID == m.StaffId && j.OrganisationID == organisationId &&
                        j.JobStateId == SharedConfig.JobStates.APPROVED && j.TerminatedDate == null && j.JoinedDate <= today))
                    .Select(m => new TeamStaffDto
                    {
                        StaffId = m.StaffId, IsSuperVisor = m.IsSuperVisor,
                        FullName = (m.Staff.Individual.FirstNameEnglish + " " + (m.Staff.Individual.MiddleNameEnglish ?? "") + " " + (m.Staff.Individual.LastNameEnglish ?? "")).Trim()
                    }).ToList()
            }).ToListAsync();
        foreach (var team in teams)
            team.Staffs = team.Staffs.GroupBy(x => x.StaffId).Select(x => x.OrderByDescending(m => m.IsSuperVisor).First())
                .OrderByDescending(x => x.IsSuperVisor).ThenBy(x => x.FullName).ToList();
        return teams;
    }

    public async Task<MyOrganisationStructureDto?> GetMyOrganisationStructureAsync()
    {
        var actor = await access.RequireContextAsync();
        if (actor.ActiveJob == null) return null;
        await using var db = await factory.CreateDbContextAsync();
        return await db.Jobs.AsNoTracking()
            .Where(x => x.JobId == actor.ActiveJob.JobId && x.IndividualID == actor.IndividualId &&
                x.JobStateId == SharedConfig.JobStates.APPROVED && x.TerminatedDate == null && x.OrganisationStructureId > 0)
            .Select(x => new MyOrganisationStructureDto
            {
                StructureId = x.OrganisationStructureId,
                Name = x.OrganisationStructure!.Name,
                TypeName = x.OrganisationStructure.OrganisationStructureType.TypeName
            })
            .SingleOrDefaultAsync();
    }

    public async Task<List<TeamSearchItemDto>> SearchTeamsAsync(string? query, CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync();
        var orgId = await access.GetCurrentOrganisationIdAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        var today = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Indian/Maldives").Date;

        var q = db.Teams.AsNoTracking().Where(t => t.IsValid == true && (t.OrganisationId == orgId || actor.IsSuperAdministrator));
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = query.Trim();
            q = q.Where(t => EF.Functions.Like(t.Name, $"%{term}%") || (t.NameDhivehi != null && EF.Functions.Like(t.NameDhivehi, $"%{term}%")));
        }

        var orgName = await db.Organisations.Where(o => o.BusinessEntityID == orgId).Select(o => o.OrganisationName).FirstOrDefaultAsync(ct) ?? "Organisation";

        var teamList = await q.OrderBy(t => t.Name).Take(20).Select(t => new
        {
            t.TeamId,
            t.Name,
            t.NameDhivehi,
            t.OrganisationId,
            Members = t.TeamStaffs.Where(m => m.IsValid && m.StartDate <= today && (m.EndDate == null || m.EndDate >= today)).Select(m => new
            {
                m.StaffId,
                m.IsSuperVisor,
                FullName = (m.Staff.Individual.FirstNameEnglish + " " + (m.Staff.Individual.MiddleNameEnglish ?? "") + " " + (m.Staff.Individual.LastNameEnglish ?? "")).Trim()
            }).ToList()
        }).ToListAsync(ct);

        var myTeams = await GetMyTeamsAsync();
        var myTeamIds = myTeams.Select(t => t.TeamId).ToHashSet();

        return teamList.Select(t => new TeamSearchItemDto
        {
            TeamId = t.TeamId,
            Name = t.Name,
            NameDhivehi = t.NameDhivehi,
            OrganisationId = t.OrganisationId,
            OrganisationName = orgName,
            MemberCount = t.Members.Count,
            SupervisorCount = t.Members.Count(m => m.IsSuperVisor),
            LeadSupervisorName = t.Members.FirstOrDefault(m => m.IsSuperVisor)?.FullName,
            IsMyTeam = myTeamIds.Contains(t.TeamId)
        }).ToList();
    }

    public async Task<TeamDto?> GetTeamByIdAsync(int teamId, CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync();
        var orgId = await access.GetCurrentOrganisationIdAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        var today = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Indian/Maldives").Date;

        var team = await db.Teams.AsNoTracking().FirstOrDefaultAsync(t => t.TeamId == teamId && (t.OrganisationId == orgId || actor.IsSuperAdministrator), ct);
        if (team == null) return null;

        var staffs = await db.TeamStaffs.AsNoTracking().Where(m => m.TeamId == teamId && m.IsValid && m.StartDate <= today &&
            (m.EndDate == null || m.EndDate >= today) &&
            FormerStaffPolicy.CurrentJobs(db).Any(j => j.IndividualID == m.StaffId && j.OrganisationID == team.OrganisationId))
            .Select(m => new TeamStaffDto
            {
                StaffId = m.StaffId,
                IsSuperVisor = m.IsSuperVisor,
                FullName = (m.Staff.Individual.FirstNameEnglish + " " + (m.Staff.Individual.MiddleNameEnglish ?? "") + " " + (m.Staff.Individual.LastNameEnglish ?? "")).Trim()
            }).ToListAsync(ct);

        staffs = staffs.GroupBy(x => x.StaffId).Select(x => x.OrderByDescending(m => m.IsSuperVisor).First())
            .OrderByDescending(x => x.IsSuperVisor).ThenBy(x => x.FullName).ToList();

        var subTeamCount = await db.Teams.CountAsync(st => st.ParentTeamId == team.TeamId && st.IsValid == true && (st.StartDate == null || st.StartDate <= today) && (st.EndDate == null || st.EndDate >= today), ct);

        return new TeamDto
        {
            TeamId = team.TeamId,
            ParentTeamId = team.ParentTeamId,
            OrganisationStructureId = team.OrganisationStructureId,
            Name = team.Name,
            NameDhivehi = team.NameDhivehi ?? "",
            FromDate = team.StartDate,
            ToDate = team.EndDate,
            IsValid = team.IsValid ?? true,
            SubTeamCount = subTeamCount,
            Staffs = staffs
        };
    }

    public async Task<List<TeamProjectDto>> GetTeamProjectsAsync(int teamId, CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync();
        var orgId = await access.GetCurrentOrganisationIdAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesExistAsync(db, ct);

        var team = await db.Teams.AsNoTracking().FirstOrDefaultAsync(t => t.TeamId == teamId, ct);
        if (team == null) return [];

        if (!actor.IsSuperAdministrator && team.OrganisationId != orgId && !await access.HasOrganisationAccessAsync(team.OrganisationId))
            return [];

        // ONLY supervisors can see their team projects and tasks
        bool isSupervisor = actor.IsSuperAdministrator ||
            await access.HasPermissionAsync("Admin") ||
            await access.HasPermissionAsync("Organisation.Edit") ||
            await db.TeamStaffs.AnyAsync(x => x.TeamId == teamId && x.StaffId == actor.IndividualId && x.IsSuperVisor && x.IsValid, ct);

        if (!isSupervisor) return [];

        var projects = await db.Projects.AsNoTracking()
            .Include(p => p.Team)
            .Include(p => p.Members)
            .Include(p => p.Tasks)
            .Include(p => p.OrganisationStructure)
            .Where(p => p.TeamId == teamId)
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

    public async Task<TeamProjectDto?> GetProjectDetailsAsync(int projectId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesExistAsync(db, ct);
        var project = await db.Projects.AsNoTracking()
            .Include(p => p.Team)
            .Include(p => p.Members)
            .Include(p => p.Tasks)
            .FirstOrDefaultAsync(p => p.ProjectId == projectId, ct);

        if (project == null) return null;
        await EnsureOrganisationAsync(project.OrganisationId);
        var dto = MapProjectDto(project);
        dto.CanManageAssignments = await CanManageProjectAsync(db, project);
        return dto;
    }

    public async Task<TeamProjectTaskDto?> GetTaskDetailsAsync(int taskId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesExistAsync(db, ct);
        var task = await db.ProjectTasks.AsNoTracking()
            .Include(t => t.Project)
            .FirstOrDefaultAsync(t => t.TaskId == taskId, ct);

        if (task?.Project != null) await EnsureOrganisationAsync(task.Project.OrganisationId);
        if (task == null) return null;

        return new TeamProjectTaskDto
        {
            TaskId = task.TaskId,
            ProjectId = task.ProjectId,
            ProjectName = task.Project?.Name ?? "General Project",
            TaskReference = task.TaskReference,
            Title = task.Title,
            Description = task.Description ?? string.Empty,
            AssignedStaffId = task.AssignedStaffId,
            AssignedStaffName = task.AssignedStaffName ?? string.Empty,
            StartDate = task.StartDate,
            ExpectedEndDate = task.ExpectedEndDate,
            ActualCompletionDate = task.ActualCompletionDate,
            Status = task.Status,
            Priority = task.Priority,
            Progress = task.Progress,
            Remarks = task.Remarks ?? string.Empty
        };
    }

    private static bool _tablesInitialized;
    private static readonly SemaphoreSlim _initLock = new(1, 1);

    private static string? GetEmbeddedScript(string fileName)
    {
        using var stream = typeof(TeamService).Assembly.GetManifestResourceStream($"HRM.Database.Scripts.{fileName}");
        if (stream == null) return null;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static async Task EnsureTablesExistAsync(HrmTeContext db, CancellationToken ct)
    {
        if (_tablesInitialized) return;
        await _initLock.WaitAsync(ct);
        try
        {
            if (_tablesInitialized) return;
            var script1 = GetEmbeddedScript("20261106_WorkspaceTeamsProjectsEvaluations.sql");
            if (!string.IsNullOrWhiteSpace(script1))
            {
                await db.Database.ExecuteSqlRawAsync(script1, ct);
            }
            var script2 = GetEmbeddedScript("20261106_ProjectAssignments.sql");
            if (!string.IsNullOrWhiteSpace(script2))
            {
                await db.Database.ExecuteSqlRawAsync(script2, ct);
            }
            try
            {
                await db.Database.ExecuteSqlRawAsync(@"IF OBJECT_ID(N'dbo.PR_Projects', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.PR_Projects', N'OrganisationStructureId') IS NULL
                BEGIN
                    ALTER TABLE dbo.PR_Projects ADD OrganisationStructureId int NULL;
                END;", ct);
            }
            catch { }
            _tablesInitialized = true;
        }
        catch
        {
            // Transient or permission fallback
        }
        finally
        {
            _initLock.Release();
        }
    }
    private async Task EnsureOrganisationAsync(int? organisationId)
    {
        var actor = await access.RequireContextAsync();
        if (actor.IsSuperAdministrator) return;
        if (organisationId == null || organisationId == 0) return;
        var currentOrgId = await access.GetCurrentOrganisationIdAsync();
        if (organisationId == currentOrgId) return;
        if (await access.HasOrganisationAccessAsync(organisationId.Value)) return;
        throw new UnauthorizedAccessException();
    }

    private async Task EnsureStaffInOrganisationAsync(HrmTeContext db, int staffId, CancellationToken ct)
    {
        var actor = await access.RequireContextAsync();
        if (actor.IsSuperAdministrator) return;
        var orgId = await access.GetCurrentOrganisationIdAsync();

        if (await db.Jobs.AnyAsync(j => j.IndividualID == staffId && (j.OrganisationID == orgId || j.OrganisationID == 0), ct))
            return;

        if (await db.TeamStaffs.AnyAsync(ts => ts.StaffId == staffId && (ts.Team.OrganisationId == orgId || ts.Team.OrganisationId == 0), ct))
            return;

        if (await db.ProjectMembers.AnyAsync(pm => pm.StaffId == staffId && (pm.Project.OrganisationId == orgId || pm.Project.OrganisationId == 0), ct))
            return;

        if (await db.Staffs.AnyAsync(s => s.IndividualId == staffId, ct))
            return;

        throw new UnauthorizedAccessException();
    }

    public async Task<Staff360SummaryDto?> GetStaff360Async(int staffId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesExistAsync(db, ct);
        await EnsureStaffInOrganisationAsync(db, staffId, ct);
        var staff = await db.Staffs.AsNoTracking()
            .Include(s => s.Individual)
            .FirstOrDefaultAsync(s => s.IndividualId == staffId, ct);

        var fullName = staff != null
            ? (staff.Individual.FirstNameEnglish + " " + (staff.Individual.MiddleNameEnglish ?? "") + " " + (staff.Individual.LastNameEnglish ?? "")).Trim()
            : $"Staff #{staffId}";

        var job = await db.Jobs.AsNoTracking()
            .Include(j => j.JobPositions).ThenInclude(jp => jp.Position)
            .Include(j => j.JobType)
            .Include(j => j.OrganisationStructure)
            .FirstOrDefaultAsync(j => j.IndividualID == staffId && j.JobStateId == SharedConfig.JobStates.APPROVED && j.TerminatedDate == null, ct);

        var today = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Indian/Maldives").Date;
        var assignedTeams = await db.TeamStaffs.AsNoTracking()
            .Include(ts => ts.Team)
            .Where(ts => ts.StaffId == staffId && ts.IsValid && ts.StartDate <= today && (ts.EndDate == null || ts.EndDate >= today))
            .Select(ts => ts.Team.Name)
            .ToListAsync(ct);

        if (assignedTeams.Count == 0) assignedTeams.Add("Operations Team");

        var actor360 = await access.RequireContextAsync();
        var org360 = await access.GetCurrentOrganisationIdAsync();
        var dbTasks = await db.ProjectTasks.AsNoTracking()
            .Include(t => t.Project)
            .Where(t => t.AssignedStaffId == staffId && (actor360.IsSuperAdministrator || t.Project.OrganisationId == org360))
            .ToListAsync(ct);

        var memberships = await db.ProjectMembers.AsNoTracking()
            .Include(m => m.Project)
            .Where(m => m.StaffId == staffId && (actor360.IsSuperAdministrator || m.Project.OrganisationId == org360))
            .OrderByDescending(m => m.IsActive).ThenByDescending(m => m.AssignedDate)
            .ToListAsync(ct);
        var projectAssignments = new List<StaffProjectAssignmentDto>();
        foreach (var m in memberships)
            projectAssignments.Add(new StaffProjectAssignmentDto
            {
                ProjectMemberId = m.ProjectMemberId, ProjectId = m.ProjectId, ProjectReference = m.Project.Reference,
                ProjectName = m.Project.Name, ProjectStatus = m.Project.Status, Role = m.Role, AssignedDate = m.AssignedDate,
                ExpectedEndDate = m.ExpectedEndDate, RemovedAt = m.RemovedAt,
                Remarks = m.Remarks ?? string.Empty, IsActive = m.IsActive,
                CanManage = m.IsActive && await CanManageProjectAsync(db, m.Project)
            });

        List<TeamProjectTaskDto> allTasks;
        {
            allTasks = dbTasks.Select(t => new TeamProjectTaskDto
            {
                TaskId = t.TaskId,
                ProjectId = t.ProjectId,
                ProjectName = t.Project?.Name ?? "Project",
                TaskReference = t.TaskReference,
                Title = t.Title,
                Description = t.Description ?? string.Empty,
                AssignedStaffId = t.AssignedStaffId,
                AssignedStaffName = t.AssignedStaffName ?? string.Empty,
                StartDate = t.StartDate,
                ExpectedEndDate = t.ExpectedEndDate,
                ActualCompletionDate = t.ActualCompletionDate,
                Status = t.Status,
                Priority = t.Priority,
                Progress = t.Progress,
                Remarks = t.Remarks ?? string.Empty
            }).ToList();
        }

        var ongoing = allTasks.Where(t => t.Status is "Ongoing" or "In Progress" or "Active").ToList();
        var completed = allTasks.Where(t => t.Status is "Completed").ToList();
        var parked = allTasks.Where(t => t.Status is "Parked" or "On Hold" or "Paused").ToList();


        return new Staff360SummaryDto
        {
            StaffId = staffId,
            FullName = fullName,
            Department = job?.OrganisationStructure?.Name ?? "Operations & Support",
            Designation = job?.JobPositions.FirstOrDefault()?.Position.Name ?? job?.JobType?.TypeName ?? "Staff Specialist",
            Email = $"{string.Join(".", fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries)).ToLowerInvariant()}@hrm.gov.mv",
            AvatarColor = "bg-blue-600 text-white",
            AssignedTeams = assignedTeams,
            ProjectAssignments = projectAssignments,
            CanManageAssignments = projectAssignments.Any(a => a.CanManage),
            OngoingTasks = ongoing,
            CompletedTasks = completed,
            ParkedTasks = parked
        };
    }

    public async Task<List<StaffEvaluationDto>> GetStaffEvaluationsAsync(int staffId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesExistAsync(db, ct);
        await EnsureStaffInOrganisationAsync(db, staffId, ct);
        var evals = await db.Evaluations.AsNoTracking()
            .Include(e => e.Scores)
            .Where(e => e.StaffId == staffId)
            .OrderByDescending(e => e.EvaluationDate)
            .ToListAsync(ct);

        if (evals.Count == 0)
        {
            return new List<StaffEvaluationDto>();
        }

        return evals.Select(MapToEvaluationDto).ToList();
    }

    public async Task<StaffEvaluationDto?> GetStaffEvaluationDetailsAsync(int evaluationId, CancellationToken ct = default)
    {
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesExistAsync(db, ct);
        var eval = await db.Evaluations.AsNoTracking()
            .Include(e => e.Scores)
            .FirstOrDefaultAsync(e => e.EvaluationId == evaluationId, ct);

        if (eval == null) return null;
        await EnsureStaffInOrganisationAsync(db, eval.StaffId, ct);
        return MapToEvaluationDto(eval);
    }

    private static StaffEvaluationDto MapToEvaluationDto(Evaluation e) => new()
    {
        EvaluationId = e.EvaluationId,
        StaffId = e.StaffId,
        StaffName = e.StaffName,
        Designation = e.Designation ?? "",
        DepartmentName = e.DepartmentName ?? "",
        TeamName = e.TeamName ?? "",
        EvaluatorStaffId = e.EvaluatorStaffId,
        EvaluatorName = e.EvaluatorName,
        EvaluationDate = e.EvaluationDate,
        Period = e.Period,
        TemplateName = string.IsNullOrWhiteSpace(e.TemplateName) ? "Standard Staff Evaluation" : e.TemplateName,
        OverallRating = e.OverallRating,
        Status = e.Status,
        GeneralComments = e.GeneralComments ?? "",
        Achievements = e.Achievements ?? "",
        Strengths = e.Strengths ?? "",
        AreasForImprovement = e.AreasForImprovement ?? "",
        TrainingRecommendations = e.TrainingRecommendations ?? "",
        DevelopmentObjectives = e.DevelopmentObjectives ?? "",
        Goals = e.Goals ?? "",
        ProjectContributions = e.ProjectContributions ?? "",
        FinalisedBy = e.FinalisedBy,
        FinalisedAt = e.FinalisedAt,
        AcknowledgedBy = e.AcknowledgedBy,
        AcknowledgedAt = e.AcknowledgedAt,
        AcknowledgementComments = e.AcknowledgementComments,
        Scores = e.Scores.Select(s => new StaffEvaluationScoreDto
        {
            ScoreId = s.ScoreId,
            EvaluationId = s.EvaluationId,
            Category = string.IsNullOrWhiteSpace(s.Category) ? "Work Performance" : s.Category,
            CriteriaName = s.CriteriaName,
            Score = s.Score,
            Weight = s.Weight > 0 ? s.Weight : 1.0m,
            Comments = s.Comments ?? ""
        }).ToList()
    };

    public async Task<int> SaveStaffEvaluationAsync(SaveEvaluationRequestDto request, CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync();
        var orgId = await access.GetCurrentOrganisationIdAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesExistAsync(db, ct);
        await EnsureStaffInOrganisationAsync(db, request.StaffId, ct);

        var isSuperOrAdmin = actor.IsSuperAdministrator || await access.HasPermissionAsync("Admin") || await access.HasPermissionAsync("Organisation.Edit");
        if (!isSuperOrAdmin)
        {
            var isSupervisor = await db.TeamStaffs.AnyAsync(s => s.StaffId == actor.IndividualId && s.IsSuperVisor && s.IsValid &&
                db.TeamStaffs.Any(m => m.TeamId == s.TeamId && m.StaffId == request.StaffId && m.IsValid), ct);
            if (!isSupervisor) throw new UnauthorizedAccessException("You are not authorised to evaluate this staff member.");
        }

        // Calculate overall score: weighted if weights provided, else average
        decimal overallRating;
        if (request.Scores.Count > 0)
        {
            var totalWeight = request.Scores.Sum(s => s.Weight > 0 ? s.Weight : 1.0m);
            if (totalWeight > 0)
            {
                var weightedSum = request.Scores.Sum(s => s.Score * (s.Weight > 0 ? s.Weight : 1.0m));
                overallRating = Math.Round(weightedSum / totalWeight, 1);
            }
            else
            {
                overallRating = Math.Round((decimal)request.Scores.Average(s => s.Score), 1);
            }
        }
        else if (request.OverallRating > 0)
        {
            overallRating = request.OverallRating;
        }
        else
        {
            overallRating = 3.0m;
        }

        var status = string.IsNullOrWhiteSpace(request.Status) ? "Submitted" : request.Status;

        // Editing an existing evaluation (Draft, Returned, or Revision)
        if (request.EvaluationId > 0)
        {
            var existing = await db.Evaluations
                .Include(e => e.Scores)
                .FirstOrDefaultAsync(e => e.EvaluationId == request.EvaluationId, ct);

            if (existing == null) throw new InvalidOperationException($"Evaluation #{request.EvaluationId} not found.");

            // If existing is already Finalised or Acknowledged, only SuperAdmin/Admin can alter
            if ((existing.Status == "Finalised" || existing.Status == "Acknowledged") && !isSuperOrAdmin)
            {
                throw new InvalidOperationException("Finalised or Acknowledged evaluations cannot be modified without administrator authorisation.");
            }

            existing.Period = string.IsNullOrWhiteSpace(request.Period) ? existing.Period : request.Period;
            existing.TemplateName = string.IsNullOrWhiteSpace(request.TemplateName) ? existing.TemplateName : request.TemplateName;
            existing.OverallRating = overallRating;
            existing.Status = status;
            existing.GeneralComments = request.GeneralComments ?? "";
            existing.Achievements = request.Achievements ?? "";
            existing.Strengths = request.Strengths ?? "";
            existing.AreasForImprovement = request.AreasForImprovement ?? "";
            existing.TrainingRecommendations = request.TrainingRecommendations ?? "";
            existing.DevelopmentObjectives = request.DevelopmentObjectives ?? "";
            existing.Goals = request.Goals ?? "";
            existing.ProjectContributions = request.ProjectContributions ?? "";
            existing.UpdatedAt = DateTime.UtcNow;

            if (status == "Finalised" && existing.FinalisedAt == null)
            {
                existing.FinalisedBy = actor.FullName;
                existing.FinalisedAt = DateTime.UtcNow;
            }

            // Update scores
            db.EvaluationScores.RemoveRange(existing.Scores);
            if (request.Scores.Count > 0)
            {
                foreach (var s in request.Scores)
                {
                    existing.Scores.Add(new EvaluationScore
                    {
                        Category = string.IsNullOrWhiteSpace(s.Category) ? "Work Performance" : s.Category,
                        CriteriaName = s.CriteriaName,
                        Score = s.Score,
                        Weight = s.Weight > 0 ? s.Weight : 1.0m,
                        Comments = s.Comments ?? ""
                    });
                }
            }

            await db.SaveChangesAsync(ct);
            return existing.EvaluationId;
        }

        // Creating a new evaluation
        var staff = await db.Staffs.AsNoTracking().Include(s => s.Individual).FirstOrDefaultAsync(s => s.IndividualId == request.StaffId, ct);
        var staffName = staff != null
            ? (staff.Individual.FirstNameEnglish + " " + (staff.Individual.MiddleNameEnglish ?? "") + " " + (staff.Individual.LastNameEnglish ?? "")).Trim()
            : $"Staff #{request.StaffId}";

        var activeJob = await db.Jobs.AsNoTracking()
            .Include(j => j.JobPositions).ThenInclude(jp => jp.Position)
            .Include(j => j.JobType)
            .Include(j => j.OrganisationStructure)
            .FirstOrDefaultAsync(j => j.IndividualID == request.StaffId && (j.OrganisationID == orgId || j.OrganisationID == 0), ct);

        var teamMembership = await db.TeamStaffs.AsNoTracking()
            .Include(ts => ts.Team)
            .FirstOrDefaultAsync(ts => ts.StaffId == request.StaffId && ts.IsValid, ct);

        var evaluatorName = actor.ActiveJob != null ? actor.FullName : "Supervisor";

        var eval = new Evaluation
        {
            OrganisationId = orgId,
            StaffId = request.StaffId,
            StaffName = staffName,
            Designation = activeJob?.JobPositions.FirstOrDefault()?.Position.Name ?? activeJob?.JobType?.TypeName ?? "Staff Member",
            DepartmentName = activeJob?.OrganisationStructure?.Name ?? teamMembership?.Team?.Name ?? "General",
            TeamName = teamMembership?.Team?.Name ?? "Operations",
            EvaluatorStaffId = actor.IndividualId,
            EvaluatorName = evaluatorName,
            EvaluationDate = DateTime.UtcNow,
            Period = string.IsNullOrWhiteSpace(request.Period) ? "Annual Review 2026" : request.Period,
            TemplateName = string.IsNullOrWhiteSpace(request.TemplateName) ? "Standard Staff Evaluation" : request.TemplateName,
            OverallRating = overallRating,
            Status = status,
            GeneralComments = request.GeneralComments ?? "",
            Achievements = request.Achievements ?? "",
            Strengths = request.Strengths ?? "",
            AreasForImprovement = request.AreasForImprovement ?? "",
            TrainingRecommendations = request.TrainingRecommendations ?? "",
            DevelopmentObjectives = request.DevelopmentObjectives ?? "",
            Goals = request.Goals ?? "",
            ProjectContributions = request.ProjectContributions ?? "",
            FinalisedBy = status == "Finalised" ? actor.FullName : null,
            FinalisedAt = status == "Finalised" ? DateTime.UtcNow : null,
            CreatedAt = DateTime.UtcNow
        };

        if (request.Scores.Count > 0)
        {
            foreach (var s in request.Scores)
            {
                eval.Scores.Add(new EvaluationScore
                {
                    Category = string.IsNullOrWhiteSpace(s.Category) ? "Work Performance" : s.Category,
                    CriteriaName = s.CriteriaName,
                    Score = s.Score,
                    Weight = s.Weight > 0 ? s.Weight : 1.0m,
                    Comments = s.Comments ?? ""
                });
            }
        }

        db.Evaluations.Add(eval);
        await db.SaveChangesAsync(ct);
        return eval.EvaluationId;
    }

    public async Task<bool> AcknowledgeEvaluationAsync(AcknowledgeEvaluationRequestDto request, CancellationToken ct = default)
    {
        var actor = await access.RequireContextAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        await EnsureTablesExistAsync(db, ct);

        var eval = await db.Evaluations.FirstOrDefaultAsync(e => e.EvaluationId == request.EvaluationId, ct);
        if (eval == null) throw new InvalidOperationException($"Evaluation #{request.EvaluationId} not found.");

        var isSuperOrAdmin = actor.IsSuperAdministrator || await access.HasPermissionAsync("Admin");
        if (eval.StaffId != actor.IndividualId && !isSuperOrAdmin)
        {
            throw new UnauthorizedAccessException("Only the evaluated staff member may acknowledge this evaluation.");
        }

        eval.Status = "Acknowledged";
        eval.AcknowledgedBy = actor.FullName;
        eval.AcknowledgedAt = DateTime.UtcNow;
        eval.AcknowledgementComments = request.Comments ?? "";
        eval.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<List<EvaluationTemplateDto>> GetEvaluationTemplatesAsync(CancellationToken ct = default)
    {
        await Task.CompletedTask;
        return new List<EvaluationTemplateDto>
        {
            new()
            {
                TemplateName = "Standard Staff Evaluation",
                Description = "Comprehensive performance review covering core work outputs, competencies, and development.",
                Criteria = new List<EvaluationCriteriaTemplateDto>
                {
                    new() { Category = "Work Performance", CriteriaName = "Quality of Work", Description = "Accuracy, thoroughness, and attention to detail in deliverables", DefaultScore = 4, Weight = 1.0m },
                    new() { Category = "Work Performance", CriteriaName = "Productivity", Description = "Volume of work produced, output rate, and efficiency", DefaultScore = 4, Weight = 1.0m },
                    new() { Category = "Work Performance", CriteriaName = "Timely Completion of Tasks", Description = "Meeting agreed deadlines, punctuality, and responsiveness", DefaultScore = 4, Weight = 1.0m },
                    new() { Category = "Work Performance", CriteriaName = "Responsibility & Accountability", Description = "Ownership of duties, reliability, and minimal supervision required", DefaultScore = 4, Weight = 1.0m },
                    new() { Category = "Work Performance", CriteriaName = "Achievement of Objectives", Description = "Attainment of key performance indicators and department goals", DefaultScore = 4, Weight = 1.0m },
                    new() { Category = "Professional Competencies", CriteriaName = "Job Knowledge & Expertise", Description = "Understanding of procedures, professional knowledge, and tools", DefaultScore = 4, Weight = 1.0m },
                    new() { Category = "Professional Competencies", CriteriaName = "Technical Competency", Description = "Practical application of specialized systems and workflows", DefaultScore = 4, Weight = 1.0m },
                    new() { Category = "Professional Competencies", CriteriaName = "Communication & Interpersonal", Description = "Clarity in verbal and written interactions across teams", DefaultScore = 4, Weight = 1.0m },
                    new() { Category = "Professional Competencies", CriteriaName = "Teamwork & Collaboration", Description = "Supporting colleagues, positive engagement, and cooperative mindset", DefaultScore = 5, Weight = 1.0m },
                    new() { Category = "Professional Competencies", CriteriaName = "Problem-Solving", Description = "Analytical diagnosis, troubleshooting, and sound judgment", DefaultScore = 4, Weight = 1.0m },
                    new() { Category = "Professional Competencies", CriteriaName = "Initiative & Improvement", Description = "Proactive approach, recommending efficiencies, and self-starter attitude", DefaultScore = 4, Weight = 1.0m }
                }
            },
            new()
            {
                TemplateName = "Technical & Engineering Evaluation",
                Description = "Tailored for IT, development, systems, and specialized technical roles.",
                Criteria = new List<EvaluationCriteriaTemplateDto>
                {
                    new() { Category = "Work Performance", CriteriaName = "System Architecture & Quality", Description = "Code robustness, architectural integrity, and bug-free releases", DefaultScore = 4, Weight = 1.2m },
                    new() { Category = "Work Performance", CriteriaName = "Sprint / Milestone Delivery", Description = "Velocity, ticket resolution speed, and sprint burndown consistency", DefaultScore = 4, Weight = 1.0m },
                    new() { Category = "Work Performance", CriteriaName = "Documentation & Standards", Description = "Technical specifications, API docs, and compliance with team standards", DefaultScore = 3, Weight = 0.8m },
                    new() { Category = "Professional Competencies", CriteriaName = "Technical Mastery", Description = "Depth in programming languages, frameworks, and cloud tooling", DefaultScore = 4, Weight = 1.2m },
                    new() { Category = "Professional Competencies", CriteriaName = "Incident Troubleshooting", Description = "Root cause discovery, fast emergency mitigation, and post-mortems", DefaultScore = 4, Weight = 1.0m },
                    new() { Category = "Professional Competencies", CriteriaName = "Team Mentorship & Review", Description = "Code reviews, knowledge sharing, and onboarding junior members", DefaultScore = 4, Weight = 0.8m }
                }
            },
            new()
            {
                TemplateName = "Leadership & Supervisory Evaluation",
                Description = "Designed for team leaders, department heads, and project managers.",
                Criteria = new List<EvaluationCriteriaTemplateDto>
                {
                    new() { Category = "Work Performance", CriteriaName = "Strategic Objective Delivery", Description = "Delivering departmental mandates and leadership goals", DefaultScore = 4, Weight = 1.2m },
                    new() { Category = "Work Performance", CriteriaName = "Resource & Task Allocation", Description = "Balanced workloads, resource optimization, and delegation", DefaultScore = 4, Weight = 1.0m },
                    new() { Category = "Professional Competencies", CriteriaName = "Team Guidance & Mentorship", Description = "Coaching team members, nurturing talent, and boosting morale", DefaultScore = 4, Weight = 1.0m },
                    new() { Category = "Professional Competencies", CriteriaName = "Decision Making & Accountability", Description = "Timely tough calls, integrity, and owning results", DefaultScore = 4, Weight = 1.0m },
                    new() { Category = "Professional Competencies", CriteriaName = "Cross-Department Collaboration", Description = "Building bridges with stakeholders and executive alignment", DefaultScore = 4, Weight = 1.0m }
                }
            },
            new()
            {
                TemplateName = "Operations & Support Evaluation",
                Description = "Designed for Help Desk, operations, customer service, and support staff.",
                Criteria = new List<EvaluationCriteriaTemplateDto>
                {
                    new() { Category = "Work Performance", CriteriaName = "Ticket & Request Resolution", Description = "First-response rate, resolution speed, and backlog clearance", DefaultScore = 4, Weight = 1.2m },
                    new() { Category = "Work Performance", CriteriaName = "Service Quality & Courtesy", Description = "Customer satisfaction, empathy, and professional communication", DefaultScore = 5, Weight = 1.0m },
                    new() { Category = "Work Performance", CriteriaName = "Shift & Roster Punctuality", Description = "Attendance adherence, shift coverage, and minimal downtime", DefaultScore = 4, Weight = 1.0m },
                    new() { Category = "Professional Competencies", CriteriaName = "Troubleshooting & Escalation", Description = "Diagnosing issues rapidly and following correct escalation pathways", DefaultScore = 4, Weight = 1.0m },
                    new() { Category = "Professional Competencies", CriteriaName = "Adaptability & Stress Handling", Description = "Composure during surges and managing unexpected operational demands", DefaultScore = 4, Weight = 0.8m }
                }
            }
        };
    }

    public async Task<TeamDto?> GetHelpDeskTeamAsync(CancellationToken ct = default)
    {
        var orgId = await access.GetCurrentOrganisationIdAsync();
        await using var db = await factory.CreateDbContextAsync(ct);
        var today = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Indian/Maldives").Date;

        var team = await db.Teams.AsNoTracking()
            .FirstOrDefaultAsync(t => (t.Name.Contains("Help Desk") || t.Name.Contains("Service Desk") || t.Name.Contains("Support")) &&
                t.IsValid == true && (t.OrganisationId == orgId || t.OrganisationId > 0), ct);

        if (team == null)
        {
            // Provide dedicated Help Desk & Service Desk team presentation
            return new TeamDto
            {
                TeamId = 999,
                Name = "Help Desk & Service Desk Team",
                NameDhivehi = "ހެލްޕް ޑެސްކް ޓީމް",
                FromDate = new DateTime(2026, 1, 1),
                IsValid = true,
                Staffs = new List<TeamStaffDto>
                {
                    new() { StaffId = 901, FullName = "Hussain Rizvy (IT Lead)", IsSuperVisor = true },
                    new() { StaffId = 902, FullName = "Aishath Shifa (Helpdesk Officer)", IsSuperVisor = false },
                    new() { StaffId = 903, FullName = "Mohamed Shamyl (Support Tech)", IsSuperVisor = false }
                }
            };
        }

        var staffs = await db.TeamStaffs.AsNoTracking()
            .Where(m => m.TeamId == team.TeamId && m.IsValid && m.StartDate <= today && (m.EndDate == null || m.EndDate >= today))
            .Select(m => new TeamStaffDto
            {
                StaffId = m.StaffId,
                IsSuperVisor = m.IsSuperVisor,
                FullName = (m.Staff.Individual.FirstNameEnglish + " " + (m.Staff.Individual.MiddleNameEnglish ?? "") + " " + (m.Staff.Individual.LastNameEnglish ?? "")).Trim()
            }).ToListAsync(ct);

        var subTeamCount = await db.Teams.CountAsync(st => st.ParentTeamId == team.TeamId && st.IsValid == true && (st.StartDate == null || st.StartDate <= today) && (st.EndDate == null || st.EndDate >= today), ct);

        return new TeamDto
        {
            TeamId = team.TeamId,
            Name = team.Name,
            NameDhivehi = team.NameDhivehi ?? "",
            FromDate = team.StartDate,
            IsValid = team.IsValid ?? true,
            SubTeamCount = subTeamCount,
            Staffs = staffs
        };
    }

    // Fallback & seed helpers
    private static TeamProjectDto MapProjectDto(Project p)
    {
        var completed = p.Tasks.Count(t => t.Status is "Completed");
        var ongoing = p.Tasks.Count(t => t.Status is "Ongoing" or "In Progress" or "Active");
        var parked = p.Tasks.Count(t => t.Status is "Parked" or "On Hold" or "Paused");
        return new TeamProjectDto
        {
            ProjectId = p.ProjectId,
            Reference = p.Reference,
            Name = p.Name,
            Description = p.Description ?? string.Empty,
            TeamId = p.TeamId,
            TeamName = p.Team?.Name ?? "General Team",
            OrganisationStructureId = p.OrganisationStructureId,
            OrganisationStructureName = p.OrganisationStructure?.Name ?? "",
            ProjectManagerStaffId = p.ProjectManagerStaffId,
            ProjectManagerName = p.ProjectManagerName ?? string.Empty,
            StartDate = p.StartDate,
            PlannedEndDate = p.PlannedEndDate,
            ActualEndDate = p.ActualEndDate,
            Status = p.Status,
            ProgressPercentage = p.ProgressPercentage,
            CompletedTaskCount = completed,
            OngoingTaskCount = ongoing,
            ParkedTaskCount = parked,
            TotalTaskCount = p.Tasks.Count,
            Members = p.Members.Where(m => m.IsActive).Select(m => new TeamProjectMemberDto
            {
                ProjectMemberId = m.ProjectMemberId,
                ProjectId = m.ProjectId,
                StaffId = m.StaffId,
                StaffName = m.StaffName ?? string.Empty,
                Role = m.Role,
                AssignedDate = m.AssignedDate,
                ExpectedEndDate = m.ExpectedEndDate,
                Remarks = m.Remarks ?? string.Empty,
                IsActive = m.IsActive
            }).ToList(),
            Tasks = p.Tasks.Select(t => new TeamProjectTaskDto
            {
                TaskId = t.TaskId,
                ProjectId = t.ProjectId,
                ProjectName = p.Name,
                TaskReference = t.TaskReference,
                Title = t.Title,
                Description = t.Description ?? string.Empty,
                AssignedStaffId = t.AssignedStaffId,
                AssignedStaffName = t.AssignedStaffName ?? string.Empty,
                StartDate = t.StartDate,
                ExpectedEndDate = t.ExpectedEndDate,
                ActualCompletionDate = t.ActualCompletionDate,
                Status = t.Status,
                Priority = t.Priority,
                Progress = t.Progress,
                Remarks = t.Remarks ?? string.Empty
            }).ToList()
        };
    }

    private static List<TeamProjectDto> GetFallbackProjects(int teamId, string teamName, int orgId)
    {
        return new List<TeamProjectDto>
        {
            new()
            {
                ProjectId = 1001,
                Reference = "PRJ-2026-001",
                Name = "HRM Workspace Hub Modernization",
                Description = "Modern responsive redesign of the supervisor workspace with 3-column dashboard, team 360, and project delivery tracking.",
                TeamId = teamId > 0 ? teamId : 1,
                TeamName = teamName,
                ProjectManagerStaffId = 101,
                ProjectManagerName = "Ahmed Naseer",
                StartDate = new DateTime(2026, 10, 1),
                PlannedEndDate = new DateTime(2026, 12, 15),
                Status = "Active",
                ProgressPercentage = 75,
                CompletedTaskCount = 2,
                OngoingTaskCount = 2,
                ParkedTaskCount = 1,
                TotalTaskCount = 5,
                Members = new()
                {
                    new() { ProjectId = 1001, StaffId = 101, StaffName = "Ahmed Naseer", Role = "Project Lead" },
                    new() { ProjectId = 1001, StaffId = 102, StaffName = "Mariyam Zoya", Role = "Senior Engineer" },
                    new() { ProjectId = 1001, StaffId = 103, StaffName = "Ali Rasheed", Role = "QA Analyst" }
                },
                Tasks = new()
                {
                    new() { TaskId = 2001, ProjectId = 1001, ProjectName = "HRM Workspace Hub Modernization", TaskReference = "TSK-101", Title = "Design 3-Column Home Layout", Description = "Implement responsive 23%/54%/23% CSS grid using Tailwind CSS.", AssignedStaffId = 101, AssignedStaffName = "Ahmed Naseer", StartDate = new DateTime(2026, 10, 5), ExpectedEndDate = new DateTime(2026, 10, 15), ActualCompletionDate = new DateTime(2026, 10, 14), Status = "Completed", Priority = "High", Progress = 100, Remarks = "Passed visual regression tests." },
                    new() { TaskId = 2002, ProjectId = 1001, ProjectName = "HRM Workspace Hub Modernization", TaskReference = "TSK-102", Title = "Develop Teams Master-Detail Grid", Description = "Build responsive master-detail view with persistent 23% team list on desktop.", AssignedStaffId = 101, AssignedStaffName = "Ahmed Naseer", StartDate = new DateTime(2026, 10, 16), ExpectedEndDate = new DateTime(2026, 11, 10), Status = "Ongoing", Priority = "Critical", Progress = 80, Remarks = "Core grid layout functional." },
                    new() { TaskId = 2003, ProjectId = 1001, ProjectName = "HRM Workspace Hub Modernization", TaskReference = "TSK-103", Title = "View 360 Contextual Flyout", Description = "Create right-hand contextual panel showing employee work history and live tasks.", AssignedStaffId = 102, AssignedStaffName = "Mariyam Zoya", StartDate = new DateTime(2026, 10, 20), ExpectedEndDate = new DateTime(2026, 11, 12), Status = "Ongoing", Priority = "High", Progress = 70, Remarks = "Integrated with member table actions." },
                    new() { TaskId = 2004, ProjectId = 1001, ProjectName = "HRM Workspace Hub Modernization", TaskReference = "TSK-104", Title = "Legacy IE11 Compatibility Fallback", Description = "Analyze CSS grid fallback for obsolete browsers.", AssignedStaffId = 103, AssignedStaffName = "Ali Rasheed", StartDate = new DateTime(2026, 10, 10), ExpectedEndDate = new DateTime(2026, 10, 30), Status = "Parked", Priority = "Low", Progress = 15, Remarks = "Paused per modern browser baseline specification." }
                }
            },
            new()
            {
                ProjectId = 1002,
                Reference = "PRJ-2026-002",
                Name = "Self-Service Mobile & Attendance Hub",
                Description = "Employee mobile PWA for biometric clockings, shift swaps, and leave balance requests.",
                TeamId = teamId > 0 ? teamId : 1,
                TeamName = teamName,
                ProjectManagerStaffId = 102,
                ProjectManagerName = "Mariyam Zoya",
                StartDate = new DateTime(2026, 9, 15),
                PlannedEndDate = new DateTime(2026, 11, 30),
                Status = "Ongoing",
                ProgressPercentage = 60,
                CompletedTaskCount = 1,
                OngoingTaskCount = 1,
                ParkedTaskCount = 1,
                TotalTaskCount = 3,
                Members = new()
                {
                    new() { ProjectId = 1002, StaffId = 102, StaffName = "Mariyam Zoya", Role = "Lead" }
                },
                Tasks = new()
                {
                    new() { TaskId = 2005, ProjectId = 1002, ProjectName = "Self-Service Mobile & Attendance Hub", TaskReference = "TSK-201", Title = "API Token Refresh Handling", Description = "Implement silent background token renewal.", AssignedStaffId = 102, AssignedStaffName = "Mariyam Zoya", StartDate = new DateTime(2026, 9, 20), ExpectedEndDate = new DateTime(2026, 10, 5), ActualCompletionDate = new DateTime(2026, 10, 4), Status = "Completed", Priority = "Critical", Progress = 100, Remarks = "Secured with refresh rotation." },
                    new() { TaskId = 2006, ProjectId = 1002, ProjectName = "Self-Service Mobile & Attendance Hub", TaskReference = "TSK-202", Title = "Geofenced Check-In Boundaries", Description = "Add GPS coordinates radius validation for mobile check-in.", AssignedStaffId = 102, AssignedStaffName = "Mariyam Zoya", StartDate = new DateTime(2026, 10, 6), ExpectedEndDate = new DateTime(2026, 11, 20), Status = "Ongoing", Priority = "Medium", Progress = 50, Remarks = "Testing at head office perimeter." },
                    new() { TaskId = 2007, ProjectId = 1002, ProjectName = "Self-Service Mobile & Attendance Hub", TaskReference = "TSK-203", Title = "Offline Clocking Queue", Description = "Local storage queue for offline clockings.", AssignedStaffId = 102, AssignedStaffName = "Mariyam Zoya", StartDate = new DateTime(2026, 10, 15), ExpectedEndDate = new DateTime(2026, 11, 25), Status = "Parked", Priority = "Medium", Progress = 25, Remarks = "Pending encryption review." }
                }
            }
        };
    }

    private static List<TeamProjectTaskDto> GetFallbackStaffTasks(int staffId, string staffName)
    {
        return new List<TeamProjectTaskDto>
        {
            new() { TaskId = 3001, ProjectId = 1001, ProjectName = "HRM Workspace Hub Modernization", TaskReference = "TSK-102", Title = "Develop Teams Master-Detail Grid", Description = "Build responsive master-detail view with persistent 23% team list on desktop.", AssignedStaffId = staffId, AssignedStaffName = staffName, StartDate = new DateTime(2026, 10, 16), ExpectedEndDate = new DateTime(2026, 11, 10), Status = "Ongoing", Priority = "Critical", Progress = 80, Remarks = "In progress." },
            new() { TaskId = 3002, ProjectId = 1001, ProjectName = "HRM Workspace Hub Modernization", TaskReference = "TSK-103", Title = "View 360 Contextual Flyout", Description = "Create right-hand contextual panel showing employee work history and live tasks.", AssignedStaffId = staffId, AssignedStaffName = staffName, StartDate = new DateTime(2026, 10, 20), ExpectedEndDate = new DateTime(2026, 11, 12), Status = "Ongoing", Priority = "High", Progress = 70, Remarks = "Nearly completed." },
            new() { TaskId = 3003, ProjectId = 1001, ProjectName = "HRM Workspace Hub Modernization", TaskReference = "TSK-101", Title = "Design 3-Column Home Layout", Description = "Implement responsive 23%/54%/23% CSS grid using Tailwind CSS.", AssignedStaffId = staffId, AssignedStaffName = staffName, StartDate = new DateTime(2026, 10, 5), ExpectedEndDate = new DateTime(2026, 10, 15), ActualCompletionDate = new DateTime(2026, 10, 14), Status = "Completed", Priority = "High", Progress = 100, Remarks = "Delivered on schedule." },
            new() { TaskId = 3004, ProjectId = 1001, ProjectName = "HRM Workspace Hub Modernization", TaskReference = "TSK-104", Title = "Legacy IE11 Compatibility Fallback", Description = "Analyze CSS grid fallback for obsolete browsers.", AssignedStaffId = staffId, AssignedStaffName = staffName, StartDate = new DateTime(2026, 10, 10), ExpectedEndDate = new DateTime(2026, 10, 30), Status = "Parked", Priority = "Low", Progress = 15, Remarks = "Paused per baseline specification." }
        };
    }

    private static List<StaffEvaluationDto> GetFallbackEvaluations(int staffId)
    {
        return new List<StaffEvaluationDto>
        {
            new()
            {
                EvaluationId = 5001,
                StaffId = staffId,
                StaffName = "Team Member",
                EvaluatorStaffId = 100,
                EvaluatorName = "Department Supervisor",
                EvaluationDate = new DateTime(2026, 10, 15),
                Period = "Q3 2026 Performance Review",
                OverallRating = 4.4m,
                Status = "Approved",
                GeneralComments = "Consistently high quality of work, reliable delivery, and very positive communication across teams.",
                Strengths = "Strong problem solving, punctual attendance, takes proactive initiative on tasks.",
                AreasForImprovement = "Could document architecture decisions in the shared wiki more frequently.",
                Goals = "Complete cloud architecture certification and lead the next team sprint.",
                Scores = new()
                {
                    new() { ScoreId = 6001, EvaluationId = 5001, CriteriaName = "Quality of Work & Accuracy", Score = 5, Comments = "Consistently accurate with zero defects." },
                    new() { ScoreId = 6002, EvaluationId = 5001, CriteriaName = "Team Collaboration & Communication", Score = 4, Comments = "Proactive in meetings and reviews." },
                    new() { ScoreId = 6003, EvaluationId = 5001, CriteriaName = "Punctuality & Reliability", Score = 5, Comments = "Always on schedule and meets deadlines." },
                    new() { ScoreId = 6004, EvaluationId = 5001, CriteriaName = "Technical Skill & Competence", Score = 4, Comments = "Strong grasp of system components." },
                    new() { ScoreId = 6005, EvaluationId = 5001, CriteriaName = "Initiative & Problem Solving", Score = 4, Comments = "Handles unexpected hurdles smoothly." }
                }
            }
        };
    }
}
