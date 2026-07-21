using HRM.Models.Archives;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace HRM.Models;

public partial class User
{
    public int UserId { get; set; }

    public Guid SsouserKey { get; set; }

    public Guid UserKey { get; set; }

    public string Username { get; set; } = null!;

    public string? FullName { get; set; }

    public string Email { get; set; } = null!;

    public string? Comment { get; set; }

    public bool? IsApproved { get; set; }

    public DateTime? CreationDate { get; set; }

    public bool? IsOnline { get; set; }

    public bool IsActive { get; set; }

    public string ApplicationName { get; set; } = null!;

    public string? Mobile { get; set; }

    public int? CreatedByUserId { get; set; }

    public int? LastUpdatedByUserId { get; set; }

    public DateTime? LastUpdatedDate { get; set; }

    //public int? BusinessEntityId { get; set; }

    public int? UserOrganisationId { get; set; }

    public int? RequestId { get; set; }

    public int? OperationLogId { get; set; }

    public int? CreatedContextId { get; set; }

    public int? LastUpdatedContextId { get; set; }

    public int? LastStateChangedContextId { get; set; }

    public DateTime? LastSynchronisedDate { get; set; }

    public int? LastSynchronisedByUserId { get; set; }

    public int? LastSynchronisedContextId { get; set; }

    public int SsouserStateId { get; set; }

    public int LocalUserStateId { get; set; }

    public int? LastStateChangedByUserId { get; set; }

    public DateTime? LastStateChangedDate { get; set; }

    public DateTime? SsouserLastUpdatedDate { get; set; }

    public int? UserPreferenceId { get; set; }

    public virtual ICollection<BulkUploadedDocument> BulkUploadedDocuments { get; set; } = new List<BulkUploadedDocument>();

    public virtual Individual? BusinessEntity { get; set; }

    public virtual ICollection<BusinessEntity> BusinessEntityLastStateChangedByUsers { get; set; } = new List<BusinessEntity>();

    public virtual ICollection<BusinessEntity> BusinessEntityVerifiedByNavigations { get; set; } = new List<BusinessEntity>();

    public virtual ICollection<DataCorrectionRequest> DataCorrectionRequestLastStateChangedByUsers { get; set; } = new List<DataCorrectionRequest>();

    public virtual ICollection<DataCorrectionRequest> DataCorrectionRequestLastUpdatedByUsers { get; set; } = new List<DataCorrectionRequest>();

    public virtual ICollection<Kpidocument> Kpidocuments { get; set; } = new List<Kpidocument>();

    public virtual ICollection<LeaveDocument> LeaveDocuments { get; set; } = new List<LeaveDocument>();

    public virtual ICollection<LeavesBulkUploadedDocument> LeavesBulkUploadedDocuments { get; set; } = new List<LeavesBulkUploadedDocument>();

    public virtual LocalUserState LocalUserState { get; set; } = null!;

    public virtual OperationLog? OperationLog { get; set; }

    public virtual ICollection<OperationLog> OperationLogs { get; set; } = new List<OperationLog>();

    public virtual Request? Request { get; set; }

    public virtual ICollection<Request> Requests { get; set; } = new List<Request>();

    public virtual SsouserState SsouserState { get; set; } = null!;

    public virtual ICollection<UserAssignedUserGroup> UserAssignedUserGroups { get; set; } = new List<UserAssignedUserGroup>();

    public virtual UserOrganisation? UserOrganisation { get; set; }

    public virtual UserPreference? UserPreference { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public virtual ICollection<UserServiceRole> UserServiceRoles { get; set; } = new List<UserServiceRole>();


    [Key]
    public int BusinessEntityID { get; set; }
    public virtual ICollection<Job> Jobs { get; set; }






}
