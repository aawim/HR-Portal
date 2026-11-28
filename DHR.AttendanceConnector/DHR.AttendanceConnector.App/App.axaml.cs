using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DHR.AttendanceConnector.App.ViewModels;
using DHR.AttendanceConnector.App.Views;
using DHR.AttendanceConnector.Core.Interfaces;
using DHR.AttendanceConnector.Devices.Abstractions;
using DHR.AttendanceConnector.Devices.GenericTcp;
using DHR.AttendanceConnector.Devices.Simulator;
using DHR.AttendanceConnector.Devices.ZKTeco;
using DHR.AttendanceConnector.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DHR.AttendanceConnector.App;

public partial class App : Application
{
    public IServiceProvider? Services { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables(prefix: "DHR_CONNECTOR_")
            .Build();

        var services = new ServiceCollection();
        services.AddLogging(b => b.AddConsole());
        services.AddDhrConnectorCoreAndInfrastructure(config);
        services.AddSingleton<IBiometricDeviceAdapter, ZkTecoTcpDeviceAdapter>();
        services.AddSingleton<IBiometricDeviceAdapter, GenericTcpDeviceAdapter>();
        services.AddSingleton<VirtualBiometricDeviceSimulator>();
        services.AddSingleton<IBiometricDeviceAdapter>(sp => sp.GetRequiredService<VirtualBiometricDeviceSimulator>());
        services.AddSingleton<IDeviceAdapterRegistry, DeviceAdapterRegistry>();
        services.AddSingleton<MainWindowViewModel>();

        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = Services.GetRequiredService<MainWindowViewModel>();
            desktop.MainWindow = new MainWindow { DataContext = vm };
            _ = vm.InitializeAsync();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
