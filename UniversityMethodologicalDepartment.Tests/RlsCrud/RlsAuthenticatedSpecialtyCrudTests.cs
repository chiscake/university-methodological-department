using Microsoft.EntityFrameworkCore;
using UniversityMethodologicalDepartment.Bus.Entities;

namespace UniversityMethodologicalDepartment.Tests.RlsCrud;

/// <summary>
/// Авторизованный пользователь test@example.com: полный CRUD по временной specialty с суффиксом GUID; фикстура удаляет остатки.
/// </summary>
[Collection("RlsCrud")]
public sealed class RlsAuthenticatedSpecialtyCrudTests(RlsCrudCollectionFixture fixture)
{
    [Fact]
    public async Task Authenticated_SpecialtyInsertUpdateDelete_Succeeds()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var sub = fixture.TestUserSub;
        var ct = TestContext.Current.CancellationToken;

        await using var db = await fixture.CreateDbContextAsync(sub, ct);
        var specialty = new Specialty
        {
            Code = $"TST_{suffix}",
            Name = $"RLS auth CRUD {suffix}",
            Qualification = "Tester",
            Duration = 4,
            FormOfStudy = "очная"
        };
        db.Specialties.Add(specialty);
        await db.SaveChangesAsync(ct);
        var id = specialty.Id;
        fixture.TrackSpecialtyForCleanup(id);

        Assert.True(
            await db.Specialties.AsNoTracking().AnyAsync(s => s.Id == id, ct),
            "Inserted specialty must be visible for authenticated SELECT.");

        var updatedName = $"RLS auth updated {suffix}";
        var updateCount = await db.Specialties
            .Where(s => s.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Name, updatedName), ct);
        Assert.Equal(1, updateCount);

        var deleteCount = await db.Specialties.Where(s => s.Id == id).ExecuteDeleteAsync(ct);
        Assert.Equal(1, deleteCount);

        var stillThere = await db.Specialties.AnyAsync(s => s.Id == id, ct);
        Assert.False(stillThere, "Specialty should be deleted.");
    }
}
