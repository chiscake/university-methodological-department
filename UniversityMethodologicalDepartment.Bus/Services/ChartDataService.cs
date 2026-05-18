using System;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.Bus.Models;

namespace UniversityMethodologicalDepartment.Bus.Services;

public sealed class ChartDataService : IChartDataService
{
    private readonly IAppDbContextFactory _dbContextFactory;

    public ChartDataService(IAppDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<LabHoursByDepartmentPoint>> GetLabHoursByDepartmentAsync(
        int semester,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = await _dbContextFactory.CreateDbContextAsync(cancellationToken);

            var rows = await db.Departments
                .AsNoTracking()
                .Select(department => new
                {
                    DepartmentId = department.Id,
                    DepartmentName = department.Name,
                    TotalLabHours = department.Disciplines
                        .SelectMany(discipline => discipline.CurriculumItems)
                        .Where(item => item.Semester == semester)
                        .Sum(item => (double?)item.LabHours) ?? 0d
                })
                .OrderBy(x => x.DepartmentName)
                .ToListAsync(cancellationToken);

            var points = rows
                .Select(x => new LabHoursByDepartmentPoint(
                    x.DepartmentId,
                    x.DepartmentName,
                    x.TotalLabHours))
                .ToList();

            Debug.WriteLine($"[ChartDataService] Loaded {points.Count} chart rows for semester {semester}.");
            return points;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ChartDataService] Failed to load chart rows for semester {semester}. Exception: {ex}");
            throw;
        }
    }
}
