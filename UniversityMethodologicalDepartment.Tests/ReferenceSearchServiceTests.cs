using Microsoft.EntityFrameworkCore;
using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.App.Bus.Repositories;
using UniversityMethodologicalDepartment.App.Models;
using UniversityMethodologicalDepartment.App.Services;
using UniversityMethodologicalDepartment.Bus.Entities;
using UniversityMethodologicalDepartment.Bus.Entities.Extensions;
using UniversityMethodologicalDepartment.Bus.Services;
using UniversityMethodologicalDepartment.Bus.Services.Errors;
using UniversityMethodologicalDepartment.Tests.RlsCrud;
using UniversityMethodologicalDepartment.Tests.Unit.Helpers;

namespace UniversityMethodologicalDepartment.Tests;

/// <summary>
/// Интеграционные тесты <see cref="ReferenceSearchService"/> c реальной БД:
/// создают временные записи с GUID-суффиксом, проверяют поиск и удаляют созданные данные.
/// </summary>
[Collection("RlsCrud")]
public sealed class ReferenceSearchServiceTests(RlsCrudCollectionFixture fixture)
{
    [Fact]
    public async Task SearchAsync_BySpecificScope_Specialty_ReturnsOnlySpecialty()
    {
        var token = Guid.NewGuid().ToString("N");
        var seeded = await SeedSearchDataAsync(token, TestContext.Current.CancellationToken);
        try
        {
            var service = CreateSearchService();

            var result = await service.SearchAsync(
                query: token,
                maxResults: 20,
                scope: ReferenceSearchScope.Specialty,
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.NotEmpty(result);
            Assert.All(result, x => Assert.Equal(ReferenceSearchKind.Specialty, x.Kind));
            Assert.Contains(result, x => x.Title.Contains(token, StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            await CleanupAsync(seeded, TestContext.Current.CancellationToken);
        }
    }

    [Fact]
    public async Task SearchAsync_ByAllScopes_ReturnsMatchesFromDifferentReferenceKinds()
    {
        var token = Guid.NewGuid().ToString("N");
        var seeded = await SeedSearchDataAsync(token, TestContext.Current.CancellationToken);
        try
        {
            var service = CreateSearchService();

            var result = await service.SearchAsync(
                query: token,
                maxResults: 50,
                scope: ReferenceSearchScope.All,
                cancellationToken: TestContext.Current.CancellationToken);

            Assert.NotEmpty(result);
            Assert.Contains(result, x => x.Kind == ReferenceSearchKind.Faculty);
            Assert.Contains(result, x => x.Kind == ReferenceSearchKind.Department);
            Assert.Contains(result, x => x.Kind == ReferenceSearchKind.Section);
            Assert.Contains(result, x => x.Kind == ReferenceSearchKind.Discipline);
            Assert.Contains(result, x => x.Kind == ReferenceSearchKind.Employee);
            Assert.Contains(result, x => x.Kind == ReferenceSearchKind.Specialty);
            Assert.Contains(result, x => x.Kind == ReferenceSearchKind.CurriculumItem);
        }
        finally
        {
            await CleanupAsync(seeded, TestContext.Current.CancellationToken);
        }
    }

    private ReferenceSearchService CreateSearchService()
    {
        var auth = new FakeAuthService { IsAuthenticated = true };
        IDataService dataService = new DataService(
            new AuthenticatedDbContextFactory(fixture.ConnectionString, fixture.TestUserSub),
            new GenericRepositoryResolver(),
            auth,
            new DatabaseErrorRecognizer(Array.Empty<IDatabaseBusinessRule>()));

        return new ReferenceSearchService(dataService);
    }

    private async Task<SeededIds> SeedSearchDataAsync(string token, CancellationToken cancellationToken)
    {
        await using var db = await fixture.CreateDbContextAsync(fixture.TestUserSub, cancellationToken);

        var dean = new Employee
        {
            Surname = $"Dean_{token}",
            Name = "Search",
            Patronymic = "Case",
            Email = $"dean-{token}@example.invalid"
        };
        var head = new Employee
        {
            Surname = $"Head_{token}",
            Name = "Search",
            Patronymic = "Case",
            Email = $"head-{token}@example.invalid"
        };
        db.Employees.AddRange(dean, head);
        await db.SaveChangesAsync(cancellationToken);

        var faculty = new Faculty
        {
            Name = $"Faculty_{token}",
            DeanId = dean.Id
        };
        db.Faculties.Add(faculty);
        await db.SaveChangesAsync(cancellationToken);

        var department = new Department
        {
            Name = $"Department_{token}",
            FacultyId = faculty.Id,
            HeadId = head.Id
        };
        db.Departments.Add(department);
        await db.SaveChangesAsync(cancellationToken);

        var section = new Section
        {
            Name = $"Section_{token}",
            DepartmentId = department.Id,
            HeadId = head.Id
        };
        db.Sections.Add(section);

        var discipline = new Discipline
        {
            Name = $"Discipline_{token}",
            DepartmentId = department.Id
        };
        db.Disciplines.Add(discipline);

        var specialty = new Specialty
        {
            Code = $"SP_{token[..8]}",
            Name = $"Specialty_{token}",
            Qualification = "Test",
            Duration = 4,
            FormOfStudy = "очная"
        };
        db.Specialties.Add(specialty);
        await db.SaveChangesAsync(cancellationToken);

        var curriculumItem = new CurriculumItem
        {
            DisciplineId = discipline.Id,
            SpecialtyId = specialty.Id,
            Semester = 3,
            LectureHours = 10,
            LabHours = 6,
            UsrHours = 4,
            PracticalHours = 2,
            HasCourseProject = false,
            IsCredit = false,
            IsExam = true
        };
        db.CurriculumItems.Add(curriculumItem);
        await db.SaveChangesAsync(cancellationToken);

        return new SeededIds(
            dean.Id,
            head.Id,
            faculty.Id,
            department.Id,
            section.Id,
            discipline.Id,
            specialty.Id,
            curriculumItem.Id);
    }

    private async Task CleanupAsync(SeededIds ids, CancellationToken cancellationToken)
    {
        await using var db = await fixture.CreateDbContextAsync(fixture.TestUserSub, cancellationToken);

        await db.CurriculumItems.Where(x => x.Id == ids.CurriculumItemId).ExecuteDeleteAsync(cancellationToken);
        await db.Sections.Where(x => x.Id == ids.SectionId).ExecuteDeleteAsync(cancellationToken);
        await db.Disciplines.Where(x => x.Id == ids.DisciplineId).ExecuteDeleteAsync(cancellationToken);
        await db.Departments
            .Where(x => x.Id == ids.DepartmentId)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.HeadId, d => ids.DeanEmployeeId), cancellationToken);
        await db.Employees
            .Where(x => x.DepartmentId == ids.DepartmentId)
            .ExecuteUpdateAsync(s => s.SetProperty(e => e.DepartmentId, e => null), cancellationToken);
        await db.Departments.Where(x => x.Id == ids.DepartmentId).ExecuteDeleteAsync(cancellationToken);
        await db.Faculties.Where(x => x.Id == ids.FacultyId).ExecuteDeleteAsync(cancellationToken);
        await db.Specialties.Where(x => x.Id == ids.SpecialtyId).ExecuteDeleteAsync(cancellationToken);
        await db.Employees.Where(x => x.Id == ids.HeadEmployeeId || x.Id == ids.DeanEmployeeId).ExecuteDeleteAsync(cancellationToken);
    }

    private sealed record SeededIds(
        int DeanEmployeeId,
        int HeadEmployeeId,
        int FacultyId,
        int DepartmentId,
        int SectionId,
        int DisciplineId,
        int SpecialtyId,
        int CurriculumItemId);

    private sealed class AuthenticatedDbContextFactory(string connectionString, string userId) : IAppDbContextFactory
    {
        public ValueTask<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(connectionString)
                .AddInterceptors(new SupabaseRlsInterceptor(userId))
                .Options;

            return ValueTask.FromResult(new AppDbContext(userId, options));
        }
    }

    private sealed class GenericRepositoryResolver : IRepositoryResolver
    {
        public IRepository<T> GetRepository<T>() where T : class => new GenericRepository<T>();
    }
}
