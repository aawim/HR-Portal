using HRM.DTOs.Contracts;
using System.Globalization;
namespace HRM.Services;
public sealed partial class FormerStaffService
{
    async Task<ExportReportDto> Contracts.FormerStaffServiceContracts.IOperations.DetailAsync(int person, CancellationToken ct)
    {
        var report = await DetailAsync(person, ct);
        return new(report.Title, report.Description, report.Tables.Select(t => new ExportTableDto(t.Name, t.Headers,
            t.Rows.Select(row => row.Select(ToCell).ToArray()).ToList())).ToList());
    }
    internal static ExportCellDto ToCell(object? value) => value switch
    {
        null or DBNull => new(ExportCellKind.Empty),
        string text => new(ExportCellKind.Text, Text: text),
        bool boolean => new(ExportCellKind.Boolean, Boolean: boolean),
        DateTime date => new(ExportCellKind.DateTime, DateTime: date),
        byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal
            => new(ExportCellKind.Number, Number: Convert.ToDecimal(value, CultureInfo.InvariantCulture)),
        DateOnly or TimeOnly or TimeSpan or Guid or char => new(ExportCellKind.Text, Text: Convert.ToString(value, CultureInfo.InvariantCulture)),
        _ => throw new InvalidOperationException("The report contains an unsupported cell value.")
    };
}
