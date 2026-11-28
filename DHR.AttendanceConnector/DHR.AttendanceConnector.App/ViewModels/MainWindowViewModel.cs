using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DHR.AttendanceConnector.Core.Configuration;
using DHR.AttendanceConnector.Core.Interfaces;
using DHR.AttendanceConnector.Core.Models;
using DHR.AttendanceConnector.Core.Services;
using Microsoft.Extensions.Options;

namespace DHR.AttendanceConnector.App.ViewModels;

public sealed class MainWindowViewModel(
    IConnectorRepository repository,
    IDhrApiClient apiClient,
    ConnectorActivationService activationService,
    DeviceCollectionService collectionService,
    AttendanceSyncService syncService,
    DiagnosticsExportService diagnosticsService,
    IOptions<ConnectorOptions> options) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private string _selectedPage = "Dashboard";
    private bool _isActivated;
    private bool _isBusy;
    private string _statusBadgeText = "Registration Required";
    private string _statusBadgeColor = "#B45309";
    private string _statusMessage = "Connect this computer to DHR using a one-time activation code.";
    private string _bannerMessage = "";
    private bool _isBannerError;

    // Activation inputs
    private string _activationApiUrl = options.Value.ApiBaseUrl;
    private string _activationCode = "";
    private string _connectorNameInput = Environment.MachineName;

    // Profile & Metrics
    private ConnectorProfile? _profile;
    private int _pendingEvents;
    private int _uploadedEvents;
    private int _failedEvents;
    private string _lastSyncText = "Never";
    private string _storageHealthText = "Checking...";
    private string _dhrConnectivityText = "Not Tested";
    private string _diagnosticOutput = "";

    public string SelectedPage
    {
        get => _selectedPage;
        set
        {
            if (SetField(ref _selectedPage, value))
            {
                OnPropertyChanged(nameof(IsDashboardPage));
                OnPropertyChanged(nameof(IsDevicesPage));
                OnPropertyChanged(nameof(IsSyncPage));
                OnPropertyChanged(nameof(IsLogsPage));
                OnPropertyChanged(nameof(IsDiagnosticsPage));
                OnPropertyChanged(nameof(IsSettingsPage));
                OnPropertyChanged(nameof(IsAboutPage));
            }
        }
    }

    public bool IsDashboardPage => SelectedPage == "Dashboard";
    public bool IsDevicesPage => SelectedPage == "Devices";
    public bool IsSyncPage => SelectedPage == "Sync";
    public bool IsLogsPage => SelectedPage == "Logs";
    public bool IsDiagnosticsPage => SelectedPage == "Diagnostics";
    public bool IsSettingsPage => SelectedPage == "Settings";
    public bool IsAboutPage => SelectedPage == "About";

    public bool IsActivated { get => _isActivated; private set { if (SetField(ref _isActivated, value)) OnPropertyChanged(nameof(RequiresActivation)); } }
    public bool RequiresActivation => !IsActivated;
    public bool IsBusy { get => _isBusy; private set => SetField(ref _isBusy, value); }
    public string StatusBadgeText { get => _statusBadgeText; private set => SetField(ref _statusBadgeText, value); }
    public string StatusBadgeColor { get => _statusBadgeColor; private set => SetField(ref _statusBadgeColor, value); }
    public string StatusMessage { get => _statusMessage; private set => SetField(ref _statusMessage, value); }
    public string BannerMessage { get => _bannerMessage; private set { if (SetField(ref _bannerMessage, value)) OnPropertyChanged(nameof(HasBanner)); } }
    public bool HasBanner => !string.IsNullOrWhiteSpace(BannerMessage);
    public bool IsBannerError { get => _isBannerError; private set => SetField(ref _isBannerError, value); }

    public string ActivationApiUrl { get => _activationApiUrl; set => SetField(ref _activationApiUrl, value); }
    public string ActivationCode { get => _activationCode; set => SetField(ref _activationCode, value); }
    public string ConnectorNameInput { get => _connectorNameInput; set => SetField(ref _connectorNameInput, value); }

    public ConnectorProfile? Profile { get => _profile; private set { if (SetField(ref _profile, value)) OnPropertyChanged(nameof(ConnectorSummaryHeader)); } }
    public string ConnectorSummaryHeader => Profile is null
        ? "Not Registered"
        : $"{Profile.AgentIdentifier} — {Profile.OrganisationName}" + (string.IsNullOrWhiteSpace(Profile.SiteName) ? "" : $" ({Profile.SiteName})");

    public int PendingEvents { get => _pendingEvents; private set => SetField(ref _pendingEvents, value); }
    public int UploadedEvents { get => _uploadedEvents; private set => SetField(ref _uploadedEvents, value); }
    public int FailedEvents { get => _failedEvents; private set => SetField(ref _failedEvents, value); }
    public string LastSyncText { get => _lastSyncText; private set => SetField(ref _lastSyncText, value); }
    public string StorageHealthText { get => _storageHealthText; private set => SetField(ref _storageHealthText, value); }
    public string DhrConnectivityText { get => _dhrConnectivityText; private set => SetField(ref _dhrConnectivityText, value); }
    public string DiagnosticOutput { get => _diagnosticOutput; private set => SetField(ref _diagnosticOutput, value); }

    public string ConnectorVersion => options.Value.ConnectorVersion;
    public string DataDirectoryPath => options.Value.ResolveDataDirectory();
    public string OperatingSystemInfo => $"{RuntimeInformation.OSDescription} ({RuntimeInformation.ProcessArchitecture})";
    public string DotNetRuntimeInfo => RuntimeInformation.FrameworkDescription;

    public ObservableCollection<DeviceConfiguration> Devices { get; } = [];
    public ObservableCollection<RawAttendanceEvent> RecentEvents { get; } = [];
    public ObservableCollection<ConnectorHealthLogEntry> HealthLogs { get; } = [];

    public async Task InitializeAsync()
    {
        await repository.InitializeAsync();
        await RefreshAsync();
    }

    public async Task RefreshAsync()
    {
        try
        {
            var dbOk = await repository.CheckHealthAsync();
            StorageHealthText = dbOk ? "Healthy (SQLite WAL)" : "Error";

            Profile = await repository.GetProfileAsync();
            IsActivated = Profile is not null;

            var devices = await repository.GetDevicesAsync();
            Devices.Clear();
            foreach (var d in devices) Devices.Add(d);

            var metrics = await repository.GetSyncMetricsAsync();
            PendingEvents = metrics.PendingCount + metrics.UploadingCount;
            UploadedEvents = metrics.AcknowledgedCount;
            FailedEvents = metrics.RejectedCount + metrics.QuarantinedCount;
            LastSyncText = metrics.LastSuccessfulSyncUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "Never";

            var events = await repository.GetRecentEventsAsync(50);
            RecentEvents.Clear();
            foreach (var e in events) RecentEvents.Add(e);

            var logs = await repository.GetHealthLogsAsync(100);
            HealthLogs.Clear();
            foreach (var l in logs) HealthLogs.Add(l);

            UpdateStatusIndicators(metrics);
        }
        catch (Exception ex)
        {
            ShowBanner($"Failed to refresh status: {ex.Message}", isError: true);
        }
    }

    public async Task ActivateConnectorAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        BannerMessage = "";
        try
        {
            var profile = await activationService.ActivateAsync(ActivationApiUrl, ActivationCode, ConnectorNameInput);
            ActivationCode = "";
            ShowBanner($"Connector activated as '{profile.AgentIdentifier}' for {profile.OrganisationName}.", isError: false);
            DhrConnectivityText = "Connected (mTLS)";
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowBanner($"Activation failed: {ex.Message}", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task SyncNowAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var collected = await collectionService.PollAllDevicesAsync();
            var result = await syncService.DrainPendingAsync();
            if (result.Succeeded)
            {
                DhrConnectivityText = "Connected (mTLS)";
                ShowBanner($"Sync completed: {collected} collected, {result.Accepted} uploaded, {result.Duplicate} duplicate, {result.Rejected} rejected.", isError: false);
            }
            else
            {
                DhrConnectivityText = "Offline / Retry Pending";
                ShowBanner($"Sync incomplete ({collected} collected locally): {result.ErrorMessage}", isError: true);
            }
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowBanner($"Sync failed: {ex.Message}", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RetryPendingAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var reset = await repository.RetryPendingAndQuarantinedAsync();
            var result = await syncService.DrainPendingAsync();
            ShowBanner($"Retried {reset} queued item(s). Uploaded: {result.Accepted}, Duplicate: {result.Duplicate}.", isError: !result.Succeeded);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowBanner($"Retry failed: {ex.Message}", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RefreshConfigFromDhrAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var updated = await activationService.RefreshConfigurationAsync();
            if (updated is not null)
            {
                DhrConnectivityText = "Connected (mTLS)";
                ShowBanner($"Configuration updated to v{updated.ConfigurationVersion}.", isError: false);
            }
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowBanner($"Config refresh failed: {ex.Message}", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task TestDhrConnectionAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var url = Profile?.ApiBaseUrl ?? ActivationApiUrl;
            var (reachable, message) = await apiClient.TestConnectionAsync(url, Profile?.AgentIdentifier);
            DhrConnectivityText = reachable ? "Reachable (HTTPS/mTLS)" : "Unreachable";
            DiagnosticOutput = $"[{DateTime.Now:HH:mm:ss}] DHR Test: {(reachable ? "OK" : "FAILED")} — {message}\n" + DiagnosticOutput;
            ShowBanner(message, isError: !reachable);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task TestDeviceAsync(int deviceId)
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var res = await collectionService.TestDeviceAsync(deviceId);
            var msg = res.IsReachable
                ? $"Device #{deviceId} ONLINE ({res.LatencyMs} ms{(string.IsNullOrWhiteSpace(res.FirmwareVersion) ? "" : $", FW: {res.FirmwareVersion}")})"
                : $"Device #{deviceId} OFFLINE: {res.ErrorMessage}";
            DiagnosticOutput = $"[{DateTime.Now:HH:mm:ss}] {msg}\n" + DiagnosticOutput;
            ShowBanner(msg, isError: !res.IsReachable);
            await RefreshAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task TestAllDevicesAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var list = await repository.GetDevicesAsync();
            if (list.Count == 0)
            {
                ShowBanner("No devices assigned to this connector.", isError: false);
                return;
            }
            var lines = new List<string>();
            foreach (var d in list)
            {
                var res = await collectionService.TestDeviceAsync(d.DeviceId);
                lines.Add($"[{DateTime.Now:HH:mm:ss}] {d.Name} ({d.IpAddress}:{d.Port}): {(res.IsReachable ? $"ONLINE ({res.LatencyMs}ms)" : $"OFFLINE — {res.ErrorMessage}")}");
            }
            DiagnosticOutput = string.Join("\n", lines) + "\n" + DiagnosticOutput;
            ShowBanner($"Tested {list.Count} device(s).", isError: false);
            await RefreshAsync();
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task ExportDiagnosticsAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var path = await diagnosticsService.ExportSanitisedZipAsync();
            DiagnosticOutput = $"[{DateTime.Now:HH:mm:ss}] Exported sanitised diagnostic package: {path}\n" + DiagnosticOutput;
            ShowBanner($"Diagnostic package saved to: {path}", isError: false);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowBanner($"Export failed: {ex.Message}", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public async Task RotateCertificateAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var updated = await activationService.RenewCredentialsIfNeededAsync(force: true);
            if (updated is not null)
                ShowBanner($"Client certificate rotated. New expiry: {updated.CertificateNotAfterUtc:yyyy-MM-dd}.", isError: false);
            await RefreshAsync();
        }
        catch (Exception ex)
        {
            ShowBanner($"Certificate rotation failed: {ex.Message}", isError: true);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void CheckForUpdates()
    {
        DiagnosticOutput = $"[{DateTime.Now:HH:mm:ss}] Current version: {ConnectorVersion}. Updates are distributed only via signed DHR packages published by the National Data Center.\n" + DiagnosticOutput;
        ShowBanner($"Running DHR Attendance Connector v{ConnectorVersion}. Check DHR → Attendance → Connectors for authorised signed updates.", isError: false);
    }

    private void UpdateStatusIndicators(SyncMetricsSnapshot metrics)
    {
        if (Profile is null)
        {
            StatusBadgeText = "Registration Required";
            StatusBadgeColor = "#B45309";
            StatusMessage = "Connect this computer to DHR using a one-time activation code.";
            return;
        }

        if (Profile.CertificateNotAfterUtc <= DateTime.UtcNow)
        {
            StatusBadgeText = "Authentication Error";
            StatusBadgeColor = "#B91C1C";
            StatusMessage = "Client certificate has expired. Rotate credentials or re-activate.";
            return;
        }

        if (metrics.QuarantinedCount > 0 || metrics.PendingCount >= 500)
        {
            StatusBadgeText = "Synchronisation Delayed";
            StatusBadgeColor = "#D97706";
            StatusMessage = $"{metrics.PendingCount} event(s) queued locally; {metrics.QuarantinedCount} quarantined.";
            return;
        }

        StatusBadgeText = "Connected to DHR";
        StatusBadgeColor = "#047857";
        StatusMessage = $"Active for {Profile.OrganisationName} • Config v{Profile.ConfigurationVersion} • {Devices.Count} device(s)";
    }

    private void ShowBanner(string message, bool isError)
    {
        IsBannerError = isError;
        BannerMessage = message;
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(name);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
