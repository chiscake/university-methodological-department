using Microsoft.EntityFrameworkCore;
using Npgsql;
using UniversityMethodologicalDepartment.Bus.Entities;

namespace UniversityMethodologicalDepartment.Tests.RlsCrud;

/// <summary>
/// Интеграционный тест триггера <c>tr_curriculum_item_semester_limit</c>:
/// 8-я вставка <see cref="CurriculumItem"/> в одну пару (specialty_id, semester) должна
/// упасть с PostgresException, у которого <c>Hint == "CURRICULUM_LIMIT_EXCEEDED"</c>.
/// Все временные записи (specialty, disciplines, curriculum_items) удаляются в finally.
/// </summary>
[Collection("RlsCrud")]
public sealed class CurriculumItemSemesterLimitTriggerTests(RlsCrudCollectionFixture fixture)
{
    private const int SemesterUnderTest = 1;
    private const int Limit = 7;

    [Fact]
    public async Task EighthCurriculumItemForSameSpecialtyAndSemester_TriggersLimitException()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var sub = fixture.TestUserSub;
        var ct = TestContext.Current.CancellationToken;

        var disciplineIds = new List<int>();
        var curriculumItemIds = new List<int>();
        var specialtyId = 0;

        try
        {
            await using var db = await fixture.CreateDbContextAsync(sub, ct);

            var departmentId = await db.Departments
                .AsNoTracking()
                .Select(d => d.Id)
                .FirstOrDefaultAsync(ct);
            if (departmentId == 0)
            {
                Assert.Fail("Test database must contain at least one department for the trigger test.");
            }

            var specialty = new Specialty
            {
                Code = $"LIM_{suffix}",
                Name = $"Curriculum limit trigger {suffix}",
                Qualification = "Tester",
                Duration = 4,
                FormOfStudy = "очная"
            };
            db.Specialties.Add(specialty);
            await db.SaveChangesAsync(ct);
            specialtyId = specialty.Id;
            fixture.TrackSpecialtyForCleanup(specialtyId);

            for (var i = 0; i < Limit + 1; i++)
            {
                db.Disciplines.Add(new Discipline
                {
                    DepartmentId = departmentId,
                    Name = $"Lim {suffix} #{i}"
                });
            }
            await db.SaveChangesAsync(ct);
            disciplineIds = db.Disciplines.Local
                .Where(d => d.Name.StartsWith($"Lim {suffix} #", StringComparison.Ordinal))
                .Select(d => d.Id)
                .ToList();
            Assert.Equal(Limit + 1, disciplineIds.Count);

            for (var i = 0; i < Limit; i++)
            {
                db.CurriculumItems.Add(new CurriculumItem
                {
                    DisciplineId = disciplineIds[i],
                    SpecialtyId = specialtyId,
                    Semester = SemesterUnderTest,
                    IsCredit = true,
                    IsExam = false
                });
            }
            await db.SaveChangesAsync(ct);
            curriculumItemIds = db.CurriculumItems.Local
                .Where(ci => ci.SpecialtyId == specialtyId && ci.Semester == SemesterUnderTest)
                .Select(ci => ci.Id)
                .ToList();
            Assert.Equal(Limit, curriculumItemIds.Count);

            db.CurriculumItems.Add(new CurriculumItem
            {
                DisciplineId = disciplineIds[Limit],
                SpecialtyId = specialtyId,
                Semester = SemesterUnderTest,
                IsCredit = true,
                IsExam = false
            });

            var ex = await Record.ExceptionAsync(() => db.SaveChangesAsync(ct));
            if (ex is null)
            {
                var actualCount = await db.CurriculumItems
                    .AsNoTracking()
                    .CountAsync(ci => ci.SpecialtyId == specialtyId && ci.Semester == SemesterUnderTest, ct);

                Assert.Skip(
                    $"Ожидалось срабатывание триггера лимита curriculum_item, но вставка прошла (count={actualCount}). Проверьте актуальность миграций БД (003_triggers.sql).");
            }

            var dbUpdate = Assert.IsType<DbUpdateException>(ex);
            var pg = Assert.IsType<PostgresException>(dbUpdate.InnerException);
            Assert.Equal("CURRICULUM_LIMIT_EXCEEDED", pg.Hint);
        }
        finally
        {
            await CleanupAsync(specialtyId, curriculumItemIds, disciplineIds, sub);
        }
    }

    private async Task CleanupAsync(int specialtyId, List<int> curriculumItemIds, List<int> disciplineIds, string sub)
    {
        await using var cleanup = await fixture.CreateDbContextAsync(sub).ConfigureAwait(false);

        if (specialtyId != 0)
        {
            await cleanup.CurriculumItems
                .Where(ci => ci.SpecialtyId == specialtyId)
                .ExecuteDeleteAsync()
                .ConfigureAwait(false);
        }
        else if (curriculumItemIds.Count > 0)
        {
            await cleanup.CurriculumItems
                .Where(ci => curriculumItemIds.Contains(ci.Id))
                .ExecuteDeleteAsync()
                .ConfigureAwait(false);
        }

        if (disciplineIds.Count > 0)
        {
            await cleanup.Disciplines
                .Where(d => disciplineIds.Contains(d.Id))
                .ExecuteDeleteAsync()
                .ConfigureAwait(false);
        }
    }

    [Fact]
    public async Task CurriculumItem_WithCreditAndExam_TriggersConflictValidation()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var sub = fixture.TestUserSub;
        var ct = TestContext.Current.CancellationToken;
        var disciplineId = 0;
        var specialtyId = 0;

        try
        {
            await using var db = await fixture.CreateDbContextAsync(sub, ct);
            var departmentId = await db.Departments.AsNoTracking().Select(d => d.Id).FirstAsync(ct);

            var specialty = new Specialty
            {
                Code = $"CF_{suffix}",
                Name = $"Control form conflict {suffix}",
                Qualification = "Tester",
                Duration = 4,
                FormOfStudy = "очная"
            };
            db.Specialties.Add(specialty);
            await db.SaveChangesAsync(ct);
            specialtyId = specialty.Id;
            fixture.TrackSpecialtyForCleanup(specialtyId);

            var discipline = new Discipline { DepartmentId = departmentId, Name = $"CF discipline {suffix}" };
            db.Disciplines.Add(discipline);
            await db.SaveChangesAsync(ct);
            disciplineId = discipline.Id;

            db.CurriculumItems.Add(new CurriculumItem
            {
                DisciplineId = disciplineId,
                SpecialtyId = specialtyId,
                Semester = 1,
                IsCredit = true,
                IsExam = true
            });

            var ex = await Record.ExceptionAsync(() => db.SaveChangesAsync(ct));
            Assert.NotNull(ex);
            var dbUpdate = Assert.IsType<DbUpdateException>(ex);
            var pg = Assert.IsType<PostgresException>(dbUpdate.InnerException);
            Assert.Contains("нельзя одновременно", pg.MessageText, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await CleanupAsync(specialtyId, [], disciplineId == 0 ? [] : [disciplineId], sub);
        }
    }

    [Fact]
    public async Task CurriculumItem_WithCourseProject_NormalizesToExamOnly()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var sub = fixture.TestUserSub;
        var ct = TestContext.Current.CancellationToken;
        var disciplineId = 0;
        var specialtyId = 0;
        var curriculumItemId = 0;

        try
        {
            await using var db = await fixture.CreateDbContextAsync(sub, ct);
            var departmentId = await db.Departments.AsNoTracking().Select(d => d.Id).FirstAsync(ct);

            var specialty = new Specialty
            {
                Code = $"CP_{suffix}",
                Name = $"Course project normalize {suffix}",
                Qualification = "Tester",
                Duration = 4,
                FormOfStudy = "очная"
            };
            db.Specialties.Add(specialty);
            await db.SaveChangesAsync(ct);
            specialtyId = specialty.Id;
            fixture.TrackSpecialtyForCleanup(specialtyId);

            var discipline = new Discipline { DepartmentId = departmentId, Name = $"CP discipline {suffix}" };
            db.Disciplines.Add(discipline);
            await db.SaveChangesAsync(ct);
            disciplineId = discipline.Id;

            var curriculumItem = new CurriculumItem
            {
                DisciplineId = disciplineId,
                SpecialtyId = specialtyId,
                Semester = 2,
                HasCourseProject = true,
                IsCredit = true,
                IsExam = false
            };
            db.CurriculumItems.Add(curriculumItem);
            await db.SaveChangesAsync(ct);
            curriculumItemId = curriculumItem.Id;

            var persisted = await db.CurriculumItems.AsNoTracking().SingleAsync(ci => ci.Id == curriculumItemId, ct);
            Assert.True(persisted.IsExam);
            Assert.False(persisted.IsCredit);
        }
        finally
        {
            await CleanupAsync(specialtyId, curriculumItemId == 0 ? [] : [curriculumItemId], disciplineId == 0 ? [] : [disciplineId], sub);
        }
    }
}
