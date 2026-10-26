using HRM.Models;
using HRM.Models.Archives;
using HRM.Services;
using HRM.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

static class UserFontSizeTests
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static async Task RunAsync()
    {
        var factory=new ReceiptFactory(new DbContextOptionsBuilder<HrmTeContext>().UseInMemoryDatabase(Guid.NewGuid().ToString(),x=>x.EnableNullChecks(false))
            .ConfigureWarnings(w=>w.Ignore(InMemoryEventId.TransactionIgnoredWarning)).Options);
        var auth=new ContextAuthentication();auth.SignIn("one");
        var logs=Proxy.For<IOperationLogService>((m,a)=>m=="CreateAsync"?Audit((HrmTeContext)a[0]!):throw new Exception(m));
        var service=new UserPreferenceService(factory,auth,logs);
        await using(var db=factory.CreateDbContext())
        {
            db.UserPreferences.Add(new(){UserPreferenceId=1,ThemeMode="light",FontSizePercent=100,SelectedLanguageId=2,RecentWorkspaceSearchesJson="[]"});
            db.Users.AddRange(new User{UserId=1,Username="one",IsActive=true,LocalUserStateId=1,UserPreferenceId=1},
                new User{UserId=2,Username="two",IsActive=true,LocalUserStateId=1,UserPreferenceId=1});
            await db.SaveChangesAsync();
        }
        await service.SaveFontSizeAsync(120,1);
        var settings=await service.GetThemeAsync();
        Check(settings.FontSizePercent==120&&settings.ThemeMode=="light","Font change lost theme or was not saved");
        await using(var db=factory.CreateDbContext())
        {
            var one=await db.Users.Include(x=>x.UserPreference).SingleAsync(x=>x.UserId==1);
            var two=await db.Users.Include(x=>x.UserPreference).SingleAsync(x=>x.UserId==2);
            Check(one.UserPreferenceId!=two.UserPreferenceId&&two.UserPreference!.FontSizePercent==100,"Shared legacy preference changed another account");
            Check(one.UserPreference!.RecentWorkspaceSearchesJson=="[]"&&one.UserPreference.SelectedLanguageId==2,"Cloning lost other settings");
        }
        await service.SaveThemeAsync("dark",1);
        Check((await service.GetThemeAsync()).FontSizePercent==120,"Theme change reset saved font size");
        var denied=false;try{await service.SaveFontSizeAsync(130,2);}catch(UnauthorizedAccessException){denied=true;}
        Check(denied,"Font preference write impersonated another user");
        denied=false;try{await service.SaveFontSizeAsync(999,1);}catch(ArgumentException){denied=true;}
        Check(denied,"Unsupported font size accepted");
        auth.SignIn("two");Check((await service.GetThemeAsync()).FontSizePercent==100,"Another account inherited font size");
        auth.SignOut();denied=false;try{await service.SaveFontSizeAsync(110,1);}catch(UnauthorizedAccessException){denied=true;}
        Check(denied,"Signed out session saved preferences");
    }
    static async Task<OperationLog> Audit(HrmTeContext db){var log=new OperationLog();db.OperationLogs.Add(log);await db.SaveChangesAsync();return log;}
}
