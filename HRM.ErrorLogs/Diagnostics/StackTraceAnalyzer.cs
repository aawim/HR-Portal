using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using HRM.ErrorLogs.Models;

namespace HRM.ErrorLogs.Diagnostics;

/// <summary>
/// Extracts detailed error location including source file, class, method, and exact line number from exceptions.
/// </summary>
public static class StackTraceAnalyzer
{
    private static readonly Regex LineRegex = new(
        @"at\s+(?<method>.+?)\s+in\s+(?<file>.+?):line\s+(?<line>\d+)(?::(?<col>\d+))?",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly Regex MethodOnlyRegex = new(
        @"at\s+(?<method>[^\r\n]+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static ErrorLocation Analyze(Exception ex)
    {
        var location = new ErrorLocation();
        if (ex == null) return location;

        // 1. Try structured StackTrace inspection with source line info
        try
        {
            var stackTrace = new StackTrace(ex, fNeedFileInfo: true);
            var frames = stackTrace.GetFrames();

            if (frames != null && frames.Length > 0)
            {
                // Prioritize the innermost frame from HRM application code
                StackFrame? appFrame = null;
                foreach (var frame in frames)
                {
                    var method = frame.GetMethod();
                    var type = method?.DeclaringType;
                    var typeName = type?.FullName ?? string.Empty;

                    if (typeName.StartsWith("HRM.", StringComparison.OrdinalIgnoreCase))
                    {
                        appFrame = frame;
                        // If this frame has line numbers, take it immediately
                        if (frame.GetFileLineNumber() > 0)
                            break;
                    }
                }

                // If no HRM frame found, use the first frame
                var selectedFrame = appFrame ?? frames[0];
                var targetMethod = selectedFrame.GetMethod();
                var declaringType = targetMethod?.DeclaringType;

                location.LineNumber = selectedFrame.GetFileLineNumber();
                location.ColumnNumber = selectedFrame.GetFileColumnNumber();
                location.FilePath = selectedFrame.GetFileName() ?? string.Empty;
                location.FileName = !string.IsNullOrEmpty(location.FilePath)
                    ? Path.GetFileName(location.FilePath)
                    : string.Empty;

                if (targetMethod != null)
                {
                    location.MethodName = targetMethod.Name;
                    location.ClassName = declaringType?.Name ?? string.Empty;
                    location.Namespace = declaringType?.Namespace ?? string.Empty;
                    location.AssemblyName = declaringType?.Assembly.GetName().Name ?? string.Empty;
                }
            }
        }
        catch
        {
            // Fall back to text parsing
        }

        // 2. If line number is still missing, parse stack trace string via regex
        if (location.LineNumber == 0 && !string.IsNullOrWhiteSpace(ex.StackTrace))
        {
            ParseStackTraceString(ex.StackTrace, location);
        }

        // 3. If inner exception exists and outer had no line number, check inner exception
        if (location.LineNumber == 0 && ex.InnerException != null)
        {
            var innerLocation = Analyze(ex.InnerException);
            if (innerLocation.LineNumber > 0)
            {
                return innerLocation;
            }
        }

        return location;
    }

    private static void ParseStackTraceString(string stackTraceText, ErrorLocation location)
    {
        var lines = stackTraceText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

        // Find the first line mentioning HRM and with file:line
        foreach (var line in lines)
        {
            var match = LineRegex.Match(line);
            if (match.Success)
            {
                var fullFile = match.Groups["file"].Value.Trim();
                var lineStr = match.Groups["line"].Value.Trim();
                var methodStr = match.Groups["method"].Value.Trim();

                if (int.TryParse(lineStr, out var lineNum) && lineNum > 0)
                {
                    location.LineNumber = lineNum;
                    location.FilePath = fullFile;
                    location.FileName = Path.GetFileName(fullFile);

                    if (match.Groups["col"].Success && int.TryParse(match.Groups["col"].Value, out var colNum))
                    {
                        location.ColumnNumber = colNum;
                    }

                    if (string.IsNullOrWhiteSpace(location.MethodName) && !string.IsNullOrWhiteSpace(methodStr))
                    {
                        ExtractMethodAndClass(methodStr, location);
                    }

                    // If this matches HRM code, stop here
                    if (fullFile.Contains("HRM", StringComparison.OrdinalIgnoreCase) ||
                        methodStr.StartsWith("HRM.", StringComparison.OrdinalIgnoreCase))
                    {
                        return;
                    }
                }
            }
        }

        // Fallback for method names if still missing
        if (string.IsNullOrWhiteSpace(location.MethodName))
        {
            foreach (var line in lines)
            {
                var m = MethodOnlyRegex.Match(line);
                if (m.Success)
                {
                    var rawMethod = m.Groups["method"].Value.Trim();
                    ExtractMethodAndClass(rawMethod, location);
                    break;
                }
            }
        }
    }

    private static void ExtractMethodAndClass(string rawMethod, ErrorLocation location)
    {
        // Example: HRM.Services.Payroll.PayrollProcessor.ProcessRunBatchesAsync(...)
        var parenIndex = rawMethod.IndexOf('(');
        var signature = parenIndex >= 0 ? rawMethod.Substring(0, parenIndex).Trim() : rawMethod;
        var parts = signature.Split('.');

        if (parts.Length >= 2)
        {
            location.MethodName = parts[^1];
            location.ClassName = parts[^2];
            if (parts.Length > 2)
            {
                location.Namespace = string.Join('.', parts.Take(parts.Length - 1));
            }
        }
        else if (parts.Length == 1)
        {
            location.MethodName = parts[0];
        }
    }
}
