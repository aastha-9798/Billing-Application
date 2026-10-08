using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Security;
using System.Text;
using PlateBilling.Models;

namespace PlateBilling.Services;

/// <summary>
/// Writes a report as an Excel workbook (.xlsx). An .xlsx file is a zip
/// of XML parts, so no Excel library is needed.
///
/// Layout: title, subtitle, a header row, one row per report row and a
/// total row. Numbers are stored as numbers, so they can be summed,
/// sorted and filtered in Excel.
/// </summary>
internal static class ReportExcelWriter
{
    // Indexes into <cellXfs> in StylesXml.
    private const int StyleTitle = 1;
    private const int StyleSubtitle = 2;
    private const int StyleHeader = 3;
    private const int StyleHeaderRight = 4;
    private const int StyleAmount = 5;       // #,##0.00
    private const int StyleCount = 6;        // #,##0
    private const int StyleTotalLabel = 7;
    private const int StyleTotalAmount = 8;
    private const int StyleTotalCount = 9;

    private const int HeaderRow = 4;

    public static void Save(Report report, string filePath)
    {
        using var stream = File.Create(filePath);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Create);

        Add(zip, "[Content_Types].xml", ContentTypesXml);
        Add(zip, "_rels/.rels", RootRelsXml);
        Add(zip, "xl/workbook.xml", WorkbookXml(report));
        Add(zip, "xl/_rels/workbook.xml.rels", WorkbookRelsXml);
        Add(zip, "xl/styles.xml", StylesXml);
        Add(zip, "xl/worksheets/sheet1.xml", SheetXml(report));
    }


    // =========================================================
    // SHEET
    // =========================================================

    private static string SheetXml(Report report)
    {
        var columns = report.Columns;
        var xml = new StringBuilder();

        xml.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        xml.Append("""<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">""");

        // Keep the title and header visible while scrolling.
        xml.Append($"""<sheetViews><sheetView workbookViewId="0"><pane ySplit="{HeaderRow}" topLeftCell="A{HeaderRow + 1}" activePane="bottomLeft" state="frozen"/></sheetView></sheetViews>""");

        xml.Append("<cols>");
        for (int i = 0; i < columns.Count; i++)
        {
            double width = Math.Round(columns[i].Width * 9 + 4, 1);
            xml.Append($"""<col min="{i + 1}" max="{i + 1}" width="{Num(width)}" customWidth="1"/>""");
        }
        xml.Append("</cols>");

        xml.Append("<sheetData>");

        AppendRow(xml, 1, [new Cell(report.Title, StyleTitle)]);
        AppendRow(xml, 2, [new Cell(report.Subtitle, StyleSubtitle)]);

        AppendRow(xml, HeaderRow, columns
            .Select(c => new Cell(c.Header, c.IsNumeric ? StyleHeaderRight : StyleHeader)));

        int row = HeaderRow + 1;

        foreach (var reportRow in report.Rows)
        {
            AppendRow(xml, row++, columns
                .Select(c => new Cell(c.GetValue(reportRow), StyleFor(c))));
        }

        // Total row: totals under Quantity and Amount, shaded across.
        AppendRow(xml, row, columns.Select((c, i) =>
            i == 0 ? new Cell("Total", StyleTotalLabel)
            : c == ReportColumn.Quantity ? new Cell(report.TotalQuantity, StyleTotalCount)
            : c == ReportColumn.Amount ? new Cell(report.GrandTotal, StyleTotalAmount)
            : new Cell(null, StyleTotalLabel)));

        xml.Append("</sheetData>");

        // Title and subtitle span the table width.
        string lastColumn = ColumnName(columns.Count - 1);
        xml.Append($"""<mergeCells count="2"><mergeCell ref="A1:{lastColumn}1"/><mergeCell ref="A2:{lastColumn}2"/></mergeCells>""");

        xml.Append("""<pageMargins left="0.5" right="0.5" top="0.6" bottom="0.6" header="0.3" footer="0.3"/>""");
        xml.Append("</worksheet>");

        return xml.ToString();
    }

    private static int StyleFor(ReportColumn column)
    {
        if (column.Format == "N2")
        {
            return StyleAmount;
        }

        return column.IsNumeric ? StyleCount : 0;
    }

    // =========================================================
    // CELLS
    // =========================================================

    private readonly record struct Cell(object? Value, int Style);

    // Cells are placed left to right starting at column A.
    private static void AppendRow(StringBuilder xml, int row, IEnumerable<Cell> cells)
    {
        xml.Append($"""<row r="{row}">""");

        int column = 0;

        foreach (var cell in cells)
        {
            string reference = $"{ColumnName(column++)}{row}";

            xml.Append(cell.Value switch
            {
                null =>
                    $"""<c r="{reference}" s="{cell.Style}"/>""",

                int or long or decimal or double =>
                    $"""<c r="{reference}" s="{cell.Style}"><v>{((IFormattable)cell.Value).ToString(null, CultureInfo.InvariantCulture)}</v></c>""",

                _ =>
                    $"""<c r="{reference}" t="inlineStr" s="{cell.Style}"><is><t xml:space="preserve">{SecurityElement.Escape(cell.Value.ToString())}</t></is></c>"""
            });
        }

        xml.Append("</row>");
    }

    private static string Num(double value) =>
        value.ToString(CultureInfo.InvariantCulture);

    // 0 -> A, 1 -> B, ..., 26 -> AA
    private static string ColumnName(int index)
    {
        string name = string.Empty;

        for (index++; index > 0; index = (index - 1) / 26)
        {
            name = (char)('A' + (index - 1) % 26) + name;
        }

        return name;
    }


    // =========================================================
    // PACKAGE PARTS
    // =========================================================

    private static void Add(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);

        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string WorkbookXml(Report report)
    {
        // Sheet names: max 31 characters, no : \ / ? * [ ]
        string sheetName = new string(
            $"{report.Type} Report"
                .Where(c => !@":\/?*[]".Contains(c))
                .Take(31)
                .ToArray());

        return $"""
            <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
            <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
              <sheets><sheet name="{SecurityElement.Escape(sheetName)}" sheetId="1" r:id="rId1"/></sheets>
            </workbook>
            """;
    }

    private const string ContentTypesXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
          <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
          <Default Extension="xml" ContentType="application/xml"/>
          <Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/>
          <Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/>
          <Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/>
        </Types>
        """;

    private const string RootRelsXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/>
        </Relationships>
        """;

    private const string WorkbookRelsXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
          <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
          <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
        </Relationships>
        """;

    // Fonts: 0 normal, 1 title, 2 grey, 3 bold.
    // Fills: 0/1 required defaults, 2 light grey (header and total rows).
    // numFmt 3 = #,##0   numFmt 4 = #,##0.00 (built in).
    private const string StylesXml = """
        <?xml version="1.0" encoding="UTF-8" standalone="yes"?>
        <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
          <fonts count="4">
            <font><sz val="11"/><name val="Calibri"/></font>
            <font><b/><sz val="16"/><name val="Calibri"/></font>
            <font><sz val="11"/><color rgb="FF667085"/><name val="Calibri"/></font>
            <font><b/><sz val="11"/><name val="Calibri"/></font>
          </fonts>
          <fills count="3">
            <fill><patternFill patternType="none"/></fill>
            <fill><patternFill patternType="gray125"/></fill>
            <fill><patternFill patternType="solid"><fgColor rgb="FFF2F4F7"/><bgColor indexed="64"/></patternFill></fill>
          </fills>
          <borders count="2">
            <border><left/><right/><top/><bottom/><diagonal/></border>
            <border><left/><right/><top/><bottom style="thin"><color rgb="FFD0D5DD"/></bottom><diagonal/></border>
          </borders>
          <cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs>
          <cellXfs count="10">
            <xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/>
            <xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1"/>
            <xf numFmtId="0" fontId="2" fillId="0" borderId="0" xfId="0" applyFont="1"/>
            <xf numFmtId="0" fontId="3" fillId="2" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1"/>
            <xf numFmtId="0" fontId="3" fillId="2" borderId="1" xfId="0" applyFont="1" applyFill="1" applyBorder="1" applyAlignment="1"><alignment horizontal="right"/></xf>
            <xf numFmtId="4" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>
            <xf numFmtId="3" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/>
            <xf numFmtId="0" fontId="3" fillId="2" borderId="0" xfId="0" applyFont="1" applyFill="1"/>
            <xf numFmtId="4" fontId="3" fillId="2" borderId="0" xfId="0" applyNumberFormat="1" applyFont="1" applyFill="1"/>
            <xf numFmtId="3" fontId="3" fillId="2" borderId="0" xfId="0" applyNumberFormat="1" applyFont="1" applyFill="1"/>
          </cellXfs>
          <cellStyles count="1"><cellStyle name="Normal" xfId="0" builtinId="0"/></cellStyles>
        </styleSheet>
        """;
}
