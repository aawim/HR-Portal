using HRM.Application.Contracts.Pages;
using HRM.DTOs;
using HRM.Models;
using Microsoft.EntityFrameworkCore;
namespace HRM.Services;

public sealed partial class StaffFinanceService
{
    public async Task<FinanceWorkspace> LoadAsync(int? staffId=null)
    {
        var actor=await ScopeAsync();await using var db=await factory.CreateDbContextAsync();
        if(staffId.HasValue&&staffId!=actor.Person&&!actor.Manage&&!actor.Post)throw new UnauthorizedAccessException("Staff finance information requires permission.");
        var source=db.StaffFinanceRequests.AsNoTracking().Include(x=>x.Request).ThenInclude(x=>x.RequestState)
            .Where(x=>x.OrganisationId==actor.Org&&(!staffId.HasValue||x.IndividualId==staffId)&&
                (x.IndividualId==actor.Person||actor.Manage||actor.Post||actor.Assess&&x.Kind=="Allowance"||db.WfRequestApprovals.Any(a=>a.ApproverUserId==actor.User&&a.RequestStageInstance.WorkflowRequestInstance.RequestId==x.RequestId)));
        var rows=await source.OrderByDescending(x=>x.RequestId).Take(200).ToListAsync();var result=new List<FinanceRequestRow>();
        foreach(var row in rows)
        {
            var name=await NameAsync(db,row.IndividualId);var instance=await WorkflowRequestProcessing.Instance(db,row.RequestId);
            result.Add(new(row.RequestId,row.Request.RequestStateId,row.Request.ReferenceNumber??$"#{row.RequestId}",name,row.Kind,
                (row.Request.RequestState.IsProcessingState&&!row.Request.RequestState.IsFinalState?"Processing · ":"")+row.Request.RequestState.StateName,Payload(row),
                row.CreatedByUserId!=actor.User&&await Processing.CanApprove(db,Subject(row),instance,actor.User,actor.Person),
                row.IndividualId==actor.Person&&!row.Request.RequestState.IsFinalState&&instance?.WorkflowStatus=="InProgress")
            {
                CanAssess=actor.Assess&&row.Kind=="Allowance"&&row.IndividualId!=actor.Person&&row.CreatedByUserId!=actor.User&&!row.Request.RequestState.IsFinalState&&instance?.WorkflowStatus=="InProgress",
                Assessments=await db.AllowanceAssessments.Where(x=>x.RequestId==row.RequestId).OrderByDescending(x=>x.Id).Select(x=>new AllowanceAssessmentRow(x.Id,x.Amount,x.CurrencyTypeId,x.AssessedByUserId,x.AssessedAtUtc)).ToListAsync()
            });
        }
        // Approval assignment permits reading the assigned request, not all of that employee's financial records.
        var person=staffId??actor.Person;var all=!staffId.HasValue&&(actor.Manage||actor.Post);
        var agreements=await db.DeductionAgreements.AsNoTracking().Where(x=>x.OrganisationId==actor.Org&&(all||x.IndividualId==person)).OrderByDescending(x=>x.Id).Take(500).ToListAsync();var agreementRows=new List<FinanceAgreementRow>();
        foreach(var a in agreements)
        {
            var instalments=await db.DeductionInstalments.AsNoTracking().Where(x=>x.AgreementId==a.Id).OrderBy(x=>x.Version).ThenBy(x=>x.DueDate).ToListAsync();var schedule=new List<FinanceInstalmentRow>();
            foreach(var i in instalments)schedule.Add(new(i.Id,i.DueDate,i.Amount,await AllocatedAsync(db,i.Id),i.Version,i.IsSuperseded));
            agreementRows.Add(new(a.Id,a.JobId,await NameAsync(db,a.IndividualId),a.Reference,a.Category,a.CurrencyTypeId,a.TotalAmount,a.TotalAmount-await PaidAsync(db,a.Id),schedule));
        }
        var allowances=await (from a in db.StaffAllowances.AsNoTracking() join t in db.AllowanceTypes on a.AllowanceTypeId equals t.Id join i in db.Individuals on a.IndividualId equals i.BusinessEntityId
            where a.OrganisationId==actor.Org&&(all||a.IndividualId==person) orderby a.FromDate descending select new FinanceAllowanceRow(a.Id,a.JobId,(i.FirstNameEnglish+" "+(i.MiddleNameEnglish??"")+" "+(i.LastNameEnglish??"")).Trim(),t.Name,a.CurrencyTypeId,a.Amount,a.FromDate,a.ToDate,a.FrequencyMonths,"Approved staff request")).Take(500).ToListAsync();
        var ids=rows.Select(x=>x.RequestId).ToArray();var documents=await db.StaffFinanceDocuments.Where(x=>ids.Contains(x.RequestId)).Select(x=>new FinanceDocumentInfo(x.Id,x.RequestId,x.Name,x.ContentType)).ToListAsync();
        var activeJobs=await db.Jobs.Where(x=>x.OrganisationID==actor.Org&&(all||x.IndividualID==person)&&x.JobStateId==4&&x.JoinedDate<=Today&&(!x.TerminatedDate.HasValue||x.TerminatedDate>Today)).Select(x=>x.JobId).Take(500).ToListAsync();
        if(activeJobs.Count>0)
        {
            var required=await PreviewPayrollAsync(db,actor,Today,activeJobs);
            foreach(var line in required.Lines.Where(x=>x.PositionRuleId.HasValue))
            {
                var rule=await db.PositionAllowanceRules.SingleAsync(x=>x.Id==line.PositionRuleId);
                allowances.Add(new(-rule.Id,line.JobId,line.Staff,line.Description,line.CurrencyTypeId,line.Amount,new DateTime(Today.Year,Today.Month,1),new DateTime(Today.Year,Today.Month,DateTime.DaysInMonth(Today.Year,Today.Month)),rule.FrequencyMonths,"Required position allowance · current month, prorated"));
            }
        }
        return new(result,agreementRows,allowances,documents,actor.Manage,actor.Post);
    }
    private static async Task<string> NameAsync(HrmTeContext db,int person)=> await db.Individuals.Where(i=>i.BusinessEntityId==person).Select(i=>(i.FirstNameEnglish+" "+(i.MiddleNameEnglish??"")+" "+(i.LastNameEnglish??"")).Trim()).SingleAsync();
    public async Task<ServiceResult> UploadDocumentAsync(FinanceDocumentUpload input)
    {
        var actor=await ScopeAsync();await using var db=await factory.CreateDbContextAsync();await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
        var row=await db.StaffFinanceRequests.Include(x=>x.Request).ThenInclude(x=>x.RequestState).SingleOrDefaultAsync(x=>x.RequestId==input.RequestId&&x.OrganisationId==actor.Org)??throw new UnauthorizedAccessException("Request unavailable.");
        if(row.Request.RequestState.IsFinalState||!(row.IndividualId==actor.Person||row.CreatedByUserId==actor.User||actor.Manage))throw new UnauthorizedAccessException("Documents can be attached only to your pending request or by authorised HR.");
        var content=input.Content;
        if(content is null||content.Length is <8 or >5242880||string.IsNullOrWhiteSpace(input.Name)||input.Name.Length>200)return ServiceResult.Fail("Choose a PDF, PNG or JPEG file up to 5 MB.");
        var valid=input.ContentType switch {"application/pdf"=>content.AsSpan(0,5).SequenceEqual("%PDF-"u8),"image/png"=>content.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}),"image/jpeg"=>content[0]==255&&content[1]==216&&content[2]==255,_=>false};
        if(!valid)return ServiceResult.Fail("The file content does not match a supported PDF or image format.");
        if(await db.StaffFinanceDocuments.CountAsync(x=>x.RequestId==input.RequestId)>=10)return ServiceResult.Fail("Up to ten documents can be attached to a request.");
        db.StaffFinanceDocuments.Add(new(){RequestId=input.RequestId,Name=Path.GetFileName(input.Name),ContentType=input.ContentType,Content=content,UploadedByUserId=actor.User});await db.SaveChangesAsync();await tx.CommitAsync();return ServiceResult.Ok("Document attached. Uploaded documents remain in the request history.");
    }
    public async Task<FinanceDocumentDownload> DownloadDocumentAsync(long id)
    {
        var actor=await ScopeAsync();await using var db=await factory.CreateDbContextAsync();
        var metadata=await db.StaffFinanceDocuments.Where(x=>x.Id==id).Select(x=>new{x.RequestId}).SingleOrDefaultAsync()??throw new UnauthorizedAccessException("Document unavailable.");
        var row=await db.StaffFinanceRequests.SingleAsync(x=>x.RequestId==metadata.RequestId);
        if(!await CanReadAsync(db,row,actor))throw new UnauthorizedAccessException("Document unavailable.");
        var file=await db.StaffFinanceDocuments.SingleAsync(x=>x.Id==id);return new(file.Name,file.ContentType,file.Content);
    }
}
