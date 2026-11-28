using System.Text.Json;
using HRM.ErrorLogs.Cli;
using HRM.ErrorLogs.Diagnostics;
using HRM.ErrorLogs.Models;
using HRM.ErrorLogs.Services;
using Microsoft.Extensions.Configuration;

Console.Title = "HRM Error Logs & Diagnostics CLI";

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets(typeof(ErrorLogEntry).Assembly, optional: true)
    .AddEnvironmentVariables()
    .Build();

var connStr = config.GetConnectionString("ErrorLogsConnection") ??
              config.GetConnectionString("HrmErrorLogs") ??
              "Server=DESKTOP-IGBGLUQ\\SQLEXPRESS;Database=HrmErrorLogs;Trusted_Connection=True;TrustServerCertificate=True;";

// Initialize store and service
var store = new CliErrorLogStore(connStr);
var errorService = new ErrorLogService(persistentStore: store, queue: null, logger: null, defaultApplicationSource: "HRM.ErrorLogs.CLI");

PrintBanner(connStr);

if (args.Contains("--simulate") || args.Contains("-s"))
{
    await RunSimulationAsync(errorService);
    return;
}

if (args.Contains("--stats"))
{
    var stats = await errorService.GetStatisticsAsync();
    Console.WriteLine(JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true }));
    return;
}

var deleteIndex = Array.IndexOf(args, "--soft-delete");
if (deleteIndex >= 0 && deleteIndex < args.Length - 1 && Guid.TryParse(args[deleteIndex + 1], out var deleteId))
{
    var success = await errorService.SoftDeleteAsync(deleteId, "CLI-Developer");
    Console.WriteLine($"Soft-delete for {deleteId}: {(success ? "SUCCESS" : "FAILED")}");
    return;
}

var restoreIndex = Array.IndexOf(args, "--restore");
if (restoreIndex >= 0 && restoreIndex < args.Length - 1 && Guid.TryParse(args[restoreIndex + 1], out var restoreId))
{
    var success = await errorService.RestoreAsync(restoreId);
    Console.WriteLine($"Restore for {restoreId}: {(success ? "SUCCESS" : "FAILED")}");
    return;
}

// Display Overview Dashboard
await DisplayDashboardAsync(errorService);

// If console is interactive, allow interactive commands
if (!Console.IsInputRedirected && Environment.UserInteractive)
{
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("\nInteractive Controls: [S] Simulate Test Error | [R] Refresh | [Q] Quit");
    Console.ResetColor();

    while (true)
    {
        if (!Console.KeyAvailable)
        {
            await Task.Delay(250);
            continue;
        }

        var key = Console.ReadKey(intercept: true).Key;
        if (key == ConsoleKey.Q)
        {
            Console.WriteLine("Exiting HRM Error Logs CLI. Goodbye!");
            break;
        }
        else if (key == ConsoleKey.S)
        {
            await RunSimulationAsync(errorService);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("\n[S] Simulate Test Error | [R] Refresh | [Q] Quit");
            Console.ResetColor();
        }
        else if (key == ConsoleKey.R)
        {
            Console.Clear();
            PrintBanner(connStr);
            await DisplayDashboardAsync(errorService);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("\n[S] Simulate Test Error | [R] Refresh | [Q] Quit");
            Console.ResetColor();
        }
    }
}

static void PrintBanner(string connStr)
{
    Console.ForegroundColor = ConsoleColor.DarkYellow;
    Console.WriteLine(@"
  ===========================================================================
    HRM APPLICATION ERROR LOGS & DIAGNOSTICS SERVICE
  ===========================================================================");
    Console.ResetColor();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"  [Database]   : HrmErrorLogs");
    Console.WriteLine($"  [Connection] : {connStr}");
    Console.WriteLine($"  [Projects]   : HRM.Api, HRM.UI, HRM.Payroll.Worker");
    Console.ResetColor();
    Console.ForegroundColor = ConsoleColor.DarkYellow;
    Console.WriteLine("  ===========================================================================\n");
    Console.ResetColor();
}

static async Task DisplayDashboardAsync(IErrorLogService errorService)
{
    var stats = await errorService.GetStatisticsAsync();
    var recent = await errorService.GetRecentErrorsAsync(10);

    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("  [LIVE TELEMETRY SUMMARY]");
    Console.ResetColor();
    Console.WriteLine($"  • Total Errors Recorded : {stats.TotalErrors}");
    Console.WriteLine($"  • Active Unresolved     : {stats.UnresolvedCount}");
    Console.WriteLine($"  • Critical / Fatal      : {stats.CriticalCount + stats.FatalCount}");
    Console.WriteLine($"  • Errors Last 24 Hours  : {stats.ErrorsLast24Hours}");
    Console.WriteLine();

    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("  [RECENT APPLICATION ERRORS & PRESCRIBED FIXES]");
    Console.ResetColor();

    if (recent.Count == 0)
    {
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("  ✓ No errors currently logged. All HRM monitored services healthy.");
        Console.ResetColor();
        return;
    }

    foreach (var entry in recent)
    {
        var color = entry.CriticalityLevel switch
        {
            ErrorCriticalityLevel.Fatal => ConsoleColor.Red,
            ErrorCriticalityLevel.Critical => ConsoleColor.DarkRed,
            ErrorCriticalityLevel.Error => ConsoleColor.Yellow,
            ErrorCriticalityLevel.Warning => ConsoleColor.DarkYellow,
            _ => ConsoleColor.Gray
        };

        Console.ForegroundColor = color;
        Console.Write($"  [{entry.CriticalityLevel.ToString().ToUpperInvariant()}] ");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Console.Write($"{entry.TimestampLocal:HH:mm:ss} ");
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.Write($"[{entry.ApplicationSource}] ");
        Console.ResetColor();

        if (entry.Location != null && !string.IsNullOrWhiteSpace(entry.Location.FileName))
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write($"{entry.Location.FileName}");
            if (entry.Location.LineNumber > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($":line {entry.Location.LineNumber}");
            }
            Console.ResetColor();
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine($"     What Happened : {entry.ExceptionType}: {entry.ErrorMessage}");

        if (!string.IsNullOrWhiteSpace(entry.Fix?.RecommendedAction))
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"     Suggested Fix : {entry.Fix.RecommendedAction}");
        }

        Console.ResetColor();
        Console.WriteLine();
    }
}

static async Task RunSimulationAsync(IErrorLogService errorService)
{
    Console.ForegroundColor = ConsoleColor.Yellow;
    Console.WriteLine("\n  --> Simulating live test exception with line number tracking...");
    Console.ResetColor();

    Exception caught;
    try
    {
        ThrowSimulatedException();
        throw new Exception("Unreachable");
    }
    catch (Exception ex)
    {
        caught = ex;
    }

    var entry = await errorService.CaptureExceptionAsync(
        caught,
        ErrorCriticalityLevel.Error,
        new ErrorContext
        {
            ApplicationSource = "HRM.ErrorLogs.CLI",
            Username = Environment.UserName,
            RemoteIp = "127.0.0.1",
            PageLocation = "CLI/Simulation",
            HttpMethod = "CLI_TEST"
        });

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("  ✓ Exception captured successfully!");
    Console.ResetColor();
    Console.WriteLine($"     Error ID      : {entry.Id}");
    Console.WriteLine($"     Location      : {entry.Location.FileName}:line {entry.Location.LineNumber}");
    Console.WriteLine($"     Method        : {entry.Location.MethodName}()");
    Console.WriteLine($"     What Happened : {entry.ExceptionType}: {entry.ErrorMessage}");
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"     Root Cause    : {entry.Fix.RootCauseSummary}");
    Console.WriteLine($"     Prescribed Fix: {entry.Fix.RecommendedAction}");
    Console.ResetColor();

    // Allow background persistence task to complete before process exit
    await Task.Delay(1500);
}

static void ThrowSimulatedException()
{
    throw new NullReferenceException("Simulated NullReferenceException in CLI test for exact line number and prescriptive fix verification.");
}
