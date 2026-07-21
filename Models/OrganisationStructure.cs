using HRM.Models.Archives;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace HRM.Models;

public partial class OrganisationStructure
{
    public int OrganisationStructureId { get; set; }

   // public int OrganisationBusinessEntityId { get; set; }
    public int OrganisationBusinessEntityID { get; set; }

    
    public int OrganisationStructureTypeId { get; set; }

    public string Name { get; set; } = null!;

    public int OrganisationStructureStateId { get; set; }

    public int OperationLogId { get; set; }

    public virtual ICollection<AttendanceClientInstance> AttendanceClientInstances { get; set; } = new List<AttendanceClientInstance>();

    public virtual ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();

    public virtual ICollection<BreakTime> BreakTimes { get; set; } = new List<BreakTime>();

    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();

    public virtual ICollection<Job> Jobs { get; set; } = new List<Job>();

    public virtual OperationLog OperationLog { get; set; } = null!;

    [ForeignKey("OrganisationBusinessEntityID")]
    public virtual Organisation OrganisationBusinessEntity { get; set; } = null!;

    public virtual ICollection<OrganisationStructureCalendar> OrganisationStructureCalendars { get; set; } = new List<OrganisationStructureCalendar>();

    public virtual ICollection<OrganisationStructureDraft> OrganisationStructureDrafts { get; set; } = new List<OrganisationStructureDraft>();

    public virtual ICollection<OrganisationStructureHeadIncharge> OrganisationStructureHeadIncharges { get; set; } = new List<OrganisationStructureHeadIncharge>();

    public virtual ICollection<OrganisationStructureLocation> OrganisationStructureLocations { get; set; } = new List<OrganisationStructureLocation>();

    public virtual ICollection<OrganisationStructureRelation> OrganisationStructureRelationOrganisationStructures { get; set; } = new List<OrganisationStructureRelation>();

    public virtual ICollection<OrganisationStructureRelation> OrganisationStructureRelationParentOrganisationStructures { get; set; } = new List<OrganisationStructureRelation>();

    public virtual ICollection<OrganisationStructureSchedule> OrganisationStructureSchedules { get; set; } = new List<OrganisationStructureSchedule>();

    public virtual OrganisationStructureState OrganisationStructureState { get; set; } = null!;

    public virtual ICollection<OrganisationStructureSupervisor> OrganisationStructureSupervisors { get; set; } = new List<OrganisationStructureSupervisor>();

    public virtual OrganisationStructureType OrganisationStructureType { get; set; } = null!;

    public virtual ICollection<OrganisationalStructureSapexception> OrganisationalStructureSapexceptions { get; set; } = new List<OrganisationalStructureSapexception>();

    public virtual ICollection<OutOfOfficeRequest> OutOfOfficeRequests { get; set; } = new List<OutOfOfficeRequest>();

    public virtual ICollection<SpecialDayType> SpecialDayTypes { get; set; } = new List<SpecialDayType>();

    public virtual ICollection<WorkShift> WorkShifts { get; set; } = new List<WorkShift>();
}
