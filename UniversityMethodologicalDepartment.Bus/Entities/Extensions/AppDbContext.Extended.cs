using Microsoft.EntityFrameworkCore;

namespace UniversityMethodologicalDepartment.Bus.Entities;

/// <summary>
/// Расширение сгенерированного <see cref="AppDbContext"/>: конструктор с userId для фабрики
/// и свойство <see cref="CurrentUserId"/>. Основной контекст в Entities/ не изменять (scaffold).
/// </summary>
public partial class AppDbContext
{
    private readonly string? _userId;

    /// <summary>Создаёт контекст с указанным идентификатором пользователя для RLS.</summary>
    /// <param name="userId">Идентификатор текущего пользователя (null — анонимный доступ).</param>
    /// <param name="options">Параметры контекста EF Core.</param>
    public AppDbContext(string? userId, DbContextOptions<AppDbContext> options)
        : this(options)
    {
        _userId = string.IsNullOrWhiteSpace(userId) ? null : userId;
    }

    /// <summary>Идентификатор текущего пользователя или null при анонимном доступе.</summary>
    public string? CurrentUserId => _userId;
}
