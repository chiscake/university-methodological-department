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
using DocumentFormat.OpenXml.Wordprocessing;
using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.App.Contracts;
using UniversityMethodologicalDepartment.App.Models;
using W = DocumentFormat.OpenXml.Wordprocessing;

namespace UniversityMethodologicalDepartment.App.Services;

internal sealed class ReportExportService : IReportExportService
{
    private readonly IQueryService _queryService;
    private readonly IDataService _dataService;

    public ReportExportService(IQueryService queryService, IDataService dataService)
    {
        _queryService = queryService;
        _dataService = dataService;
    }

    public Task<byte[]> GenerateAsync(ReportExportKind kind, int? specialtyId = null, CancellationToken cancellationToken = default)
    {
        return kind switch
        {
            ReportExportKind.DepartmentsSummaryExcel => GenerateDepartmentsSummaryExcelAsync(cancellationToken),
            ReportExportKind.SpecialtyMatrixExcel => GenerateSpecialtyMatrixExcelAsync(specialtyId, cancellationToken),
            ReportExportKind.DepartmentDisciplinesWord => GenerateDepartmentDisciplinesWordAsync(cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown export kind.")
        };
    }

    public async Task<ReportPreviewDocument> GetPreviewAsync(ReportExportKind kind, CancellationToken cancellationToken = default)
    {
        return kind switch
        {
            ReportExportKind.DepartmentsSummaryExcel => await BuildDepartmentsPreviewAsync(cancellationToken),
            ReportExportKind.SpecialtyMatrixExcel => await BuildSpecialtyMatrixPreviewAsync(cancellationToken),
            ReportExportKind.DepartmentDisciplinesWord => await BuildWordPreviewAsync(cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown export kind.")
        };
    }

    private async Task<ReportPreviewDocument> BuildDepartmentsPreviewAsync(CancellationToken cancellationToken)
    {
        var rows = await _queryService.GetDepartmentDisciplineCountsAsync(sortByCountDescending: true, cancellationToken);
        var mapped = rows
            .Select(x => new DepartmentSummaryPreviewRow(
                x.FacultyName,
                x.DepartmentName,
                x.HeadFullName,
                x.EmployeeCount,
                x.Phones,
                x.DisciplineCount))
            .ToList();

        return new ReportPreviewDocument
        {
            Kind = ReportExportKind.DepartmentsSummaryExcel,
            DepartmentRows = mapped
        };
    }

    private async Task<ReportPreviewDocument> BuildSpecialtyMatrixPreviewAsync(CancellationToken cancellationToken)
    {
        var specialties = await _dataService.GetSpecialtiesAsync(cancellationToken);
        var curriculumItems = await _dataService.GetCurriculumItemsAsync(cancellationToken: cancellationToken);

        var options = specialties
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => new SpecialtyOptionItem
            {
                Id = x.Id,
                DisplayName = $"{x.Code} — {x.Name}"
            })
            .ToList();

        var matrixRows = new List<MatrixPreviewRow>();
        foreach (var specialty in specialties.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            var items = curriculumItems
                .Where(x => x.SpecialtyId == specialty.Id)
                .OrderBy(x => x.Discipline?.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.Semester)
                .ToList();

            foreach (var item in items)
            {
                var disciplineName = item.Discipline?.Name ?? $"Discipline #{item.DisciplineId}";
                var fullName = $"{disciplineName} (сем. {item.Semester})";
                var isCredit = item.IsCredit;
                var isExam = item.IsExam;
                var hasCourseProject = item.HasCourseProject;

                matrixRows.Add(new MatrixPreviewRow
                {
                    SpecialtyId = specialty.Id,
                    DisciplineLabel = fullName,
                    LectureHours = item.LectureHours,
                    LabHours = item.LabHours,
                    PracticalHours = item.PracticalHours,
                    UsrHours = item.UsrHours,
                    CreditMark = isCredit ? "+" : "-",
                    ExamMark = isExam ? "+" : "-",
                    CourseMark = hasCourseProject ? "+" : "-"
                });
            }
        }

        return new ReportPreviewDocument
        {
            Kind = ReportExportKind.SpecialtyMatrixExcel,
            SpecialtyOptions = options,
            MatrixRows = matrixRows
        };
    }

    private async Task<ReportPreviewDocument> BuildWordPreviewAsync(CancellationToken cancellationToken)
    {
        var departments = await _dataService.GetDepartmentsAsync(cancellationToken: cancellationToken);
        var curriculumItems = await _dataService.GetCurriculumItemsAsync(cancellationToken: cancellationToken);

        var sections = new List<WordDepartmentPreviewSection>();
        foreach (var department in departments.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            var rowsByDiscipline = curriculumItems
                .Where(x => x.Discipline?.DepartmentId == department.Id)
                .GroupBy(x => x.Discipline?.Name ?? $"Discipline #{x.DisciplineId}")
                .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (rowsByDiscipline.Count == 0)
            {
                sections.Add(new WordDepartmentPreviewSection
                {
                    DepartmentName = department.Name,
                    Rows = new List<WordDisciplinePreviewRow>
                    {
                        new()
                        {
                            DisciplineName = "—",
                            Semesters = "Нет данных для выбранной кафедры.",
                            Specialties = string.Empty,
                            ControlForms = string.Empty
                        }
                    }
                });
                continue;
            }

            var rows = new List<WordDisciplinePreviewRow>();
            foreach (var group in rowsByDiscipline)
            {
                var semesters = string.Join(", ", group.Select(x => x.Semester).Distinct().OrderBy(x => x));
                var specialties = string.Join(", ", group
                    .Select(x => x.Specialty?.Name)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
                var controlForms = string.Join(", ", group
                    .Select(x => x switch
                    {
                        { IsExam: true } => "Экзамен",
                        { IsCredit: true } => "Зачет",
                        _ => "Не указано"
                    })
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase));

                rows.Add(new WordDisciplinePreviewRow
                {
                    DisciplineName = group.Key,
                    Semesters = semesters,
                    Specialties = specialties,
                    ControlForms = controlForms
                });
            }

            sections.Add(new WordDepartmentPreviewSection
            {
                DepartmentName = department.Name,
                Rows = rows
            });
        }

        return new ReportPreviewDocument
        {
            Kind = ReportExportKind.DepartmentDisciplinesWord,
            WordSections = sections
        };
    }

    public string GetDefaultFileName(ReportExportKind kind)
    {
        var stamp = DateTime.Now.ToString("yyyyMMdd_HHmm", CultureInfo.InvariantCulture);
        return kind switch
        {
            ReportExportKind.DepartmentsSummaryExcel => $"departments_summary_{stamp}",
            ReportExportKind.SpecialtyMatrixExcel => $"specialty_matrix_{stamp}",
            ReportExportKind.DepartmentDisciplinesWord => $"department_disciplines_{stamp}",
            _ => $"report_{stamp}"
        };
    }

    public string GetDefaultExtension(ReportExportKind kind)
    {
        return kind switch
        {
            ReportExportKind.DepartmentDisciplinesWord => ".docx",
            _ => ".xlsx"
        };
    }

    private async Task<byte[]> GenerateDepartmentsSummaryExcelAsync(CancellationToken cancellationToken)
    {
        var rows = await _queryService.GetDepartmentDisciplineCountsAsync(sortByCountDescending: true, cancellationToken);

        using var stream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var borderedStyleIndex = EnsureSpreadsheetStyles(workbookPart);
            var worksheetRows = new List<string[]>
            {
                new[]
                {
                    "Факультет",
                    "Кафедра",
                    "Заведующий",
                    "Телефон(ы)",
                    "Число сотрудников",
                    "Количество дисциплин"
                }
            };
            foreach (var row in rows)
            {
                worksheetRows.Add(new[]
                {
                    row.FacultyName,
                    row.DepartmentName,
                    row.HeadFullName,
                    row.Phones ?? string.Empty,
                    row.EmployeeCount.ToString(CultureInfo.InvariantCulture),
                    row.DisciplineCount.ToString(CultureInfo.InvariantCulture)
                });
            }
            worksheetPart.Worksheet = CreateWorksheetWithAutoWidthAndBorders(worksheetRows, borderedStyleIndex);

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Кафедры"
            });

            workbookPart.Workbook.Save();
        }

        return stream.ToArray();
    }

    private async Task<byte[]> GenerateSpecialtyMatrixExcelAsync(int? specialtyId, CancellationToken cancellationToken)
    {
        var specialties = await _dataService.GetSpecialtiesAsync(cancellationToken);
        var curriculumItems = await _dataService.GetCurriculumItemsAsync(cancellationToken: cancellationToken);
        var specialtiesToExport = specialties
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (specialtyId.HasValue)
        {
            specialtiesToExport = specialtiesToExport
                .Where(x => x.Id == specialtyId.Value)
                .ToList();

            if (specialtiesToExport.Count == 0)
            {
                throw new InvalidOperationException("Выбранная специальность не найдена.");
            }
        }

        using var stream = new MemoryStream();
        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            var borderedStyleIndex = EnsureSpreadsheetStyles(workbookPart);
            var sheets = workbookPart.Workbook.AppendChild(new Sheets());

            uint index = 1;
            foreach (var specialty in specialtiesToExport)
            {
                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                var worksheetRows = new List<string[]>
                {
                    new[]
                    {
                    "Дисциплина",
                    "Лекции",
                    "Лабораторные",
                    "Практические",
                    "УСР",
                    "Зачет",
                    "Экзамен",
                    "Курсовой проект"
                    }
                };

                var items = curriculumItems
                    .Where(x => x.SpecialtyId == specialty.Id)
                    .OrderBy(x => x.Discipline?.Name, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(x => x.Semester)
                    .ToList();

                foreach (var item in items)
                {
                    var disciplineName = item.Discipline?.Name ?? $"Discipline #{item.DisciplineId}";
                    var fullName = $"{disciplineName} (сем. {item.Semester})";
                    var isCredit = item.IsCredit;
                    var isExam = item.IsExam;
                    var hasCourseProject = item.HasCourseProject;

                    worksheetRows.Add(new[]
                    {
                        fullName,
                        item.LectureHours.ToString(CultureInfo.InvariantCulture),
                        item.LabHours.ToString(CultureInfo.InvariantCulture),
                        item.PracticalHours.ToString(CultureInfo.InvariantCulture),
                        item.UsrHours.ToString(CultureInfo.InvariantCulture),
                        isCredit ? "+" : "-",
                        isExam ? "+" : "-",
                        hasCourseProject ? "+" : "-"
                    });
                }
                worksheetPart.Worksheet = CreateWorksheetWithAutoWidthAndBorders(worksheetRows, borderedStyleIndex);

                sheets.Append(new Sheet
                {
                    Id = workbookPart.GetIdOfPart(worksheetPart),
                    SheetId = index++,
                    Name = TruncateSheetName($"{specialty.Code}_{specialty.Name}")
                });
            }

            workbookPart.Workbook.Save();
        }

        return stream.ToArray();
    }

    private async Task<byte[]> GenerateDepartmentDisciplinesWordAsync(CancellationToken cancellationToken)
    {
        var departments = await _dataService.GetDepartmentsAsync(cancellationToken: cancellationToken);
        var curriculumItems = await _dataService.GetCurriculumItemsAsync(cancellationToken: cancellationToken);

        using var stream = new MemoryStream();
        using (var document = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = document.AddMainDocumentPart();
            mainPart.Document = new W.Document(new W.Body());
            var body = mainPart.Document.Body!;

            body.Append(CreateParagraph("Отчёт по кафедрам о читаемых дисциплинах", bold: true));

            foreach (var department in departments.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            {
                body.Append(CreateParagraph(string.Empty));
                body.Append(CreateParagraph($"Кафедра: {department.Name}", bold: true));

                var rowsByDiscipline = curriculumItems
                    .Where(x => x.Discipline?.DepartmentId == department.Id)
                    .GroupBy(x => x.Discipline?.Name ?? $"Discipline #{x.DisciplineId}")
                    .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (rowsByDiscipline.Count == 0)
                {
                    body.Append(CreateParagraph("Нет данных для выбранной кафедры."));
                    continue;
                }

                var table = new W.Table();
                table.AppendChild(CreateBlackTableProperties());
                table.Append(CreateTableHeaderRow("Дисциплина", "Семестры", "Специальности", "Форма контроля"));

                foreach (var group in rowsByDiscipline)
                {
                    var semesters = string.Join(", ", group.Select(x => x.Semester).Distinct().OrderBy(x => x));
                    var specialties = string.Join(", ", group
                        .Select(x => x.Specialty?.Name)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
                    var controlForms = string.Join(", ", group
                        .Select(x => x switch
                        {
                            { IsExam: true } => "Экзамен",
                            { IsCredit: true } => "Зачет",
                            _ => "Не указано"
                        })
                        .Distinct(StringComparer.OrdinalIgnoreCase)
                        .OrderBy(x => x, StringComparer.OrdinalIgnoreCase));

                    table.Append(CreateTableRow(group.Key, semesters, specialties, controlForms));
                }

                body.Append(table);
            }

            mainPart.Document.Save();
        }

        return stream.ToArray();
    }

    private static Worksheet CreateWorksheetWithAutoWidthAndBorders(IReadOnlyList<string[]> rows, uint borderedStyleIndex)
    {
        var sheetData = new SheetData();

        foreach (var values in rows)
        {
            sheetData.Append(CreateRow(values, borderedStyleIndex));
        }

        var columns = CreateAutoSizedColumns(rows);
        return new Worksheet(columns, sheetData);
    }

    private static DocumentFormat.OpenXml.Spreadsheet.Columns CreateAutoSizedColumns(IReadOnlyList<string[]> rows)
    {
        var maxColumns = rows.Count == 0 ? 0 : rows.Max(r => r.Length);
        var result = new DocumentFormat.OpenXml.Spreadsheet.Columns();

        for (var i = 0; i < maxColumns; i++)
        {
            var maxLength = rows
                .Where(r => i < r.Length)
                .Select(r => r[i]?.Length ?? 0)
                .DefaultIfEmpty(0)
                .Max();

            // OpenXML width units are roughly based on the width of "0" glyphs.
            var width = Math.Min(80d, Math.Max(10d, maxLength + 2d));
            result.Append(new DocumentFormat.OpenXml.Spreadsheet.Column
            {
                Min = (uint)(i + 1),
                Max = (uint)(i + 1),
                Width = width,
                CustomWidth = true
            });
        }

        return result;
    }

    private static Row CreateRow(string[] values, uint borderedStyleIndex)
    {
        var row = new Row();
        foreach (var value in values)
        {
            var cell = new Cell
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

        var stylesheet = new Stylesheet(
            new DocumentFormat.OpenXml.Spreadsheet.Fonts(
                new DocumentFormat.OpenXml.Spreadsheet.Font()),
            new Fills(
                new Fill(new PatternFill { PatternType = PatternValues.None }),
                new Fill(new PatternFill { PatternType = PatternValues.Gray125 })),
            new Borders(
                new DocumentFormat.OpenXml.Spreadsheet.Border(
                    new DocumentFormat.OpenXml.Spreadsheet.LeftBorder(),
                    new DocumentFormat.OpenXml.Spreadsheet.RightBorder(),
                    new DocumentFormat.OpenXml.Spreadsheet.TopBorder(),
                    new DocumentFormat.OpenXml.Spreadsheet.BottomBorder(),
                    new DiagonalBorder()),
                new DocumentFormat.OpenXml.Spreadsheet.Border(
                    new DocumentFormat.OpenXml.Spreadsheet.LeftBorder { Style = BorderStyleValues.Thin, Color = new DocumentFormat.OpenXml.Spreadsheet.Color { Rgb = HexBinaryValue.FromString("000000") } },
                    new DocumentFormat.OpenXml.Spreadsheet.RightBorder { Style = BorderStyleValues.Thin, Color = new DocumentFormat.OpenXml.Spreadsheet.Color { Rgb = HexBinaryValue.FromString("000000") } },
                    new DocumentFormat.OpenXml.Spreadsheet.TopBorder { Style = BorderStyleValues.Thin, Color = new DocumentFormat.OpenXml.Spreadsheet.Color { Rgb = HexBinaryValue.FromString("000000") } },
                    new DocumentFormat.OpenXml.Spreadsheet.BottomBorder { Style = BorderStyleValues.Thin, Color = new DocumentFormat.OpenXml.Spreadsheet.Color { Rgb = HexBinaryValue.FromString("000000") } },
                    new DiagonalBorder())),
            new CellStyleFormats(
                new CellFormat()),
            new CellFormats(
                new CellFormat(),
                new CellFormat { BorderId = 1, ApplyBorder = true }));

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

    private static W.TableRow CreateTableHeaderRow(params string[] values)
    {
        var row = new W.TableRow();
        foreach (var value in values)
        {
            var cell = new W.TableCell(
                new W.Paragraph(new W.Run(new W.RunProperties(new W.Bold()), new W.Text(value ?? string.Empty))),
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
