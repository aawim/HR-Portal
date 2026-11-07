using System;
using System.IO;
using HRM.DTOs.Certificates;
using HRM.Helpers;
using MigraDoc.DocumentObjectModel;
using MigraDoc.DocumentObjectModel.Shapes;
using MigraDoc.DocumentObjectModel.Tables;
using MigraDoc.Rendering;
using PdfSharp.Fonts;

namespace HRM.Services;

public static class CertificatePdfGenerator
{
    public static byte[] Generate(CertificateSnapshotDto snapshot, string fontPath)
    {
        OrganisationStaffExport.EnsureReportFont(fontPath);

        var document = new Document();
        document.Info.Title = $"{snapshot.CertificateTitle} - {snapshot.CertificateNumber}";
        document.Info.Subject = $"Official Employment Certificate for {snapshot.FullName}";
        document.Info.Author = snapshot.OrganisationName;

        var normalStyle = document.Styles[StyleNames.Normal] ?? throw new InvalidOperationException("Normal style unavailable.");
        normalStyle.Font.Name = "StaffReport";
        normalStyle.Font.Size = 9.5;
        normalStyle.Font.Color = new Color(30, 41, 59); // slate-800

        var section = document.AddSection();
        section.PageSetup.PageFormat = PageFormat.A4;
        section.PageSetup.Orientation = Orientation.Portrait;
        section.PageSetup.TopMargin = Unit.FromCentimeter(1.6);
        section.PageSetup.BottomMargin = Unit.FromCentimeter(1.6);
        section.PageSetup.LeftMargin = Unit.FromCentimeter(1.8);
        section.PageSetup.RightMargin = Unit.FromCentimeter(1.8);

        // ==========================================
        // 1. ORGANISATION LETTERHEAD
        // ==========================================
        var letterhead = section.AddParagraph();
        letterhead.Format.Alignment = ParagraphAlignment.Center;
        letterhead.Format.SpaceAfter = Unit.FromPoint(2);

        var govText = letterhead.AddFormattedText("REPUBLIC OF MALDIVES\n", TextFormat.Bold);
        govText.Font.Size = 10;
        govText.Font.Color = new Color(15, 23, 42); // slate-900

        var orgText = letterhead.AddFormattedText($"{snapshot.OrganisationName.ToUpperInvariant()}\n", TextFormat.Bold);
        orgText.Font.Size = 13;
        orgText.Font.Color = new Color(15, 118, 110); // teal-700

        var deptText = letterhead.AddFormattedText("Human Resources Management Directorate • Official Personnel Records", TextFormat.NotBold);
        deptText.Font.Size = 8;
        deptText.Font.Color = new Color(100, 116, 139); // slate-500

        // Divider rule
        var rule = section.AddTable();
        rule.Borders.Bottom.Width = 1.2;
        rule.Borders.Bottom.Color = new Color(15, 118, 110); // teal-700
        rule.AddColumn(Unit.FromCentimeter(17.4));
        var rRow = rule.AddRow();
        rRow.TopPadding = Unit.FromPoint(2);
        rRow.BottomPadding = Unit.FromPoint(4);

        // ==========================================
        // 2. CERTIFICATE TITLE & METADATA TABLE
        // ==========================================
        var titlePara = section.AddParagraph();
        titlePara.Format.Alignment = ParagraphAlignment.Center;
        titlePara.Format.SpaceBefore = Unit.FromPoint(12);
        titlePara.Format.SpaceAfter = Unit.FromPoint(8);

        var titleFormatted = titlePara.AddFormattedText(snapshot.CertificateTitle, TextFormat.Bold);
        titleFormatted.Font.Size = 14;
        titleFormatted.Font.Color = new Color(15, 23, 42);

        // Metadata grid (Ref No / Date)
        var metaTable = section.AddTable();
        metaTable.Format.SpaceAfter = Unit.FromPoint(12);
        metaTable.AddColumn(Unit.FromCentimeter(8.7));
        metaTable.AddColumn(Unit.FromCentimeter(8.7));
        var metaRow = metaTable.AddRow();
        metaRow.TopPadding = metaRow.BottomPadding = Unit.FromPoint(2);

        var refPara = metaRow.Cells[0].AddParagraph();
        refPara.AddFormattedText("Certificate Ref: ", TextFormat.Bold);
        refPara.AddText(snapshot.CertificateNumber);

        var datePara = metaRow.Cells[1].AddParagraph();
        datePara.Format.Alignment = ParagraphAlignment.Right;
        datePara.AddFormattedText("Date of Issue: ", TextFormat.Bold);
        datePara.AddText(snapshot.IssueDate.ToString("dd MMMM yyyy"));

        // Addressed to
        var addresseePara = section.AddParagraph();
        addresseePara.Format.SpaceBefore = Unit.FromPoint(4);
        addresseePara.Format.SpaceAfter = Unit.FromPoint(8);
        var addrText = addresseePara.AddFormattedText(snapshot.AddressedTo, TextFormat.Bold);
        addrText.Font.Size = 10;
        addrText.Font.Color = new Color(30, 41, 59);

        // ==========================================
        // 3. NARRATIVE CERTIFICATION TEXT
        // ==========================================
        var introPara = section.AddParagraph();
        introPara.Format.Alignment = ParagraphAlignment.Justify;
        introPara.Format.LineSpacing = Unit.FromPoint(13);
        introPara.Format.SpaceAfter = Unit.FromPoint(8);
        introPara.AddText(snapshot.IntroductoryText);

        var servicePara = section.AddParagraph();
        servicePara.Format.Alignment = ParagraphAlignment.Justify;
        servicePara.Format.LineSpacing = Unit.FromPoint(13);
        servicePara.Format.SpaceAfter = Unit.FromPoint(10);
        servicePara.AddText(snapshot.ServiceStatement);

        // ==========================================
        // 4. SUMMARY SERVICE ATTRIBUTES TABLE
        // ==========================================
        var summaryTable = section.AddTable();
        summaryTable.Borders.Width = 0.5;
        summaryTable.Borders.Color = new Color(226, 232, 240); // slate-200
        summaryTable.Format.SpaceAfter = Unit.FromPoint(12);

        summaryTable.AddColumn(Unit.FromCentimeter(4.35));
        summaryTable.AddColumn(Unit.FromCentimeter(4.35));
        summaryTable.AddColumn(Unit.FromCentimeter(4.35));
        summaryTable.AddColumn(Unit.FromCentimeter(4.35));

        // Header Row
        var sHeader = summaryTable.AddRow();
        sHeader.HeadingFormat = true;
        sHeader.Shading.Color = new Color(241, 245, 249); // slate-100
        sHeader.TopPadding = sHeader.BottomPadding = Unit.FromPoint(4);

        AddCell(sHeader.Cells[0], "Employee ID", isBold: true);
        AddCell(sHeader.Cells[1], "Substantive Title", isBold: true);
        AddCell(sHeader.Cells[2], "Department / Unit", isBold: true);
        AddCell(sHeader.Cells[3], "Service Tenure", isBold: true);

        // Value Row
        var sVal = summaryTable.AddRow();
        sVal.TopPadding = sVal.BottomPadding = Unit.FromPoint(4);
        AddCell(sVal.Cells[0], snapshot.EmployeeNumber);
        AddCell(sVal.Cells[1], snapshot.PrimaryDesignation);
        AddCell(sVal.Cells[2], snapshot.PrimaryDepartment);
        AddCell(sVal.Cells[3], snapshot.FormattedDuration);

        if (snapshot.BasicSalary.HasValue)
        {
            var salRow = summaryTable.AddRow();
            salRow.TopPadding = salRow.BottomPadding = Unit.FromPoint(3);
            AddCell(salRow.Cells[0], "Verified Basic Salary:", isBold: true);
            AddCell(salRow.Cells[1], $"MVR {snapshot.BasicSalary.Value:N2}");
            AddCell(salRow.Cells[2], "Employment Status:", isBold: true);
            AddCell(salRow.Cells[3], snapshot.IsCurrentlyEmployed ? "Active in Service" : "Concluded");
        }

        // ==========================================
        // 5. DETAILED POSITION & RESPONSIBILITY HISTORY
        // ==========================================
        if (snapshot.Positions.Count > 0)
        {
            var posHeading = section.AddParagraph();
            posHeading.Format.SpaceBefore = Unit.FromPoint(8);
            posHeading.Format.SpaceAfter = Unit.FromPoint(4);
            var h1 = posHeading.AddFormattedText("VERIFIED POSITION & RESPONSIBILITY RECORD", TextFormat.Bold);
            h1.Font.Size = 9.5;
            h1.Font.Color = new Color(15, 118, 110);

            var posTable = section.AddTable();
            posTable.Borders.Width = 0.5;
            posTable.Borders.Color = new Color(226, 232, 240);
            posTable.Format.SpaceAfter = Unit.FromPoint(10);

            posTable.AddColumn(Unit.FromCentimeter(5.5)); // Title & Dept
            posTable.AddColumn(Unit.FromCentimeter(4.0)); // Period
            posTable.AddColumn(Unit.FromCentimeter(7.9)); // Official Duties

            var pHeader = posTable.AddRow();
            pHeader.HeadingFormat = true;
            pHeader.Shading.Color = new Color(241, 245, 249);
            pHeader.TopPadding = pHeader.BottomPadding = Unit.FromPoint(3);
            AddCell(pHeader.Cells[0], "Position & Structure", isBold: true);
            AddCell(pHeader.Cells[1], "Effective Period", isBold: true);
            AddCell(pHeader.Cells[2], "Key Official Responsibilities", isBold: true);

            foreach (var pos in snapshot.Positions)
            {
                var row = posTable.AddRow();
                row.TopPadding = row.BottomPadding = Unit.FromPoint(3);

                var c0 = row.Cells[0].AddParagraph();
                c0.AddFormattedText(pos.Title, TextFormat.Bold);
                c0.AddText($"\n{pos.Department}");

                var c1 = row.Cells[1].AddParagraph();
                c1.AddText(pos.PeriodText);

                var c2 = row.Cells[2].AddParagraph();
                if (pos.Responsibilities.Count > 0)
                {
                    foreach (var resp in pos.Responsibilities)
                    {
                        c2.AddText($"• {resp}\n");
                    }
                }
                else
                {
                    c2.AddText("Official civil service responsibilities verified per position framework.");
                }
            }
        }

        // ==========================================
        // 6. DETAILED PROJECT PARTICIPATION RECORD
        // ==========================================
        if (snapshot.Projects.Count > 0)
        {
            var prjHeading = section.AddParagraph();
            prjHeading.Format.SpaceBefore = Unit.FromPoint(8);
            prjHeading.Format.SpaceAfter = Unit.FromPoint(4);
            var h2 = prjHeading.AddFormattedText("VERIFIED PROJECT ASSIGNMENTS & INITIATIVES", TextFormat.Bold);
            h2.Font.Size = 9.5;
            h2.Font.Color = new Color(15, 118, 110);

            var prjTable = section.AddTable();
            prjTable.Borders.Width = 0.5;
            prjTable.Borders.Color = new Color(226, 232, 240);
            prjTable.Format.SpaceAfter = Unit.FromPoint(10);

            prjTable.AddColumn(Unit.FromCentimeter(6.5));
            prjTable.AddColumn(Unit.FromCentimeter(4.5));
            prjTable.AddColumn(Unit.FromCentimeter(6.4));

            var prHeader = prjTable.AddRow();
            prHeader.HeadingFormat = true;
            prHeader.Shading.Color = new Color(241, 245, 249);
            prHeader.TopPadding = prHeader.BottomPadding = Unit.FromPoint(3);
            AddCell(prHeader.Cells[0], "Project / Initiative", isBold: true);
            AddCell(prHeader.Cells[1], "Assigned Role", isBold: true);
            AddCell(prHeader.Cells[2], "Participation Period & Scope", isBold: true);

            foreach (var prj in snapshot.Projects)
            {
                var r = prjTable.AddRow();
                r.TopPadding = r.BottomPadding = Unit.FromPoint(3);
                AddCell(r.Cells[0], $"{prj.ProjectName} ({prj.Reference})");
                AddCell(r.Cells[1], prj.Role);
                AddCell(r.Cells[2], $"{prj.PeriodText}{(string.IsNullOrWhiteSpace(prj.Remarks) ? "" : $" • {prj.Remarks}")}");
            }
        }

        // Concluding remarks
        var concPara = section.AddParagraph();
        concPara.Format.Alignment = ParagraphAlignment.Justify;
        concPara.Format.LineSpacing = Unit.FromPoint(13);
        concPara.Format.SpaceAfter = Unit.FromPoint(16);
        concPara.AddText(snapshot.ConcludingText);

        // ==========================================
        // 7. SIGNATORY & QR VERIFICATION BLOCK
        // ==========================================
        var signTable = section.AddTable();
        signTable.Format.SpaceAfter = Unit.FromPoint(12);
        signTable.AddColumn(Unit.FromCentimeter(11.0));
        signTable.AddColumn(Unit.FromCentimeter(6.4));

        var signRow = signTable.AddRow();
        signRow.TopPadding = signRow.BottomPadding = Unit.FromPoint(4);

        // Left Cell: Authorised Signatory details
        var signCell = signRow.Cells[0].AddParagraph();
        signCell.Format.LineSpacing = Unit.FromPoint(13);
        signCell.AddFormattedText("AUTHORISED SIGNATORY:\n", TextFormat.Bold);
        signCell.AddFormattedText(snapshot.SignatoryName + "\n", TextFormat.Bold);
        signCell.AddText($"{snapshot.SignatoryTitle}\n");
        signCell.AddText($"{snapshot.OrganisationName}\n");
        signCell.AddFormattedText("[OFFICIAL HUMAN RESOURCE SEAL & DIGITAL ISSUANCE]", TextFormat.Italic);
        signCell.Format.Font.Size = 8.5;

        // Right Cell: QR Verification Code
        string? tempQrPath = null;
        try
        {
            var verifyUrl = $"https://hrm.gov.mv/verify/certificate/{snapshot.VerificationToken}";
            var qrMatrix = QrCodeGenerator.GenerateMatrix(verifyUrl);
            var bmpBytes = QrCodeGenerator.ToBmp(qrMatrix, pixelSize: 4, quietZone: 2);

            tempQrPath = Path.Combine(Path.GetTempPath(), $"cert_qr_{Guid.NewGuid():N}.bmp");
            File.WriteAllBytes(tempQrPath, bmpBytes);

            var qrPara = signRow.Cells[1].AddParagraph();
            qrPara.Format.Alignment = ParagraphAlignment.Right;
            var qrImg = qrPara.AddImage(tempQrPath);
            qrImg.Width = Unit.FromCentimeter(2.5);
            qrImg.Height = Unit.FromCentimeter(2.5);

            var tokenPara = signRow.Cells[1].AddParagraph();
            tokenPara.Format.Alignment = ParagraphAlignment.Right;
            tokenPara.Format.Font.Size = 7.5;
            tokenPara.Format.Font.Color = new Color(100, 116, 139);
            tokenPara.AddText($"Verify Online:\nCode: {snapshot.VerificationToken[..Math.Min(12, snapshot.VerificationToken.Length)]}...");
        }
        catch
        {
            var fallback = signRow.Cells[1].AddParagraph();
            fallback.Format.Alignment = ParagraphAlignment.Right;
            fallback.AddText($"Verification Token:\n{snapshot.VerificationToken}");
        }

        // ==========================================
        // 8. SECURITY & INTEGRITY FOOTER
        // ==========================================
        var footer = section.Footers.Primary.AddParagraph();
        footer.Format.Alignment = ParagraphAlignment.Center;
        footer.Format.Font.Size = 7.5;
        footer.Format.Font.Color = new Color(100, 116, 139);
        footer.AddText($"Document Reference: {snapshot.CertificateNumber} • SHA-256 Hash: {snapshot.Sha256Hash[..Math.Min(24, snapshot.Sha256Hash.Length)]}... • Generated via Digital HR Platform\n");
        footer.AddText("This is an official government credential. Verification is publicly accessible at https://hrm.gov.mv/verify/certificate");

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

    private static void AddCell(Cell cell, string text, bool isBold = false)
    {
        var p = cell.AddParagraph();
        if (isBold) p.AddFormattedText(text, TextFormat.Bold);
        else p.AddText(text);
    }
}
