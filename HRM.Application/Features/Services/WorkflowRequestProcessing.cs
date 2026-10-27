using HRM.DTOs.ApprovalFlow;
using HRM.Models;
using HRM.Models.Workflow;
using HRM.Services.Interfaces.AppovalWF;
namespace HRM.Services;

public sealed record WorkflowRequestSubject(Request Request, int JobId, int IndividualId, int OrganisationId)
{
    public int RequestId => Request.RequestId;
}
