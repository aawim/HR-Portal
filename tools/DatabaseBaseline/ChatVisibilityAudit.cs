using HRM.DTOs.UserContext;
using HRM.Models;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Reflection;
internal static class ChatVisibilityAudit
{
    public static async Task RunAsync(HrmTeContext db)
    {
        var factory=new AuditFactory(db.Database.GetConnectionString()!);
        foreach(var user in await db.Users.AsNoTracking().Where(u=>new[]{425,530,535}.Contains(u.UserId))
            .Select(u=>new{u.UserId,u.BusinessEntityID,u.FullName,Org=u.UserOrganisation!.BusinessEntityID}).ToListAsync())
        {
            var access=DispatchProxy.Create<IUserAccessService,ChatSmokeAccess>();
            ((ChatSmokeAccess)(object)access).Handler=(method,args)=>method switch{
                "RequireContextAsync"=>Task.FromResult(new UserContextDto{UserId=user.UserId,IndividualId=user.BusinessEntityID,IsSuperAdministrator=true}),
                "GetCurrentOrganisationIdAsync"=>Task.FromResult(user.Org),"HasOrganisationAccessAsync"=>Task.FromResult((int)args[0]! ==user.Org),_=>throw new InvalidOperationException(method)};
            try
            {
                var visible=await new StaffChatService(factory,access).ListAsync();
                var expected=await db.StaffChatMemberships.AsNoTracking().Where(m=>m.UserId==user.UserId&&m.LeftAtUtc==null&&m.Conversation.OrganisationId==user.Org)
                    .Select(m=>m.ConversationId).OrderBy(id=>id).ToListAsync();
                if(!visible.Select(c=>c.Id).Order().SequenceEqual(expected))throw new InvalidOperationException("Visibility does not match active memberships.");
                Console.WriteLine($"PASS {user.FullName}: only member conversations [{string.Join(',',visible.Select(c=>c.Id))}], including with elevated context.");
            }
            catch(UnauthorizedAccessException){Console.WriteLine($"PASS {user.FullName}: ineligible account denied chat access.");}
        }
    }
    sealed class AuditFactory(string connection):IDbContextFactory<HrmTeContext>
    {public HrmTeContext CreateDbContext()=>new(new DbContextOptionsBuilder<HrmTeContext>().UseSqlServer(connection).Options);}
}
