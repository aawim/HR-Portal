// Typed endpoints delegate to canonical application services.
using Microsoft.AspNetCore.Mvc;
using HRM.Api.Infrastructure;
namespace HRM.Api.Controllers.V1;

[Route("api/v1/team")]
public sealed class TeamController(global::HRM.Services.Interfaces.ITeam service) : HrmApiController
{
    [HttpGet("get-my-teams")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.TeamDto>>> GetMyTeams()
    {
        return Ok(await service.GetMyTeamsAsync());
    }

    [HttpPost("create")]
    public async Task<ActionResult<global::System.Int32>> Create([FromBody] TeamCreateRequest request)
    {
        return Ok(await service.CreateAsync(request.organisationId, request.name, request.nameDhivehi, request.startDate, HttpContext.RequestAborted));
    }

    public sealed class TeamCreateRequest
    {
        public global::System.Int32 organisationId { get; set; }
        public global::System.String name { get; set; } = default!;
        public global::System.String? nameDhivehi { get; set; }
        public global::System.DateTime startDate { get; set; }
    }

    [HttpGet("get-my-team-members")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.TeamDto>>> GetMyTeamMembers([FromQuery] global::System.Int32 TeamId)
    {
        return Ok(await service.GetMyTeamMembersAsync(TeamId));
    }

    [HttpPost("add-staff")]
    public async Task<ActionResult<global::System.Int32>> AddStaff([FromBody] TeamAddStaffRequest request)
    {
        return Ok(await service.AddStaffAsync(request.teamId, request.staffId, request.startDate, request.isSupervisor, HttpContext.RequestAborted));
    }

    public sealed class TeamAddStaffRequest
    {
        public global::System.Int32 teamId { get; set; }
        public global::System.Int32 staffId { get; set; }
        public global::System.DateTime startDate { get; set; }
        public global::System.Boolean isSupervisor { get; set; } = false;
    }

    [HttpPost("remove-staff")]
    public async Task<ActionResult<global::System.Boolean>> RemoveStaff([FromBody] TeamRemoveStaffRequest request)
    {
        return Ok(await service.RemoveStaffAsync(request.teamId, request.staffId, HttpContext.RequestAborted));
    }

    public sealed class TeamRemoveStaffRequest
    {
        public global::System.Int32 teamId { get; set; }
        public global::System.Int32 staffId { get; set; }
    }

    [HttpGet("search-teams")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.TeamSearchItemDto>>> SearchTeams([FromQuery] global::System.String? query)
    {
        return Ok(await service.SearchTeamsAsync(query, HttpContext.RequestAborted));
    }

    [HttpGet("get-team-by-id")]
    public async Task<ActionResult<global::HRM.DTOs.TeamDto?>> GetTeamById([FromQuery] global::System.Int32 teamId)
    {
        return Ok(await service.GetTeamByIdAsync(teamId, HttpContext.RequestAborted));
    }

    [HttpGet("get-team-projects")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.TeamProjectDto>>> GetTeamProjects([FromQuery] global::System.Int32 teamId)
    {
        return Ok(await service.GetTeamProjectsAsync(teamId, HttpContext.RequestAborted));
    }

    [HttpGet("get-project-details")]
    public async Task<ActionResult<global::HRM.DTOs.TeamProjectDto?>> GetProjectDetails([FromQuery] global::System.Int32 projectId)
    {
        return Ok(await service.GetProjectDetailsAsync(projectId, HttpContext.RequestAborted));
    }

    [HttpGet("get-task-details")]
    public async Task<ActionResult<global::HRM.DTOs.TeamProjectTaskDto?>> GetTaskDetails([FromQuery] global::System.Int32 taskId)
    {
        return Ok(await service.GetTaskDetailsAsync(taskId, HttpContext.RequestAborted));
    }

    [HttpGet("get-staff360")]
    public async Task<ActionResult<global::HRM.DTOs.Staff360SummaryDto?>> GetStaff360([FromQuery] global::System.Int32 staffId)
    {
        return Ok(await service.GetStaff360Async(staffId, HttpContext.RequestAborted));
    }

    [HttpGet("get-staff-evaluations")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.StaffEvaluationDto>>> GetStaffEvaluations([FromQuery] global::System.Int32 staffId)
    {
        return Ok(await service.GetStaffEvaluationsAsync(staffId, HttpContext.RequestAborted));
    }

    [HttpGet("get-staff-evaluation-details")]
    public async Task<ActionResult<global::HRM.DTOs.StaffEvaluationDto?>> GetStaffEvaluationDetails([FromQuery] global::System.Int32 evaluationId)
    {
        return Ok(await service.GetStaffEvaluationDetailsAsync(evaluationId, HttpContext.RequestAborted));
    }

    [HttpPost("save-staff-evaluation")]
    public async Task<ActionResult<global::System.Int32>> SaveStaffEvaluation([FromBody] TeamSaveStaffEvaluationRequest request)
    {
        return Ok(await service.SaveStaffEvaluationAsync(request.request, HttpContext.RequestAborted));
    }

    public sealed class TeamSaveStaffEvaluationRequest
    {
        public global::HRM.DTOs.SaveEvaluationRequestDto request { get; set; } = default!;
    }

    [HttpPost("acknowledge-evaluation")]
    public async Task<ActionResult<global::System.Boolean>> AcknowledgeEvaluation([FromBody] TeamAcknowledgeEvaluationRequest request)
    {
        return Ok(await service.AcknowledgeEvaluationAsync(request.request, HttpContext.RequestAborted));
    }

    public sealed class TeamAcknowledgeEvaluationRequest
    {
        public global::HRM.DTOs.AcknowledgeEvaluationRequestDto request { get; set; } = default!;
    }

    [HttpGet("get-evaluation-templates")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.EvaluationTemplateDto>>> GetEvaluationTemplates()
    {
        return Ok(await service.GetEvaluationTemplatesAsync(HttpContext.RequestAborted));
    }

    [HttpGet("get-help-desk-team")]
    public async Task<ActionResult<global::HRM.DTOs.TeamDto?>> GetHelpDeskTeam()
    {
        return Ok(await service.GetHelpDeskTeamAsync(HttpContext.RequestAborted));
    }

    [HttpGet("search-assignable-projects")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.ProjectLookupDto>>> SearchAssignableProjects([FromQuery] global::System.String? query)
    {
        return Ok(await service.SearchAssignableProjectsAsync(query, HttpContext.RequestAborted));
    }

    [HttpPost("assign-staff-to-project")]
    public async Task<ActionResult<global::System.Int32>> AssignStaffToProject([FromBody] TeamAssignStaffToProjectRequest request)
    {
        return Ok(await service.AssignStaffToProjectAsync(request.request, HttpContext.RequestAborted));
    }

    public sealed class TeamAssignStaffToProjectRequest
    {
        public global::HRM.DTOs.AssignProjectRequestDto request { get; set; } = default!;
    }

    [HttpPost("update-project-assignment")]
    public async Task<ActionResult<global::System.Boolean>> UpdateProjectAssignment([FromBody] TeamUpdateProjectAssignmentRequest request)
    {
        return Ok(await service.UpdateProjectAssignmentAsync(request.request, HttpContext.RequestAborted));
    }

    public sealed class TeamUpdateProjectAssignmentRequest
    {
        public global::HRM.DTOs.UpdateProjectAssignmentRequestDto request { get; set; } = default!;
    }

    [HttpPost("remove-project-assignment")]
    public async Task<ActionResult<global::System.Boolean>> RemoveProjectAssignment([FromBody] TeamRemoveProjectAssignmentRequest request)
    {
        return Ok(await service.RemoveProjectAssignmentAsync(request.projectMemberId, request.remarks, HttpContext.RequestAborted));
    }

    public sealed class TeamRemoveProjectAssignmentRequest
    {
        public global::System.Int32 projectMemberId { get; set; }
        public global::System.String? remarks { get; set; }
    }

    [HttpPost("assign-task")]
    public async Task<ActionResult<global::System.Boolean>> AssignTask([FromBody] TeamAssignTaskRequest request)
    {
        return Ok(await service.AssignTaskAsync(request.request, HttpContext.RequestAborted));
    }

    public sealed class TeamAssignTaskRequest
    {
        public global::HRM.DTOs.AssignTaskRequestDto request { get; set; } = default!;
    }

    [HttpPost("create-project")]
    public async Task<ActionResult<global::System.Int32>> CreateProject([FromBody] TeamCreateProjectRequest request)
    {
        return Ok(await service.CreateProjectAsync(request.input, HttpContext.RequestAborted));
    }

    public sealed class TeamCreateProjectRequest
    {
        public global::HRM.DTOs.CreateProjectInputDto input { get; set; } = default!;
    }

    [HttpPost("update-project")]
    public async Task<ActionResult<global::System.Boolean>> UpdateProject([FromBody] TeamUpdateProjectRequest request)
    {
        return Ok(await service.UpdateProjectAsync(request.input, HttpContext.RequestAborted));
    }

    public sealed class TeamUpdateProjectRequest
    {
        public global::HRM.DTOs.UpdateProjectInputDto input { get; set; } = default!;
    }

    [HttpPost("delete-project")]
    public async Task<ActionResult<global::System.Boolean>> DeleteProject([FromBody] TeamDeleteProjectRequest request)
    {
        return Ok(await service.DeleteProjectAsync(request.projectId, HttpContext.RequestAborted));
    }

    public sealed class TeamDeleteProjectRequest
    {
        public global::System.Int32 projectId { get; set; }
    }

    [HttpGet("get-all-projects")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.TeamProjectDto>>> GetAllProjects([FromQuery] global::System.Int32? organisationId = null, [FromQuery] global::System.Int32? organisationStructureId = null, [FromQuery] global::System.Int32? teamId = null)
    {
        return Ok(await service.GetAllProjectsAsync(organisationId, organisationStructureId, teamId, HttpContext.RequestAborted));
    }

    [HttpGet("get-department-projects")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.TeamProjectDto>>> GetDepartmentProjects([FromQuery] global::System.Int32 organisationStructureId)
    {
        return Ok(await service.GetDepartmentProjectsAsync(organisationStructureId, HttpContext.RequestAborted));
    }

    [HttpGet("get-organisation-structures")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.OrganisationStructureDropdownDto>>> GetOrganisationStructures([FromQuery] global::System.Int32? organisationId = null)
    {
        return Ok(await service.GetOrganisationStructuresAsync(organisationId, HttpContext.RequestAborted));
    }

    [HttpPost("create-task")]
    public async Task<ActionResult<global::System.Int32>> CreateTask([FromBody] TeamCreateTaskRequest request)
    {
        return Ok(await service.CreateTaskAsync(request.input, HttpContext.RequestAborted));
    }

    public sealed class TeamCreateTaskRequest
    {
        public global::HRM.DTOs.CreateTaskInputDto input { get; set; } = default!;
    }

    [HttpPost("update-task")]
    public async Task<ActionResult<global::System.Boolean>> UpdateTask([FromBody] TeamUpdateTaskRequest request)
    {
        return Ok(await service.UpdateTaskAsync(request.input, HttpContext.RequestAborted));
    }

    public sealed class TeamUpdateTaskRequest
    {
        public global::HRM.DTOs.UpdateTaskInputDto input { get; set; } = default!;
    }

    [HttpPost("delete-task")]
    public async Task<ActionResult<global::System.Boolean>> DeleteTask([FromBody] TeamDeleteTaskRequest request)
    {
        return Ok(await service.DeleteTaskAsync(request.taskId, HttpContext.RequestAborted));
    }

    public sealed class TeamDeleteTaskRequest
    {
        public global::System.Int32 taskId { get; set; }
    }

    [HttpGet("get-assignment-history")]
    public async Task<ActionResult<global::System.Collections.Generic.List<global::HRM.DTOs.AssignmentHistoryDto>>> GetAssignmentHistory([FromQuery] global::System.Int32 projectId, [FromQuery] global::System.Int32? staffId)
    {
        return Ok(await service.GetAssignmentHistoryAsync(projectId, staffId, HttpContext.RequestAborted));
    }

    [HttpGet("load-sub-teams")]
    public async Task<ActionResult<global::HRM.DTOs.SubTeamWorkspaceDto>> LoadSubTeams([FromQuery] global::System.Int32 parentTeamId)
    {
        return Ok(await service.LoadSubTeamsAsync(parentTeamId, HttpContext.RequestAborted));
    }

    [HttpPost("save-sub-team")]
    public async Task<ActionResult<global::System.Int32>> SaveSubTeam([FromBody] TeamSaveSubTeamRequest request)
    {
        return Ok(await service.SaveSubTeamAsync(request.model, HttpContext.RequestAborted));
    }

    public sealed class TeamSaveSubTeamRequest
    {
        public global::HRM.DTOs.SaveSubTeamDto model { get; set; } = default!;
    }

    [HttpPost("delete-sub-team")]
    public async Task<ActionResult<global::System.Boolean>> DeleteSubTeam([FromBody] TeamDeleteSubTeamRequest request)
    {
        return Ok(await service.DeleteSubTeamAsync(request.teamId, HttpContext.RequestAborted));
    }

    public sealed class TeamDeleteSubTeamRequest
    {
        public global::System.Int32 teamId { get; set; }
    }

    [HttpPost("save-sub-team-member")]
    public async Task<ActionResult<global::System.Int32>> SaveSubTeamMember([FromBody] TeamSaveSubTeamMemberRequest request)
    {
        return Ok(await service.SaveSubTeamMemberAsync(request.model, HttpContext.RequestAborted));
    }

    public sealed class TeamSaveSubTeamMemberRequest
    {
        public global::HRM.DTOs.SaveSubTeamMemberDto model { get; set; } = default!;
    }

    [HttpPost("remove-sub-team-member")]
    public async Task<ActionResult<global::System.Boolean>> RemoveSubTeamMember([FromBody] TeamRemoveSubTeamMemberRequest request)
    {
        return Ok(await service.RemoveSubTeamMemberAsync(request.teamId, request.staffId, HttpContext.RequestAborted));
    }

    public sealed class TeamRemoveSubTeamMemberRequest
    {
        public global::System.Int32 teamId { get; set; }
        public global::System.Int32 staffId { get; set; }
    }

    [HttpPost("save-team-schedule-adjustment")]
    public async Task<ActionResult<global::System.Int32>> SaveTeamScheduleAdjustment([FromBody] TeamSaveTeamScheduleAdjustmentRequest request)
    {
        return Ok(await service.SaveTeamScheduleAdjustmentAsync(request.model, HttpContext.RequestAborted));
    }

    public sealed class TeamSaveTeamScheduleAdjustmentRequest
    {
        public global::HRM.DTOs.TeamScheduleAdjustmentDto model { get; set; } = default!;
    }

}
