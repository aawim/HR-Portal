using System.Data; using System.Text.Json; using HRM.Application.Contracts.Recruitment; using HRM.Components.Shared; using HRM.DTOs; using HRM.Models; using HRM.Models.Recruitment; using HRM.Services.Interfaces; using Microsoft.EntityFrameworkCore;
namespace HRM.Services;
public sealed class RecruitmentRequisitionService(IDbContextFactory<HrmTeContext> factory,IUserAccessService access,IOperationLogService logs):IRecruitmentRequisitionService
{
 async Task<int> Org(){var o=await access.GetCurrentOrganisationIdAsync();if(o<=0||!await access.HasOrganisationAccessAsync(o)||!await access.HasPermissionAsync("Recruitment.Manage"))throw new UnauthorizedAccessException("Recruitment management permission is required.");return o;}
 public async Task<IReadOnlyList<RecruitmentRequisitionRow>> ListAsync(CancellationToken ct=default){var o=await access.GetCurrentOrganisationIdAsync();if(o<=0||!await access.HasOrganisationAccessAsync(o)||(!await access.HasPermissionAsync("Recruitment.View")&&!await access.HasPermissionAsync("Recruitment.Manage")&&!await access.HasPermissionAsync("Admin")))throw new UnauthorizedAccessException("Recruitment viewing permission is required.");await using var d=await factory.CreateDbContextAsync(ct);return await(from r in d.RecruitmentRequisitions join p in d.RecruitmentPolicyVersions on r.RecruitmentPolicyVersionId equals p.Id where r.OrganisationId==o orderby r.CreatedAtUtc descending select new RecruitmentRequisitionRow(r.Id,r.Reference,r.Status,r.PositionMatrixId,r.RequestedSlots,p.VersionCode,r.CreatedAtUtc)).ToListAsync(ct);}
 public async Task<IReadOnlyList<AvailablePositionMatrixOption>> ListAvailablePositionsAsync(CancellationToken ct=default){var o=await access.GetCurrentOrganisationIdAsync();if(o<=0||!await access.HasOrganisationAccessAsync(o)||(!await access.HasPermissionAsync("Recruitment.View")&&!await access.HasPermissionAsync("Recruitment.Manage")&&!await access.HasPermissionAsync("Admin")))throw new UnauthorizedAccessException("Recruitment viewing permission is required.");var day=DateTime.UtcNow.AddHours(5).Date;await using var d=await factory.CreateDbContextAsync(ct);var rows=await(from m in d.PositionMatrices.AsNoTracking() join jt in d.FrameworkJobTitles.AsNoTracking() on m.JobTitleId equals jt.JobTitleId into jts from jt in jts.DefaultIfEmpty() join jm in d.FrameworkJobMatrices.AsNoTracking() on m.JobMatrixId equals jm.JobMatrixId into jms from jm in jms.DefaultIfEmpty() join s in d.OrganisationStructures.AsNoTracking() on m.OrganisationStructureId equals s.OrganisationStructureId into ss from s in ss.DefaultIfEmpty() where m.OrganisationId==o&&m.IsActive&&m.EffectiveFrom<=day&&(!m.EffectiveTo.HasValue||m.EffectiveTo>=day) select new{m.PositionMatrixId,m.ApprovedSlots,Title=jt!=null?jt.Name:(jm!=null?jm.Name:$"Matrix #{m.PositionMatrixId}"),MatrixCode=jm!=null?jm.MatrixCode:"",Unit=s!=null?s.Name:null}).ToListAsync(ct);var ids=rows.Select(x=>x.PositionMatrixId).ToList();var reserved=await d.RecruitmentPositionReservations.AsNoTracking().Where(x=>x.OrganisationId==o&&x.Status=="Active"&&ids.Contains(x.PositionMatrixId)).GroupBy(x=>x.PositionMatrixId).Select(g=>new{Id=g.Key,Slots=g.Sum(x=>x.ReservedSlots)}).ToDictionaryAsync(x=>x.Id,x=>x.Slots,ct);return rows.Select(x=>{var used=reserved.GetValueOrDefault(x.PositionMatrixId);var remaining=Math.Max(0,x.ApprovedSlots-used);var unit=string.IsNullOrWhiteSpace(x.Unit)?"":$" · {x.Unit}";var code=string.IsNullOrWhiteSpace(x.MatrixCode)?"":$" [{x.MatrixCode}]";return new AvailablePositionMatrixOption(x.PositionMatrixId,$"{x.Title}{code}{unit} (#{x.PositionMatrixId} · {remaining}/{x.ApprovedSlots} slots)",x.ApprovedSlots,remaining);}).OrderByDescending(x=>x.RemainingSlots>0).ThenBy(x=>x.Label).ToList();}
 public async Task<ServiceResult> CreateAsync(RecruitmentRequisitionInput i,CancellationToken ct=default){if(i.RequestedSlots<1||string.IsNullOrWhiteSpace(i.EmploymentType)||string.IsNullOrWhiteSpace(i.RecruitmentType)||string.IsNullOrWhiteSpace(i.Reason)||i.Reason.Length>2000)return ServiceResult.Fail("Provide valid requisition details.");var o=await Org();var a=await access.RequireContextAsync();var now=DateTime.UtcNow;var day=now.AddHours(5).Date;await using var d=await factory.CreateDbContextAsync(ct);await using var t=await d.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);var m=await d.PositionMatrices.SingleOrDefaultAsync(x=>x.PositionMatrixId==i.PositionMatrixId&&x.OrganisationId==o&&x.IsActive&&x.EffectiveFrom<=day&&(!x.EffectiveTo.HasValue||x.EffectiveTo>=day),ct)??throw new InvalidOperationException("Select an active position matrix.");if(d.Database.IsSqlServer())await d.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r=sys.sp_getapplock @Resource={"HRM.PositionMatrix:"+m.PositionMatrixId},@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=5000;IF @r<0 THROW 51064,'Position matrix is busy.',1;",ct);var p=await(from x in d.RecruitmentPolicyAssignments join v in d.RecruitmentPolicyVersions on x.RecruitmentPolicyVersionId equals v.Id where x.OrganisationId==o&&x.IsActive&&x.RecruitmentType==i.RecruitmentType&&x.EffectiveFrom<=day&&(!x.EffectiveTo.HasValue||x.EffectiveTo>=day)&&v.IsApprovedForUse&&v.VerificationStatus=="Verified" select v).FirstOrDefaultAsync(ct);if(p is null&&string.Equals(i.RecruitmentType,"CivilService",StringComparison.OrdinalIgnoreCase))p=await EnsureDefaultCscPolicyAsync(d,o,a.UserId,day,ct);if(p is null)throw new InvalidOperationException("No verified approved policy applies.");var used=await d.RecruitmentPositionReservations.Where(x=>x.PositionMatrixId==m.PositionMatrixId&&x.Status=="Active").SumAsync(x=>(int?)x.ReservedSlots,ct)??0;if(used+i.RequestedSlots>m.ApprovedSlots)throw new InvalidOperationException("Requested slots exceed remaining reserved capacity.");var l=await logs.CreateAsync(d,SharedConfig.OperationLogActionTypes.REQUESTS_CREATE,$"Recruitment requisition for matrix {m.PositionMatrixId}.");var r=new RecruitmentRequisition{Reference=$"REQ-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..30],OrganisationId=o,OrganisationStructureId=i.OrganisationStructureId,PositionMatrixId=m.PositionMatrixId,RecruitmentPolicyVersionId=p.Id,RequestedSlots=i.RequestedSlots,EmploymentType=i.EmploymentType.Trim(),RecruitmentType=i.RecruitmentType.Trim(),Reason=i.Reason.Trim(),PositionSnapshotJson=JsonSerializer.Serialize(new{m.PositionMatrixId,m.ApprovedSlots}),RequestedByUserId=a.UserId,CreatedAtUtc=now,OperationLogId=l.OperationLogId};d.Add(r);await d.SaveChangesAsync(ct);d.Add(new RecruitmentPositionReservation{OrganisationId=o,PositionMatrixId=m.PositionMatrixId,RecruitmentRequisitionId=r.Id,ReservedSlots=i.RequestedSlots,ReservedAtUtc=now,OperationLogId=l.OperationLogId});await d.SaveChangesAsync(ct);await t.CommitAsync(ct);return ServiceResult.Ok("Recruitment requisition created and capacity reserved.");}
 public async Task<ServiceResult> ApproveAsync(int id,CancellationToken ct=default){var o=await Org();await using var d=await factory.CreateDbContextAsync(ct);var r=await d.RecruitmentRequisitions.SingleOrDefaultAsync(x=>x.Id==id&&x.OrganisationId==o,ct)??throw new UnauthorizedAccessException();if(r.Status!="Draft")return ServiceResult.Fail("Only draft requisitions may be approved.");var l=await logs.CreateAsync(d,SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE,$"Recruitment requisition {r.Reference} approved.");r.Status="Approved";r.ApprovedAtUtc=DateTime.UtcNow;r.OperationLogId=l.OperationLogId;await d.SaveChangesAsync(ct);return ServiceResult.Ok("Recruitment requisition approved.");}
 public async Task<ServiceResult> CreateVacancyAsync(RecruitmentVacancyInput i,CancellationToken ct=default){var o=await Org();if(i.ClosingAtUtc<=DateTime.UtcNow||string.IsNullOrWhiteSpace(i.AdvertisementSnapshotJson)||i.AdvertisementSnapshotJson.Length>16000)return ServiceResult.Fail("Provide a future closing time and approved advertisement snapshot.");await using var d=await factory.CreateDbContextAsync(ct);var r=await d.RecruitmentRequisitions.SingleOrDefaultAsync(x=>x.Id==i.RequisitionId&&x.OrganisationId==o,ct)??throw new UnauthorizedAccessException();if(r.Status!="Approved")return ServiceResult.Fail("Approve the requisition before creating a vacancy.");var l=await logs.CreateAsync(d,SharedConfig.OperationLogActionTypes.REQUESTS_CREATE,$"Vacancy created from recruitment requisition {r.Reference}.");d.RecruitmentVacancies.Add(new RecruitmentVacancy{Reference=$"VAC-{DateTime.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..30],OrganisationId=o,RecruitmentRequisitionId=r.Id,RecruitmentPolicyVersionId=r.RecruitmentPolicyVersionId,PositionMatrixId=r.PositionMatrixId,VacancySlots=r.RequestedSlots,AdvertisementSnapshotJson=i.AdvertisementSnapshotJson,ClosingAtUtc=i.ClosingAtUtc,OperationLogId=l.OperationLogId});await d.SaveChangesAsync(ct);return ServiceResult.Ok("Vacancy created with frozen requisition policy and position snapshot.");}
 public async Task<ServiceResult> PublishVacancyAsync(int id,CancellationToken ct=default){var o=await Org();await using var d=await factory.CreateDbContextAsync(ct);var v=await d.RecruitmentVacancies.SingleOrDefaultAsync(x=>x.Id==id&&x.OrganisationId==o,ct)??throw new UnauthorizedAccessException();if(v.Status!="Draft"||v.ClosingAtUtc<=DateTime.UtcNow)return ServiceResult.Fail("Only a draft vacancy with a future closing time may be published.");var l=await logs.CreateAsync(d,SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE,$"Recruitment vacancy {v.Reference} published.");v.Status="Published";v.PublishedAtUtc=DateTime.UtcNow;v.OperationLogId=l.OperationLogId;await d.SaveChangesAsync(ct);return ServiceResult.Ok("Vacancy published.");}

 private async Task<RecruitmentPolicyVersion> EnsureDefaultCscPolicyAsync(HrmTeContext d,int orgId,int userId,DateTime day,CancellationToken ct)
 {
     var profile=await d.RecruitmentPolicyProfiles.FirstOrDefaultAsync(x=>x.Code=="CSC-DEFAULT",ct);
     if(profile is null)
     {
         profile=new RecruitmentPolicyProfile{Code="CSC-DEFAULT",Name="Maldives Civil Service Recruitment Regulation",Jurisdiction="Maldives",IsCscProfile=true,IsActive=true,Version=1};
         d.RecruitmentPolicyProfiles.Add(profile);
         await d.SaveChangesAsync(ct);
     }
     var version=await d.RecruitmentPolicyVersions.FirstOrDefaultAsync(x=>x.RecruitmentPolicyProfileId==profile.Id&&x.VersionCode=="CSC-DEFAULT-V1",ct);
     if(version is null)
     {
         version=new RecruitmentPolicyVersion
         {
             RecruitmentPolicyProfileId=profile.Id,
             VersionCode="CSC-DEFAULT-V1",
             OfficialSourceUrl="https://www.csc.gov.mv/",
             OfficialSourceReference="Maldives Civil Service Regulation (Default Baseline)",
             EffectiveFrom=new DateTime(2024,1,1),
             VerificationStatus="Verified",
             VerifiedByUserId=userId>0?userId:null,
             VerifiedAtUtc=DateTime.UtcNow,
             IsApprovedForUse=true,
             Version=1
         };
         d.RecruitmentPolicyVersions.Add(version);
         await d.SaveChangesAsync(ct);
     }
     var hasAssignment=await d.RecruitmentPolicyAssignments.AnyAsync(x=>x.OrganisationId==orgId&&x.IsActive&&x.RecruitmentType=="CivilService"&&x.EffectiveFrom<=day&&(!x.EffectiveTo.HasValue||x.EffectiveTo>=day),ct);
     if(!hasAssignment)
     {
         var existsExact=await d.RecruitmentPolicyAssignments.FirstOrDefaultAsync(x=>x.OrganisationId==orgId&&x.RecruitmentType=="CivilService"&&x.PositionCategory=="All"&&x.EffectiveFrom==new DateTime(2024,1,1),ct);
         if(existsExact is not null)
         {
             existsExact.RecruitmentPolicyVersionId=version.Id;
             existsExact.IsActive=true;
             existsExact.EffectiveTo=null;
         }
         else
         {
             var log=await logs.CreateAsync(d,SharedConfig.OperationLogActionTypes.REQUESTS_CREATE,$"Default CSC recruitment policy assigned to organisation {orgId}.");
             d.RecruitmentPolicyAssignments.Add(new RecruitmentPolicyAssignment
             {
                 OrganisationId=orgId,
                 RecruitmentPolicyVersionId=version.Id,
                 RecruitmentType="CivilService",
                 PositionCategory="All",
                 EffectiveFrom=new DateTime(2024,1,1),
                 IsActive=true,
                 Version=1,
                 OperationLogId=log.OperationLogId
             });
         }
         await d.SaveChangesAsync(ct);
     }
     return version;
 }

 public async Task<RecruitmentPolicyWorkspaceDto> GetPoliciesAsync(CancellationToken ct=default)
 {
     var o=await Org();
     var a=await access.RequireContextAsync();
     var day=DateTime.UtcNow.AddHours(5).Date;
     await using var d=await factory.CreateDbContextAsync(ct);
     if(!await d.RecruitmentPolicyAssignments.AnyAsync(x=>x.OrganisationId==o&&x.IsActive&&x.RecruitmentType=="CivilService",ct))
         await EnsureDefaultCscPolicyAsync(d,o,a.UserId,day,ct);

     var versions=await(from v in d.RecruitmentPolicyVersions.AsNoTracking()
                        join p in d.RecruitmentPolicyProfiles.AsNoTracking() on v.RecruitmentPolicyProfileId equals p.Id
                        orderby v.IsApprovedForUse descending, v.EffectiveFrom descending, v.Id descending
                        select new RecruitmentPolicyVersionRow(v.Id,p.Id,p.Code,p.Name,v.VersionCode,v.OfficialSourceUrl,v.OfficialSourceReference,v.EffectiveFrom,v.EffectiveTo,v.VerificationStatus,v.IsApprovedForUse)).ToListAsync(ct);

     var assignments=await(from x in d.RecruitmentPolicyAssignments.AsNoTracking()
                           join v in d.RecruitmentPolicyVersions.AsNoTracking() on x.RecruitmentPolicyVersionId equals v.Id
                           join p in d.RecruitmentPolicyProfiles.AsNoTracking() on v.RecruitmentPolicyProfileId equals p.Id
                           where x.OrganisationId==o
                           orderby x.IsActive descending, x.EffectiveFrom descending, x.Id descending
                           select new RecruitmentPolicyAssignmentRow(x.Id,x.OrganisationId,x.RecruitmentPolicyVersionId,v.VersionCode,p.Name,x.RecruitmentType,x.PositionCategory,x.EffectiveFrom,x.EffectiveTo,x.IsActive)).ToListAsync(ct);

     return new RecruitmentPolicyWorkspaceDto(versions,assignments);
 }

 public async Task<ServiceResult> CreatePolicyAsync(CreateRecruitmentPolicyInput input,CancellationToken ct=default)
 {
     var o=await Org();
     var a=await access.RequireContextAsync();
     var code=(input.ProfileCode??"").Trim().ToUpperInvariant();
     var name=(input.ProfileName??"").Trim();
     var ver=(input.VersionCode??"").Trim();
     var url=(input.OfficialSourceUrl??"").Trim();
     var sourceRef=(input.OfficialSourceReference??"").Trim();
     if(string.IsNullOrWhiteSpace(code)||string.IsNullOrWhiteSpace(name)||string.IsNullOrWhiteSpace(ver)||string.IsNullOrWhiteSpace(sourceRef))
         return ServiceResult.Fail("Profile code, profile name, version code, and official source reference are required.");
     if(input.EffectiveTo.HasValue&&input.EffectiveTo.Value.Date<input.EffectiveFrom.Date)
         return ServiceResult.Fail("Effective end date cannot be earlier than effective start date.");

     await using var d=await factory.CreateDbContextAsync(ct);
     var profile=await d.RecruitmentPolicyProfiles.FirstOrDefaultAsync(x=>x.Code==code,ct);
     if(profile is null)
     {
         profile=new RecruitmentPolicyProfile{Code=code,Name=name,Jurisdiction="Maldives",IsCscProfile=input.IsCscProfile,IsActive=true,Version=1};
         d.RecruitmentPolicyProfiles.Add(profile);
         await d.SaveChangesAsync(ct);
     }
     if(await d.RecruitmentPolicyVersions.AnyAsync(x=>x.RecruitmentPolicyProfileId==profile.Id&&x.VersionCode==ver,ct))
         return ServiceResult.Fail($"Policy version '{ver}' already exists under profile '{code}'.");

     d.RecruitmentPolicyVersions.Add(new RecruitmentPolicyVersion
     {
         RecruitmentPolicyProfileId=profile.Id,
         VersionCode=ver,
         OfficialSourceUrl=string.IsNullOrWhiteSpace(url)?"https://www.csc.gov.mv/":url,
         OfficialSourceReference=sourceRef,
         EffectiveFrom=input.EffectiveFrom.Date,
         EffectiveTo=input.EffectiveTo?.Date,
         VerificationStatus=input.VerifyAndApproveNow?"Verified":"PendingVerification",
         VerifiedByUserId=input.VerifyAndApproveNow&&a.UserId>0?a.UserId:null,
         VerifiedAtUtc=input.VerifyAndApproveNow?DateTime.UtcNow:null,
         IsApprovedForUse=input.VerifyAndApproveNow,
         Version=1
     });
     await d.SaveChangesAsync(ct);
     return ServiceResult.Ok($"Policy version '{ver}' created{(input.VerifyAndApproveNow?" and verified for use":"")}.");
 }

 public async Task<ServiceResult> VerifyApprovePolicyAsync(int versionId,CancellationToken ct=default)
 {
     await Org();
     var a=await access.RequireContextAsync();
     await using var d=await factory.CreateDbContextAsync(ct);
     var v=await d.RecruitmentPolicyVersions.SingleOrDefaultAsync(x=>x.Id==versionId,ct);
     if(v is null)return ServiceResult.Fail("Policy version not found.");
     v.VerificationStatus="Verified";
     v.IsApprovedForUse=true;
     v.VerifiedByUserId=a.UserId>0?a.UserId:null;
     v.VerifiedAtUtc=DateTime.UtcNow;
     v.Version++;
     await d.SaveChangesAsync(ct);
     return ServiceResult.Ok($"Policy version '{v.VersionCode}' verified and approved for use.");
 }

 public async Task<ServiceResult> AssignPolicyAsync(AssignRecruitmentPolicyInput input,CancellationToken ct=default)
 {
     var o=await Org();
     var recType=string.IsNullOrWhiteSpace(input.RecruitmentType)?"CivilService":input.RecruitmentType.Trim();
     var posCat=string.IsNullOrWhiteSpace(input.PositionCategory)?"All":input.PositionCategory.Trim();
     if(input.PolicyVersionId<=0)return ServiceResult.Fail("Select a verified policy version.");
     if(input.EffectiveTo.HasValue&&input.EffectiveTo.Value.Date<input.EffectiveFrom.Date)
         return ServiceResult.Fail("Effective end date cannot be earlier than effective start date.");

     await using var d=await factory.CreateDbContextAsync(ct);
     var v=await d.RecruitmentPolicyVersions.SingleOrDefaultAsync(x=>x.Id==input.PolicyVersionId,ct);
     if(v is null||!v.IsApprovedForUse||v.VerificationStatus!="Verified")
         return ServiceResult.Fail("Only a verified and approved policy version can be assigned.");

     var existing=await d.RecruitmentPolicyAssignments.FirstOrDefaultAsync(x=>x.OrganisationId==o&&x.RecruitmentType==recType&&x.PositionCategory==posCat&&x.EffectiveFrom==input.EffectiveFrom.Date,ct);
     var log=await logs.CreateAsync(d,SharedConfig.OperationLogActionTypes.REQUESTS_UPDATE,$"Assigned recruitment policy {v.VersionCode} ({recType}/{posCat}) to org {o}.");
     if(existing is not null)
     {
         existing.RecruitmentPolicyVersionId=v.Id;
         existing.EffectiveTo=input.EffectiveTo?.Date;
         existing.IsActive=true;
         existing.Version++;
         existing.OperationLogId=log.OperationLogId;
     }
     else
     {
         d.RecruitmentPolicyAssignments.Add(new RecruitmentPolicyAssignment
         {
             OrganisationId=o,
             RecruitmentPolicyVersionId=v.Id,
             RecruitmentType=recType,
             PositionCategory=posCat,
             EffectiveFrom=input.EffectiveFrom.Date,
             EffectiveTo=input.EffectiveTo?.Date,
             IsActive=true,
             Version=1,
             OperationLogId=log.OperationLogId
         });
     }
     await d.SaveChangesAsync(ct);
     return ServiceResult.Ok($"Policy '{v.VersionCode}' assigned to this organisation for {recType}.");
 }

 public async Task<ServiceResult> TogglePolicyAssignmentAsync(int assignmentId,CancellationToken ct=default)
 {
     var o=await Org();
     await using var d=await factory.CreateDbContextAsync(ct);
     var x=await d.RecruitmentPolicyAssignments.SingleOrDefaultAsync(a=>a.Id==assignmentId&&a.OrganisationId==o,ct);
     if(x is null)return ServiceResult.Fail("Policy assignment not found.");
     x.IsActive=!x.IsActive;
     x.Version++;
     await d.SaveChangesAsync(ct);
     return ServiceResult.Ok(x.IsActive?"Policy assignment activated.":"Policy assignment deactivated.");
 }

 public async Task<VacancyDraftContextDto> GetVacancyDraftContextAsync(int requisitionId,CancellationToken ct=default)
 {
     var o=await access.GetCurrentOrganisationIdAsync();
     if(o<=0||!await access.HasOrganisationAccessAsync(o)||(!await access.HasPermissionAsync("Recruitment.View")&&!await access.HasPermissionAsync("Recruitment.Manage")&&!await access.HasPermissionAsync("Admin")))
         throw new UnauthorizedAccessException("Recruitment viewing permission is required.");

     await using var d=await factory.CreateDbContextAsync(ct);
     var req=await d.RecruitmentRequisitions.AsNoTracking().SingleOrDefaultAsync(x=>x.Id==requisitionId&&x.OrganisationId==o,ct)
         ??throw new InvalidOperationException("Recruitment requisition was not found.");
     var pm=await d.PositionMatrices.AsNoTracking().SingleOrDefaultAsync(x=>x.PositionMatrixId==req.PositionMatrixId,ct)
         ??throw new InvalidOperationException("Position matrix linked to this requisition was not found.");
     var jm=await d.FrameworkJobMatrices.AsNoTracking().SingleOrDefaultAsync(x=>x.JobMatrixId==pm.JobMatrixId,ct)
         ??throw new InvalidOperationException("Job matrix linked to this position was not found.");

     var unitName=pm.OrganisationStructureId.HasValue
         ?await d.OrganisationStructures.AsNoTracking().Where(x=>x.OrganisationStructureId==pm.OrganisationStructureId.Value).Select(x=>x.Name).FirstOrDefaultAsync(ct)
         :null;
     var step=pm.JobMatrixStepId.HasValue
         ?await d.FrameworkJobMatrixSteps.AsNoTracking().SingleOrDefaultAsync(x=>x.JobMatrixStepId==pm.JobMatrixStepId.Value,ct)
         :null;
     var basicSalary=step?.BasicSalaryOverride??jm.BasicSalary;
     var jobAllowance=step?.JobAllowance;

     var matrixTitles=await(from link in d.FrameworkJobMatrixTitles.AsNoTracking()
                            join title in d.FrameworkJobTitles.AsNoTracking() on link.JobTitleId equals title.JobTitleId
                            where link.JobMatrixId==jm.JobMatrixId&&title.IsActive
                            select new{title.JobTitleId,title.Name}).ToListAsync(ct);

     var titleMap=matrixTitles.ToDictionary(x=>x.JobTitleId,x=>x.Name);
     if(pm.JobTitleId>0&&!titleMap.ContainsKey(pm.JobTitleId))
     {
         var assignedTitle=await d.FrameworkJobTitles.AsNoTracking().Where(x=>x.JobTitleId==pm.JobTitleId).Select(x=>new{x.JobTitleId,x.Name}).FirstOrDefaultAsync(ct);
         if(assignedTitle is not null)titleMap[assignedTitle.JobTitleId]=assignedTitle.Name;
     }

     var availableTitles=titleMap
         .Select(kv=>new VacancyTitleOption(kv.Key,kv.Value,kv.Key==pm.JobTitleId))
         .OrderByDescending(x=>x.IsPositionDefault)
         .ThenBy(x=>x.Name)
         .ToList();

     var defaultOption=availableTitles.FirstOrDefault(x=>x.IsPositionDefault)??availableTitles.FirstOrDefault();
     var defaultTitle=defaultOption?.Name??jm.Name;

     var qualificationCriteria=new List<string>();
     var pathways=await d.MatrixQualificationPathways.AsNoTracking().Where(x=>x.JobMatrixId==jm.JobMatrixId&&x.IsActive).OrderBy(x=>x.Id).ToListAsync(ct);
     if(pathways.Count>0)
     {
         var pathwayIds=pathways.Select(x=>x.Id).ToArray();
         var conditions=await d.MatrixQualificationConditions.AsNoTracking().Where(x=>pathwayIds.Contains(x.PathwayId)&&x.IsActive).OrderBy(x=>x.Id).ToListAsync(ct);
         foreach(var p in pathways)
         {
             var parts=new List<string>();
             var pConds=conditions.Where(c=>c.PathwayId==p.Id).ToList();
             foreach(var c in pConds)
             {
                 var seg=c.Qualification.Trim();
                 if(c.MinimumLevel.HasValue)
                     seg+=c.MaximumLevel.HasValue&&c.MaximumLevel!=c.MinimumLevel?$" (MNQF Level {c.MinimumLevel}–{c.MaximumLevel})":$" (MNQF Level {c.MinimumLevel})";
                 if(!string.IsNullOrWhiteSpace(c.FieldOfStudy))
                     seg+=$" in {c.FieldOfStudy.Trim()}";
                 if(c.AccreditedRequired)
                     seg+=" [Accredited]";
                 parts.Add(seg);
             }
             if(p.RelevantExperienceMonths>0)parts.Add($"{FormatMonths(p.RelevantExperienceMonths)} relevant experience");
             if(p.PostQualificationMonths>0)parts.Add($"{FormatMonths(p.PostQualificationMonths)} post-qualification experience");
             if(p.ManagerialExperienceMonths>0)parts.Add($"{FormatMonths(p.ManagerialExperienceMonths)} managerial experience");
             if(!string.IsNullOrWhiteSpace(p.Conditions))parts.Add(p.Conditions.Trim());
             var summary=parts.Count>0?string.Join("; ",parts):p.Name;
             qualificationCriteria.Add(string.IsNullOrWhiteSpace(p.Name)?summary:$"{p.Name.Trim()}: {summary}");
         }
     }
     else
     {
         var textQuals=await d.FrameworkMatrixQualifications.AsNoTracking().Where(x=>x.JobMatrixId==jm.JobMatrixId).OrderBy(x=>x.PathwayNumber).Select(x=>x.RequirementText).ToListAsync(ct);
         qualificationCriteria.AddRange(textQuals.Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Trim()));
     }

     var credentials=await d.MatrixCredentialRequirements.AsNoTracking().Where(x=>x.JobMatrixId==jm.JobMatrixId&&x.IsActive).OrderBy(x=>x.Id).ToListAsync(ct);
     var credentialCriteria=credentials.Select(c=>
     {
         var text=$"[{c.Requirement}] {c.Name.Trim()} ({c.Kind} — {c.Issuer.Trim()})";
         if(!string.IsNullOrWhiteSpace(c.Condition))text+=$" · Condition: {c.Condition.Trim()}";
         if(!string.IsNullOrWhiteSpace(c.Equivalents))text+=$" · Equivalents: {c.Equivalents.Trim()}";
         if(c.TrainingHours.HasValue&&c.TrainingHours>0)text+=$" · {c.TrainingHours:0.##} hrs";
         return text;
     }).ToList();

     var duties=await d.FrameworkMatrixDuties.AsNoTracking().Where(x=>x.JobMatrixId==jm.JobMatrixId).OrderBy(x=>x.SequenceNumber).Select(x=>x.DutyDescription).ToListAsync(ct);
     duties=duties.Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Trim()).ToList();

     var formatted=BuildFormattedCriteria(jm,basicSalary,jobAllowance,qualificationCriteria,credentialCriteria,duties);

     return new VacancyDraftContextDto(
         req.Id,
         pm.PositionMatrixId,
         jm.JobMatrixId,
         jm.MatrixCode,
         jm.Name,
         unitName,
         jm.GradeCode,
         jm.PublicServiceRank,
         basicSalary,
         jobAllowance,
         defaultOption?.JobTitleId,
         defaultTitle,
         availableTitles,
         qualificationCriteria,
         credentialCriteria,
         duties,
         jm.FieldsOfStudy,
         jm.ExperienceAreas,
         jm.LicensesAndTraining,
         formatted);
 }

 private static string FormatMonths(int months)
 {
     if(months>=12&&months%12==0)return $"{months/12} year{(months/12==1?"":"s")}";
     if(months>12)return $"{months/12} yr {months%12} mo";
     return $"{months} month{(months==1?"":"s")}";
 }

 private static string BuildFormattedCriteria(FrameworkJobMatrix jm,decimal basicSalary,decimal? jobAllowance,List<string> qualifications,List<string> credentials,List<string> duties)
 {
     var sections=new List<string>();
     var meta=new List<string> { $"Job Matrix: {jm.MatrixCode} — {jm.Name}" };
     if(!string.IsNullOrWhiteSpace(jm.GradeCode))meta.Add($"Grade: {jm.GradeCode}");
     if(!string.IsNullOrWhiteSpace(jm.PublicServiceRank))meta.Add($"Rank: {jm.PublicServiceRank}");
     meta.Add($"Basic Salary: MVR {basicSalary:N2}");
     if(jobAllowance.HasValue&&jobAllowance.Value>0)meta.Add($"Job Allowance: MVR {jobAllowance.Value:N2}");
     sections.Add(string.Join(" | ",meta));

     if(qualifications.Count>0)
     {
         var qLines=qualifications.Select((q,idx)=>$"{idx+1}. {q}");
         sections.Add("Qualification & Experience Criteria:\n"+string.Join("\n",qLines));
     }
     if(credentials.Count>0)
     {
         var cLines=credentials.Select(c=>$"- {c}");
         sections.Add("Licences, Registrations & Training:\n"+string.Join("\n",cLines));
     }
     else if(!string.IsNullOrWhiteSpace(jm.LicensesAndTraining))
     {
         sections.Add("Licences & Training:\n"+jm.LicensesAndTraining.Trim());
     }
     if(!string.IsNullOrWhiteSpace(jm.FieldsOfStudy))
         sections.Add("Fields of Study:\n"+jm.FieldsOfStudy.Trim());
     if(!string.IsNullOrWhiteSpace(jm.ExperienceAreas))
         sections.Add("Experience Areas:\n"+jm.ExperienceAreas.Trim());
     if(duties.Count>0)
     {
         var dLines=duties.Select((duty,idx)=>$"{idx+1}. {duty}");
         sections.Add("Key Duties & Responsibilities:\n"+string.Join("\n",dLines));
     }
     var text=string.Join("\n\n",sections);
     return text.Length<=7900?text:text[..7900];
 }
}
