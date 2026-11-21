using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HRM.AttendanceEdgeAgent.Installation;

public static class ServiceInstaller
{
    private const string ServiceName = "HRMAttendanceEdgeAgent";
    private const string DisplayName = "HRM Attendance Edge Agent";

    public static async Task InstallAsync()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(exePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Could not determine current executable path.");
            Console.ResetColor();
            return;
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Console.WriteLine($"Installing Windows Service '{ServiceName}'...");
            var binPath = $"\"{exePath}\"";

            await RunCommandAsync("sc.exe", $"create {ServiceName} binPath= {binPath} start= auto DisplayName= \"{DisplayName}\"");
            await RunCommandAsync("sc.exe", $"description {ServiceName} \"Local attendance edge agent for collecting LAN biometric clock events and relaying to DHR via mTLS.\"");
            await RunCommandAsync("sc.exe", $"failure {ServiceName} reset= 86400 actions= restart/60000/restart/60000/restart/60000");

            Console.WriteLine($"Attempting to start {ServiceName}...");
            await RunCommandAsync("sc.exe", $"start {ServiceName}");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Windows Service '{ServiceName}' installed and configured for auto-start on boot.");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine("=== Linux systemd Service Configuration ===");
            var unitContent = $"""
[Unit]
Description={DisplayName}
After=network.target

[Service]
Type=simple
ExecStart={exePath}
WorkingDirectory={AppContext.BaseDirectory}
Restart=always
RestartSec=10
KillSignal=SIGINT
SyslogIdentifier=hrm-edge-agent
Environment=DOTNET_ENVIRONMENT=Production

[Install]
WantedBy=multi-user.target
""";

            var serviceFilePath = "/etc/systemd/system/hrm-attendance-edge.service";
            Console.WriteLine($"Systemd unit file template:\n");
            Console.WriteLine(unitContent);
            Console.WriteLine("\nTo install on Linux:");
            Console.WriteLine($"1. sudo tee {serviceFilePath} << 'EOF'");
            Console.WriteLine(unitContent);
            Console.WriteLine("EOF");
            Console.WriteLine("2. sudo systemctl daemon-reload");
            Console.WriteLine("3. sudo systemctl enable --now hrm-attendance-edge");
            Console.WriteLine("4. sudo systemctl status hrm-attendance-edge");
        }
    }

    public static async Task UninstallAsync()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            Console.WriteLine($"Stopping and deleting Windows Service '{ServiceName}'...");
            await RunCommandAsync("sc.exe", $"stop {ServiceName}");
            await RunCommandAsync("sc.exe", $"delete {ServiceName}");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Windows Service '{ServiceName}' uninstalled.");
            Console.ResetColor();
        }
        else
        {
            Console.WriteLine("To uninstall on Linux, run:");
            Console.WriteLine("  sudo systemctl stop hrm-attendance-edge");
            Console.WriteLine("  sudo systemctl disable hrm-attendance-edge");
            Console.WriteLine("  sudo rm /etc/systemd/system/hrm-attendance-edge.service");
            Console.WriteLine("  sudo systemctl daemon-reload");
        }
    }

    private static async Task RunCommandAsync(string command, string arguments)
    {
        try
        {
            using var proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            proc.Start();
            var stdout = await proc.StandardOutput.ReadToEndAsync();
            var stderr = await proc.StandardError.ReadToEndAsync();
            await proc.WaitForExitAsync();

            if (!string.IsNullOrWhiteSpace(stdout)) Console.WriteLine($"  {stdout.Trim()}");
            if (!string.IsNullOrWhiteSpace(stderr)) Console.WriteLine($"  {stderr.Trim()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  Command '{command} {arguments}' encountered: {ex.Message}");
        }
    }
}
