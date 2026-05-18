using System.Threading;
using System.Threading.Tasks;
using UniversityMethodologicalDepartment.Bus.Entities;

namespace UniversityMethodologicalDepartment.App.Bus.Contracts;

/// <summary>
/// Фабрика контекста БД, создающая экземпляры <see cref="AppDbContext"/> с учётом текущего пользователя
/// и RLS-интерцептора (роль anon/authenticated и request.jwt.claims для политик Supabase).
/// </summary>
public interface IAppDbContextFactory
{
    /// <summary>Создаёт контекст БД с учётом текущего пользователя и RLS (роль и JWT-claims).</summary>
    /// <param name="cancellationToken">Токен отмены.</param>
    /// <returns>Настроенный экземпляр <see cref="AppDbContext"/>.</returns>
    ValueTask<AppDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default);
}
