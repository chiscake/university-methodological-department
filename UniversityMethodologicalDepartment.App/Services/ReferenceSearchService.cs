using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.App.Contracts;
using UniversityMethodologicalDepartment.App.Models;
using UniversityMethodologicalDepartment.Bus.Entities;

namespace UniversityMethodologicalDepartment.App.Services;

/// <summary>Сводный поиск по справочникам: загрузка списков через <see cref="IDataService"/>, фильтрация в памяти.</summary>
public sealed class ReferenceSearchService : IReferenceSearchService
{
    private readonly IDataService _dataService;

    public ReferenceSearchService(IDataService dataService)
    {
        _dataService = dataService;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ReferenceSearchItem>> SearchAsync(
        string query,
        int maxResults,
        ReferenceSearchScope scope = ReferenceSearchScope.All,
        CancellationToken cancellationToken = default)
    {
        var q = query.Trim();
        if (string.IsNullOrEmpty(q) || maxResults <= 0 || scope == ReferenceSearchScope.None)
            return [];

        var shouldLoadFaculties = HasScope(scope, ReferenceSearchScope.Faculty);
        var shouldLoadDepartments = HasScope(scope, ReferenceSearchScope.Department);
        var shouldLoadSections = HasScope(scope, ReferenceSearchScope.Section);
        var shouldLoadDisciplines = HasScope(scope, ReferenceSearchScope.Discipline);
        var shouldLoadEmployees = HasScope(scope, ReferenceSearchScope.Employee);
        var shouldLoadCurriculum = HasScope(scope, ReferenceSearchScope.CurriculumItem);
        var shouldLoadSpecialties = HasScope(scope, ReferenceSearchScope.Specialty);

        var loadTasks = new List<Task>(7);

        Task<IReadOnlyList<Faculty>>? facultiesTask = null;
        Task<IReadOnlyList<Department>>? departmentsTask = null;
        Task<IReadOnlyList<Section>>? sectionsTask = null;
        Task<IReadOnlyList<Discipline>>? disciplinesTask = null;
        Task<IReadOnlyList<Employee>>? employeesTask = null;
        Task<IReadOnlyList<CurriculumItem>>? curriculumTask = null;
        Task<IReadOnlyList<Specialty>>? specialtiesTask = null;

        if (shouldLoadFaculties)
        {
            facultiesTask = _dataService.GetFacultiesAsync(cancellationToken);
            loadTasks.Add(facultiesTask);
        }

        if (shouldLoadDepartments)
        {
            departmentsTask = _dataService.GetDepartmentsAsync(cancellationToken: cancellationToken);
            loadTasks.Add(departmentsTask);
        }

        if (shouldLoadSections)
        {
            sectionsTask = _dataService.GetSectionsAsync(cancellationToken: cancellationToken);
            loadTasks.Add(sectionsTask);
        }

        if (shouldLoadDisciplines)
        {
            disciplinesTask = _dataService.GetDisciplinesAsync(cancellationToken: cancellationToken);
            loadTasks.Add(disciplinesTask);
        }

        if (shouldLoadEmployees)
        {
            employeesTask = _dataService.GetEmployeesAsync(cancellationToken: cancellationToken);
            loadTasks.Add(employeesTask);
        }

        if (shouldLoadCurriculum)
        {
            curriculumTask = _dataService.GetCurriculumItemsAsync(cancellationToken: cancellationToken);
            loadTasks.Add(curriculumTask);
        }

        if (shouldLoadSpecialties)
        {
            specialtiesTask = _dataService.GetSpecialtiesAsync(cancellationToken);
            loadTasks.Add(specialtiesTask);
        }

        if (loadTasks.Count > 0)
        {
            await Task.WhenAll(loadTasks).ConfigureAwait(false);
        }

        var faculties = facultiesTask?.Result ?? [];
        var departments = departmentsTask?.Result ?? [];
        var sections = sectionsTask?.Result ?? [];
        var disciplines = disciplinesTask?.Result ?? [];
        var employees = employeesTask?.Result ?? [];
        var curriculum = curriculumTask?.Result ?? [];
        var specialties = specialtiesTask?.Result ?? [];

        var candidates = new List<ReferenceSearchItem>(256);

        foreach (var f in faculties)
        {
            if (!MatchesFaculty(f, q))
                continue;
            candidates.Add(new ReferenceSearchItem
            {
                Kind = ReferenceSearchKind.Faculty,
                Id = f.Id,
                Title = f.Name,
                Subtitle = KindLabel(ReferenceSearchKind.Faculty)
            });
        }

        foreach (var d in departments)
        {
            if (!MatchesDepartment(d, q))
                continue;
            var fac = d.Faculty?.Name;
            var sub = string.IsNullOrEmpty(fac)
                ? KindLabel(ReferenceSearchKind.Department)
                : $"{KindLabel(ReferenceSearchKind.Department)} · {fac}";
            candidates.Add(new ReferenceSearchItem
            {
                Kind = ReferenceSearchKind.Department,
                Id = d.Id,
                Title = d.Name,
                Subtitle = sub
            });
        }

        foreach (var s in sections)
        {
            if (!MatchesSection(s, q))
                continue;
            var dep = s.Department?.Name;
            var sub = string.IsNullOrEmpty(dep)
                ? KindLabel(ReferenceSearchKind.Section)
                : $"{KindLabel(ReferenceSearchKind.Section)} · {dep}";
            candidates.Add(new ReferenceSearchItem
            {
                Kind = ReferenceSearchKind.Section,
                Id = s.Id,
                Title = s.Name,
                Subtitle = sub
            });
        }

        foreach (var d in disciplines)
        {
            if (!MatchesDiscipline(d, q))
                continue;
            var dep = d.Department?.Name;
            var sub = string.IsNullOrEmpty(dep)
                ? KindLabel(ReferenceSearchKind.Discipline)
                : $"{KindLabel(ReferenceSearchKind.Discipline)} · {dep}";
            candidates.Add(new ReferenceSearchItem
            {
                Kind = ReferenceSearchKind.Discipline,
                Id = d.Id,
                Title = d.Name,
                Subtitle = sub
            });
        }

        foreach (var e in employees)
        {
            if (!MatchesEmployee(e, q))
                continue;
            var dep = e.Department?.Name;
            var sub = string.IsNullOrEmpty(dep)
                ? KindLabel(ReferenceSearchKind.Employee)
                : $"{KindLabel(ReferenceSearchKind.Employee)} · {dep}";
            var title = FullName(e);
            candidates.Add(new ReferenceSearchItem
            {
                Kind = ReferenceSearchKind.Employee,
                Id = e.Id,
                Title = title,
                Subtitle = sub
            });
        }

        foreach (var item in curriculum)
        {
            if (!MatchesCurriculumItem(item, q))
                continue;
            var dName = item.Discipline.Name;
            var sName = item.Specialty.Name;
            var sub = $"{KindLabel(ReferenceSearchKind.CurriculumItem)} · {sName}, сем. {item.Semester}";
            candidates.Add(new ReferenceSearchItem
            {
                Kind = ReferenceSearchKind.CurriculumItem,
                Id = item.Id,
                Title = dName,
                Subtitle = sub
            });
        }

        foreach (var sp in specialties)
        {
            if (!MatchesSpecialty(sp, q))
                continue;
            var sub = string.IsNullOrEmpty(sp.Code)
                ? KindLabel(ReferenceSearchKind.Specialty)
                : $"{KindLabel(ReferenceSearchKind.Specialty)} · {sp.Code}";
            candidates.Add(new ReferenceSearchItem
            {
                Kind = ReferenceSearchKind.Specialty,
                Id = sp.Id,
                Title = sp.Name,
                Subtitle = sub
            });
        }

        return RankAndTake(candidates, q, maxResults);
    }

    private static IReadOnlyList<ReferenceSearchItem> RankAndTake(
        List<ReferenceSearchItem> items,
        string q,
        int maxResults)
    {
        if (items.Count == 0)
            return [];
        if (items.Count <= maxResults)
            return items;

        var starts = new List<ReferenceSearchItem>();
        var rest = new List<ReferenceSearchItem>();
        foreach (var it in items)
        {
            if (it.Title.StartsWith(q, StringComparison.CurrentCultureIgnoreCase))
                starts.Add(it);
            else
                rest.Add(it);
        }

        var ordered = new List<ReferenceSearchItem>(maxResults);
        AddUpTo(ordered, starts, maxResults);
        if (ordered.Count < maxResults)
            AddUpTo(ordered, rest, maxResults);

        return ordered;
    }

    private static void AddUpTo(List<ReferenceSearchItem> target, List<ReferenceSearchItem> source, int maxResults)
    {
        foreach (var it in source)
        {
            if (target.Count >= maxResults)
                return;
            target.Add(it);
        }
    }

    private static string KindLabel(ReferenceSearchKind kind) => kind switch
    {
        ReferenceSearchKind.Faculty => "Факультет",
        ReferenceSearchKind.Department => "Кафедра",
        ReferenceSearchKind.Section => "Секция",
        ReferenceSearchKind.Discipline => "Дисциплина",
        ReferenceSearchKind.Employee => "Сотрудник",
        ReferenceSearchKind.CurriculumItem => "Учебный план",
        ReferenceSearchKind.Specialty => "Специальность",
        _ => string.Empty
    };

    private static bool ContainsI(string? text, string q) =>
        !string.IsNullOrEmpty(text) && text.Contains(q, StringComparison.OrdinalIgnoreCase);

    private static bool MatchesFaculty(Faculty f, string q) => ContainsI(f.Name, q);

    private static bool MatchesDepartment(Department d, string q) =>
        ContainsI(d.Name, q) || (d.Faculty != null && ContainsI(d.Faculty.Name, q));

    private static bool MatchesSection(Section s, string q) =>
        ContainsI(s.Name, q) || (s.Department != null && ContainsI(s.Department.Name, q));

    private static bool MatchesDiscipline(Discipline d, string q) =>
        ContainsI(d.Name, q) || (d.Department != null && ContainsI(d.Department.Name, q));

    private static bool MatchesEmployee(Employee e, string q) =>
        ContainsI(e.Surname, q)
        || ContainsI(e.Name, q)
        || ContainsI(e.Patronymic, q)
        || ContainsI(e.Degree, q)
        || ContainsI(e.Title, q)
        || ContainsI(e.Phone, q)
        || ContainsI(e.Email, q)
        || ContainsI(FullName(e), q);

    private static string FullName(Employee e)
    {
        var parts = new[] { e.Surname, e.Name, e.Patronymic ?? string.Empty };
        return string.Join(' ', parts.Where(static p => !string.IsNullOrWhiteSpace(p))).Trim();
    }

    private static bool MatchesCurriculumItem(CurriculumItem item, string q) =>
        ContainsI(item.Discipline?.Name, q)
        || ContainsI(item.Specialty?.Name, q)
        || item.Semester.ToString(CultureInfo.InvariantCulture).Contains(q, StringComparison.OrdinalIgnoreCase);

    private static bool MatchesSpecialty(Specialty s, string q) =>
        ContainsI(s.Name, q)
        || ContainsI(s.Code, q)
        || ContainsI(s.Qualification, q)
        || ContainsI(s.FormOfStudy, q);

    private static bool HasScope(ReferenceSearchScope scope, ReferenceSearchScope expected) =>
        (scope & expected) == expected;
}
