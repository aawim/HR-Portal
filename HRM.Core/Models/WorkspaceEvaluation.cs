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

    public int EvaluatorStaffId { get; set; }

    [MaxLength(200)]
    public string EvaluatorName { get; set; } = string.Empty;

    public DateTime EvaluationDate { get; set; } = DateTime.UtcNow;

    [MaxLength(50)]
    public string Period { get; set; } = string.Empty;

    [Column(TypeName = "decimal(3,1)")]
    public decimal OverallRating { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "Submitted"; // Draft, Submitted, Approved

    [MaxLength(2000)]
    public string GeneralComments { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Strengths { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string AreasForImprovement { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Goals { get; set; } = string.Empty;

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

    [Required]
    [MaxLength(100)]
    public string CriteriaName { get; set; } = string.Empty;

    public int Score { get; set; } = 3;

    [MaxLength(500)]
    public string Comments { get; set; } = string.Empty;

    public virtual Evaluation Evaluation { get; set; } = null!;
}
