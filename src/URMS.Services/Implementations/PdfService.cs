using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.IO.Font.Constants;
using URMS.Models.DTOs;
using URMS.Models.Entities;

namespace URMS.Services.Implementations;

public class PdfService
{
    // ── Colours ───────────────────────────────────────────────────────────────
    private static readonly DeviceRgb NavyBlue = new(30, 58, 95);
    private static readonly DeviceRgb Gold = new(212, 172, 13);
    private static readonly DeviceRgb LightGray = new(208, 208, 208);
    private static readonly DeviceRgb VLightGray = new(245, 245, 245);
    private static readonly DeviceRgb DarkGray = new(85, 85, 85);
    private static readonly DeviceRgb GreenOk = new(21, 87, 36);
    private static readonly DeviceRgb RedFail = new(146, 43, 33);

    // ── Fonts — instance fields, recreated per PDF ────────────────────────────
    private PdfFont _regular = null!;
    private PdfFont _bold = null!;
    private PdfFont _italic = null!;
    private PdfFont _boldItal = null!;

    private void EnsureFonts(PdfDocument pdf)
    {
        _regular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        _bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
        _italic = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);
        _boldItal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLDOBLIQUE);
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  1. INDIVIDUAL STUDENT RESULT CARD
    // ═══════════════════════════════════════════════════════════════════════════
    public byte[] GenerateStudentResultCard(StudentResultCardData data)
    {
        using var ms = new MemoryStream();
        using var writer = new PdfWriter(ms);
        using var pdf = new PdfDocument(writer);
        using var doc = new Document(pdf, PageSize.A4);

        EnsureFonts(pdf);
        doc.SetMargins(28, 45, 28, 45);

        var hdrTbl = new Table(new float[] { 55, 370, 55 }).UseAllAvailableWidth();

        // FIX 1: Removed duplicate 'istCell' declaration (was declared twice).
        // FIX 2: Replaced ambiguous 'Path' with 'System.IO.Path' explicitly.

        // LEFT — IST logo
        var istCell = new Cell().SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.MIDDLE);
        var istPath = new[] { "istlogo.jpg", "istlogo.png", "istlogo.jpeg" }
            .Select(f => System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", f))
            .FirstOrDefault(System.IO.File.Exists);
        if (istPath != null)
            istCell.Add(new iText.Layout.Element.Image(iText.IO.Image.ImageDataFactory.Create(istPath)).SetWidth(50).SetHeight(50));
        else
            istCell.Add(new Paragraph("IST").SetFont(_bold).SetFontSize(7));
        hdrTbl.AddCell(istCell);

        // CENTER — institute name
        var centre = new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.CENTER);
        centre.Add(new Paragraph("Dr. A. Q. Khan Institute of Computer Sciences &\nInformation Technology")
            .SetFont(_bold).SetFontSize(13).SetMultipliedLeading(1.2f));
        centre.Add(new Paragraph("Campus of Institute of Space Technology, Islamabad")
            .SetFont(_bold).SetFontSize(11).SetMultipliedLeading(1.2f));
        centre.Add(new Paragraph("KRL Kahuta, Distt. Rawalpindi, Pakistan.  Tel +92.51.9285059, Fax +92.51.9285245")
            .SetFont(_regular).SetFontSize(8).SetFontColor(DarkGray));
        centre.Add(new Paragraph("www.kicsit.edu.pk")
            .SetFont(_regular).SetFontSize(8).SetFontColor(ColorConstants.BLUE).SetUnderline());
        hdrTbl.AddCell(centre);

        // RIGHT — KICSIT logo
        // FIX 2 (same): Replaced ambiguous 'Path' with 'System.IO.Path' explicitly.
        var kicsitCell = new Cell().SetBorder(Border.NO_BORDER).SetVerticalAlignment(VerticalAlignment.MIDDLE).SetTextAlignment(TextAlignment.RIGHT);
        var kicsitPath = new[] { "kicsitlogo.jpg", "kicsitlogo.png", "kicsitlogo.jpeg" }
            .Select(f => System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", f))
            .FirstOrDefault(System.IO.File.Exists);
        if (kicsitPath != null)
            kicsitCell.Add(new iText.Layout.Element.Image(iText.IO.Image.ImageDataFactory.Create(kicsitPath)).SetWidth(50).SetHeight(50));
        else
            kicsitCell.Add(new Paragraph("KICSIT").SetFont(_bold).SetFontSize(7));
        hdrTbl.AddCell(kicsitCell);

        doc.Add(hdrTbl);

        doc.Add(new LineSeparator(new iText.Kernel.Pdf.Canvas.Draw.SolidLine(1.2f)).SetMarginBottom(6));

        doc.Add(Para("To:", 10));
        doc.Add(Para($"      {data.ParentName}", 10, bold: true).SetMarginBottom(4));
        doc.Add(Para($"Subject:      {data.ResultTitle}", 10, bold: true, underline: true).SetMarginBottom(6));
        doc.Add(Para("Dear Parents/Guardians", 10).SetMarginBottom(3));
        doc.Add(new Paragraph()
            .Add(new Text("1.   Your son/daughter/ward ").SetFont(_regular).SetFontSize(10))
            .Add(new Text(data.StudentName).SetFont(_bold).SetFontSize(10).SetUnderline())
            .Add(new Text("  Registration No. ").SetFont(_regular).SetFontSize(10))
            .Add(new Text(data.RegNo).SetFont(_bold).SetFontSize(10).SetUnderline())
            .Add(new Text("  of Class ").SetFont(_regular).SetFontSize(10))
            .Add(new Text(data.ClassName).SetFont(_bold).SetFontSize(10).SetUnderline())
            .Add(new Text("  has scored the following grades in the ").SetFont(_regular).SetFontSize(10))
            .Add(new Text(data.SemesterLabel).SetFont(_regular).SetFontSize(10).SetUnderline())
            .Add(new Text("  exams.").SetFont(_regular).SetFontSize(10))
            .SetMarginBottom(8));

        float pageW = PageSize.A4.GetWidth() - 90;
        float[] cw = { 28, 68, pageW - 28 - 68 - 54 - 46, 54, 46 };
        var tbl = new Table(cw).UseAllAvailableWidth().SetMarginBottom(0);

        string[] headers = { "S.\nNo.", "Course\nCode", "Course Name", "Credit\nHour", "Grade" };
        foreach (var h in headers)
            tbl.AddHeaderCell(HeaderCell(h));

        for (int i = 0; i < data.Subjects.Count; i++)
        {
            var s = data.Subjects[i];
            var bg = i % 2 == 1 ? VLightGray : null;
            tbl.AddCell(DataCell((i + 1).ToString(), CENTER: true, bg: bg));
            tbl.AddCell(DataCell(s.Code, CENTER: true, bg: bg));
            tbl.AddCell(DataCell(s.Name, CENTER: false, bg: bg));
            tbl.AddCell(DataCell(s.CreditHours.ToString(), CENTER: true, bg: bg));
            tbl.AddCell(DataCell(s.Grade, CENTER: true, bg: bg));
        }

        tbl.AddCell(new Cell(1, 2).SetBorder(Border.NO_BORDER));
        tbl.AddCell(new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT)
            .Add(new Paragraph("Semester GPA:").SetFont(_bold).SetFontSize(9))
            .SetPaddingTop(3).SetPaddingBottom(3));
        tbl.AddCell(new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT)
            .Add(new Paragraph(data.SGPA_2dp).SetFont(_bold).SetFontSize(9))
            .SetPaddingTop(3).SetPaddingBottom(3));
        tbl.AddCell(new Cell(1, 1).SetBorder(Border.NO_BORDER)
            .Add(new Paragraph("Out of 4.00").SetFont(_regular).SetFontSize(9))
            .SetPaddingTop(3).SetPaddingBottom(3));

        tbl.AddCell(new Cell(1, 2).SetBorder(Border.NO_BORDER));
        tbl.AddCell(new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT)
            .Add(new Paragraph("Cumulative GPA:").SetFont(_bold).SetFontSize(9))
            .SetPaddingTop(2).SetPaddingBottom(4));
        tbl.AddCell(new Cell(1, 1).SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT)
            .Add(new Paragraph(data.CGPA_2dp).SetFont(_bold).SetFontSize(9))
            .SetPaddingTop(2).SetPaddingBottom(4));
        tbl.AddCell(new Cell(1, 1).SetBorder(Border.NO_BORDER)
            .Add(new Paragraph("Out of 4.00").SetFont(_regular).SetFontSize(9))
            .SetPaddingTop(2).SetPaddingBottom(4));

        doc.Add(tbl);
        doc.Add(new Paragraph("\n").SetFontSize(4));

        doc.Add(Para("2.   His/ Her class position is:", 10).SetMarginBottom(2));

        var posTable = new Table(new float[] { 28, 20, 120, 42, 40, 42 })
            .UseAllAvailableWidth().SetBorder(Border.NO_BORDER);
        posTable.AddCell(NoBorderCell(""));
        posTable.AddCell(NoBorderCell("a."));
        posTable.AddCell(NoBorderCell("Current Semester:"));
        posTable.AddCell(NoBorderCell(data.CurrentPos.ToString(), bold: true));
        posTable.AddCell(NoBorderCell("out of"));
        posTable.AddCell(NoBorderCell(data.TotalStudents.ToString(), bold: true));
        posTable.AddCell(NoBorderCell(""));
        posTable.AddCell(NoBorderCell("b."));
        posTable.AddCell(NoBorderCell("Over All Semester:"));
        posTable.AddCell(NoBorderCell(data.OverallPos.ToString(), bold: true));
        posTable.AddCell(NoBorderCell("out of"));
        posTable.AddCell(NoBorderCell(data.TotalStudents.ToString(), bold: true));
        doc.Add(posTable);

        doc.Add(Para($"3.   Academic Status:       {data.AcademicStatus}", 10, bold: true)
            .SetMarginTop(4).SetMarginBottom(18));

        var sigTbl = new Table(new float[] { 230, 270 }).UseAllAvailableWidth().SetBorder(Border.NO_BORDER);
        sigTbl.AddCell(NoBorderCell(""));
        var sigCell = new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.CENTER);
        sigCell.Add(new Paragraph("_________________________").SetFont(_regular).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER));
        sigCell.Add(new Paragraph(data.ExaminerName).SetFont(_regular).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER));
        sigCell.Add(new Paragraph(data.ExaminerTitle).SetFont(_regular).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER));
        sigTbl.AddCell(sigCell);
        doc.Add(sigTbl);

        doc.Add(new Paragraph("\n").SetFontSize(8));
        doc.Add(new LineSeparator(new iText.Kernel.Pdf.Canvas.Draw.SolidLine(10f)).SetMarginBottom(5));
        doc.Add(new Paragraph("This result is issued subject to the rectification of any error or omission as and when detected.")
            .SetFont(_italic).SetFontSize(8).SetFontColor(DarkGray));

        doc.Close();
        return ms.ToArray();
    }

    // ═══════════════════════════════════════════════════════════════════════════
    //  2. CLASS RESULT SHEET
    // ═══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the generated PDF bytes and the suggested filename.
    /// e.g. filename = "Batch9CSS1ClassResult.pdf"
    /// </summary>
    public (byte[] PdfBytes, string FileName) GenerateClassResultSheet(ClassResultData data, int studentsPerPage)
    {
        var memStream = new MemoryStream();
        var pdfWriter = new PdfWriter(memStream);
        var pdfDocument = new PdfDocument(pdfWriter);
        var document = new Document(pdfDocument, PageSize.A4.Rotate());
        document.SetMargins(20, 20, 20, 20);
        EnsureFonts(pdfDocument);
        AddResultHeader(document, data);
        document.Add(CreateResultTable(data));

        // ── Signature footer ─────────────────────────────────────────────────
        document.Add(new Paragraph("\n").SetFontSize(6));
        document.Add(new LineSeparator(new iText.Kernel.Pdf.Canvas.Draw.SolidLine(0.5f))
            .SetMarginBottom(6));
        var sigTable = new Table(new float[] { 400, 400 })
            .UseAllAvailableWidth().SetBorder(Border.NO_BORDER);
        // Left — JE
        var jeCell = new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.LEFT);
        jeCell.Add(new Paragraph(" ").SetFontSize(8).SetMinHeight(35));
        jeCell.Add(new Paragraph("_______________________________").SetFont(_regular).SetFontSize(8));
        jeCell.Add(new Paragraph(data.JEName).SetFont(_bold).SetFontSize(8));
        jeCell.Add(new Paragraph(data.JETitle).SetFont(_regular).SetFontSize(8));
        jeCell.Add(new Paragraph("Prepared By:").SetFont(_bold).SetFontSize(8));
        sigTable.AddCell(jeCell);
        // Right — Examiner
        var exCell = new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT);
        exCell.Add(new Paragraph(" ").SetFontSize(8).SetMinHeight(35));
        exCell.Add(new Paragraph("_______________________________").SetFont(_regular).SetFontSize(8));
        exCell.Add(new Paragraph(data.ExaminerName).SetFont(_bold).SetFontSize(8));
        exCell.Add(new Paragraph(data.ExaminerTitle).SetFont(_regular).SetFontSize(8));
        exCell.Add(new Paragraph("Verified By:").SetFont(_bold).SetFontSize(8));
        sigTable.AddCell(exCell);

        document.Add(sigTable);

        document.Close();

        var safeBatch = data.BatchName?.Trim() ?? "";
        var safeProgram = data.Program?.Trim() ?? "";
        var safeSection = data.Section?.Trim() ?? "";
        var fileName = $"Batch{safeBatch}{safeProgram}{safeSection}ClassResult.pdf";

        return (memStream.ToArray(), fileName);
    }
    private void AddResultHeader(Document document, ClassResultData data)
    {
        var topBar = new Table(new float[] { 400, 400 }).UseAllAvailableWidth().SetBorder(Border.NO_BORDER);
        topBar.AddCell(new Cell().SetBorder(Border.NO_BORDER)
            .Add(new Paragraph($"Notification No. IST/Exams/KICSIT/BSCS-I/Fall-{DateTime.UtcNow.Year - 1}/01   dated: {DateTime.UtcNow:dd-MM-yyyy}")
                .SetFont(_regular).SetFontSize(6.5f)));
        topBar.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetTextAlignment(TextAlignment.RIGHT)
            .Add(new Paragraph("Page 1 of 1").SetFont(_regular).SetFontSize(6.5f)));
        document.Add(topBar);
        document.Add(new Paragraph("\n").SetFontSize(3));

        var logoTable = new Table(new float[] { 60, 680, 60 }).UseAllAvailableWidth().SetBorder(Border.NO_BORDER);

        try
        {
            var istLogoPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "istlogo.jpg");
            if (System.IO.File.Exists(istLogoPath))
            {
                var istImg = new iText.Layout.Element.Image(
                    iText.IO.Image.ImageDataFactory.Create(istLogoPath))
                    .SetWidth(55).SetHeight(55);
                logoTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE).Add(istImg));
            }
            else
                logoTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)
                    .Add(new Paragraph("IST").SetFontSize(7)));
        }
        catch { logoTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)); }

        var centre = new Cell().SetBorder(Border.NO_BORDER)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE);
        centre.Add(new Paragraph("Dr. A. Q. Khan Institute of Computer Sciences and Information Technology Kahuta")
            .SetFont(_bold).SetFontSize(10).SetTextAlignment(TextAlignment.CENTER).SetUnderline().SetMultipliedLeading(1.2f));
        centre.Add(new Paragraph("Campus of")
            .SetFont(_regular).SetFontSize(8).SetTextAlignment(TextAlignment.CENTER));
        centre.Add(new Paragraph("Institute of Space Technology, Islamabad")
            .SetFont(_bold).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER).SetUnderline());
        centre.Add(new Paragraph($"Bachelor of Science in Computer Science (BSCS) ({data.SemesterLabel.Split(' ').LastOrDefault()})")
            .SetFont(_bold).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER).SetUnderline());
        centre.Add(new Paragraph($"Semester {data.SemesterLabel}")
            .SetFont(_bold).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER).SetUnderline());
        centre.Add(new Paragraph($"{OrdinalSemester(data.SemesterNo)} Semester")
            .SetFont(_bold).SetFontSize(9).SetTextAlignment(TextAlignment.CENTER).SetUnderline());
        logoTable.AddCell(centre);

        try
        {
            var kicsitLogoPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "kicsitlogo.jpg");
            if (System.IO.File.Exists(kicsitLogoPath))
            {
                var kicsitImg = new iText.Layout.Element.Image(
                    iText.IO.Image.ImageDataFactory.Create(kicsitLogoPath))
                    .SetWidth(55).SetHeight(55);
                logoTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)
                    .SetVerticalAlignment(VerticalAlignment.MIDDLE)
                    .SetTextAlignment(TextAlignment.RIGHT).Add(kicsitImg));
            }
            else
                logoTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)
                    .Add(new Paragraph("KICSIT").SetFontSize(7)));
        }
        catch { logoTable.AddCell(new Cell().SetBorder(Border.NO_BORDER)); }

        document.Add(logoTable);
        document.Add(new LineSeparator(new iText.Kernel.Pdf.Canvas.Draw.SolidLine(1f))
            .SetMarginTop(4).SetMarginBottom(6));
    }

    private static string OrdinalSemester(int n) => n switch
    {
        1 => "First",
        2 => "Second",
        3 => "Third",
        4 => "Fourth",
        5 => "Fifth",
        6 => "Sixth",
        7 => "Seventh",
        8 => "Eighth",
        _ => $"{n}th"
    };

    private Table CreateResultTable(ClassResultData data)
    {
        int totalCols = 4 + (data.Subjects.Count * 2) + 5;
        var table = new Table(totalCols).UseAllAvailableWidth().SetFontSize(6);

        table.AddHeaderCell(new Cell(4, 1).SetVerticalAlignment(VerticalAlignment.MIDDLE)
            .SetTextAlignment(TextAlignment.CENTER).SetBackgroundColor(LightGray)
            .Add(new Paragraph("No.").SetFontSize(5.5f).SetBold()));
        table.AddHeaderCell(new Cell(4, 1).SetVerticalAlignment(VerticalAlignment.MIDDLE)
            .SetTextAlignment(TextAlignment.CENTER).SetBackgroundColor(LightGray)
            .Add(new Paragraph("Reg No").SetFontSize(5.5f).SetBold()));
        table.AddHeaderCell(new Cell(4, 1).SetVerticalAlignment(VerticalAlignment.MIDDLE)
            .SetTextAlignment(TextAlignment.CENTER).SetBackgroundColor(LightGray)
            .Add(new Paragraph("Father\nName").SetFontSize(5.5f).SetBold()));
        table.AddHeaderCell(new Cell(4, 1).SetVerticalAlignment(VerticalAlignment.MIDDLE)
            .SetTextAlignment(TextAlignment.CENTER).SetBackgroundColor(LightGray)
            .Add(new Paragraph("Name").SetFontSize(5.5f).SetBold()));

        foreach (var s in data.Subjects)
            table.AddHeaderCell(new Cell(1, 2).SetTextAlignment(TextAlignment.CENTER)
                .SetBackgroundColor(LightGray)
                .Add(new Paragraph($"{s.Name}\n({s.Type})").SetFontSize(5f).SetBold()));

        table.AddHeaderCell(new Cell(4, 1).SetVerticalAlignment(VerticalAlignment.MIDDLE)
            .SetTextAlignment(TextAlignment.CENTER).SetBackgroundColor(LightGray)
            .Add(new Paragraph("SGPA\n(upto 9\ndecimal\nplaces)").SetFontSize(4.5f).SetBold()));
        table.AddHeaderCell(new Cell(4, 1).SetVerticalAlignment(VerticalAlignment.MIDDLE)
            .SetTextAlignment(TextAlignment.CENTER).SetBackgroundColor(LightGray)
            .Add(new Paragraph("SGPA\n(2dp)").SetFontSize(4.5f).SetBold()));
        table.AddHeaderCell(new Cell(4, 1).SetVerticalAlignment(VerticalAlignment.MIDDLE)
            .SetTextAlignment(TextAlignment.CENTER).SetBackgroundColor(LightGray)
            .Add(new Paragraph("CGPA\n(upto 9\ndecimal\nplaces)").SetFontSize(4.5f).SetBold()));
        table.AddHeaderCell(new Cell(4, 1).SetVerticalAlignment(VerticalAlignment.MIDDLE)
            .SetTextAlignment(TextAlignment.CENTER).SetBackgroundColor(LightGray)
            .Add(new Paragraph("CGPA\n(2dp)").SetFontSize(4.5f).SetBold()));
        table.AddHeaderCell(new Cell(4, 1).SetVerticalAlignment(VerticalAlignment.MIDDLE)
            .SetTextAlignment(TextAlignment.CENTER).SetBackgroundColor(LightGray)
            .Add(new Paragraph("Academic\nStatus").SetFontSize(4.5f).SetBold()));

        foreach (var s in data.Subjects)
            table.AddHeaderCell(new Cell(1, 2).SetTextAlignment(TextAlignment.CENTER)
                .SetBackgroundColor(LightGray)
                .Add(new Paragraph($"Credit Hours    {s.CreditHours}").SetFontSize(4.5f)));

        foreach (var s in data.Subjects)
            table.AddHeaderCell(new Cell(1, 2).SetTextAlignment(TextAlignment.CENTER)
                .SetBackgroundColor(LightGray)
                .Add(new Paragraph($"Course No    {s.Code}").SetFontSize(4.5f)));

        foreach (var s in data.Subjects)
        {
            table.AddHeaderCell(new Cell(1, 1).SetTextAlignment(TextAlignment.CENTER)
                .SetBackgroundColor(LightGray)
                .Add(new Paragraph("Grade").SetFontSize(4.5f).SetBold()));
            table.AddHeaderCell(new Cell(1, 1).SetTextAlignment(TextAlignment.CENTER)
                .SetBackgroundColor(LightGray)
                .Add(new Paragraph("Grade\nPoints").SetFontSize(4.5f).SetBold()));
        }

        int sr = 1;
        foreach (var student in data.Students)
        {
            var bg = sr % 2 == 0 ? VLightGray : null;
            table.AddCell(Cell6(sr.ToString(), true, bg));
            table.AddCell(Cell6(student.RegNo, true, bg));
            table.AddCell(Cell6(student.FatherName, false, bg));
            table.AddCell(Cell6(student.Name, false, bg));

            for (int i = 0; i < data.Subjects.Count; i++)
            {
                var grade = i < student.SubjectMarks.Count ? student.SubjectMarks[i].Grade : "";
                var points = i < student.SubjectMarks.Count ? student.SubjectMarks[i].GradePoints.ToString("F2") : "";
                table.AddCell(Cell6(grade, true, bg));
                table.AddCell(Cell6(points, true, bg));
            }

            table.AddCell(Cell6(student.SGPA_9dp.ToString("F9"), true, bg));
            table.AddCell(Cell6(student.SGPA_2dp.ToString("F2"), true, bg));
            table.AddCell(Cell6(student.CGPA_9dp.ToString("F9"), true, bg));
            table.AddCell(Cell6(student.CGPA_2dp.ToString("F2"), true, bg));
            table.AddCell(Cell6(student.AcademicStatus, true, bg));
            sr++;
        }

        return table;
    }

    private static Cell Cell6(string text, bool center, DeviceRgb? bg = null)
    {
        var c = new Cell()
            .SetBorder(new SolidBorder(new DeviceRgb(180, 180, 180), 0.3f))
            .SetPadding(1)
            .SetTextAlignment(center ? TextAlignment.CENTER : TextAlignment.LEFT)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE)
            .Add(new Paragraph(text ?? "")
                .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA))
                .SetFontSize(5.5f).SetMultipliedLeading(1.1f));
        if (bg != null) c.SetBackgroundColor(bg);
        return c;
    }

    private static Cell LogoCell(string label, float size)
    {
        var c = new Cell()
            .SetWidth(size).SetHeight(size)
            .SetBorder(new SolidBorder(new DeviceRgb(187, 187, 187), 1))
            .SetTextAlignment(TextAlignment.CENTER)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE);
        c.Add(new Paragraph(label)
            .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
            .SetFontSize(7).SetFontColor(new DeviceRgb(187, 187, 187)));
        return c;
    }

    private static Cell HeaderCell(string text) =>
        new Cell()
            .SetBackgroundColor(LightGray)
            .SetBorder(new SolidBorder(0.5f))
            .SetTextAlignment(TextAlignment.CENTER)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE)
            .SetPadding(3)
            .Add(new Paragraph(text)
                .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                .SetFontSize(9).SetMultipliedLeading(1.2f));

    private static Cell DataCell(string text, bool CENTER, DeviceRgb? bg = null)
    {
        var c = new Cell()
            .SetBorder(new SolidBorder(0.5f))
            .SetPadding(3)
            .SetTextAlignment(CENTER ? TextAlignment.CENTER : TextAlignment.LEFT)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE)
            .Add(new Paragraph(text)
                .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA))
                .SetFontSize(9).SetMultipliedLeading(1.2f));
        if (bg is not null) c.SetBackgroundColor(bg);
        return c;
    }

    private static Cell TinyCell(string text, bool CENTER, bool bold = false,
        float fontSize = 5.5f, DeviceRgb? bg = null)
    {
        var font = bold
            ? PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)
            : PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        var c = new Cell()
            .SetBorder(new SolidBorder(new DeviceRgb(153, 153, 153), 0.25f))
            .SetPadding(1)
            .SetTextAlignment(CENTER ? TextAlignment.CENTER : TextAlignment.LEFT)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE)
            .Add(new Paragraph(text ?? "").SetFont(font).SetFontSize(fontSize).SetMultipliedLeading(1.1f));
        if (bg is not null) c.SetBackgroundColor(bg);
        return c;
    }

    private static Cell SpanCell(string text, int colSpan, int rowSpan, bool CENTER, bool isHdr)
    {
        var c = new Cell(rowSpan, colSpan)
            .SetBorder(new SolidBorder(new DeviceRgb(153, 153, 153), 0.25f))
            .SetPadding(1)
            .SetTextAlignment(CENTER ? TextAlignment.CENTER : TextAlignment.LEFT)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE);
        if (isHdr) c.SetBackgroundColor(LightGray);
        c.Add(new Paragraph(text)
            .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
            .SetFontSize(5.2f).SetMultipliedLeading(1.1f));
        return c;
    }

    private static Cell SmallHdrCell(string text) =>
        new Cell()
            .SetBackgroundColor(LightGray)
            .SetBorder(new SolidBorder(new DeviceRgb(153, 153, 153), 0.25f))
            .SetPadding(1)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetVerticalAlignment(VerticalAlignment.MIDDLE)
            .Add(new Paragraph(text)
                .SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD))
                .SetFontSize(5.2f).SetMultipliedLeading(1.1f));

    private static Cell SkipCell() =>
        new Cell().SetBorder(Border.NO_BORDER).Add(new Paragraph(""));

    private static Cell NoBorderCell(string text, bool bold = false)
    {
        var font = bold
            ? PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)
            : PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        return new Cell().SetBorder(Border.NO_BORDER).SetPadding(2)
            .Add(new Paragraph(text).SetFont(font).SetFontSize(10));
    }

    private static Paragraph Para(string text, float size, bool bold = false, bool underline = false)
    {
        var font = bold
            ? PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD)
            : PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
        var p = new Paragraph(text).SetFont(font).SetFontSize(size).SetMultipliedLeading(1.3f);
        if (underline) p.SetUnderline();
        return p;
    }

    private static DeviceRgb GetStatusColor(string status) => status switch
    {
        "Excellent" => new DeviceRgb(21, 87, 36),
        "Very Good" => new DeviceRgb(26, 82, 118),
        "Good" => new DeviceRgb(24, 106, 59),
        "Satisfactory" => new DeviceRgb(20, 90, 50),
        "Fair" => new DeviceRgb(120, 66, 18),
        "Warning" => new DeviceRgb(125, 102, 8),
        "Extended Temporary Enrollment" => new DeviceRgb(146, 43, 33),
        _ => new DeviceRgb(0, 0, 0)
    };
}

// ── DTOs ──────────────────────────────────────────────────────────────────────

public class StudentResultCardData
{
    public string ParentName { get; set; } = "";
    public string StudentName { get; set; } = "";
    public string RegNo { get; set; } = "";
    public string ClassName { get; set; } = "";
    public string SemesterLabel { get; set; } = "";
    public string ResultTitle { get; set; } = "";
    public List<SubjectLine> Subjects { get; set; } = new();
    public string SGPA_2dp { get; set; } = "0.00";
    public string CGPA_2dp { get; set; } = "0.00";
    public int CurrentPos { get; set; }
    public int OverallPos { get; set; }
    public int TotalStudents { get; set; }
    public string AcademicStatus { get; set; } = "";
    public string ExaminerName { get; set; } = "Faheem Ahmed";
    public string ExaminerTitle { get; set; } = "Deputy Controller of Examinations KICSIT";
}

public class SubjectLine
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public int CreditHours { get; set; }
    public string Grade { get; set; } = "";
}

public class ClassResultData
{
    public string University { get; set; } = "";
    public string Program { get; set; } = "";
    public string BatchName { get; set; } = "";
    public string Section { get; set; } = "";
    public int SemesterNo { get; set; }
    public string SemesterLabel { get; set; } = "";
    public string NotificationNo { get; set; } = "";
    public string JEName { get; set; } = "";
    public string JETitle { get; set; } = "";
    public string ExaminerName { get; set; } = "";
    public string ExaminerTitle { get; set; } = "";
    public List<SubjectHeader> Subjects { get; set; } = new();
    public List<StudentRow> Students { get; set; } = new();
}

public class SubjectHeader
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public int CreditHours { get; set; }
}

public class StudentRow
{
    public string RegNo { get; set; } = "";
    public string FatherName { get; set; } = "";
    public string Name { get; set; } = "";
    public List<MarkEntry> SubjectMarks { get; set; } = new();
    public double SGPA_9dp { get; set; }
    public double CGPA_9dp { get; set; }
    public double SGPA_2dp { get; set; }
    public double CGPA_2dp { get; set; }
    public string AcademicStatus { get; set; } = "";
}

public class MarkEntry
{
    public string Grade { get; set; } = "";
    public decimal GradePoints { get; set; }
}