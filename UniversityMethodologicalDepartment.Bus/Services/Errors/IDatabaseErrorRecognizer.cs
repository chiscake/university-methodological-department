using System;

namespace UniversityMethodologicalDepartment.Bus.Services.Errors;

/// <summary>
/// Распознаватель бизнес-ошибок БД. Принимает сырое исключение из EF Core/Npgsql или
/// заранее извлечённую <see cref="PostgresErrorInfo"/> и возвращает типизированную модель ошибки
/// (или <c>null</c>, если ошибка не распознана и должен сработать общий fallback).
/// </summary>
public interface IDatabaseErrorRecognizer
{
    /// <summary>
    /// Обходит цепочку <see cref="Exception.InnerException"/>, ищет первое <see cref="Npgsql.PostgresException"/>
    /// и пытается распознать его правилами. Если PostgresException не найден или не распознан — возвращает <c>null</c>.
    /// </summary>
    DatabaseBusinessError? Recognize(Exception exception);

    /// <summary>Применяет правила к заранее извлечённой информации (для прямого вызова из тестов).</summary>
    DatabaseBusinessError? Recognize(PostgresErrorInfo info);
}
