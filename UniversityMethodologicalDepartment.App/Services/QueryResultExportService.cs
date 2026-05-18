using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using UniversityMethodologicalDepartment.App.Contracts;
using UniversityMethodologicalDepartment.App.Pages.Analytics;
using S = DocumentFormat.OpenXml.Spreadsheet;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace UniversityMethodologicalDepartment.App.Services;

internal sealed class QueryResultExportService : IQueryResultExportService
{
    public Task<byte[]> ExportAsync(
        QueryScenario scenario,
        string queryTitle,
        IReadOnlyList<QueryResultRow> rows,
        QueryExportFormat format,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return format switch
        {
            QueryExportFormat.Excel => Task.FromResult(GenerateExcel(scenario, queryTitle, rows)),
            QueryExportFormat.Word => Task.FromResult(GenerateWord(scenario, queryTitle, rows)),
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, null)
        };
    }

    public string GetDefaultFileName(QueryScenario scenario)
    {
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture);
        var slug = scenario switch
        {
            QueryScenario.MultiDepartmentDisciplines => "multi_department_disciplines",
            QueryScenario.MultiSemesterDisciplines => "multi_semester_disciplines",
            QueryScenario.DepartmentDisciplineCounts => "department_discipline_counts",
            QueryScenario.DepartmentDisciplineCountsSorted => "department_discipline_counts_sorted",
            QueryScenario.LectureLabDifference => "lecture_lab_difference",
            _ => "query"
        };
        return $"query_{slug}_{stamp}";
    }

    public string GetDefaultExtension(QueryExportFormat format)
    {
        return format switch
        {
            QueryExportFormat.Word => ".docx",
            QueryExportFormat.Excel => ".xlsx",
            _ => ".xlsx"
        };
    }

    private static byte[] GenerateExcel(QueryScenario scenario, string queryTitle, IReadOnlyList<QueryResultRow> rows)
    {
        var headers = GetColumnHeaders(scenario);
        var worksheetRows = new List<string[]> { headers };
        foreach (var row in rows)
        {
            worksheetRows.Add(new[]
            {
                row.Primary ?? string.Empty,
                row.Secondary ?? string.Empty,
                row.Details ?? string.Empty,
                row.Extra ?? string.Empty
            });
        }

        using var stream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var borderedStyleIndex = EnsureSpreadsheetStyles(workbookPart);
            worksheetPart.Worksheet = CreateWorksheetWithAutoWidthAndBorders(worksheetRows, borderedStyleIndex);

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = TruncateSheetName(string.IsNullOrWhiteSpace(queryTitle) ? "Запрос" : queryTitle)
            });

            workbookPart.Workbook.Save();
        }

        return stream.ToArray();
    }

    private static byte[] GenerateWord(QueryScenario scenario, string queryTitle, IReadOnlyList<QueryResultRow> rows)
    {
        var headers = GetColumnHeaders(scenario);
        var stamp = DateTime.Now.ToString("g", CultureInfo.CurrentCulture);

        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new W.Document(new W.Body());
            var body = mainPart.Document.Body!;

            body.Append(CreateParagraph(string.IsNullOrWhiteSpace(queryTitle) ? "Результаты запроса" : queryTitle, bold: true));
            body.Append(CreateParagraph($"Сформировано: {stamp}"));

            var table = new W.Table();
            table.AppendChild(CreateBlackTableProperties());
            table.Append(CreateTableRow(headers));

            foreach (var row in rows)
            {
                table.Append(CreateTableRow(
                    row.Primary ?? string.Empty,
                    row.Secondary ?? string.Empty,
                    row.Details ?? string.Empty,
                    row.Extra ?? string.Empty));
            }

            body.Append(table);
            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    private static string[] GetColumnHeaders(QueryScenario scenario)
    {
        return scenario switch
        {
            QueryScenario.MultiDepartmentDisciplines =>
                ["Дисциплина", "Сводка", "Подробности", "Кафедры"],
            QueryScenario.MultiSemesterDisciplines =>
                ["Дисциплина", "Сводка", "Семестры", "Кафедры"],
            QueryScenario.DepartmentDisciplineCounts or QueryScenario.DepartmentDisciplineCountsSorted =>
                ["Кафедра", "Показатель", "—", "—"],
            QueryScenario.LectureLabDifference =>
                ["Дисциплина", "Часы", "Разница", "—"],
            _ => ["Столбец 1", "Столбец 2", "Столбец 3", "Столбец 4"]
        };
    }

    private static S.Worksheet CreateWorksheetWithAutoWidthAndBorders(IReadOnlyList<string[]> rows, uint borderedStyleIndex)
    {
        var sheetData = new S.SheetData();

        foreach (var values in rows)
        {
            sheetData.Append(CreateSpreadsheetRow(values, borderedStyleIndex));
        }

        var columns = CreateAutoSizedColumns(rows);
        return new S.Worksheet(columns, sheetData);
    }

    private static S.Columns CreateAutoSizedColumns(IReadOnlyList<string[]> rows)
    {
        var maxColumns = rows.Count == 0 ? 0 : rows.Max(r => r.Length);
        var result = new S.Columns();

        for (var i = 0; i < maxColumns; i++)
        {
            var maxLength = rows
                .Where(r => i < r.Length)
                .Select(r => r[i]?.Length ?? 0)
                .DefaultIfEmpty(0)
                .Max();

            var width = Math.Min(80d, Math.Max(10d, maxLength + 2d));
            result.Append(new S.Column
            {
                Min = (uint)(i + 1),
                Max = (uint)(i + 1),
                Width = width,
                CustomWidth = true
            });
        }

        return result;
    }

    private static S.Row CreateSpreadsheetRow(string[] values, uint borderedStyleIndex)
    {
        var row = new S.Row();
        foreach (var value in values)
        {
            var cell = new S.Cell
            {
                DataType = CellValues.String,
                CellValue = new CellValue(value ?? string.Empty)
            };

            if (!string.IsNullOrWhiteSpace(value))
            {
                cell.StyleIndex = borderedStyleIndex;
            }

            row.Append(cell);
        }

        return row;
    }

    private static uint EnsureSpreadsheetStyles(WorkbookPart workbookPart)
    {
        var stylesPart = workbookPart.WorkbookStylesPart ?? workbookPart.AddNewPart<WorkbookStylesPart>();
        if (stylesPart.Stylesheet is not null)
        {
            return 1;
        }

        var stylesheet = new S.Stylesheet(
            new S.Fonts(new S.Font()),
            new S.Fills(
                new S.Fill(new S.PatternFill { PatternType = PatternValues.None }),
                new S.Fill(new S.PatternFill { PatternType = PatternValues.Gray125 })),
            new S.Borders(
                new S.Border(
                    new S.LeftBorder(),
                    new S.RightBorder(),
                    new S.TopBorder(),
                    new S.BottomBorder(),
                    new S.DiagonalBorder()),
                new S.Border(
                    new S.LeftBorder { Style = BorderStyleValues.Thin, Color = new S.Color { Rgb = HexBinaryValue.FromString("000000") } },
                    new S.RightBorder { Style = BorderStyleValues.Thin, Color = new S.Color { Rgb = HexBinaryValue.FromString("000000") } },
                    new S.TopBorder { Style = BorderStyleValues.Thin, Color = new S.Color { Rgb = HexBinaryValue.FromString("000000") } },
                    new S.BottomBorder { Style = BorderStyleValues.Thin, Color = new S.Color { Rgb = HexBinaryValue.FromString("000000") } },
                    new S.DiagonalBorder())),
            new S.CellStyleFormats(new S.CellFormat()),
            new S.CellFormats(
                new S.CellFormat(),
                new S.CellFormat { BorderId = 1, ApplyBorder = true }));

        stylesPart.Stylesheet = stylesheet;
        stylesPart.Stylesheet.Save();
        return 1;
    }

    private static W.TableProperties CreateBlackTableProperties()
    {
        return new W.TableProperties(
            new W.TableBorders(
                new W.TopBorder { Val = W.BorderValues.Single, Color = "000000", Size = 4U },
                new W.LeftBorder { Val = W.BorderValues.Single, Color = "000000", Size = 4U },
                new W.BottomBorder { Val = W.BorderValues.Single, Color = "000000", Size = 4U },
                new W.RightBorder { Val = W.BorderValues.Single, Color = "000000", Size = 4U },
                new W.InsideHorizontalBorder { Val = W.BorderValues.Single, Color = "000000", Size = 4U },
                new W.InsideVerticalBorder { Val = W.BorderValues.Single, Color = "000000", Size = 4U }));
    }

    private static W.TableRow CreateTableRow(params string[] values)
    {
        var row = new W.TableRow();
        foreach (var value in values)
        {
            var cell = new W.TableCell(
                new W.Paragraph(new W.Run(new W.Text(value ?? string.Empty))),
                new W.TableCellProperties(new W.TableCellWidth { Type = W.TableWidthUnitValues.Auto }));
            row.Append(cell);
        }

        return row;
    }

    private static W.Paragraph CreateParagraph(string value, bool bold = false)
    {
        W.Run run = bold
            ? new W.Run(new W.RunProperties(new W.Bold()), new W.Text(value))
            : new W.Run(new W.Text(value));

        return new W.Paragraph(run);
    }

    private static string TruncateSheetName(string input)
    {
        var sanitized = input.Replace(':', '_').Replace('/', '_').Replace('\\', '_').Replace('?', '_').Replace('*', '_').Replace('[', '_').Replace(']', '_');
        return sanitized.Length <= 31 ? sanitized : sanitized[..31];
    }
}
