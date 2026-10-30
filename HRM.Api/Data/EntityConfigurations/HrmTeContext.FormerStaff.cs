using HRM.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
namespace HRM.Models;

public partial class HrmTeContext
{
    // A single write boundary protects legacy CRUD, workflow actions and background services alike.
    // Identity/ownership is resolved from both old and new foreign keys to prevent reassignment bypasses.
    private Guid? formerGuardTransaction;
    private readonly HashSet<int> formerGuardJobs=[];
    private readonly HashSet<(int,int?)> formerGuardPeople=[];
    private Request? staffAppointmentRequest;
    // Only the authorised onboarding service opens this boundary. Historical jobs
    // still pass JobRecord, and edits to personal/history records remain blocked.
    internal void AllowStaffAppointment(Request request) => staffAppointmentRequest = request;
    private async Task ProtectFormerStaffAsync(CancellationToken ct)
    {
        if(Model.FindEntityType(typeof(Job))==null||Model.FindEntityType(typeof(JobPosition))==null)return;
        ChangeTracker.DetectChanges();
        var changed=ChangeTracker.Entries().Where(e=>e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted).ToArray();
        var transaction=Database.CurrentTransaction?.TransactionId;
        if(transaction==null||transaction!=formerGuardTransaction){formerGuardJobs.Clear();formerGuardPeople.Clear();formerGuardTransaction=transaction;}
        var visited=new HashSet<string>();var checkedJobs=formerGuardJobs;var checkedPeople=formerGuardPeople;
        async Task Person(int id,int? org,bool appointmentPath)
        {
            if(appointmentPath && staffAppointmentRequest is {} appointment && id==appointment.ApplicantBusinessEntityId &&
                (!org.HasValue || org==appointment.OrganisationBusinessEntityID))return;
            if(id<=0||checkedPeople.Contains((id,org)))return;
            if(await FormerStaffPolicy.IsFormerAsync(this,id,org,ct).ConfigureAwait(false))throw new InvalidOperationException(FormerStaffPolicy.ReadOnlyMessage);
            checkedPeople.Add((id,org));
        }
        async Task JobRecord(int id)
        {
            if(id<=0||checkedJobs.Contains(id))return;
            if(await FormerStaffPolicy.EndedJobs(this).AnyAsync(j=>j.JobId==id,ct).ConfigureAwait(false))throw new InvalidOperationException(FormerStaffPolicy.ReadOnlyMessage);
            checkedJobs.Add(id);
        }
        static bool Runtime(IEntityType t)=>t.ClrType.Name is "OperationLog" or "RequestNotification" or "StaffNotification" or "UserPreference" or "UserPreferences" or "UserTodo" or "StaffChatConversation" or "StaffChatMessage" or "StaffChatMembership" or "StaffChatEvent" || t.ClrType.Name.StartsWith("Audit",StringComparison.Ordinal);
        static bool Ownership(IForeignKey fk)=>fk.Properties.All(p=>!p.Name.Contains("CreatedBy",StringComparison.OrdinalIgnoreCase)&&!p.Name.Contains("UpdatedBy",StringComparison.OrdinalIgnoreCase)&&!p.Name.Contains("PerformedBy",StringComparison.OrdinalIgnoreCase)&&!p.Name.Contains("ApprovedBy",StringComparison.OrdinalIgnoreCase)&&!p.Name.Contains("LastStateChangedBy",StringComparison.OrdinalIgnoreCase)&&!p.Name.Contains("Approver",StringComparison.OrdinalIgnoreCase)&&!p.Name.Contains("OperationLog",StringComparison.OrdinalIgnoreCase)&&!p.Name.Contains("Actor",StringComparison.OrdinalIgnoreCase)&&!p.Name.Contains("VerifiedBy",StringComparison.OrdinalIgnoreCase)&&!p.Name.Contains("LinkedBy",StringComparison.OrdinalIgnoreCase));
        async Task Visit(EntityEntry entry,int depth,bool appointmentPath=false)
        {
            if(depth>12||Runtime(entry.Metadata))return;
            if(entry.State!=EntityState.Unchanged)
                appointmentPath=staffAppointmentRequest!=null && (
                    ReferenceEquals(entry.Entity,staffAppointmentRequest) ||
                    entry.Metadata.ClrType.Name is "StaffOnboardingRequest" ||
                    entry.Metadata.ClrType.Name.StartsWith("WfRequest",StringComparison.Ordinal) ||
                    entry.State==EntityState.Added && entry.Metadata.ClrType.Name is "Job" or "JobPosition" or "JobPositionsRequest" or "JobRequest" or "RequestJob" or "FrameworkJobStepHistory" or "Staff" or "User");
            var key=entry.Metadata.FindPrimaryKey();
            var identity=entry.Metadata.Name+":"+string.Join(":",key?.Properties.Select(p=>entry.Property(p.Name).CurrentValue)??[]);
            if(!visited.Add(identity))return;
            foreach(var values in entry.State==EntityState.Added?new[]{entry.CurrentValues}:new[]{entry.OriginalValues,entry.CurrentValues})
            {
                int Value(string name)=>entry.Metadata.FindProperty(name)!=null&&values[name] is int id?id:0;
                int? org=Value("OrganisationId");if(org<=0)org=Value("OrganisationID");if(org<=0)org=Value("OrganisationBusinessEntityID");if(org<=0)org=Value("OrganisationBusinessEntityId");if(org<=0)org=null;
                if(entry.Entity is Job)await JobRecord(Value("JobId"));
                foreach(var prop in new[]{"JobId","OriginalJobId"})if(Value(prop)>0)await JobRecord(Value(prop));
                foreach(var prop in new[]{"IndividualId","IndividualID","StaffId","EmployeeId","ApplicantBusinessEntityId","BusinessEntityId","BusinessEntityID","BussinessEntityId","SupervisorStaffId"})if(Value(prop)>0)await Person(Value(prop),org,appointmentPath);
                if(entry.Entity is Location or Address || entry.Metadata.ClrType.Name=="AddressInstance")
                {
                    var location=Value("LocationId");
                    if(location>0)foreach(var person in await BusinessEntityLocations.AsNoTracking().Where(x=>x.LocationId==location).Select(x=>x.BusinessEntityId).ToListAsync(ct).ConfigureAwait(false))await Person(person,null,false);
                }
                if(entry.Entity is AddressBasis)
                {
                    var address=Value("AddressBaseId");
                    var locations=AddressInstances.Where(x=>x.AddressBaseId==address).Select(x=>x.LocationId);
                    foreach(var person in await BusinessEntityLocations.AsNoTracking().Where(x=>locations.Contains(x.LocationId)).Select(x=>x.BusinessEntityId).ToListAsync(ct).ConfigureAwait(false))await Person(person,null,false);
                }
                if(entry.Metadata.ClrType.Name is "Document" or "BulkUploadedDocument")
                {
                    var document=Value("DocumentId");
                    foreach(var person in await BusinessEntitiesDocuments.AsNoTracking().Where(x=>x.DocumentId==document).Select(x=>x.BusinessEntityId).ToListAsync(ct).ConfigureAwait(false))await Person(person,null,false);
                }
                foreach(var fk in entry.Metadata.GetForeignKeys().Where(Ownership))
                {
                    if(Runtime(fk.PrincipalEntityType)||fk.PrincipalEntityType.ClrType.Name is not ("Job" or "JobPosition" or "Staff" or "Individual" or "BusinessEntity" or "User" or "Request" or "WfRequestInstance" or "WfRequestStageInstance" or "Leaf" or "LeaveRequest" or "WorkPlan" or "WorkAssignment" or "AttendanceLog" or "Document" or "Location" or "JobLeaveType"))continue;
                    var keys=fk.Properties.Select(p=>values[p.Name]).ToArray();
                    if(keys.Any(v=>v==null||v is int i&&i<=0||v is long l&&l<=0))continue;
                    var parent=await FindAsync(fk.PrincipalEntityType.ClrType,keys,ct).ConfigureAwait(false);
                    if(parent!=null)await Visit(Entry(parent),depth+1,appointmentPath).ConfigureAwait(false);
                }
            }
        }
        foreach(var entry in changed)await Visit(entry,0).ConfigureAwait(false);
    }
}
