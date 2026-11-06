using System;
using System.Collections.Generic;

namespace HRM.DTOs
{
    public class TeamSearchItemDto
    {
        public int TeamId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? NameDhivehi { get; set; }
        public int OrganisationId { get; set; }
        public string OrganisationName { get; set; } = string.Empty;
        public int MemberCount { get; set; }
        public int SupervisorCount { get; set; }
        public string? LeadSupervisorName { get; set; }
        public bool IsMyTeam { get; set; }
    }

    public class TeamProjectDto
    {
        public int ProjectId { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? TeamId { get; set; }
        public string TeamName { get; set; } = string.Empty;
        public int? ProjectManagerStaffId { get; set; }
        public string ProjectManagerName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? PlannedEndDate { get; set; }
        public DateTime? ActualEndDate { get; set; }
        public string Status { get; set; } = "Active";
        public int ProgressPercentage { get; set; }
        public int CompletedTaskCount { get; set; }
        public int OngoingTaskCount { get; set; }
        public int ParkedTaskCount { get; set; }
        public int TotalTaskCount { get; set; }
        public bool CanManageAssignments { get; set; }
        public List<TeamProjectMemberDto> Members { get; set; } = new();
        public List<TeamProjectTaskDto> Tasks { get; set; } = new();
    }

    public class TeamProjectMemberDto
    {
        public int ProjectMemberId { get; set; }
        public int ProjectId { get; set; }
        public int StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime AssignedDate { get; set; }
        public DateTime? ExpectedEndDate { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }

    public class TeamProjectTaskDto
    {
        public int TaskId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public string TaskReference { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int? AssignedStaffId { get; set; }
        public string AssignedStaffName { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? ExpectedEndDate { get; set; }
        public DateTime? ActualCompletionDate { get; set; }
        public string Status { get; set; } = "Ongoing"; // Ongoing, Completed, Parked
        public string Priority { get; set; } = "Medium";
        public int Progress { get; set; }
        public string Remarks { get; set; } = string.Empty;
    }

    public class Staff360SummaryDto
    {
        public int StaffId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Department { get; set; }
        public string? Designation { get; set; }
        public string? Email { get; set; }
        public string? AvatarColor { get; set; }
        public List<string> AssignedTeams { get; set; } = new();
        public List<TeamProjectDto> AssignedProjects { get; set; } = new();
        public List<StaffProjectAssignmentDto> ProjectAssignments { get; set; } = new();
        public bool CanManageAssignments { get; set; }
        public List<TeamProjectTaskDto> OngoingTasks { get; set; } = new();
        public List<TeamProjectTaskDto> CompletedTasks { get; set; } = new();
        public List<TeamProjectTaskDto> ParkedTasks { get; set; } = new();
        public int TotalTasks => OngoingTasks.Count + CompletedTasks.Count + ParkedTasks.Count;
        public int CompletionRate => TotalTasks == 0 ? 0 : (int)Math.Round((double)CompletedTasks.Count / TotalTasks * 100);
    }

    public class StaffEvaluationDto
    {
        public int EvaluationId { get; set; }
        public int StaffId { get; set; }
        public string StaffName { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public int EvaluatorStaffId { get; set; }
        public string EvaluatorName { get; set; } = string.Empty;
        public DateTime EvaluationDate { get; set; }
        public string Period { get; set; } = string.Empty;
        public string TemplateName { get; set; } = "Standard Staff Evaluation";
        public decimal OverallRating { get; set; }
        public string Status { get; set; } = "Submitted"; // Draft, Submitted, Under Review, Finalised, Acknowledged, Returned
        public string GeneralComments { get; set; } = string.Empty;
        public string Achievements { get; set; } = string.Empty;
        public string Strengths { get; set; } = string.Empty;
        public string AreasForImprovement { get; set; } = string.Empty;
        public string TrainingRecommendations { get; set; } = string.Empty;
        public string DevelopmentObjectives { get; set; } = string.Empty;
        public string Goals { get; set; } = string.Empty;
        public string ProjectContributions { get; set; } = string.Empty;
        public string? FinalisedBy { get; set; }
        public DateTime? FinalisedAt { get; set; }
        public string? AcknowledgedBy { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public string? AcknowledgementComments { get; set; }
        public List<StaffEvaluationScoreDto> Scores { get; set; } = new();
    }

    public class StaffEvaluationScoreDto
    {
        public int ScoreId { get; set; }
        public int EvaluationId { get; set; }
        public string Category { get; set; } = "Work Performance";
        public string CriteriaName { get; set; } = string.Empty;
        public int Score { get; set; } = 3;
        public decimal Weight { get; set; } = 1.0m;
        public string Comments { get; set; } = string.Empty;
    }

    public class SaveEvaluationRequestDto
    {
        public int EvaluationId { get; set; }
        public int StaffId { get; set; }
        public string Period { get; set; } = string.Empty;
        public string TemplateName { get; set; } = "Standard Staff Evaluation";
        public decimal OverallRating { get; set; }
        public string Status { get; set; } = "Submitted"; // Draft, Submitted, Finalised, Returned
        public string GeneralComments { get; set; } = string.Empty;
        public string Achievements { get; set; } = string.Empty;
        public string Strengths { get; set; } = string.Empty;
        public string AreasForImprovement { get; set; } = string.Empty;
        public string TrainingRecommendations { get; set; } = string.Empty;
        public string DevelopmentObjectives { get; set; } = string.Empty;
        public string Goals { get; set; } = string.Empty;
        public string ProjectContributions { get; set; } = string.Empty;
        public List<StaffEvaluationScoreDto> Scores { get; set; } = new();
    }

    public class AcknowledgeEvaluationRequestDto
    {
        public int EvaluationId { get; set; }
        public string? Comments { get; set; }
    }

    public class EvaluationTemplateDto
    {
        public string TemplateName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<EvaluationCriteriaTemplateDto> Criteria { get; set; } = new();
    }

    public class EvaluationCriteriaTemplateDto
    {
        public string Category { get; set; } = "Work Performance";
        public string CriteriaName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DefaultScore { get; set; } = 3;
        public decimal Weight { get; set; } = 1.0m;
    }

    public class ProjectLookupDto
    {
        public int ProjectId { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string TeamName { get; set; } = string.Empty;
        public string ProjectManagerName { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime? PlannedEndDate { get; set; }
    }

    public class StaffProjectAssignmentDto
    {
        public int ProjectMemberId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectReference { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public string ProjectStatus { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime AssignedDate { get; set; }
        public DateTime? ExpectedEndDate { get; set; }
        public DateTime? RemovedAt { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool CanManage { get; set; }
    }

    public class AssignProjectRequestDto
    {
        public int ProjectId { get; set; }
        public int StaffId { get; set; }
        public string Role { get; set; } = "Member";
        public DateTime StartDate { get; set; }
        public DateTime? ExpectedEndDate { get; set; }
        public string Remarks { get; set; } = string.Empty;
    }

    public class UpdateProjectAssignmentRequestDto
    {
        public int ProjectMemberId { get; set; }
        public string Role { get; set; } = "Member";
        public DateTime? ExpectedEndDate { get; set; }
        public string Remarks { get; set; } = string.Empty;
    }

    public class AssignTaskRequestDto
    {
        public int TaskId { get; set; }
        public int StaffId { get; set; }
        public string Priority { get; set; } = "Medium";
        public DateTime? StartDate { get; set; }
        public DateTime? DueDate { get; set; }
        public string Remarks { get; set; } = string.Empty;
    }

    public class AssignmentHistoryDto
    {
        public int HistoryId { get; set; }
        public int ProjectId { get; set; }
        public int? TaskId { get; set; }
        public string TaskTitle { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string StaffName { get; set; } = string.Empty;
        public string PreviousStaffName { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string PerformedByName { get; set; } = string.Empty;
        public DateTime PerformedAt { get; set; }
    }
}