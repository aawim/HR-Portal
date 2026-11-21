using System.Text;
using ClosedXML.Excel;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

namespace HRM.Services;

public sealed class StaffExportRow
{
    public int IndividualId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? EmployeeNumber { get; set; }
    public string Department { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Groups { get; set; } = string.Empty;
}

public static class StaffManagementExport
{
    private static readonly string[] CsvHeaders = ["#", "Employee Number", "Staff Name", "Department", "Job Type", "Groups / Teams", "Status"];

    public static byte[] ToCsv(string organisation, string filters, IReadOnlyList<StaffExportRow> rows)
    {
        var sb = new StringBuilder();
        // UTF-8 BOM
        sb.AppendLine($"# Organisation: {EscapeCsv(organisation)}");
        sb.AppendLine($"# Filters: {EscapeCsv(filters)}");
        sb.AppendLine($"# Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"# Total Records: {rows.Count}");
        sb.AppendLine(string.Join(",", CsvHeaders));

        for (var i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            var line = string.Join(",", [
                (i + 1).ToString(),
                EscapeCsv(r.EmployeeNumber ?? ""),
                EscapeCsv(r.Name),
                EscapeCsv(r.Department),
                EscapeCsv(r.JobType),
                EscapeCsv(r.Groups),
                EscapeCsv(r.Status)
            ]);
            sb.AppendLine(line);
        }

        return new UTF8Encoding(true).GetBytes(sb.ToString());
    }

    public static byte[] ToPdf(string organisation, string filters, IReadOnlyList<StaffExportRow> rows, string fontPath)
    {
        OrganisationStaffExport.EnsureReportFont(fontPath);
        var document = new Document();
        document.Info.Title = $"{organisation} - Staff Management Roster";
        var normalStyle = document.Styles[StyleNames.Normal] ?? throw new InvalidOperationException("The report normal style is unavailable.");
        normalStyle.Font.Name = "StaffReport";
        normalStyle.Font.Size = 9;

        var section = document.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.Orientation = Orientation.Landscape;
        section.PageSetup.TopMargin = Unit.FromCentimeter(1.2);
        section.PageSetup.BottomMargin = Unit.FromCentimeter(1.2);
        section.PageSetup.LeftMargin = Unit.FromCentimeter(1.2);
        section.PageSetup.RightMargin = Unit.FromCentimeter(1.2);

        var titlePara = section.AddParagraph(organisation);
        titlePara.Format.Font.Size = 14;
        titlePara.Format.Font.Bold = true;
        titlePara.Format.SpaceAfter = Unit.FromPoint(2);

        var subPara = section.AddParagraph("Staff Directory & Operational Workforce Roster");
        subPara.Format.Font.Size = 10;
        subPara.Format.Font.Color = Colors.DarkSlateGray;
        subPara.Format.SpaceAfter = Unit.FromPoint(4);

        var metaPara = section.AddParagraph($"Filters: {filters}  |  Generated: {DateTime.Now:yyyy-MM-dd HH:mm}  |  Total Staff: {rows.Count}");
        metaPara.Format.Font.Size = 8;
        metaPara.Format.Font.Color = Colors.DimGray;
        metaPara.Format.SpaceAfter = Unit.FromPoint(8);

        var table = section.AddTable();
        table.Borders.Width = 0.35;
        table.Borders.Color = Colors.LightGray;

        // A4 landscape width is 29.7cm - margins (2.4cm) = ~27.3cm available
        // # (1.2), Emp# (2.8), Staff Name (5.8), Department (5.6), Job Type (4.2), Groups (4.8), Status (2.5) = 26.9cm
        var widths = new[] { 1.2, 2.8, 5.8, 5.6, 4.2, 4.8, 2.5 };
        foreach (var width in widths)
        {
            table.AddColumn(Unit.FromCentimeter(width));
        }

        var header = table.AddRow();
        header.HeadingFormat = true;
        header.Shading.Color = Colors.Lavender;
        for (var i = 0; i < CsvHeaders.Length; i++)
        {
            var p = header.Cells[i].AddParagraph(CsvHeaders[i]);
            p.Format.Font.Bold = true;
            p.Format.Font.Size = 8.5;
            header.Cells[i].VerticalAlignment = VerticalAlignment.Center;
        }

        for (var i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            var row = table.AddRow();
            row.TopPadding = Unit.FromPoint(3);
            row.BottomPadding = Unit.FromPoint(3);
            if (i % 2 == 1)
            {
                row.Shading.Color = Colors.WhiteSmoke;
            }

            var values = new[]
            {
                (i + 1).ToString(),
                r.EmployeeNumber ?? "-",
                r.Name,
                r.Department,
                r.JobType,
                string.IsNullOrWhiteSpace(r.Groups) ? "-" : r.Groups,
                r.Status
            };

            for (var c = 0; c < values.Length; c++)
            {
                var p = row.Cells[c].AddParagraph(values[c]);
                p.Format.Font.Size = 8;
                row.Cells[c].VerticalAlignment = VerticalAlignment.Center;
            }
        }

        var footer = section.Footers.Primary.AddParagraph();
        footer.Format.Alignment = ParagraphAlignment.Right;
        footer.Format.Font.Size = 8;
        footer.Format.Font.Color = Colors.DimGray;
        footer.AddText("Page ");
        footer.AddPageField();
        footer.AddText(" of ");
        footer.AddNumPagesField();

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();
        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, false);
        renderer.PdfDocument.Dispose();
        return stream.ToArray();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        var val = value.Trim();
        if (val.Contains(',') || val.Contains('"') || val.Contains('\n') || val.Contains('\r'))
        {
            return $"\"{val.Replace("\"", "\"\"")}\"";
        }
        return val;
    }
}
