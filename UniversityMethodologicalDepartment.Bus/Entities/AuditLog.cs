using System;
using System.Collections.Generic;

namespace UniversityMethodologicalDepartment.Bus.Entities;

/// <summary>
/// Аудит изменений: кто, когда и что изменил/удалил/создал
/// </summary>
public partial class AuditLog
{
    public long Id { get; set; }

    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// auth.uid() пользователя в момент операции
    /// </summary>
    public Guid? ActorUserId { get; set; }

    /// <summary>
    /// Отображаемое имя пользователя в момент операции (снимок из JWT claims)
    /// </summary>
    public string? ActorDisplayName { get; set; }

    public string Action { get; set; } = null!;

    public string TableName { get; set; } = null!;

    /// <summary>
    /// Значение поля id изменяемой сущности
    /// </summary>
    public string? EntityId { get; set; }

    public string? OldRow { get; set; }

    public string? NewRow { get; set; }
}
