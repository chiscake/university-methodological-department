using System;

namespace UniversityMethodologicalDepartment.Bus.Services.Errors;

/// <summary>
/// Исключение слоя данных, переносящее распознанную бизнес-ошибку БД в верхние слои (ViewModel).
/// <see cref="Exception.Message"/> совпадает с <see cref="DatabaseBusinessError.Message"/> и пригоден
/// для прямого показа пользователю.
/// </summary>
public class DatabaseBusinessException : Exception
{
    /// <summary>Создаёт исключение, переносящее заданную бизнес-ошибку.</summary>
    public DatabaseBusinessException(DatabaseBusinessError businessError, Exception? innerException = null)
        : base(businessError?.Message ?? string.Empty, innerException)
    {
        BusinessError = businessError ?? throw new ArgumentNullException(nameof(businessError));
    }

    /// <summary>Распознанная модель бизнес-ошибки.</summary>
    public DatabaseBusinessError BusinessError { get; }
}
