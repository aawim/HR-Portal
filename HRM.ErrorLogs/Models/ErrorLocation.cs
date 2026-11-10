namespace HRM.ErrorLogs.Models;

/// <summary>
/// Pinpoints the exact source code location where an exception was thrown.
/// </summary>
public sealed class ErrorLocation
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int LineNumber { get; set; }
    public int ColumnNumber { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public string MethodName { get; set; } = string.Empty;
    public string Namespace { get; set; } = string.Empty;
    public string AssemblyName { get; set; } = string.Empty;

    public bool HasLineNumber => LineNumber > 0;

    public string FormattedLocation
    {
        get
        {
            if (string.IsNullOrWhiteSpace(FileName) && string.IsNullOrWhiteSpace(MethodName))
                return "Unknown location";

            var filePart = !string.IsNullOrWhiteSpace(FileName)
                ? (LineNumber > 0 ? $"{FileName}:{LineNumber}" : FileName)
                : (string.IsNullOrWhiteSpace(ClassName) ? "App" : ClassName);

            var methodPart = !string.IsNullOrWhiteSpace(MethodName) ? $"{MethodName}()" : string.Empty;

            return !string.IsNullOrWhiteSpace(methodPart)
                ? $"{filePart} in {methodPart}"
                : filePart;
        }
    }
}
