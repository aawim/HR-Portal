using HRM.DTOs.Attendance;
using System.Globalization;
namespace HRM.Services;

public sealed record AttendanceExportTable(string Name, string[] Headers, IReadOnlyList<object?[]> Rows);

public sealed record AttendanceExportReport(string Title, string Description, IReadOnlyList<AttendanceExportTable> Tables);
