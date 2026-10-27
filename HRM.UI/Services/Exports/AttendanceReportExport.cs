using ClosedXML.Excel;
using HRM.DTOs.Attendance;
using HRM.Helpers;
using MigraDoc.DocumentObjectModel;
using MigraDoc.Rendering;
using System.Globalization;

namespace HRM.Services;




// Both formats consume the same authorised, filtered snapshot as the attendance view.
public static class AttendanceReportExport
{
    public static AttendanceExportTable Daily(IEnumerable<YearlyAttendanceDayDto> days) => new("Daily attendance",
        ["Date", "Status", "Scheduled in", "Scheduled out", "Check in", "Check out", "Late minutes", "Worked hours", "Clock events"],
        days.OrderBy(d => d.Date).Select(d => new object?[] { d.Date.ToString("yyyy-MM-dd"), AttendancePresentation.Status(d),
            d.ScheduledStart?.ToString("HH:mm"), d.ScheduledEnd?.ToString("HH:mm"), d.FirstCheckIn?.ToString("HH:mm"),
            d.LastCheckOut?.ToString("HH:mm"), d.IsLate ? d.LateMinutes : 0, d.WorkedHours, d.ClockEventCount }).ToList());

    public static byte[] Excel(AttendanceExportReport report)
    {
        using var book = new XLWorkbook();
        foreach (var data in report.Tables)
        {
            var sheet = book.Worksheets.Add(data.Name);
            var count = data.Headers.Length;
            sheet.Range(1, 1, 1, count).Merge().Value = report.Title;
            sheet.Cell(1, 1).Style.Font.SetBold().Font.SetFontSize(16);
            sheet.Range(2, 1, 2, count).Merge().Value = report.Description;
            sheet.Range(3, 1, 3, count).Merge().Value = $"Generated {DateTime.UtcNow.AddHours(5):dd MMM yyyy HH:mm} (Maldives) · {data.Rows.Count} records";
            for (var c = 0; c < count; c++) sheet.Cell(5, c + 1).Value = data.Headers[c];
            for (var r = 0; r < data.Rows.Count; r++)
                for (var c = 0; c < count; c++)
                {
                    var cell = sheet.Cell(r + 6, c + 1);
                    var value = data.Rows[r][c];
                    // Assign text as values, never formulas supplied by staff names or comments.
                    if (value is int n) cell.Value = n;
                    else if (value is decimal dec) { cell.Value = dec; cell.Style.NumberFormat.Format = "0.##"; }
                    else if (value is double dbl) { cell.Value = dbl; cell.Style.NumberFormat.Format = "0.##"; }
                    else cell.Value = Text(value);
                }
            sheet.Range(5, 1, Math.Max(6, data.Rows.Count + 5), count).CreateTable().Theme = XLTableTheme.TableStyleMedium2;
            sheet.Columns(1, count).Width = 20;
            sheet.Range(2, 1, Math.Max(6, data.Rows.Count + 5), count).Style.Alignment.WrapText = true;
            sheet.Row(2).Height = 35;
            sheet.SheetView.FreezeRows(5);
        }
        using var stream = new MemoryStream(); book.SaveAs(stream); return stream.ToArray();
    }

    public static byte[] Pdf(AttendanceExportReport report, string fontPath)
    {
        OrganisationStaffExport.EnsureReportFont(fontPath);
        var document = new Document(); document.Info.Title = report.Title;
        var normalStyle = document.Styles[StyleNames.Normal] ?? throw new InvalidOperationException("The report normal style is unavailable.");
        normalStyle.Font.Name = "StaffReport";
        normalStyle.Font.Size = 8;
        foreach (var data in report.Tables)
        {
            var section = document.AddSection();
            section.PageSetup.PageFormat = PageFormat.A4; section.PageSetup.Orientation = Orientation.Landscape;
            section.PageSetup.TopMargin = section.PageSetup.BottomMargin = Unit.FromCentimeter(1.3);
            section.PageSetup.LeftMargin = section.PageSetup.RightMargin = Unit.FromCentimeter(1.3);
            section.AddParagraph(report.Title).Format.Font.Size = 15;
            section.AddParagraph(report.Description).Format.SpaceAfter = Unit.FromPoint(6);
            section.AddParagraph($"{data.Name} · {data.Rows.Count} records · Generated {DateTime.UtcNow.AddHours(5):dd MMM yyyy HH:mm} (Maldives)").Format.SpaceAfter = Unit.FromPoint(10);
            var table = section.AddTable(); table.Borders.Width = 0.4; table.Borders.Color = Colors.LightGray;
            var weights = data.Headers.Select(h => h is "Status" or "Employee" or "Schedule" ? 2d : 1d).ToArray();
            foreach (var weight in weights) table.AddColumn(Unit.FromCentimeter(27.1 * weight / weights.Sum()));
            var header = table.AddRow(); header.HeadingFormat = true; header.Shading.Color = Colors.LightSteelBlue;
            for (var c = 0; c < data.Headers.Length; c++) header.Cells[c].AddParagraph(data.Headers[c]);
            foreach (var values in data.Rows)
            {
                var row = table.AddRow(); row.TopPadding = row.BottomPadding = Unit.FromPoint(3);
                for (var c = 0; c < data.Headers.Length; c++) row.Cells[c].AddParagraph(Text(values[c]));
            }
            if (data.Rows.Count == 0) section.AddParagraph("No records for this selection.");
            var footer = section.Footers.Primary.AddParagraph(); footer.Format.Alignment = ParagraphAlignment.Right;
            footer.AddText("Page "); footer.AddPageField(); footer.AddText(" of "); footer.AddNumPagesField();
        }
        var renderer = new PdfDocumentRenderer { Document = document }; renderer.RenderDocument();
        using var stream = new MemoryStream(); renderer.PdfDocument.Save(stream, false); renderer.PdfDocument.Dispose(); return stream.ToArray();
    }
    private static string Text(object? value) => value is IFormattable f ? f.ToString("0.##", CultureInfo.InvariantCulture) : value?.ToString() ?? "—";
}
