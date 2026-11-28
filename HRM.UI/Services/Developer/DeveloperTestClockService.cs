using Microsoft.JSInterop;

namespace HRM.UI.Services.Developer;

/// <summary>
/// Maintains the test-clock preference for one browser circuit and synchronizes
/// the browser-local value with other tabs from the same browser profile.
/// </summary>
public sealed class DeveloperTestClockService(IJSRuntime js) : IDisposable, IAsyncDisposable
{
    public const string StorageKey = "hrm.developer.test-time-clock.enabled";

    private DotNetObjectReference<DeveloperTestClockService>? callbackReference;
    private long? listenerId;
    private bool disposed;

    public event Action? OnChange;

    public bool CurrentState { get; private set; } = true;
    public bool IsInitialized { get; private set; }

    public async Task InitializeAsync()
    {
        if (IsInitialized) return;

        try
        {
            var stored = await js.InvokeAsync<string?>("localStorage.getItem", StorageKey);
            CurrentState = string.IsNullOrWhiteSpace(stored) || !bool.TryParse(stored, out var parsed) || parsed;
        }
        catch
        {
            // Keep the enabled default when browser storage is unavailable.
        }

        try
        {
            callbackReference = DotNetObjectReference.Create(this);
            listenerId = await js.InvokeAsync<long>("hrmDeveloperTestClock.subscribe", callbackReference, StorageKey);
        }
        catch
        {
            callbackReference?.Dispose();
            callbackReference = null;
            listenerId = null;
        }

        IsInitialized = true;
        OnChange?.Invoke();
    }

    public async Task SetEnabledAsync(bool enabled)
    {
        CurrentState = enabled;
        IsInitialized = true;

        try
        {
            await js.InvokeVoidAsync("localStorage.setItem", StorageKey, enabled ? "true" : "false");
        }
        catch
        {
            // The current circuit still keeps the preference when storage is unavailable.
        }

        OnChange?.Invoke();
    }

    [JSInvokable]
    public Task OnStorageChanged(string? value)
    {
        var enabled = string.IsNullOrWhiteSpace(value) || !bool.TryParse(value, out var parsed) || parsed;
        IsInitialized = true;
        if (CurrentState == enabled) return Task.CompletedTask;

        CurrentState = enabled;
        OnChange?.Invoke();
        return Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (disposed) return;

        if (listenerId.HasValue)
        {
            try
            {
                await js.InvokeVoidAsync("hrmDeveloperTestClock.unsubscribe", listenerId.Value);
            }
            catch
            {
                // The browser or circuit may already be unavailable.
            }
        }

        Dispose();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        listenerId = null;
        callbackReference?.Dispose();
        callbackReference = null;
    }
}
