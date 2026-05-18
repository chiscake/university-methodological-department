using System;

namespace UniversityMethodologicalDepartment.App.Bus.Contracts;

/// <summary>
/// Аргументы события об успешном обновлении кеша набора сущностей из БД.
/// </summary>
public sealed class CacheRefreshedEventArgs : EventArgs
{
    public CacheRefreshedEventArgs(EntitySet entitySet)
    {
        EntitySet = entitySet;
    }

    public EntitySet EntitySet { get; }
}
