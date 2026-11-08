using System;
using System.IO;
using HRM.DTOs.Documents;
using HRM.Helpers;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;

namespace HRM.Services;

public static class DmsDocumentPdfGenerator
{
    public static byte[] Generate(DmsTemplatePreviewDto preview, string fontPath)
    {
        OrganisationStaffExport.EnsureReportFont(fontPath);

        var document = new Document();
        document.Info.Title = $"{preview.ResolvedTitle} - {preview.DocumentNumber}";
        document.Info.Subject = $"Official Document for {preview.EmployeeFullName}";
        document.Info.Author = preview.OrganisationName;

        var normalStyle = document.Styles[StyleNames.Normal] ?? throw new InvalidOperationException("Normal style unavailable.");
        normalStyle.Font.Name = "StaffReport";
        normalStyle.Font.Size = 10;
        normalStyle.Font.Color = new Color(30, 41, 59); // slate-800

        var section = document.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.Orientation = Orientation.Portrait;
        section.PageSetup.TopMargin = Unit.FromCentimeter(1.8);
        section.PageSetup.BottomMargin = Unit.FromCentimeter(1.8);
        section.PageSetup.LeftMargin = Unit.FromCentimeter(2.0);
        section.PageSetup.RightMargin = Unit.FromCentimeter(2.0);

        if (preview.IncludeOrganisationLetterhead)
        {
            var letterhead = section.AddParagraph();
            letterhead.Format.Alignment = ParagraphAlignment.Center;
            letterhead.Format.SpaceAfter = Unit.FromPoint(4);

            var govText = letterhead.AddFormattedText("REPUBLIC OF MALDIVES\n", TextFormat.Bold);
            govText.Font.Size = 10;
            govText.Font.Color = new Color(15, 23, 42);

            var orgText = letterhead.AddFormattedText($"{preview.OrganisationName.ToUpperInvariant()}\n", TextFormat.Bold);
            orgText.Font.Size = 13;
            orgText.Font.Color = new Color(37, 99, 235); // blue-600

            var subText = letterhead.AddFormattedText("Enterprise Document & Records Management System (DHR-DMS)", TextFormat.NotBold);
            subText.Font.Size = 8;
            subText.Font.Color = new Color(100, 116, 139);

            var rule = section.AddTable();
            rule.Borders.Bottom.Width = 1.2;
            rule.Borders.Bottom.Color = new Color(37, 99, 235);
            rule.AddColumn(Unit.FromCentimeter(17.0));
            var rRow = rule.AddRow();
            rRow.TopPadding = Unit.FromPoint(2);
            rRow.BottomPadding = Unit.FromPoint(4);
        }

        // Document Header & Reference Table
        var metaTable = section.AddTable();
        metaTable.Format.SpaceBefore = Unit.FromPoint(10);
        metaTable.Format.SpaceAfter = Unit.FromPoint(14);
        metaTable.AddColumn(Unit.FromCentimeter(8.5));
        metaTable.AddColumn(Unit.FromCentimeter(8.5));
        var metaRow = metaTable.AddRow();

        var refPara = metaRow.Cells[0].AddParagraph();
        refPara.AddFormattedText("Document No: ", TextFormat.Bold);
        refPara.AddText(preview.DocumentNumber);

        var datePara = metaRow.Cells[1].AddParagraph();
        datePara.Format.Alignment = ParagraphAlignment.Right;
        datePara.AddFormattedText("Issue Date: ", TextFormat.Bold);
        datePara.AddText(preview.IssueDateFormatted);

        // Heading
        var titlePara = section.AddParagraph();
        titlePara.Format.Alignment = ParagraphAlignment.Center;
        titlePara.Format.SpaceBefore = Unit.FromPoint(8);
        titlePara.Format.SpaceAfter = Unit.FromPoint(14);
        var headingText = string.IsNullOrWhiteSpace(preview.HeaderText) ? preview.ResolvedTitle : preview.HeaderText;
        var titleFormatted = titlePara.AddFormattedText(headingText.ToUpperInvariant(), TextFormat.Bold);
        titleFormatted.Font.Size = 13;
        titleFormatted.Font.Color = new Color(15, 23, 42);

        // Body paragraphs
        var paragraphs = (preview.ResolvedBodyText ?? string.Empty)
            .Split(["\r\n\r\n", "\n\n"], StringSplitOptions.RemoveEmptyEntries);

        foreach (var block in paragraphs)
        {
            var bodyPara = section.AddParagraph();
            bodyPara.Format.Alignment = ParagraphAlignment.Justify;
            bodyPara.Format.LineSpacingRule = LineSpacingRule.Multiple;
            bodyPara.Format.LineSpacing = Unit.FromPoint(1.25);
            bodyPara.Format.SpaceAfter = Unit.FromPoint(10);
            bodyPara.AddText(block.Trim());
        }

        // Optional Verified Position History Table
        if (preview.IncludePositionHistoryTable && preview.PositionHistory.Count > 0)
        {
            var tableLabel = section.AddParagraph();
            tableLabel.Format.SpaceBefore = Unit.FromPoint(8);
            tableLabel.Format.SpaceAfter = Unit.FromPoint(4);
            tableLabel.AddFormattedText("Verified Chronological Position Record:", TextFormat.Bold);

            var posTable = section.AddTable();
            posTable.Borders.Width = 0.5;
            posTable.Borders.Color = new Color(203, 213, 225);
            posTable.Format.SpaceAfter = Unit.FromPoint(14);
            posTable.AddColumn(Unit.FromCentimeter(6.0));
            posTable.AddColumn(Unit.FromCentimeter(5.5));
            posTable.AddColumn(Unit.FromCentimeter(2.75));
            posTable.AddColumn(Unit.FromCentimeter(2.75));

            var hRow = posTable.AddRow();
            hRow.Shading.Color = new Color(241, 245, 249);
            hRow.HeadingFormat = true;
            hRow.TopPadding = hRow.BottomPadding = Unit.FromPoint(4);
            hRow.Cells[0].AddParagraph().AddFormattedText("Designation", TextFormat.Bold);
            hRow.Cells[1].AddParagraph().AddFormattedText("Department", TextFormat.Bold);
            hRow.Cells[2].AddParagraph().AddFormattedText("From", TextFormat.Bold);
            hRow.Cells[3].AddParagraph().AddFormattedText("To", TextFormat.Bold);

            foreach (var pos in preview.PositionHistory)
            {
                var r = posTable.AddRow();
                r.TopPadding = r.BottomPadding = Unit.FromPoint(3);
                r.Cells[0].AddParagraph(pos.PositionTitle);
                r.Cells[1].AddParagraph(pos.DepartmentName);
                r.Cells[2].AddParagraph(pos.StartDate);
                r.Cells[3].AddParagraph(pos.EndDate);
            }
        }

        // Signatory & QR Verification Block
        var signTable = section.AddTable();
        signTable.Format.SpaceBefore = Unit.FromPoint(18);
        signTable.AddColumn(Unit.FromCentimeter(11.0));
        signTable.AddColumn(Unit.FromCentimeter(6.0));
        var signRow = signTable.AddRow();

        var signPara = signRow.Cells[0].AddParagraph();
        signPara.AddFormattedText("AUTHORISED SIGNATORY:\n", TextFormat.Bold);
        signPara.AddFormattedText($"{preview.SignatoryName}\n", TextFormat.Bold);
        signPara.AddText($"{preview.SignatoryTitle}\n{preview.OrganisationName}\n");
        signPara.AddFormattedText("[DIGITALLY APPROVED OFFICIAL RECORD]", TextFormat.Italic);

        string? tempQrPath = null;
        if (preview.IncludeQrVerification && !string.IsNullOrWhiteSpace(preview.VerificationToken))
        {
            try
            {
                var qrMatrix = QrCodeGenerator.GenerateMatrix($"DMS:{preview.DocumentNumber}:{preview.VerificationToken}");
                var bmpBytes = QrCodeGenerator.ToBmp(qrMatrix, pixelSize: 4, quietZone: 2);
                tempQrPath = Path.Combine(Path.GetTempPath(), $"dms_qr_{Guid.NewGuid():N}.bmp");
                File.WriteAllBytes(tempQrPath, bmpBytes);

                var qrPara = signRow.Cells[1].AddParagraph();
                qrPara.Format.Alignment = ParagraphAlignment.Right;
                var img = qrPara.AddImage(tempQrPath);
                img.Width = Unit.FromCentimeter(2.4);
                img.Height = Unit.FromCentimeter(2.4);
            }
            catch
            {
                // Ignore QR image fallback
            }
        }

        var footer = section.Footers.Primary.AddParagraph();
        footer.Format.Alignment = ParagraphAlignment.Center;
        footer.Format.Font.Size = 7.5;
        footer.Format.Font.Color = new Color(100, 116, 139);
        footer.AddText($"{preview.FooterText ?? "Official DHR-DMS Record"} • Ref: {preview.DocumentNumber} • SHA-256: {preview.Sha256Hash[..Math.Min(20, preview.Sha256Hash.Length)]}...");

        var renderer = new PdfDocumentRenderer { Document = document };
        renderer.RenderDocument();

        using var stream = new MemoryStream();
        renderer.PdfDocument.Save(stream, false);
        renderer.PdfDocument.Dispose();

        if (tempQrPath != null && File.Exists(tempQrPath))
        {
            try { File.Delete(tempQrPath); } catch { }
        }

        return stream.ToArray();
    }
}
