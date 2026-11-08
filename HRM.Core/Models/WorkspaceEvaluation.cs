using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

[Table("EV_Evaluations")]
public class Evaluation
{
    [Key]
    public int EvaluationId { get; set; }

    public int OrganisationId { get; set; }

    public int StaffId { get; set; }

    [MaxLength(200)]
    public string StaffName { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Designation { get; set; }

    [MaxLength(150)]
    public string? DepartmentName { get; set; }

    [MaxLength(150)]
    public string? TeamName { get; set; }

    public int EvaluatorStaffId { get; set; }

    [MaxLength(200)]
    public string EvaluatorName { get; set; } = string.Empty;

    public DateTime EvaluationDate { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string Period { get; set; } = string.Empty;

    [MaxLength(100)]
    public string TemplateName { get; set; } = "Standard Staff Evaluation";

    [Column(TypeName = "decimal(3,1)")]
    public decimal OverallRating { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Submitted"; // Draft, Submitted, Under Review, Finalised, Acknowledged, Returned

    [MaxLength(2000)]
    public string? GeneralComments { get; set; }

    [MaxLength(1000)]
    public string? Achievements { get; set; }

    [MaxLength(1000)]
    public string? Strengths { get; set; }

    [MaxLength(1000)]
    public string? AreasForImprovement { get; set; }

    [MaxLength(1000)]
    public string? TrainingRecommendations { get; set; }

    [MaxLength(1000)]
    public string? DevelopmentObjectives { get; set; }

    [MaxLength(1000)]
    public string? Goals { get; set; }

    [MaxLength(2000)]
    public string? ProjectContributions { get; set; }

    [MaxLength(200)]
    public string? FinalisedBy { get; set; }

    public DateTime? FinalisedAt { get; set; }

    [MaxLength(200)]
    public string? AcknowledgedBy { get; set; }

    public DateTime? AcknowledgedAt { get; set; }

    [MaxLength(1000)]
    public string? AcknowledgementComments { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<EvaluationScore> Scores { get; set; } = new List<EvaluationScore>();
}

[Table("EV_EvaluationScores")]
public class EvaluationScore
{
    [Key]
    public int ScoreId { get; set; }

    public int EvaluationId { get; set; }

    [MaxLength(100)]
    public string Category { get; set; } = "Work Performance"; // Work Performance, Professional Competencies

    [Required]
    [MaxLength(150)]
    public string CriteriaName { get; set; } = string.Empty;

    public int Score { get; set; } = 3;

    [Column(TypeName = "decimal(4,2)")]
    public decimal Weight { get; set; } = 1.0m;

    [MaxLength(500)]
    public string? Comments { get; set; }

    public virtual Evaluation Evaluation { get; set; } = null!;
}
