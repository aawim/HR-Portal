using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class Request
{
    public int RequestId { get; set; }

    public int RequestTypeId { get; set; }

    public int RequestStateId { get; set; }

    public int? ServiceId { get; set; }

    public DateTime ApplicationDate { get; set; }

    public string? ReferenceNumber { get; set; }

    public string? StateChangeRemarks { get; set; }

    public int? LastStateChangedByUserId { get; set; }

    public DateTime LastStateChangedDate { get; set; }

    public int OperationLogId { get; set; }

    public int? ApplicantBusinessEntityId { get; set; }

    public int? CurrentApplicationTab { get; set; }

    public int? OrganisationBusinessEntityID { get; set; }

    public DateTime? RequestEffectiveDate { get; set; }

    public virtual AttendanceLogChangeRequest? AttendanceLogChangeRequest { get; set; }

    public virtual ICollection<AttendanceLogRequest> AttendanceLogRequests { get; set; } = new List<AttendanceLogRequest>();

    public virtual ICollection<DataCorrectionRequest> DataCorrectionRequests { get; set; } = new List<DataCorrectionRequest>();

    public virtual ICollection<DeductionType> DeductionTypes { get; set; } = new List<DeductionType>();

    public virtual JobRequest? JobRequest { get; set; }

    public virtual User? LastStateChangedByUser { get; set; }

    public virtual ICollection<LeaveChangeRequest> LeaveChangeRequests { get; set; } = new List<LeaveChangeRequest>();

    public virtual ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual Organisation? OrganisationBusinessEntity { get; set; }

    public virtual ICollection<OrganisationStructureHistory> OrganisationStructureHistories { get; set; } = new List<OrganisationStructureHistory>();

    public virtual ICollection<OrganisationStructureRequest> OrganisationStructureRequests { get; set; } = new List<OrganisationStructureRequest>();

    public virtual OtpreApproval? OtpreApproval { get; set; }

    public virtual Otrequest? Otrequest { get; set; }

    public virtual OutOfOfficeRequest? OutOfOfficeRequest { get; set; }

    public virtual PayrollCycle? PayrollCycle { get; set; }

    public virtual ICollection<PayrollCycleLinkedRequest> PayrollCycleLinkedRequests { get; set; } = new List<PayrollCycleLinkedRequest>();

    public virtual ICollection<PayrollItemType> PayrollItemTypes { get; set; } = new List<PayrollItemType>();

    public virtual ICollection<RequestCheckListVerification> RequestCheckListVerifications { get; set; } = new List<RequestCheckListVerification>();

    public virtual ICollection<RequestDocument> RequestDocuments { get; set; } = new List<RequestDocument>();

    public virtual ICollection<RequestJob> RequestJobs { get; set; } = new List<RequestJob>();

    public virtual RequestState RequestState { get; set; } = null!;

    public virtual ICollection<RequestTeam> RequestTeams { get; set; } = new List<RequestTeam>();

    public virtual RequestType RequestType { get; set; } = null!;

    public virtual ICollection<SalaryRateDefinition> SalaryRateDefinitions { get; set; } = new List<SalaryRateDefinition>();

    public virtual Service? Service { get; set; }

    public virtual ICollection<StaffAssignedDeduction> StaffAssignedDeductions { get; set; } = new List<StaffAssignedDeduction>();

    public virtual ICollection<StaffPayrollItemType> StaffPayrollItemTypes { get; set; } = new List<StaffPayrollItemType>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();

    public virtual ICollection<BusinessEntityRelation> BusinessEntityRelations { get; set; } = new List<BusinessEntityRelation>();
}
