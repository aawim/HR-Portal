namespace HRM.WorkPlanning;

/// <summary>Coordinates generation per tenant/job/day without retaining inactive keys.</summary>
public sealed class WorkPlanGenerationGate
{
    public static WorkPlanGenerationGate Shared {get;}=new();
    private readonly object sync=new();
    private readonly Dictionary<(int Org,int Job,DateOnly Day),Entry> entries=[];
    private sealed class Entry {public readonly SemaphoreSlim Semaphore=new(1,1);public int Users;}
    public int ActiveKeyCount {get{lock(sync)return entries.Count;}}
    public async Task<IDisposable> EnterAsync(int org,int job,DateOnly day,CancellationToken ct=default)
    {
        ct.ThrowIfCancellationRequested();var key=(org,job,day);Entry entry;
        lock(sync){if(!entries.TryGetValue(key,out entry!)){entry=new();entries.Add(key,entry);}entry.Users++;}
        try
        {
            if(!await entry.Semaphore.WaitAsync(TimeSpan.FromSeconds(5),ct))throw new WorkPlanBusyException();
        }
        catch{Release(key,entry,false);throw;}
        return new Lease(()=>Release(key,entry,true));
    }
    private void Release((int Org,int Job,DateOnly Day) key,Entry entry,bool acquired)
    {
        lock(sync){if(acquired)entry.Semaphore.Release();if(--entry.Users==0){entries.Remove(key);entry.Semaphore.Dispose();}}
    }
    private sealed class Lease(Action release):IDisposable
    {
        private Action? callback=release;
        public void Dispose()=>Interlocked.Exchange(ref callback,null)?.Invoke();
    }
}
public sealed class WorkPlanBusyException(Exception? inner=null):Exception("The work plan is temporarily busy. Please refresh shortly.",inner);
