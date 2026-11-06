using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

[Table("PR_Projects")]
public class Project
{
    [Key]
    public int ProjectId { get; set; }

    public int OrganisationId { get; set; }

    public int? TeamId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Reference { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public int? ProjectManagerStaffId { get; set; }

    [MaxLength(200)]
    public string ProjectManagerName { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }

    public DateTime? PlannedEndDate { get; set; }

    public DateTime? ActualEndDate { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Active";

    public int ProgressPercentage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Team? Team { get; set; }
    public virtual ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
    public virtual ICollection<ProjectTask> Tasks { get; set; } = new List<ProjectTask>();
}

[Table("PR_ProjectMembers")]
public class ProjectMember
{
    [Key]
    public int ProjectMemberId { get; set; }

    public int ProjectId { get; set; }

    public int StaffId { get; set; }

    [MaxLength(200)]
    public string StaffName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string Role { get; set; } = "Member";

    public DateTime AssignedDate { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public virtual Project Project { get; set; } = null!;
}

[Table("PR_ProjectTasks")]
public class ProjectTask
{
    [Key]
    public int TaskId { get; set; }

    public int ProjectId { get; set; }

    [Required]
    [MaxLength(50)]
    public string TaskReference { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    public int? AssignedStaffId { get; set; }

    [MaxLength(200)]
    public string AssignedStaffName { get; set; } = string.Empty;

    public DateTime? StartDate { get; set; }

    public DateTime? ExpectedEndDate { get; set; }

    public DateTime? ActualCompletionDate { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Ongoing"; // Ongoing, Completed, Parked

    [MaxLength(20)]
    public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical

    public int Progress { get; set; }

    [MaxLength(1000)]
    public string Remarks { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual Project Project { get; set; } = null!;
}
