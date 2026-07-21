using System;
using System.Collections.Generic;

namespace HRM.Models.Archives;

public partial class Service
{
    public int ServiceId { get; set; }

    public int ServiceTypeId { get; set; }

    public int OperationLogId { get; set; }

    public string RegistrationNumber { get; set; } = null!;

    public int ServiceStateId { get; set; }

    public virtual ICollection<DataCorrectionRequest> DataCorrectionRequests { get; set; } = new List<DataCorrectionRequest>();

    public virtual ICollection<Job> Jobs { get; set; } = new List<Job>();

    public virtual OperationLog OperationLog { get; set; } = null!;

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();

    public virtual ICollection<ServiceCheckListVerification> ServiceCheckListVerifications { get; set; } = new List<ServiceCheckListVerification>();

    public virtual ICollection<ServiceDocument> ServiceDocuments { get; set; } = new List<ServiceDocument>();

    public virtual ServiceState ServiceState { get; set; } = null!;

    public virtual ServiceType ServiceType { get; set; } = null!;

    public virtual ICollection<UserServiceRole> UserServiceRoles { get; set; } = new List<UserServiceRole>();
}
