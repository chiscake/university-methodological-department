using Microsoft.EntityFrameworkCore;
using UniversityMethodologicalDepartment.Bus.Entities;

namespace UniversityMethodologicalDepartment.Tests.RlsCrud;

/// <summary>
/// Анонимный доступ (userId null): чтение разрешено, INSERT/UPDATE/DELETE через EF должны завершаться ошибкой RLS.
/// </summary>
[Collection("RlsCrud")]
public sealed class RlsAnonMutationTests(RlsCrudCollectionFixture fixture)
{
    [Fact]
    public async Task Anon_CanReadSpecialties()
    {
        await using var db = await fixture.CreateDbContextAsync(userId: null, TestContext.Current.CancellationToken);
        var rows = await db.Specialties.AsNoTracking().ToListAsync(TestContext.Current.CancellationToken);
        Assert.True(rows.Count >= 1, "Expected at least one specialty row for anon SELECT.");
    }

    [Fact]
    public async Task Anon_InsertSpecialty_ThrowsDbUpdateException()
    {
        var suffix = Guid.NewGuid().ToString("N");
        await using var db = await fixture.CreateDbContextAsync(userId: null, TestContext.Current.CancellationToken);
        db.Specialties.Add(new Specialty
        {
            Code = $"TST_{suffix}",
            Name = $"RLS anon insert {suffix}",
            Qualification = null,
            Duration = 4,
            FormOfStudy = "очная"
        });

        var ex = await Record.ExceptionAsync(() =>
            db.SaveChangesAsync(TestContext.Current.CancellationToken));
        Assert.NotNull(ex);
        Assert.IsAssignableFrom<DbUpdateException>(ex);
    }

    [Fact]
    public async Task Anon_UpdateExistingSpecialty_ThrowsDbUpdateException()
    {
        await using var db = await fixture.CreateDbContextAsync(userId: null, TestContext.Current.CancellationToken);
        var row = await db.Specialties.AsTracking().OrderBy(s => s.Id).FirstAsync(TestContext.Current.CancellationToken);
        var originalCode = row.Code;
        row.Code = $"TMP_{Guid.NewGuid():N}";

        var ex = await Record.ExceptionAsync(() =>
            db.SaveChangesAsync(TestContext.Current.CancellationToken));
        Assert.NotNull(ex);
        Assert.IsAssignableFrom<DbUpdateException>(ex);
        row.Code = originalCode;
    }

    [Fact]
    public async Task Anon_DeleteExistingSpecialty_ThrowsDbUpdateException()
    {
        await using var db = await fixture.CreateDbContextAsync(userId: null, TestContext.Current.CancellationToken);
        var row = await db.Specialties.AsTracking().OrderBy(s => s.Id).FirstAsync(TestContext.Current.CancellationToken);
        db.Specialties.Remove(row);

        var ex = await Record.ExceptionAsync(() =>
            db.SaveChangesAsync(TestContext.Current.CancellationToken));
        Assert.NotNull(ex);
        Assert.IsAssignableFrom<DbUpdateException>(ex);
    }
}
