using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HRM.Application.DTOs.Budget;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;

namespace HRM.Services.Exports;

public static class BudgetPdfGenerator
{
    public static byte[] GenerateBudgetExecutionPdf(BudgetSummaryDto summary)
    {
        var document = new Document();
        document.Info.Title = $"Maldives State Budget Execution Statement {summary.FiscalYear}";
        document.Info.Subject = "Public Finance Act (Law 3/2006) Budget Execution & Payroll Reconciliation";
        document.Info.Author = "Direct HR (DHR) Public Finance System";

        var style = document.Styles["Normal"] ?? document.Styles.AddStyle("Normal", "Normal");
        style.Font.Name = "Arial";
        style.Font.Size = 8;
        style.Font.Color = Colors.DarkSlateGray;

        var section = document.AddSection();
        section.PageSetup.Orientation = Orientation.Landscape;
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.TopMargin = Unit.FromCentimeter(1.2);
        section.PageSetup.BottomMargin = Unit.FromCentimeter(1.2);
        section.PageSetup.LeftMargin = Unit.FromCentimeter(1.2);
        section.PageSetup.RightMargin = Unit.FromCentimeter(1.2);

        // Header
        var titlePara = section.AddParagraph();
        titlePara.Format.Alignment = ParagraphAlignment.Center;
        titlePara.Format.SpaceAfter = Unit.FromPoint(2);
        var r1 = titlePara.AddFormattedText("GOVERNMENT OF THE REPUBLIC OF MALDIVES", TextFormat.Bold);
        r1.Font.Size = 12;
        r1.Font.Color = new Color(15, 52, 96);

        var subPara = section.AddParagraph();
        subPara.Format.Alignment = ParagraphAlignment.Center;
        subPara.Format.SpaceAfter = Unit.FromPoint(4);
        var r2 = subPara.AddFormattedText($"{summary.OrganisationName.ToUpperInvariant()}", TextFormat.Bold);
        r2.Font.Size = 10;
        r2.Font.Color = new Color(22, 33, 62);

        var docTitle = section.AddParagraph();
        docTitle.Format.Alignment = ParagraphAlignment.Center;
        docTitle.Format.SpaceAfter = Unit.FromPoint(6);
        var r3 = docTitle.AddFormattedText($"BUDGET EXECUTION & PAYROLL RECONCILIATION STATEMENT — FISCAL YEAR {summary.FiscalYear}", TextFormat.Bold);
        r3.Font.Size = 9.5;
        r3.Font.Color = new Color(31, 64, 104);

        var legalPara = section.AddParagraph();
        legalPara.Format.Alignment = ParagraphAlignment.Center;
        legalPara.Format.SpaceAfter = Unit.FromPoint(12);
        var r4 = legalPara.AddFormattedText("Issued under the Maldives Public Finance Act (Law No. 3/2006), Public Finance Regulations & Ministry of Finance Chart of Accounts", TextFormat.Italic);
        r4.Font.Size = 7.5;
        r4.Font.Color = Colors.Gray;

        // Financial Metrics Banner
        var bannerTable = section.AddTable();
        bannerTable.Borders.Color = new Color(226, 232, 240);
        bannerTable.Borders.Width = 0.5;
        bannerTable.Rows.LeftIndent = 0;

        for (int i = 0; i < 4; i++)
        {
            var col = bannerTable.AddColumn(Unit.FromCentimeter(6.8));
            col.Format.Alignment = ParagraphAlignment.Center;
        }

        var bannerRow = bannerTable.AddRow();
        bannerRow.Shading.Color = new Color(248, 250, 252);

        var c0 = bannerRow.Cells[0];
        c0.AddParagraph("Total Approved Budget").Format.Font.Bold = true;
        c0.AddParagraph($"MVR {summary.TotalApprovedBudget:N2}").Format.Font.Size = 9;

        var c1 = bannerRow.Cells[1];
        c1.AddParagraph("Revised Budget (Net Virements)").Format.Font.Bold = true;
        c1.AddParagraph($"MVR {summary.TotalRevisedBudget:N2}").Format.Font.Size = 9;

        var c2 = bannerRow.Cells[2];
        c2.AddParagraph("Expended & Payroll Emoluments").Format.Font.Bold = true;
        c2.AddParagraph($"MVR {summary.TotalExpended:N2} ({summary.OverallBurnRatePercentage:F1}%)").Format.Font.Size = 9;

        var c3 = bannerRow.Cells[3];
        c3.AddParagraph("Available Unencumbered Balance").Format.Font.Bold = true;
        c3.AddParagraph($"MVR {summary.TotalAvailableBalance:N2}").Format.Font.Size = 9;

        section.AddParagraph().Format.SpaceAfter = Unit.FromPoint(10);

        // Main Breakdown Table
        var table = section.AddTable();
        table.Borders.Color = new Color(203, 213, 225);
        table.Borders.Width = 0.5;

        // Widths total: 27.2 cm (fits landscape A4 29.7 - 2.4 = 27.3cm)
        table.AddColumn(Unit.FromCentimeter(2.0)); // MoF Code
        table.AddColumn(Unit.FromCentimeter(6.2)); // Line Item Description
        table.AddColumn(Unit.FromCentimeter(3.0)); // Approved
        table.AddColumn(Unit.FromCentimeter(2.6)); // Net Virements
        table.AddColumn(Unit.FromCentimeter(3.0)); // Revised
        table.AddColumn(Unit.FromCentimeter(2.8)); // Committed
        table.AddColumn(Unit.FromCentimeter(3.0)); // Expended
        table.AddColumn(Unit.FromCentimeter(3.0)); // Available Balance
        table.AddColumn(Unit.FromCentimeter(1.6)); // % Util

        var headerRow = table.AddRow();
        headerRow.HeadingFormat = true;
        headerRow.Shading.Color = new Color(15, 52, 96);
        headerRow.Format.Font.Bold = true;
        headerRow.Format.Font.Color = Colors.White;
        headerRow.Format.Font.Size = 7.5;
        headerRow.Format.Alignment = ParagraphAlignment.Center;

        headerRow.Cells[0].AddParagraph("MoF Code");
        headerRow.Cells[1].AddParagraph("Classification / Economic Line Item");
        headerRow.Cells[2].AddParagraph("Approved (MVR)");
        headerRow.Cells[3].AddParagraph("Net Virement");
        headerRow.Cells[4].AddParagraph("Revised (MVR)");
        headerRow.Cells[5].AddParagraph("Committed");
        headerRow.Cells[6].AddParagraph("Expended (MVR)");
        headerRow.Cells[7].AddParagraph("Available (MVR)");
        headerRow.Cells[8].AddParagraph("% Util");

        int rowIndex = 0;
        foreach (var item in summary.Allocations)
        {
            var row = table.AddRow();
            row.Format.Font.Size = 7.5;
            row.Shading.Color = rowIndex % 2 == 1 ? new Color(248, 250, 252) : Colors.White;

            row.Cells[0].AddParagraph(item.EconomicCode).Format.Alignment = ParagraphAlignment.Center;
            
            var descCell = row.Cells[1];
            descCell.AddParagraph(string.IsNullOrWhiteSpace(item.EnglishName) ? item.DhivehiName : $"{item.EnglishName} ({item.DhivehiName})");

            row.Cells[2].AddParagraph(item.ApprovedAmount.ToString("N2")).Format.Alignment = ParagraphAlignment.Right;
            row.Cells[3].AddParagraph(item.NetVirement >= 0 ? $"+{item.NetVirement:N2}" : item.NetVirement.ToString("N2")).Format.Alignment = ParagraphAlignment.Right;
            row.Cells[4].AddParagraph(item.CurrentBudget.ToString("N2")).Format.Alignment = ParagraphAlignment.Right;
            row.Cells[5].AddParagraph(item.CommittedAmount.ToString("N2")).Format.Alignment = ParagraphAlignment.Right;
            row.Cells[6].AddParagraph(item.ExpendedAmount.ToString("N2")).Format.Alignment = ParagraphAlignment.Right;
            
            var availCell = row.Cells[7];
            availCell.AddParagraph(item.AvailableBalance.ToString("N2"));
            availCell.Format.Alignment = ParagraphAlignment.Right;
            if (item.AvailableBalance < 0) availCell.Format.Font.Color = Colors.Red;

            row.Cells[8].AddParagraph($"{item.UtilizationPercentage:F1}%").Format.Alignment = ParagraphAlignment.Center;

            rowIndex++;
        }

        // Totals Row
        var totalRow = table.AddRow();
        totalRow.Format.Font.Bold = true;
        totalRow.Format.Font.Size = 8;
        totalRow.Shading.Color = new Color(241, 245, 249);

        totalRow.Cells[0].AddParagraph("TOTAL");
        totalRow.Cells[0].MergeRight = 1;
        totalRow.Cells[0].Format.Alignment = ParagraphAlignment.Center;

        totalRow.Cells[2].AddParagraph(summary.TotalApprovedBudget.ToString("N2")).Format.Alignment = ParagraphAlignment.Right;
        var totalNetVir = summary.TotalVirementsIn - summary.TotalVirementsOut;
        totalRow.Cells[3].AddParagraph(totalNetVir >= 0 ? $"+{totalNetVir:N2}" : totalNetVir.ToString("N2")).Format.Alignment = ParagraphAlignment.Right;
        totalRow.Cells[4].AddParagraph(summary.TotalRevisedBudget.ToString("N2")).Format.Alignment = ParagraphAlignment.Right;
        totalRow.Cells[5].AddParagraph(summary.TotalCommitted.ToString("N2")).Format.Alignment = ParagraphAlignment.Right;
        totalRow.Cells[6].AddParagraph(summary.TotalExpended.ToString("N2")).Format.Alignment = ParagraphAlignment.Right;
        totalRow.Cells[7].AddParagraph(summary.TotalAvailableBalance.ToString("N2")).Format.Alignment = ParagraphAlignment.Right;
        totalRow.Cells[8].AddParagraph($"{summary.OverallBurnRatePercentage:F1}%").Format.Alignment = ParagraphAlignment.Center;

        // Statutory 3-Tier Sign-Off Block (Maldives Public Finance Regulations)
        section.AddParagraph().Format.SpaceAfter = Unit.FromPoint(16);

        var sigTable = section.AddTable();
        sigTable.Rows.LeftIndent = 0;
        sigTable.Borders.Color = new Color(203, 213, 225);
        sigTable.Borders.Width = 0.5;

        for (int i = 0; i < 3; i++)
        {
            var col = sigTable.AddColumn(Unit.FromCentimeter(9.0));
            col.Format.Alignment = ParagraphAlignment.Center;
        }

        var sigHeaderRow = sigTable.AddRow();
        sigHeaderRow.Shading.Color = new Color(248, 250, 252);
        sigHeaderRow.Format.Font.Bold = true;
        sigHeaderRow.Format.Font.Size = 7.5;

        sigHeaderRow.Cells[0].AddParagraph("PREPARED BY (FINANCE OFFICER)");
        sigHeaderRow.Cells[1].AddParagraph("VERIFIED BY (HEAD OF FINANCE)");
        sigHeaderRow.Cells[2].AddParagraph("APPROVED BY (RESPONSIBLE FINANCIAL OFFICER)");

        var sigBodyRow = sigTable.AddRow();
        sigBodyRow.Height = Unit.FromCentimeter(1.6);
        sigBodyRow.Format.Font.Size = 7;
        sigBodyRow.Format.Alignment = ParagraphAlignment.Left;

        sigBodyRow.Cells[0].AddParagraph("\nSignature: _______________________\nName:\nDate:");
        sigBodyRow.Cells[1].AddParagraph("\nSignature: _______________________\nName:\nDate:");
        sigBodyRow.Cells[2].AddParagraph("\nSignature: _______________________\nName:\nDesignation:\nDate:");

        // Footer
        var footer = section.Footers.Primary.AddParagraph();
        footer.Format.Alignment = ParagraphAlignment.Right;
        footer.Format.Font.Size = 7;
        footer.Format.Font.Color = Colors.Gray;
        footer.AddText("DHR Official Financial Statement | Bandeyri SAP Reconciled | Page ");
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
}
