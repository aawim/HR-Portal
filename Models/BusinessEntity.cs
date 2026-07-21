using HRM.Models.Archives;
using System;
using System.Collections.Generic;

namespace HRM.Models;

public partial class BusinessEntity
{
    //public int BusinessEntityId { get; set; }
    
    public int BusinessEntityID { get; set; }
    

    public int BusinessEntityTypeId { get; set; }

    public int? VerifiedStateId { get; set; }

    public int? BusinessEntityStateId { get; set; }

    public int? VerifiedBy { get; set; }

    public DateTime? VerifiedDate { get; set; }

    public int? LastStateChangedByUserId { get; set; }

    public DateTime? LastStateChangeDate { get; set; }

    public int? OperationLogId { get; set; }

    public virtual ICollection<BusinessEntitiesDocument> BusinessEntitiesDocuments { get; set; } = new List<BusinessEntitiesDocument>();

    public virtual ICollection<BusinessEntityCalendar> BusinessEntityCalendars { get; set; } = new List<BusinessEntityCalendar>();

    public virtual ICollection<BusinessEntityLocation> BusinessEntityLocations { get; set; } = new List<BusinessEntityLocation>();

    public virtual ICollection<BusinessEntityRelation> BusinessEntityRelationDelegateeBusinessEntities { get; set; } = new List<BusinessEntityRelation>();

    public virtual ICollection<BusinessEntityRelation> BusinessEntityRelationDelegatorBusinessEntities { get; set; } = new List<BusinessEntityRelation>();

    public virtual ICollection<BusinessEntityRelationType> BusinessEntityRelationTypes { get; set; } = new List<BusinessEntityRelationType>();

    public virtual ICollection<BusinessEntitySchedule> BusinessEntitySchedules { get; set; } = new List<BusinessEntitySchedule>();

    public virtual BusinessEntityState? BusinessEntityState { get; set; }

    public virtual BusinessEntityType BusinessEntityType { get; set; } = null!;

    public virtual ICollection<BussinessEntityContactInformation> BussinessEntityContactInformations { get; set; } = new List<BussinessEntityContactInformation>();

    public virtual ICollection<DataCorrectionRequest> DataCorrectionRequests { get; set; } = new List<DataCorrectionRequest>();

    public virtual Individual? Individual { get; set; }

    public virtual User? LastStateChangedByUser { get; set; }

    public virtual ICollection<OnlineBusinessEntityLocation> OnlineBusinessEntityLocations { get; set; } = new List<OnlineBusinessEntityLocation>();

    public virtual OperationLog? OperationLog { get; set; }

    public virtual Organisation Organisation { get; set; } = null!;

    public virtual User? VerifiedByNavigation { get; set; }

    public virtual VerifiedState? VerifiedState { get; set; }
}
