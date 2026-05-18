using System.Text.Json;
using System.Text.Json.Serialization;
using UniversityMethodologicalDepartment.App.Bus.Contracts;
using UniversityMethodologicalDepartment.Bus.Entities;
using UniversityMethodologicalDepartment.Bus.Services;
using UniversityMethodologicalDepartment.Tests.Unit.Helpers;

namespace UniversityMethodologicalDepartment.Tests.Unit;

public sealed class CachedDataServiceTests
{
    private static readonly JsonSerializerOptions CacheJsonOptions = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private static CachedDataService CreateSut(
        RecordingDataService inner,
        FakeAuthService? auth = null,
        InMemoryCacheStorage? storage = null)
    {
        auth ??= new FakeAuthService
        {
            IsAuthenticated = true,
            CurrentUser = new UserInfo { Id = "user-1", Email = "u@example.com", Role = "user" }
        };
        storage ??= new InMemoryCacheStorage();
        return new CachedDataService(inner, auth, storage);
    }

    [Fact]
    public async Task AddAsync_Success_InvalidatesCacheKey_ForEntity()
    {
        var inner = new RecordingDataService { AddResult = true };
        var auth = new FakeAuthService { CurrentUser = new UserInfo { Id = "user-1" } };
        var storage = new InMemoryCacheStorage();
        storage.Seed("Cache.user-1.Faculties", "{}");
        var sut = CreateSut(inner, auth, storage);

        var result = await sut.AddAsync(new Faculty { Name = "F", DeanId = 1 }, TestContext.Current.CancellationToken);

        Assert.True(result);
        Assert.Equal(1, inner.AddCallCount);
        Assert.Equal(1, storage.RemoveCount);
        Assert.False(storage.ContainsKey("Cache.user-1.Faculties"));
    }

    [Fact]
    public async Task AddAsync_FromInnerReturnsFalse_DoesNotInvalidate()
    {
        var inner = new RecordingDataService { AddResult = false };
        var auth = new FakeAuthService { CurrentUser = new UserInfo { Id = "user-1" } };
        var storage = new InMemoryCacheStorage();
        storage.Seed("Cache.user-1.Faculties", "{}");
        var sut = CreateSut(inner, auth, storage);

        var result = await sut.AddAsync(new Faculty { Name = "F", DeanId = 1 }, TestContext.Current.CancellationToken);

        Assert.False(result);
        Assert.Equal(1, inner.AddCallCount);
        Assert.Equal(0, storage.RemoveCount);
        Assert.True(storage.ContainsKey("Cache.user-1.Faculties"));
    }

    [Fact]
    public async Task UpdateAsync_Success_InvalidatesCacheKey()
    {
        var inner = new RecordingDataService { UpdateResult = true };
        var auth = new FakeAuthService { CurrentUser = new UserInfo { Id = "user-1" } };
        var storage = new InMemoryCacheStorage();
        storage.Seed("Cache.user-1.Faculties", "{}");
        var sut = CreateSut(inner, auth, storage);

        var result = await sut.UpdateAsync(new Faculty { Id = 1, Name = "F", DeanId = 1 }, TestContext.Current.CancellationToken);

        Assert.True(result);
        Assert.Equal(1, inner.UpdateCallCount);
        Assert.Equal(1, storage.RemoveCount);
        Assert.False(storage.ContainsKey("Cache.user-1.Faculties"));
    }

    [Fact]
    public async Task DeleteAsync_Success_InvalidatesCacheKey()
    {
        var inner = new RecordingDataService { DeleteResult = true };
        var auth = new FakeAuthService { CurrentUser = new UserInfo { Id = "user-1" } };
        var storage = new InMemoryCacheStorage();
        storage.Seed("Cache.user-1.Faculties", "{}");
        var sut = CreateSut(inner, auth, storage);

        var result = await sut.DeleteAsync<Faculty>(1, TestContext.Current.CancellationToken);

        Assert.True(result);
        Assert.Equal(1, inner.DeleteCallCount);
        Assert.Equal(1, storage.RemoveCount);
        Assert.False(storage.ContainsKey("Cache.user-1.Faculties"));
    }

    [Fact]
    public async Task Mutation_WithUnknownEntityType_DoesNotInvalidate()
    {
        var inner = new RecordingDataService { DeleteResult = true };
        var auth = new FakeAuthService { CurrentUser = new UserInfo { Id = "user-1" } };
        var storage = new InMemoryCacheStorage();
        storage.Seed("Cache.user-1.Faculties", "{}");
        var sut = CreateSut(inner, auth, storage);

        var result = await sut.DeleteAsync<AuditLog>(1, TestContext.Current.CancellationToken);

        Assert.True(result);
        Assert.Equal(1, inner.DeleteCallCount);
        Assert.Equal(0, storage.RemoveCount);
        Assert.True(storage.ContainsKey("Cache.user-1.Faculties"));
    }

    [Fact]
    public async Task GetFaculties_WhenInnerThrows_AndNoCache_ReturnsEmpty()
    {
        var inner = new RecordingDataService
        {
            FacultiesProvider = () => throw new InvalidOperationException("boom")
        };
        var sut = CreateSut(inner);

        var result = await sut.GetFacultiesAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Empty(result);
        Assert.Equal(1, inner.FacultiesCallCount);
    }

    [Fact]
    public async Task GetFaculties_WhenInnerThrows_AndCachedEmptyListPresent_ReturnsCachedFallback()
    {
        var inner = new RecordingDataService
        {
            FacultiesProvider = () => throw new InvalidOperationException("boom")
        };
        var auth = new FakeAuthService { CurrentUser = new UserInfo { Id = "user-1" } };
        var storage = new InMemoryCacheStorage();
        storage.Seed("Cache.user-1.Faculties", SerializeForCache(new List<Faculty>()));
        var sut = CreateSut(inner, auth, storage);

        var result = await sut.GetFacultiesAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Empty(result);
        Assert.Equal(1, inner.FacultiesCallCount);
    }

    [Fact]
    public async Task HasCachedDataAsync_WhenStorageEmpty_ReturnsFalse()
    {
        var inner = new RecordingDataService
        {
            FacultiesProvider = () => new List<Faculty>()
        };
        var sut = CreateSut(inner);

        var has = await sut.HasCachedDataAsync(EntitySet.Faculties, TestContext.Current.CancellationToken);

        Assert.False(has);
    }

    [Fact]
    public async Task HasCachedDataAsync_WhenStorageHasItems_ReturnsTrue()
    {
        var inner = new RecordingDataService
        {
            FacultiesProvider = () => new List<Faculty>()
        };
        var auth = new FakeAuthService { CurrentUser = new UserInfo { Id = "user-1" } };
        var storage = new InMemoryCacheStorage();
        storage.Seed("Cache.user-1.Faculties", SerializeForCache(new List<Faculty> { new() { Id = 1, Name = "F", DeanId = 2 } }));
        var sut = CreateSut(inner, auth, storage);

        var has = await sut.HasCachedDataAsync(EntitySet.Faculties, TestContext.Current.CancellationToken);

        Assert.True(has);
    }

    [Fact]
    public async Task HasCachedDataAsync_WhenStorageJsonCorrupted_ReturnsFalse_DoesNotThrow()
    {
        var inner = new RecordingDataService
        {
            FacultiesProvider = () => new List<Faculty>()
        };
        var auth = new FakeAuthService { CurrentUser = new UserInfo { Id = "user-1" } };
        var storage = new InMemoryCacheStorage();
        storage.Seed("Cache.user-1.Faculties", "{broken-json");
        var sut = CreateSut(inner, auth, storage);

        var has = await sut.HasCachedDataAsync(EntitySet.Faculties, TestContext.Current.CancellationToken);

        Assert.False(has);
    }

    [Fact]
    public async Task GetFaculties_RaisesCacheRefreshed_WithFacultiesEntitySet()
    {
        var inner = new RecordingDataService
        {
            FacultiesProvider = () => new List<Faculty> { new() { Id = 1, Name = "F", DeanId = 2 } }
        };
        var sut = CreateSut(inner);

        CacheRefreshedEventArgs? args = null;
        sut.CacheRefreshed += (_, e) => args = e;

        _ = await sut.GetFacultiesAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(args);
        Assert.Equal(EntitySet.Faculties, args!.EntitySet);
    }

    private static string SerializeForCache<T>(T value)
        => JsonSerializer.Serialize(value, CacheJsonOptions);
}
