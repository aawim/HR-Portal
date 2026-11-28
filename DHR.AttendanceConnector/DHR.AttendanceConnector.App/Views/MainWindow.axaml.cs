using Avalonia.Controls;
using Avalonia.Interactivity;
using DHR.AttendanceConnector.App.ViewModels;

namespace DHR.AttendanceConnector.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private MainWindowViewModel? Vm => DataContext as MainWindowViewModel;

    private void OnNavClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string page } && Vm is not null)
            Vm.SelectedPage = page;
    }

    private async void OnRefreshClick(object? sender, RoutedEventArgs e)
    {
        if (Vm is not null) await Vm.RefreshAsync();
    }

    private async void OnActivateClick(object? sender, RoutedEventArgs e)
    {
        if (Vm is not null) await Vm.ActivateConnectorAsync();
    }

    private async void OnSyncNowClick(object? sender, RoutedEventArgs e)
    {
        if (Vm is not null) await Vm.SyncNowAsync();
    }

    private async void OnRetryPendingClick(object? sender, RoutedEventArgs e)
    {
        if (Vm is not null) await Vm.RetryPendingAsync();
    }

    private async void OnRefreshConfigClick(object? sender, RoutedEventArgs e)
    {
        if (Vm is not null) await Vm.RefreshConfigFromDhrAsync();
    }

    private async void OnTestDhrClick(object? sender, RoutedEventArgs e)
    {
        if (Vm is not null) await Vm.TestDhrConnectionAsync();
    }

    private async void OnTestDeviceClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: int deviceId } && Vm is not null)
            await Vm.TestDeviceAsync(deviceId);
    }

    private async void OnTestAllDevicesClick(object? sender, RoutedEventArgs e)
    {
        if (Vm is not null) await Vm.TestAllDevicesAsync();
    }

    private async void OnExportDiagnosticsClick(object? sender, RoutedEventArgs e)
    {
        if (Vm is not null) await Vm.ExportDiagnosticsAsync();
    }

    private async void OnRotateCertClick(object? sender, RoutedEventArgs e)
    {
        if (Vm is not null) await Vm.RotateCertificateAsync();
    }

    private void OnCheckUpdatesClick(object? sender, RoutedEventArgs e)
    {
        Vm?.CheckForUpdates();
    }
}
