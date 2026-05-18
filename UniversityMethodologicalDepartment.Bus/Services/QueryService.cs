using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.Bus.Entities;
using UniversityMethodologicalDepartment.Bus.Models;

namespace UniversityMethodologicalDepartment.Bus.Services;

public sealed class QueryService : IQueryService
{
    private readonly IAppDbContextFactory _dbContextFactory;

    public QueryService(IAppDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<MultiDepartmentDisciplineRow>> GetMultiDepartmentDisciplinesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var disciplines = await db.Disciplines
            .AsNoTracking()
            .Include(x => x.Department)
            .ToListAsync(cancellationToken);

        var curriculumItems = await db.CurriculumItems
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        var rows = disciplines
            .GroupBy(x => x.Name)
            .Select(group =>
            {
                var disciplineIds = group.Select(x => x.Id).ToHashSet();
                var relatedItems = curriculumItems.Where(x => disciplineIds.Contains(x.DisciplineId)).ToList();
                var departments = group
                    .Select(x => x.Department?.Name)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return new MultiDepartmentDisciplineRow(
                    DisciplineName: group.Key,
                    DepartmentCount: departments.Count,
                    Departments: string.Join(", ", departments),
                    SemesterCount: relatedItems.Select(x => x.Semester).Distinct().Count(),
                    SpecialtyCount: relatedItems.Select(x => x.SpecialtyId).Distinct().Count());
            })
            .Where(x => x.DepartmentCount > 1)
            .OrderBy(x => x.DisciplineName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return rows;
    }

    public async Task<IReadOnlyList<MultiSemesterDisciplineRow>> GetMultiSemesterDisciplinesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var curriculumItems = await db.CurriculumItems
            .AsNoTracking()
            .Include(x => x.Discipline)
            .ThenInclude(x => x.Department)
            .ToListAsync(cancellationToken);

        var rows = curriculumItems
            .GroupBy(x => x.Discipline.Name)
            .Select(group =>
            {
                var semesters = group.Select(x => x.Semester).Distinct().OrderBy(x => x).ToList();
                var departments = group
                    .Select(x => x.Discipline.Department?.Name)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return new MultiSemesterDisciplineRow(
                    DisciplineName: group.Key,
                    SemesterCount: semesters.Count,
                    Semesters: string.Join(", ", semesters),
                    Departments: string.Join(", ", departments));
            })
            .Where(x => x.SemesterCount > 1)
            .OrderBy(x => x.DisciplineName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return rows;
    }

    public async Task<IReadOnlyList<DepartmentDisciplineCountRow>> GetDepartmentDisciplineCountsAsync(
        bool sortByCountDescending = false,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        var departments = await db.Departments
            .AsNoTracking()
            .Include(x => x.Faculty)
            .Include(x => x.Head)
            .Include(x => x.Disciplines)
            .Include(x => x.Employees)
            .ToListAsync(cancellationToken);

        var rows = departments
            .Select(x => new DepartmentDisciplineCountRow(
                x.Id,
                x.Name,
                x.Disciplines.Select(d => d.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
                x.Faculty?.Name ?? string.Empty,
                FormatEmployeeFullName(x.Head),
                x.Employees.Count,
                x.Phones))
            .ToList();

        var ordered = sortByCountDescending
            ? rows
                .OrderByDescending(x => x.DisciplineCount)
                .ThenBy(x => x.DepartmentName, StringComparer.OrdinalIgnoreCase)
                .ToList()
            : rows
                .OrderBy(x => x.DepartmentName, StringComparer.OrdinalIgnoreCase)
                .ToList();

        return ordered;
    }

    public async Task<IReadOnlyList<LectureLabDifferenceRow>> GetLectureLabDifferencesAsync(
        int departmentId,
        int semester,
        CancellationToken cancellationToken = default)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

        try
        {
            var rows = await db.CurriculumItems
                .AsNoTracking()
                .Where(x => x.Discipline != null && x.Discipline.DepartmentId == departmentId && x.Semester == semester)
                .OrderBy(x => x.Discipline!.Name)
                .Select(x => new LectureLabDifferenceRow(
                    x.DisciplineId,
                    x.Discipline!.Name,
                    x.LectureHours,
                    x.LabHours,
                    Math.Abs(x.LabHours - x.LectureHours)))
                .ToListAsync(cancellationToken);

            return rows;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"[QueryService] GetLectureLabDifferencesAsync failed. DepartmentId={departmentId}, Semester={semester}. Exception: {ex}");
            throw;
        }
    }

    private static string FormatEmployeeFullName(Employee? employee)
    {
        if (employee is null)
        {
            return string.Empty;
        }

        var parts = new List<string>(3);
        if (!string.IsNullOrWhiteSpace(employee.Surname))
        {
            parts.Add(employee.Surname.Trim());
        }

        if (!string.IsNullOrWhiteSpace(employee.Name))
        {
            parts.Add(employee.Name.Trim());
        }

        if (!string.IsNullOrWhiteSpace(employee.Patronymic))
        {
            parts.Add(employee.Patronymic.Trim());
        }

        return string.Join(' ', parts);
    }
}
