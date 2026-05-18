using System;
using Microsoft.Extensions.DependencyInjection;
using UniversityMethodologicalDepartment.App.Bus.Contracts;

namespace UniversityMethodologicalDepartment.App.Services;

/// <summary>
/// Реализация <see cref="IRepositoryResolver"/> через <see cref="IServiceProvider"/>:
/// каждый вызов <see cref="GetRepository{T}"/> делегируется в DI (GetRequiredService для IRepository&lt;T&gt;).
/// </summary>
public sealed class ServiceProviderRepositoryResolver : IRepositoryResolver
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>Создаёт резолвер, использующий указанный контейнер для разрешения репозиториев.</summary>
    /// <param name="serviceProvider">Контейнер DI приложения (должны быть зарегистрированы IRepository&lt;T&gt; по типам сущностей).</param>
    public ServiceProviderRepositoryResolver(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public IRepository<T> GetRepository<T>() where T : class
    {
        return _serviceProvider.GetRequiredService<IRepository<T>>();
    }
}
