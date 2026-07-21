using HRM.Models.Archives;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace HRM.Models;


[Table("Teams")]
public class Team
{
    [Key]
    public int TeamId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    // Foreign Key to the Organisation
    public int OrganisationId { get; set; }
    
    public DateTime? StartDate { get; set; }

    public DateTime? EndDate { get; set; }

    public bool? IsValid { get; set; }

    public int? OperationLogId { get; set; }

    [MaxLength(200)]
    public string? NameDhivehi { get; set; } = string.Empty;

    // Indicates if this team has supervisory authority over others
    public bool? IsSuperVisor { get; set; }

    // Navigation Properties
    public virtual Organisation Organisation { get; set; } = null!;
    public virtual OperationLog OperationLog { get; set; } = null!;
 
    public virtual ICollection<TeamStaff> TeamStaffs { get; set; } = new List<TeamStaff>();
    public virtual ICollection<RequestTeam> RequestTeams { get; set; } = new List<RequestTeam>();


    // Optional: If you want to assign a default Shift to an entire team
    // public int? DefaultShiftId { get; set; }
    // public virtual Shift DefaultShift { get; set; }
}
//public partial class Team
//{
//    public int TeamId { get; set; }

//    public string Name { get; set; } = null!;

//    public int OrganisationId { get; set; }

//    public DateTime StartDate { get; set; }

//    public DateTime? EndDate { get; set; }

//    public bool IsValid { get; set; }

//    public int OperationLogId { get; set; }

//    public string? NameDhivehi { get; set; }

//    public virtual OperationLog OperationLog { get; set; } = null!;

//    public virtual Organisation Organisation { get; set; } = null!;

//    public virtual ICollection<RequestTeam> RequestTeams { get; set; } = new List<RequestTeam>();

//    public virtual ICollection<TeamStaff> TeamStaffs { get; set; } = new List<TeamStaff>();
//}
 