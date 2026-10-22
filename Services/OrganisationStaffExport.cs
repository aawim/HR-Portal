using ClosedXML.Excel;
using HRM.DTOs.Organisation;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

namespace HRM.Services;

public static class OrganisationStaffExport
{
    private static readonly string[] Headers = ["Staff", "Employee number", "Department", "Job type", "Status"];
    private static string[] Values(OrganisationStaffRow row) => [row.Name.Trim(), row.EmployeeNumber ?? "", row.Department, row.JobType, row.Status];
    public static int EmployeeCount(IEnumerable<OrganisationStaffRow> rows) => rows.Select(x => x.IndividualId).Distinct().Count();
    public static byte[] Excel(string organisation, string filters, IReadOnlyList<OrganisationStaffRow> rows)
    {
        using var book = new XLWorkbook();
        var sheet = book.Worksheets.Add("Organisation staff");
        sheet.Range("A1:E1").Merge().Value = organisation;
        sheet.Range("A1:E1").Style.Font.Bold = true;
        sheet.Range("A1:E1").Style.Font.FontSize = 16;
        sheet.Range("A2:E2").Merge().Value = filters;
        sheet.Range("A3:E3").Merge().Value = $"Employees: {EmployeeCount(rows)} | Employment records: {rows.Count}";
        for (var col = 0; col < Headers.Length; col++) sheet.Cell(5, col + 1).Value = Headers[col];
        for (var i = 0; i < rows.Count; i++)
        {
            var values = Values(rows[i]);
            for (var col = 0; col < values.Length; col++) sheet.Cell(i + 6, col + 1).Value = values[col];
        }
        var table = sheet.Range(5, 1, Math.Max(6, rows.Count + 5), 5).CreateTable();
        table.Theme = XLTableTheme.TableStyleMedium2;
        sheet.Columns(1, 5).Width = 25;
        sheet.Column(1).Width = 34; sheet.Column(3).Width = 38;
        sheet.Range(5, 1, Math.Max(6, rows.Count + 5), 5).Style.Alignment.WrapText = true;
        sheet.SheetView.FreezeRows(5);
        using var stream = new MemoryStream(); book.SaveAs(stream); return stream.ToArray();
    }
    private static readonly object FontLock = new();
    internal static void EnsureReportFont(string fontPath)
    {
        lock (FontLock) GlobalFontSettings.FontResolver ??= new ReportFontResolver(File.ReadAllBytes(fontPath));
    }
    public static byte[] Pdf(string organisation, string filters, IReadOnlyList<OrganisationStaffRow> rows, string fontPath)
    {
        EnsureReportFont(fontPath);
        var document = new Document();
        document.Info.Title = organisation + " - Organisation staff";
        document.Styles[StyleNames.Normal].Font.Name = "StaffReport";
        document.Styles[StyleNames.Normal].Font.Size = 10;
        var section = document.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4; section.PageSetup.Orientation = Orientation.Landscape;
        section.PageSetup.TopMargin = Unit.FromCentimeter(1.5); section.PageSetup.BottomMargin = Unit.FromCentimeter(1.5);
        section.PageSetup.LeftMargin = Unit.FromCentimeter(1.5); section.PageSetup.RightMargin = Unit.FromCentimeter(1.5);
        section.AddParagraph(organisation).Format.Font.Size = 16;
        section.AddParagraph(filters).Format.SpaceAfter = Unit.FromPoint(6);
        section.AddParagraph($"Employees: {EmployeeCount(rows)} | Employment records: {rows.Count}").Format.SpaceAfter = Unit.FromPoint(12);
        var table = section.AddTable(); table.Borders.Width = 0.4; table.Borders.Color = Colors.LightGray;
        foreach (var width in new[] { 6.0, 3.3, 7.0, 5.2, 5.0 }) table.AddColumn(Unit.FromCentimeter(width));
        var header = table.AddRow(); header.HeadingFormat = true; header.Shading.Color = Colors.LightSteelBlue;
        for (var i = 0; i < Headers.Length; i++) header.Cells[i].AddParagraph(Headers[i]);
        foreach (var item in rows)
        {
            var row = table.AddRow(); row.TopPadding = Unit.FromPoint(4); row.BottomPadding = Unit.FromPoint(4);
            var values = Values(item);
            for (var i = 0; i < values.Length; i++) row.Cells[i].AddParagraph(values[i]);
        }
        var footer = section.Footers.Primary.AddParagraph(); footer.Format.Alignment = ParagraphAlignment.Right;
        footer.AddText("Page "); footer.AddPageField(); footer.AddText(" of "); footer.AddNumPagesField();
        var renderer = new PdfDocumentRenderer { Document = document }; renderer.RenderDocument();
        using var stream = new MemoryStream(); renderer.PdfDocument.Save(stream, false); renderer.PdfDocument.Dispose(); return stream.ToArray();
    }
    private sealed class ReportFontResolver(byte[] font) : IFontResolver
    {
        public byte[] GetFont(string faceName) => font;
        public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic) => new("StaffReport");
    }
}
